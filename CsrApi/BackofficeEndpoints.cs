using System;
using System.Collections.Generic;
using System.Linq;
using CsrApi.Models;
using CsrApi.Repositories;
using CsrApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CsrApi;

public static class BackofficeEndpoints
{
    public static void MapBackofficeEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/backoffice")
            .AddEndpointFilter(async (context, next) =>
            {
                var lineUserId = context.HttpContext.Items["LineUserId"]?.ToString();
                if (string.IsNullOrEmpty(lineUserId))
                    return Results.Unauthorized();

                var staffRepo = context.HttpContext.RequestServices.GetRequiredService<IStaffRepository>();
                var staffResult = await staffRepo.GetStaffByLineIdAsync(lineUserId);

                if (staffResult.IsLeft)
                    return Results.StatusCode(403); // Not authorized

                var staff = staffResult.Match(s => s, _ => null!);
                if (staff == null)
                     return Results.StatusCode(403);
                     
                context.HttpContext.Items["StaffRole"] = staff.Role;
                return await next(context);
            });

        group.MapGet("/dashboard", async (IStudentRepository repo) =>
        {
            var result = await repo.GetStudentsAsync();
            return result.Match(
                Right: students => 
                {
                    var total = students.Count();
                    var pending = students.Count(s => s.Status == "Pending");
                    var completed = students.Count(s => s.Status != "Pending");
                    return Results.Ok(new { Total = total, Pending = pending, Completed = completed });
                },
                Left: err => Results.StatusCode(err.StatusCode)
            );
        });

        group.MapGet("/students", async (IStudentRepository repo, IEncryptionService encryption) =>
        {
            var result = await repo.GetStudentsAsync();
            return result.Match(
                Right: students => 
                {
                    var list = students.Select(student => new {
                        Id = student.Id,
                        StudentId = student.StudentId,
                        Name = encryption.Decrypt(student.EncryptedName),
                        Nickname = student.Nickname,
                        Status = student.Status
                    });
                    return Results.Ok(list);
                },
                Left: err => Results.StatusCode(err.StatusCode)
            );
        });

        group.MapGet("/students/{id:guid}", async (Guid id, IStudentRepository repo, IEncryptionService encryption) =>
        {
            var studentResult = await repo.GetStudentByIdAsync(id);
            if (studentResult.IsLeft) return Results.NotFound();
            var student = studentResult.Match(s => s, _ => null!);

            var guardiansResult = await repo.GetGuardiansByStudentIdAsync(id);
            var guardians = guardiansResult.Match(g => g.ToList(), _ => new List<Guardian>());

            return Results.Ok(new {
                Student = new {
                    Id = student.Id,
                    StudentId = student.StudentId,
                    OldRoom = student.OldRoom,
                    OldNo = student.OldNo,
                    NewRoom = student.NewRoom,
                    NewNo = student.NewNo,
                    Name = encryption.Decrypt(student.EncryptedName),
                    Nickname = student.Nickname,
                    BloodType = student.BloodType,
                    DOB = student.DOB,
                    Phone = encryption.Decrypt(student.EncryptedPhone),
                    Status = student.Status,
                    InternalNote = student.InternalNote,
                    PhotoFileName = student.PhotoFileName
                },
                Guardians = guardians.Select(g => new {
                    RelationType = g.RelationType,
                    GuardianOrder = g.GuardianOrder,
                    Name = string.IsNullOrEmpty(g.EncryptedName) ? "" : encryption.Decrypt(g.EncryptedName!),
                    Phone = string.IsNullOrEmpty(g.EncryptedPhone) ? "" : encryption.Decrypt(g.EncryptedPhone!),
                    Occupation = g.Occupation,
                    PhotoFileName = g.PhotoFileName
                })
            });
        });

        group.MapPut("/students/{id:guid}/status", async (Guid id, UpdateStatusRequest req, IStudentRepository repo) =>
        {
            var studentResult = await repo.GetStudentByIdAsync(id);
            if (studentResult.IsLeft) return Results.NotFound();
            
            var student = studentResult.Match(s => s, _ => null!);
            student.Status = req.Status;
            
            var updateResult = await repo.UpdateStudentAsync(student);
            return updateResult.Match(
                Right: _ => Results.Ok(),
                Left: err => Results.StatusCode(err.StatusCode)
            );
        });

        group.MapPut("/students/{id:guid}/note", async (Guid id, UpdateNoteRequest req, IStudentRepository repo) =>
        {
            var studentResult = await repo.GetStudentByIdAsync(id);
            if (studentResult.IsLeft) return Results.NotFound();
            
            var student = studentResult.Match(s => s, _ => null!);
            student.InternalNote = req.InternalNote;
            
            var updateResult = await repo.UpdateStudentAsync(student);
            return updateResult.Match(
                Right: _ => Results.Ok(),
                Left: err => Results.StatusCode(err.StatusCode)
            );
        });

        group.MapGet("/students/{id:guid}/student-photo", async (Guid id, IStudentRepository repo, IPhotoStorageService photoStorage, CancellationToken cancellationToken) =>
        {
            var studentResult = await repo.GetStudentByIdAsync(id);
            if (studentResult.IsLeft) return Results.NotFound();
            
            var student = studentResult.Match(s => s, _ => null!);
            if (string.IsNullOrEmpty(student.PhotoFileName) || string.IsNullOrEmpty(student.PhotoContentType))
                return Results.NotFound("Student has no photo");

            var photoResult = await photoStorage.OpenReadAsync(ProtectedPhotoSubject.Student, student.Id.ToString(), student.PhotoFileName, student.PhotoContentType, cancellationToken);
            return photoResult.Match<IResult>(
                Right: data => Results.Stream(data.Stream, data.ContentType),
                Left: err => Results.StatusCode(err.StatusCode)
            );
        });

        group.MapGet("/students/{id:guid}/guardians/{guardianOrder:int}/photo", async (Guid id, int guardianOrder, IStudentRepository repo, IPhotoStorageService photoStorage, CancellationToken cancellationToken) =>
        {
            var guardiansResult = await repo.GetGuardiansByStudentIdAsync(id);
            if (guardiansResult.IsLeft) return Results.NotFound();
            
            var guardians = guardiansResult.Match(g => g.ToList(), _ => new List<Guardian>());
            var guardian = guardians.FirstOrDefault(g => g.GuardianOrder == guardianOrder);
            
            if (guardian == null || string.IsNullOrEmpty(guardian.PhotoFileName) || string.IsNullOrEmpty(guardian.PhotoContentType))
                return Results.NotFound("Guardian has no photo");

            var photoResult = await photoStorage.OpenReadAsync(ProtectedPhotoSubject.Guardian, guardian.Id.ToString(), guardian.PhotoFileName, guardian.PhotoContentType, cancellationToken);
            return photoResult.Match<IResult>(
                Right: data => Results.Stream(data.Stream, data.ContentType),
                Left: err => Results.StatusCode(err.StatusCode)
            );
        });

        group.MapGet("/students/{id:guid}/document", async (Guid id, IStudentRepository repo, IEncryptionService encryption) =>
        {
            var studentResult = await repo.GetStudentByIdAsync(id);
            if (studentResult.IsLeft) return Results.NotFound();
            
            var student = studentResult.Match(s => s, _ => null!);
            
            var guardiansResult = await repo.GetGuardiansByStudentIdAsync(id);
            var guardians = guardiansResult.Match(g => g.ToList(), _ => new List<Guardian>());

            return Results.Ok(new IntroductionDocumentResponse
            {
                Student = new StudentDocumentData
                {
                    StudentId = student.StudentId,
                    FirstName = string.IsNullOrEmpty(student.EncryptedFirstName) ? null : encryption.Decrypt(student.EncryptedFirstName),
                    LastName = string.IsNullOrEmpty(student.EncryptedLastName) ? null : encryption.Decrypt(student.EncryptedLastName),
                    Nickname = student.Nickname,
                    Room = string.IsNullOrEmpty(student.NewRoom) ? student.OldRoom : student.NewRoom,
                    NewNo = student.NewNo,
                    Phone = string.IsNullOrEmpty(student.EncryptedPhone) ? null : encryption.Decrypt(student.EncryptedPhone),
                    BloodType = student.BloodType,
                    DOB = student.DOB,
                    HasPhoto = !string.IsNullOrWhiteSpace(student.PhotoFileName),
                    PhotoUrl = string.IsNullOrWhiteSpace(student.PhotoFileName) ? null : $"/api/backoffice/students/{student.Id}/student-photo"
                },
                Guardians = guardians.Select(g => new GuardianDocumentData
                {
                    Order = g.GuardianOrder,
                    FirstName = string.IsNullOrEmpty(g.EncryptedFirstName) ? null : encryption.Decrypt(g.EncryptedFirstName),
                    LastName = string.IsNullOrEmpty(g.EncryptedLastName) ? null : encryption.Decrypt(g.EncryptedLastName),
                    Phone = string.IsNullOrEmpty(g.EncryptedPhone) ? null : encryption.Decrypt(g.EncryptedPhone),
                    RelationType = g.RelationType,
                    Occupation = g.Occupation,
                    Email = g.Email,
                    LineUserId = g.LineUserId,
                    HasPhoto = !string.IsNullOrWhiteSpace(g.PhotoFileName),
                    PhotoUrl = string.IsNullOrWhiteSpace(g.PhotoFileName) ? null : $"/api/backoffice/students/{student.Id}/guardians/{g.GuardianOrder}/photo"
                }).ToList()
            });
        });
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class UpdateNoteRequest
    {
        public string? InternalNote { get; set; }
    }
}
