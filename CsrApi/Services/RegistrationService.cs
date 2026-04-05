using System;
using System.Threading;
using System.Threading.Tasks;
using CsrApi.Models;
using CsrApi.Repositories;
using LanguageExt;
using Microsoft.AspNetCore.Http;

namespace CsrApi.Services;

public interface IRegistrationService
{
    Task<Either<AppError, Guid>> UpsertRegistrationAsync(RegistrationRequest request, string lineUserId, IFormFile? studentPhoto, IFormFile? guardianPhoto, CancellationToken cancellationToken);
    Task<Either<AppError, ProfileResponse>> GetMyProfileAsync(string lineUserId, CancellationToken cancellationToken);
    Task<Either<AppError, PhotoReadResult>> GetStudentPhotoAsync(string lineUserId, CancellationToken cancellationToken);
    Task<Either<AppError, PhotoReadResult>> GetGuardianPhotoAsync(string lineUserId, CancellationToken cancellationToken);
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

    public async Task<Either<AppError, Guid>> UpsertRegistrationAsync(RegistrationRequest request, string lineUserId, IFormFile? studentPhoto, IFormFile? guardianPhoto, CancellationToken cancellationToken)
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

        StoredPhoto? storedGuardianPhoto = null;
        if (guardianPhoto is not null)
        {
            var guardianPhotoResult = await _photoStorage.SaveAsync(guardianPhoto, ProtectedPhotoSubject.Guardian, lineUserId, cancellationToken);
            var guardianPhotoError = GetError(guardianPhotoResult);
            if (guardianPhotoError is not null)
            {
                return guardianPhotoError;
            }

            storedGuardianPhoto = guardianPhotoResult.Match(photo => photo, _ => null!);
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

        var guardian = BuildGuardian(request.Guardian, lineUserId, studentId, existingGuardian, storedGuardianPhoto);
        var guardianResult = await _repo.UpsertGuardianAsync(guardian);
        var guardianResultError = GetError(guardianResult);
        if (guardianResultError is not null)
        {
            return guardianResultError;
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

        var guardian = guardianResult.Match(guardian => guardian, _ => null)!;
        var studentResult = await _repo.GetStudentByIdAsync(guardian.StudentId);
        var studentError = GetError(studentResult);
        if (studentError is not null)
        {
            return studentError;
        }

        var student = studentResult.Match(student => student, _ => null)!;
        return new ProfileResponse
        {
            Student = new StudentProfileResponse
            {
                Id = student.Id,
                StudentId = student.StudentId,
                Name = _encryption.Decrypt(student.EncryptedName),
                Room = string.IsNullOrEmpty(student.NewRoom) ? student.OldRoom : student.NewRoom,
                NewRoom = student.NewRoom,
                NewNo = student.NewNo,
                Phone = string.IsNullOrEmpty(student.EncryptedPhone) ? string.Empty : _encryption.Decrypt(student.EncryptedPhone),
                HasPhoto = !string.IsNullOrWhiteSpace(student.PhotoFileName),
                PhotoUrl = string.IsNullOrWhiteSpace(student.PhotoFileName) ? null : "/api/me/student-photo"
            },
            Guardian = new GuardianProfileResponse
            {
                Name = string.IsNullOrEmpty(guardian.EncryptedName) ? string.Empty : _encryption.Decrypt(guardian.EncryptedName),
                Phone = string.IsNullOrEmpty(guardian.EncryptedPhone) ? string.Empty : _encryption.Decrypt(guardian.EncryptedPhone),
                RelationType = guardian.RelationType,
                Occupation = guardian.Occupation,
                Email = guardian.Email,
                HasPhoto = !string.IsNullOrWhiteSpace(guardian.PhotoFileName),
                PhotoUrl = string.IsNullOrWhiteSpace(guardian.PhotoFileName) ? null : "/api/me/guardian-photo"
            }
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

    public async Task<Either<AppError, PhotoReadResult>> GetGuardianPhotoAsync(string lineUserId, CancellationToken cancellationToken)
    {
        var profileResult = await GetGuardianAndStudentAsync(lineUserId, cancellationToken);
        var profileError = GetError(profileResult);
        if (profileError is not null)
        {
            return profileError;
        }

        var (guardian, _) = profileResult.Match(tuple => tuple, _ => default);
        return await _photoStorage.OpenReadAsync(
            ProtectedPhotoSubject.Guardian,
            lineUserId,
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
            EncryptedName = _encryption.Encrypt(request.Name),
            EncryptedPhone = string.IsNullOrEmpty(request.Phone) ? string.Empty : _encryption.Encrypt(request.Phone),
            PhotoFileName = photo?.FileName,
            PhotoContentType = photo?.ContentType,
            PhotoUploadedAtUtc = photo?.UploadedAtUtc,
            Status = existingStudent?.Status ?? "Pending"
        };
    }

    private Guardian BuildGuardian(GuardianInfo request, string lineUserId, Guid studentId, Guardian? existingGuardian, StoredPhoto? storedPhoto)
    {
        var photo = storedPhoto ?? ToStoredPhoto(existingGuardian?.PhotoFileName, existingGuardian?.PhotoContentType, existingGuardian?.PhotoUploadedAtUtc);
        return new Guardian
        {
            Id = existingGuardian?.Id ?? Guid.NewGuid(),
            StudentId = studentId,
            RelationType = request.RelationType,
            Occupation = request.Occupation,
            Email = request.Email,
            LineUserId = lineUserId,
            EncryptedName = _encryption.Encrypt(request.Name),
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
