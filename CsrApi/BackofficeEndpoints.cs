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
        // Dev-only: role switching without restart
        if (app.Environment.IsDevelopment())
        {
            app.MapPost("/api/backoffice/dev/switch-role", (SwitchRoleRequest req, IStaffRepository staffRepo, IEncryptionService _) =>
            {
                var staff = new StaffUser
                {
                    Id = Guid.NewGuid().ToString(),
                    LineUserId = req.LineUserId,
                    Role = req.Role,
                    Name = req.Role == "Teacher" ? "ครูสมหญิง ใจดี" : "ผู้ปกครอง เครือข่าย"
                };
                var result = staffRepo.UpsertStaffUserAsync(staff).Result;
                return result.Match(
                    Right: _ => Results.Ok(new { Message = $"Switched to {req.Role}", LineUserId = req.LineUserId }),
                    Left: err => Results.StatusCode(500)
                );
            });
        }

        var group = app.MapGroup("/api/backoffice")
            .AddEndpointFilter(async (context, next) =>
            {
                var lineUserId = context.HttpContext.Items["LineUserId"]?.ToString();
                if (string.IsNullOrEmpty(lineUserId))
                    return Results.Unauthorized();

                var staffRepo = context.HttpContext.RequestServices.GetRequiredService<IStaffRepository>();
                var staffResult = await staffRepo.GetStaffByLineIdAsync(lineUserId);

                if (staffResult.IsLeft)
                    return Results.StatusCode(403);

                var staff = staffResult.Match(s => s, _ => null!);
                if (staff == null)
                     return Results.StatusCode(403);

                context.HttpContext.Items["StaffRole"] = staff.Role;
                context.HttpContext.Items["StaffName"] = staff.Name;
                return await next(context);
            });

        // GET /api/backoffice/me — current user info + capabilities
        group.MapGet("/me", (HttpContext ctx) =>
        {
            var role = ctx.Items["StaffRole"]?.ToString() ?? "";
            var name = ctx.Items["StaffName"]?.ToString() ?? "";
            var policy = ctx.RequestServices.GetRequiredService<IBackofficePolicy>();

            return Results.Ok(new
            {
                Name = name,
                Role = role,
                IsReadOnly = role == "ParentNetworkStaff",
                Capabilities = new
                {
                    CanViewFullProfile = policy.CanViewFullProfile(ctx),
                    CanViewPhotos = policy.CanViewPhotos(ctx),
                    CanViewDocuments = policy.CanViewDocuments(ctx),
                    CanUpdateReviewStatus = policy.CanUpdateReviewStatus(ctx),
                    CanEditInternalNote = policy.CanEditInternalNote(ctx)
                }
            });
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

        group.MapGet("/students", async (HttpContext ctx, IStudentRepository repo, IEncryptionService encryption, IMaskingService masking) =>
        {
            var policy = ctx.RequestServices.GetRequiredService<IBackofficePolicy>();
            var canViewFull = policy.CanViewFullProfile(ctx);
            var result = await repo.GetStudentsAsync();

            return result.Match(
                Right: students =>
                {
                    if (canViewFull)
                    {
                        var list = students.Select(student => new
                        {
                            Id = student.Id,
                            StudentId = student.StudentId,
                            Name = encryption.Decrypt(student.EncryptedName),
                            Nickname = student.Nickname,
                            NewRoom = student.NewRoom,
                            NewNo = student.NewNo,
                            Status = student.Status
                        });
                        return Results.Ok(list);
                    }
                    else
                    {
                        // ParentNetworkStaff: show name with masked last name, no sensitive data
                        var list = students.Select(student =>
                        {
                            var plainName = encryption.Decrypt(student.EncryptedName);
                            return new
                            {
                                Id = student.Id,
                                StudentId = student.StudentId,
                                Name = MaskLastName(plainName),
                                Nickname = student.Nickname,
                                NewNo = student.NewNo,
                                Status = student.Status
                            };
                        });
                        return Results.Ok(list);
                    }
                },
                Left: err => Results.StatusCode(err.StatusCode)
            );
        });

        group.MapGet("/students/{id:guid}", async (HttpContext ctx, Guid id, IStudentRepository repo, IEncryptionService encryption) =>
        {
            var policy = ctx.RequestServices.GetRequiredService<IBackofficePolicy>();
            var canViewFull = policy.CanViewFullProfile(ctx);

            var studentResult = await repo.GetStudentByIdAsync(id);
            if (studentResult.IsLeft) return Results.NotFound();
            var student = studentResult.Match(s => s, _ => null!);

            var guardiansResult = await repo.GetGuardiansByStudentIdAsync(id);
            var guardians = guardiansResult.Match(g => g.ToList(), _ => new List<Guardian>());

            if (canViewFull)
            {
                return Results.Ok(new
                {
                    Student = new
                    {
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
                    Guardians = guardians.Select(g => new
                    {
                        RelationType = g.RelationType,
                        GuardianOrder = g.GuardianOrder,
                        Name = string.IsNullOrEmpty(g.EncryptedName) ? "" : encryption.Decrypt(g.EncryptedName!),
                        Phone = string.IsNullOrEmpty(g.EncryptedPhone) ? "" : encryption.Decrypt(g.EncryptedPhone!),
                        Occupation = g.Occupation,
                        PhotoFileName = g.PhotoFileName
                    })
                });
            }
            else
            {
                // ParentNetworkStaff: limited view — mask phones & last names, hide internal note, hide photo filenames
                return Results.Ok(new
                {
                    Student = new
                    {
                        Id = student.Id,
                        StudentId = student.StudentId,
                        OldRoom = student.OldRoom,
                        OldNo = student.OldNo,
                        NewRoom = student.NewRoom,
                        NewNo = student.NewNo,
                        Name = MaskLastName(encryption.Decrypt(student.EncryptedName)),
                        Nickname = student.Nickname,
                        BloodType = student.BloodType,
                        DOB = student.DOB,
                        Phone = MaskPhone(encryption.Decrypt(student.EncryptedPhone)),
                        Status = student.Status,
                        InternalNote = student.InternalNote,
                        PhotoFileName = (string?)null
                    },
                    Guardians = guardians.Select(g => new
                    {
                        RelationType = g.RelationType,
                        GuardianOrder = g.GuardianOrder,
                        Name = MaskLastName(string.IsNullOrEmpty(g.EncryptedName) ? "" : encryption.Decrypt(g.EncryptedName!)),
                        Phone = MaskPhone(string.IsNullOrEmpty(g.EncryptedPhone) ? "" : encryption.Decrypt(g.EncryptedPhone!)),
                        Occupation = g.Occupation,
                        PhotoFileName = (string?)null
                    })
                });
            }
        });

        group.MapPut("/students/{id:guid}/status", async (HttpContext ctx, Guid id, UpdateStatusRequest req, IStudentRepository repo) =>
        {
            var policy = ctx.RequestServices.GetRequiredService<IBackofficePolicy>();
            if (!policy.CanUpdateReviewStatus(ctx))
                return Results.StatusCode(403);

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

        group.MapPut("/students/{id:guid}/note", async (HttpContext ctx, Guid id, UpdateNoteRequest req, IStudentRepository repo) =>
        {
            var policy = ctx.RequestServices.GetRequiredService<IBackofficePolicy>();
            if (!policy.CanEditInternalNote(ctx))
                return Results.StatusCode(403);

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

        group.MapGet("/students/{id:guid}/student-photo", async (HttpContext ctx, Guid id, IStudentRepository repo, IPhotoStorageService photoStorage, CancellationToken cancellationToken) =>
        {
            var policy = ctx.RequestServices.GetRequiredService<IBackofficePolicy>();
            if (!policy.CanViewPhotos(ctx))
                return Results.StatusCode(403);

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

        group.MapGet("/students/{id:guid}/guardians/{guardianOrder:int}/photo", async (HttpContext ctx, Guid id, int guardianOrder, IStudentRepository repo, IPhotoStorageService photoStorage, CancellationToken cancellationToken) =>
        {
            var policy = ctx.RequestServices.GetRequiredService<IBackofficePolicy>();
            if (!policy.CanViewPhotos(ctx))
                return Results.StatusCode(403);

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

        group.MapGet("/students/{id:guid}/document", async (HttpContext ctx, Guid id, IStudentRepository repo, IEncryptionService encryption) =>
        {
            var policy = ctx.RequestServices.GetRequiredService<IBackofficePolicy>();
            if (!policy.CanViewDocuments(ctx))
                return Results.StatusCode(403);

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

    private static string MaskPhone(string? phone)
    {
        if (string.IsNullOrEmpty(phone)) return phone ?? "";
        phone = phone.Replace("-", "");
        if (phone.Length == 10)
        {
            return $"{phone.Substring(0, 3)}-XXX-XXXX";
        }
        if (phone.Length <= 3) return new string('*', phone.Length);
        return phone.Substring(0, 3) + new string('*', phone.Length - 3);
    }

    private static string MaskLastName(string? name)
    {
        if (string.IsNullOrEmpty(name)) return name ?? "";
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1) return name;
        var firstName = string.Join(' ', parts[..^1]);
        var lastName = parts[^1];
        var masked = lastName.Length > 2
            ? lastName.Substring(0, 2) + new string('*', lastName.Length - 2)
            : lastName;
        return $"{firstName} {masked}";
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
    }

    public class UpdateNoteRequest
    {
        public string? InternalNote { get; set; }
    }

    public class SwitchRoleRequest
    {
        public string LineUserId { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
