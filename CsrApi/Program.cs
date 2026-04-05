using System;
using CsrApi.Middleware;
using CsrApi.Models;
using CsrApi.Repositories;
using CsrApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// Register Custom Services
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddSingleton<IMaskingService, MaskingService>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();

// Configure SQLCipher provider for SQLite
SQLitePCL.Batteries_V2.Init();

var app = builder.Build();

// Initialize DB (Simple script on startup)
using (var scope = app.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<IStudentRepository>();
    await repo.InitializeDatabaseAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Add LINE LIFF Auth Middleware
app.UseMiddleware<LiffAuthMiddleware>();

// Minimal API Endpoints
app.MapPost("/api/students", async (StudentRequest request, IStudentRepository repo, IEncryptionService encryption) =>
{
    var student = new Student
    {
        Id = Guid.NewGuid(),
        StudentId = request.StudentId,
        EncryptedName = encryption.Encrypt(request.Name),
        EncryptedPhone = encryption.Encrypt(request.Phone),
        Status = "Pending"
    };

    var result = await repo.AddStudentAsync(student);

    return result.Match(
        Right: _ => Results.Created($"/api/students/{student.Id}", student.Id),
        Left: err => err.StatusCode == 400 ? Results.BadRequest(err.Message) : Results.StatusCode(err.StatusCode)
    );
});

app.MapGet("/api/students/{id}", async (Guid id, IStudentRepository repo, IEncryptionService encryption, IMaskingService masking) =>
{
    var result = await repo.GetStudentByIdAsync(id);

    return result.Match(
        Right: student => 
        {
            var plainName = encryption.Decrypt(student.EncryptedName);
            var plainPhone = string.IsNullOrEmpty(student.EncryptedPhone) ? string.Empty : encryption.Decrypt(student.EncryptedPhone);
            var maskedDto = masking.Mask(student, plainName, plainPhone);
            return Results.Ok(maskedDto);
        },
        Left: err => err.StatusCode == 404 ? Results.NotFound(err.Message) : Results.StatusCode(err.StatusCode)
    );
});

app.MapPost("/api/register", async (RegistrationRequest request, HttpContext context, IStudentRepository repo, IEncryptionService encryption) =>
{
    var lineUserId = context.Items["LineUserId"]?.ToString();
    if (string.IsNullOrEmpty(lineUserId))
    {
        return Results.Unauthorized();
    }

    var existingGuardian = await repo.GetGuardianByLineIdAsync(lineUserId);

    var studentIdToUse = existingGuardian.Match(
        Right: g => g.StudentId,
        Left: _ => Guid.NewGuid()
    );

    var student = new Student
    {
        Id = studentIdToUse,
        StudentId = request.Student.StudentId,
        OldRoom = request.Student.OldRoom,
        OldNo = request.Student.OldNo,
        NewRoom = request.Student.NewRoom,
        NewNo = request.Student.NewNo,
        Nickname = request.Student.Nickname,
        BloodType = request.Student.BloodType,
        DOB = request.Student.DOB,
        EncryptedName = encryption.Encrypt(request.Student.Name),
        EncryptedPhone = string.IsNullOrEmpty(request.Student.Phone) ? string.Empty : encryption.Encrypt(request.Student.Phone),
        Status = "Pending"
    };

    if (existingGuardian.IsRight)
    {
        await repo.UpdateStudentAsync(student);
    }
    else
    {
        await repo.AddStudentAsync(student);
    }

    var guardian = new Guardian
    {
        Id = Guid.NewGuid(),
        StudentId = studentIdToUse,
        RelationType = request.Guardian.RelationType,
        Occupation = request.Guardian.Occupation,
        Email = request.Guardian.Email,
        LineUserId = lineUserId,
        EncryptedName = encryption.Encrypt(request.Guardian.Name),
        EncryptedPhone = string.IsNullOrEmpty(request.Guardian.Phone) ? string.Empty : encryption.Encrypt(request.Guardian.Phone)
    };

    var guardianResult = await repo.UpsertGuardianAsync(guardian);

    return guardianResult.Match(
        Right: _ => Results.Ok(new { Message = "Registration successful", StudentId = studentIdToUse }),
        Left: err => err.StatusCode == 400 ? Results.BadRequest(err.Message) : Results.StatusCode(err.StatusCode)
    );
});

app.Run();

// Request Dto
public class StudentRequest 
{
    public string StudentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}
