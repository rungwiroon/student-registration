using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CsrApi.Models;
using CsrApi.Repositories;
using LanguageExt;
using Microsoft.AspNetCore.Http;

namespace CsrApi.Services;

public interface IRegistrationService
{
    Task<Either<AppError, Guid>> UpsertRegistrationAsync(RegistrationRequest request, string lineUserId, IFormFile? studentPhoto, List<IFormFile>? guardianPhotos, CancellationToken cancellationToken);
    Task<Either<AppError, ProfileResponse>> GetMyProfileAsync(string lineUserId, CancellationToken cancellationToken);
    Task<Either<AppError, IntroductionDocumentResponse>> GetIntroductionDocumentAsync(string lineUserId, CancellationToken cancellationToken);
    Task<Either<AppError, PhotoReadResult>> GetStudentPhotoAsync(string lineUserId, CancellationToken cancellationToken);
    Task<Either<AppError, PhotoReadResult>> GetGuardianPhotoAsync(string lineUserId, int guardianOrder, CancellationToken cancellationToken);
}

public sealed class RegistrationService : IRegistrationService
{
    private readonly IStudentRepository _repo;
    private readonly IEncryptionService _encryption;
    private readonly IPhotoStorageService _photoStorage;

    public RegistrationService(IStudentRepository repo, IEncryptionService encryption, IPhotoStorageService photoStorage)
    {
        _repo = repo;
        _encryption = encryption;
        _photoStorage = photoStorage;
    }

    public async Task<Either<AppError, Guid>> UpsertRegistrationAsync(RegistrationRequest request, string lineUserId, IFormFile? studentPhoto, List<IFormFile>? guardianPhotos, CancellationToken cancellationToken)
    {
        var existingGuardianResult = await _repo.GetGuardianByLineIdAsync(lineUserId);
        var existingGuardianError = GetError(existingGuardianResult);
        if (existingGuardianError is not null && existingGuardianError.StatusCode != 404)
        {
            return existingGuardianError;
        }

        var existingGuardian = existingGuardianResult.Match<Guardian?>(guardian => guardian, _ => null);
        var studentId = existingGuardian?.StudentId ?? Guid.NewGuid();
        Student? currentStudent = null;
        if (existingGuardian is not null)
        {
            var existingStudentResult = await _repo.GetStudentByIdAsync(existingGuardian.StudentId);
            var existingStudentError = GetError(existingStudentResult);
            if (existingStudentError is not null)
            {
                return existingStudentError;
            }

            currentStudent = existingStudentResult.Match(student => student, _ => null!);
        }

        StoredPhoto? storedStudentPhoto = null;
        if (studentPhoto is not null)
        {
            var studentPhotoResult = await _photoStorage.SaveAsync(studentPhoto, ProtectedPhotoSubject.Student, studentId.ToString(), cancellationToken);
            var studentPhotoError = GetError(studentPhotoResult);
            if (studentPhotoError is not null)
            {
                return studentPhotoError;
            }

            storedStudentPhoto = studentPhotoResult.Match(photo => photo, _ => null!);
        }

        var student = BuildStudent(request.Student, studentId, currentStudent, storedStudentPhoto);
        var studentResult = existingGuardian is null
            ? await _repo.AddStudentAsync(student)
            : await _repo.UpdateStudentAsync(student);
        var studentResultError = GetError(studentResult);
        if (studentResultError is not null)
        {
            return studentResultError;
        }

        // Get existing guardians for this student
        var existingGuardiansResult = await _repo.GetGuardiansByStudentIdAsync(studentId);
        var existingGuardians = existingGuardiansResult.Match(
            guardians => guardians.ToList(),
            _ => new List<Guardian>()
        );

        var guardiansToUpsert = new List<Guardian>();
        for (var i = 0; i < request.Guardians.Count; i++)
        {
            var guardianInfo = request.Guardians[i];
            var guardianOrder = guardianInfo.Order > 0 ? guardianInfo.Order : i + 1;
            
            // Find existing guardian by order or use the one with matching LineUserId
            var existing = guardianOrder == 1 && existingGuardian is not null
                ? existingGuardian
                : existingGuardians.FirstOrDefault(g => g.GuardianOrder == guardianOrder);

            StoredPhoto? storedGuardianPhoto = null;
            if (guardianPhotos is not null && i < guardianPhotos.Count && guardianPhotos[i] is not null)
            {
                var photoKey = existing?.Id.ToString() ?? Guid.NewGuid().ToString();
                var guardianPhotoResult = await _photoStorage.SaveAsync(guardianPhotos[i], ProtectedPhotoSubject.Guardian, photoKey, cancellationToken);
                var guardianPhotoError = GetError(guardianPhotoResult);
                if (guardianPhotoError is not null)
                {
                    return guardianPhotoError;
                }

                storedGuardianPhoto = guardianPhotoResult.Match(photo => photo, _ => null!);
            }

            guardiansToUpsert.Add(BuildGuardian(guardianInfo, guardianOrder, lineUserId, studentId, existing, storedGuardianPhoto));
        }

        var guardiansResult = await _repo.UpsertGuardiansAsync(guardiansToUpsert);
        var guardiansResultError = GetError(guardiansResult);
        if (guardiansResultError is not null)
        {
            return guardiansResultError;
        }

        return studentId;
    }

    public async Task<Either<AppError, ProfileResponse>> GetMyProfileAsync(string lineUserId, CancellationToken cancellationToken)
    {
        var guardianResult = await _repo.GetGuardianByLineIdAsync(lineUserId);
        var guardianError = GetError(guardianResult);
        if (guardianError is not null)
        {
            return guardianError;
        }

        var primaryGuardian = guardianResult.Match(guardian => guardian, _ => null)!;
        var studentResult = await _repo.GetStudentByIdAsync(primaryGuardian.StudentId);
        var studentError = GetError(studentResult);
        if (studentError is not null)
        {
            return studentError;
        }

        var student = studentResult.Match(student => student, _ => null)!;
        
        // Get all guardians for this student
        var guardiansResult = await _repo.GetGuardiansByStudentIdAsync(student.Id);
        var guardians = guardiansResult.Match(
            g => g.ToList(),
            _ => new List<Guardian>()
        );

        return new ProfileResponse
        {
            Student = new StudentProfileResponse
            {
                Id = student.Id,
                StudentId = student.StudentId,
                FirstName = string.IsNullOrEmpty(student.EncryptedFirstName) ? null : _encryption.Decrypt(student.EncryptedFirstName),
                LastName = string.IsNullOrEmpty(student.EncryptedLastName) ? null : _encryption.Decrypt(student.EncryptedLastName),
                Nickname = student.Nickname,
                Room = string.IsNullOrEmpty(student.NewRoom) ? student.OldRoom : student.NewRoom,
                NewRoom = student.NewRoom,
                NewNo = student.NewNo,
                Phone = string.IsNullOrEmpty(student.EncryptedPhone) ? string.Empty : _encryption.Decrypt(student.EncryptedPhone),
                BloodType = student.BloodType,
                DOB = student.DOB,
                LineUserId = primaryGuardian.LineUserId,
                HasPhoto = !string.IsNullOrWhiteSpace(student.PhotoFileName),
                PhotoUrl = string.IsNullOrWhiteSpace(student.PhotoFileName) ? null : "/api/me/student-photo"
            },
            Guardians = guardians.Select(g => new GuardianProfileResponse
            {
                Order = g.GuardianOrder,
                FirstName = string.IsNullOrEmpty(g.EncryptedFirstName) ? null : _encryption.Decrypt(g.EncryptedFirstName),
                LastName = string.IsNullOrEmpty(g.EncryptedLastName) ? null : _encryption.Decrypt(g.EncryptedLastName),
                Phone = string.IsNullOrEmpty(g.EncryptedPhone) ? string.Empty : _encryption.Decrypt(g.EncryptedPhone),
                RelationType = g.RelationType,
                Occupation = g.Occupation,
                Email = g.Email,
                LineUserId = g.LineUserId,
                HasPhoto = !string.IsNullOrWhiteSpace(g.PhotoFileName),
                PhotoUrl = string.IsNullOrWhiteSpace(g.PhotoFileName) ? null : $"/api/me/guardian-photo/{g.GuardianOrder}"
            }).ToList()
        };
    }

    public async Task<Either<AppError, IntroductionDocumentResponse>> GetIntroductionDocumentAsync(string lineUserId, CancellationToken cancellationToken)
    {
        var guardianResult = await _repo.GetGuardianByLineIdAsync(lineUserId);
        var guardianError = GetError(guardianResult);
        if (guardianError is not null)
        {
            return guardianError;
        }

        var primaryGuardian = guardianResult.Match(guardian => guardian, _ => null)!;
        var studentResult = await _repo.GetStudentByIdAsync(primaryGuardian.StudentId);
        var studentError = GetError(studentResult);
        if (studentError is not null)
        {
            return studentError;
        }

        var student = studentResult.Match(student => student, _ => null)!;
        
        // Get all guardians for this student
        var guardiansResult = await _repo.GetGuardiansByStudentIdAsync(student.Id);
        var guardians = guardiansResult.Match(
            g => g.ToList(),
            _ => new List<Guardian>()
        );

        return new IntroductionDocumentResponse
        {
            Student = new StudentDocumentData
            {
                StudentId = student.StudentId,
                FirstName = string.IsNullOrEmpty(student.EncryptedFirstName) ? null : _encryption.Decrypt(student.EncryptedFirstName),
                LastName = string.IsNullOrEmpty(student.EncryptedLastName) ? null : _encryption.Decrypt(student.EncryptedLastName),
                Nickname = student.Nickname,
                Room = string.IsNullOrEmpty(student.NewRoom) ? student.OldRoom : student.NewRoom,
                NewNo = student.NewNo,
                Phone = string.IsNullOrEmpty(student.EncryptedPhone) ? null : _encryption.Decrypt(student.EncryptedPhone),
                BloodType = student.BloodType,
                DOB = student.DOB,
                HasPhoto = !string.IsNullOrWhiteSpace(student.PhotoFileName),
                PhotoUrl = string.IsNullOrWhiteSpace(student.PhotoFileName) ? null : "/api/me/student-photo"
            },
            Guardians = guardians.Select(g => new GuardianDocumentData
            {
                Order = g.GuardianOrder,
                FirstName = string.IsNullOrEmpty(g.EncryptedFirstName) ? null : _encryption.Decrypt(g.EncryptedFirstName),
                LastName = string.IsNullOrEmpty(g.EncryptedLastName) ? null : _encryption.Decrypt(g.EncryptedLastName),
                Phone = string.IsNullOrEmpty(g.EncryptedPhone) ? null : _encryption.Decrypt(g.EncryptedPhone),
                RelationType = g.RelationType,
                Occupation = g.Occupation,
                Email = g.Email,
                LineUserId = g.LineUserId,
                HasPhoto = !string.IsNullOrWhiteSpace(g.PhotoFileName),
                PhotoUrl = string.IsNullOrWhiteSpace(g.PhotoFileName) ? null : $"/api/me/guardian-photo/{g.GuardianOrder}"
            }).ToList()
        };
    }

    public async Task<Either<AppError, PhotoReadResult>> GetStudentPhotoAsync(string lineUserId, CancellationToken cancellationToken)
    {
        var profileResult = await GetGuardianAndStudentAsync(lineUserId, cancellationToken);
        var profileError = GetError(profileResult);
        if (profileError is not null)
        {
            return profileError;
        }

        var (_, student) = profileResult.Match(tuple => tuple, _ => default);
        return await _photoStorage.OpenReadAsync(
            ProtectedPhotoSubject.Student,
            student.Id.ToString(),
            student.PhotoFileName,
            student.PhotoContentType,
            cancellationToken);
    }

    public async Task<Either<AppError, PhotoReadResult>> GetGuardianPhotoAsync(string lineUserId, int guardianOrder, CancellationToken cancellationToken)
    {
        var profileResult = await GetGuardianAndStudentAsync(lineUserId, cancellationToken);
        var profileError = GetError(profileResult);
        if (profileError is not null)
        {
            return profileError;
        }

        var (_, student) = profileResult.Match(tuple => tuple, _ => default);
        
        // Get guardians for this student and find by order
        var guardiansResult = await _repo.GetGuardiansByStudentIdAsync(student.Id);
        var guardians = guardiansResult.Match(
            g => g.ToList(),
            _ => new List<Guardian>()
        );
        
        var guardian = guardians.FirstOrDefault(g => g.GuardianOrder == guardianOrder);
        if (guardian is null)
        {
            return AppError.NotFound($"Guardian with order {guardianOrder} not found.");
        }
        
        return await _photoStorage.OpenReadAsync(
            ProtectedPhotoSubject.Guardian,
            guardian.Id.ToString(),
            guardian.PhotoFileName,
            guardian.PhotoContentType,
            cancellationToken);
    }

    private async Task<Either<AppError, (Guardian Guardian, Student Student)>> GetGuardianAndStudentAsync(string lineUserId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var guardianResult = await _repo.GetGuardianByLineIdAsync(lineUserId);
        var guardianError = GetError(guardianResult);
        if (guardianError is not null)
        {
            return guardianError;
        }

        var guardian = guardianResult.Match(guardian => guardian, _ => null)!;
        var studentResult = await _repo.GetStudentByIdAsync(guardian.StudentId);
        var studentError = GetError(studentResult);
        if (studentError is not null)
        {
            return studentError;
        }

        var student = studentResult.Match(student => student, _ => null)!;
        return (guardian, student);
    }

    private Student BuildStudent(StudentInfo request, Guid studentId, Student? existingStudent, StoredPhoto? storedPhoto)
    {
        var photo = storedPhoto ?? ToStoredPhoto(existingStudent?.PhotoFileName, existingStudent?.PhotoContentType, existingStudent?.PhotoUploadedAtUtc);
        var fullName = $"{request.FirstName} {request.LastName}".Trim();
        return new Student
        {
            Id = studentId,
            StudentId = request.StudentId,
            OldRoom = request.OldRoom,
            OldNo = request.OldNo,
            NewRoom = request.NewRoom,
            NewNo = request.NewNo,
            Nickname = request.Nickname,
            BloodType = request.BloodType,
            DOB = request.DOB,
            EncryptedName = _encryption.Encrypt(fullName),
            EncryptedFirstName = string.IsNullOrEmpty(request.FirstName) ? null : _encryption.Encrypt(request.FirstName),
            EncryptedLastName = string.IsNullOrEmpty(request.LastName) ? null : _encryption.Encrypt(request.LastName),
            EncryptedPhone = string.IsNullOrEmpty(request.Phone) ? string.Empty : _encryption.Encrypt(request.Phone),
            PhotoFileName = photo?.FileName,
            PhotoContentType = photo?.ContentType,
            PhotoUploadedAtUtc = photo?.UploadedAtUtc,
            Status = existingStudent?.Status ?? "Pending"
        };
    }

    private Guardian BuildGuardian(GuardianInfo request, int guardianOrder, string lineUserId, Guid studentId, Guardian? existingGuardian, StoredPhoto? storedPhoto)
    {
        var photo = storedPhoto ?? ToStoredPhoto(existingGuardian?.PhotoFileName, existingGuardian?.PhotoContentType, existingGuardian?.PhotoUploadedAtUtc);
        var fullName = $"{request.FirstName} {request.LastName}".Trim();
        return new Guardian
        {
            Id = existingGuardian?.Id ?? Guid.NewGuid(),
            StudentId = studentId,
            RelationType = request.RelationType,
            GuardianOrder = guardianOrder,
            Occupation = request.Occupation,
            Email = request.Email,
            LineUserId = guardianOrder == 1 ? lineUserId : existingGuardian?.LineUserId,
            EncryptedName = _encryption.Encrypt(fullName),
            EncryptedFirstName = string.IsNullOrEmpty(request.FirstName) ? null : _encryption.Encrypt(request.FirstName),
            EncryptedLastName = string.IsNullOrEmpty(request.LastName) ? null : _encryption.Encrypt(request.LastName),
            EncryptedPhone = string.IsNullOrEmpty(request.Phone) ? string.Empty : _encryption.Encrypt(request.Phone),
            PhotoFileName = photo?.FileName,
            PhotoContentType = photo?.ContentType,
            PhotoUploadedAtUtc = photo?.UploadedAtUtc
        };
    }

    private static StoredPhoto? ToStoredPhoto(string? fileName, string? contentType, DateTime? uploadedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(contentType) || uploadedAtUtc is null)
        {
            return null;
        }

        return new StoredPhoto(fileName, contentType, uploadedAtUtc.Value);
    }

    private static AppError? GetError<T>(Either<AppError, T> result)
    {
        if (result.IsRight)
        {
            return null;
        }

        return result.Match(
            Right: _ => AppError.Internal("Unexpected successful result state."),
            Left: error => error);
    }
}
