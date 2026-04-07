using System;
using System.Text.Json;
using CsrApi;
using CsrApi.Middleware;
using CsrApi.Models;
using CsrApi.Repositories;
using CsrApi.Services;
using Dapper;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

SqlMapper.AddTypeHandler(new SqliteGuidTypeHandler());

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// Register Custom Services
builder.Services.AddSingleton<IEncryptionService, EncryptionService>();
builder.Services.AddSingleton<IMaskingService, MaskingService>();
builder.Services.AddSingleton<IBackofficePolicy, BackofficePolicy>();
builder.Services.AddSingleton<ILineProfileService, LineProfileService>();
builder.Services.Configure<PhotoStorageOptions>(builder.Configuration.GetSection("PhotoStorage"));
builder.Services.AddSingleton<IPhotoStorageService, PhotoStorageService>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IStaffRepository, StaffRepository>();
builder.Services.AddScoped<IDevelopmentDataSeeder, DevelopmentDataSeeder>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IBackofficeStudentExportService, BackofficeStudentExportService>();

// Configure SQLCipher provider for SQLite
SQLitePCL.Batteries_V2.Init();

var app = builder.Build();

// Initialize DB (Simple script on startup)
using (var scope = app.Services.CreateScope())
{
    var staffRepo = scope.ServiceProvider.GetRequiredService<IStaffRepository>();
    var repo = scope.ServiceProvider.GetRequiredService<IStudentRepository>();
    var seeder = scope.ServiceProvider.GetRequiredService<IDevelopmentDataSeeder>();
    await repo.InitializeDatabaseAsync();
    await staffRepo.InitializeDatabaseAsync();
    await seeder.SeedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (builder.Configuration.GetValue<bool>("Http:UseHttpsRedirection"))
{
    app.UseHttpsRedirection();
}

// Add LINE LIFF Auth Middleware
app.UseMiddleware<LiffAuthMiddleware>();

app.MapBackofficeEndpoints();

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

app.MapPost("/api/register", async (HttpContext context, IRegistrationService requestServices) =>
{
    var lineUserId = GetLineUserId(context);
    if (lineUserId is null)
    {
        return Results.Unauthorized();
    }

    var formDataResult = await ReadRegistrationFormAsync(context.Request, context.RequestAborted);
    if (formDataResult.IsLeft)
    {
        return formDataResult.Match<IResult>(
            Right: _ => Results.Ok(),
            Left: ToErrorResult);
    }

    var formData = formDataResult.Match(data => data, _ => null)!;
    var result = await requestServices.UpsertRegistrationAsync(
        formData.Registration,
        lineUserId,
        formData.StudentPhoto,
        formData.GuardianPhotos,
        context.RequestAborted);

    return result.Match(
        Right: studentId => Results.Ok(new { Message = "Registration successful", StudentId = studentId }),
        Left: ToErrorResult);
});

app.MapGet("/api/me", async (HttpContext context, IRegistrationService requestServices) =>
{
    var lineUserId = GetLineUserId(context);
    if (lineUserId is null)
    {
        return Results.Unauthorized();
    }

    var result = await requestServices.GetMyProfileAsync(lineUserId, context.RequestAborted);
    return result.Match(
        Right: profile => Results.Ok(profile),
        Left: ToErrorResult);
});

app.MapGet("/api/me/student-photo", async (HttpContext context, IRegistrationService requestServices) =>
{
    var lineUserId = GetLineUserId(context);
    if (lineUserId is null)
    {
        return Results.Unauthorized();
    }

    var result = await requestServices.GetStudentPhotoAsync(lineUserId, context.RequestAborted);
    if (result.IsLeft)
    {
        return result.Match<IResult>(
            Right: _ => Results.Ok(),
            Left: ToErrorResult);
    }

    var photo = result.Match(
        Right: data => data,
        Left: _ => throw new InvalidOperationException("Student photo result was expected to be successful."));
    context.Response.OnCompleted(() => photo.DisposeAsync().AsTask());
    return Results.Stream(photo.Stream, photo.ContentType);
});

app.MapGet("/api/me/guardian-photo/{guardianOrder:int}", async (HttpContext context, IRegistrationService requestServices, int guardianOrder) =>
{
    var lineUserId = GetLineUserId(context);
    if (lineUserId is null)
    {
        return Results.Unauthorized();
    }

    var result = await requestServices.GetGuardianPhotoAsync(lineUserId, guardianOrder, context.RequestAborted);
    if (result.IsLeft)
    {
        return result.Match<IResult>(
            Right: _ => Results.Ok(),
            Left: ToErrorResult);
    }

    var photo = result.Match(
        Right: data => data,
        Left: _ => throw new InvalidOperationException("Guardian photo result was expected to be successful."));
    context.Response.OnCompleted(() => photo.DisposeAsync().AsTask());
    return Results.Stream(photo.Stream, photo.ContentType);
});

app.MapGet("/api/me/introduction-document", async (HttpContext context, IRegistrationService requestServices) =>
{
    var lineUserId = GetLineUserId(context);
    if (lineUserId is null)
    {
        return Results.Unauthorized();
    }

    var result = await requestServices.GetIntroductionDocumentAsync(lineUserId, context.RequestAborted);
    return result.Match(
        Right: document => Results.Ok(document),
        Left: ToErrorResult);
});

app.MapGet("/api/class", async (IStudentRepository repo, IEncryptionService encryption, IMaskingService masking) =>
{
    var result = await repo.GetStudentsAsync();

    return result.Match(
        Right: students => 
        {
            var list = students.Select(student => {
                var plainName = encryption.Decrypt(student.EncryptedName);
                var plainPhone = string.IsNullOrEmpty(student.EncryptedPhone) ? "" : encryption.Decrypt(student.EncryptedPhone);
                return masking.Mask(student, plainName, plainPhone);
            });
            return Results.Ok(list);
        },
        Left: err => Results.StatusCode(err.StatusCode)
    );
});

app.Run();

static string? GetLineUserId(HttpContext context)
{
    return context.Items["LineUserId"]?.ToString();
}

static async Task<LanguageExt.Either<AppError, RegistrationFormData>> ReadRegistrationFormAsync(HttpRequest request, CancellationToken cancellationToken)
{
    if (!request.HasFormContentType)
    {
        return AppError.BadRequest("Registration requires multipart/form-data.");
    }

    var form = await request.ReadFormAsync(cancellationToken);
    var payload = form["payload"].ToString();
    if (string.IsNullOrWhiteSpace(payload))
    {
        return AppError.BadRequest("Registration payload is required.");
    }

    try
    {
        var registration = JsonSerializer.Deserialize<RegistrationRequest>(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (registration is null)
        {
            return AppError.BadRequest("Registration payload is invalid.");
        }

        // Collect guardian photos (guardianPhoto1, guardianPhoto2, etc.)
        var guardianPhotos = new List<IFormFile>();
        for (var i = 1; i <= 2; i++)
        {
            var file = form.Files.GetFile($"guardianPhoto{i}");
            if (file is not null)
            {
                guardianPhotos.Add(file);
            }
        }
        
        // Also check for generic "guardianPhoto" for backward compatibility
        var legacyFile = form.Files.GetFile("guardianPhoto");
        if (legacyFile is not null && guardianPhotos.Count == 0)
        {
            guardianPhotos.Add(legacyFile);
        }

        return new RegistrationFormData(
            registration,
            form.Files.GetFile("studentPhoto"),
            guardianPhotos);
    }
    catch (JsonException ex)
    {
        return AppError.BadRequest($"Registration payload is invalid JSON: {ex.Message}");
    }
}

static IResult ToErrorResult(AppError error)
{
    return error.StatusCode switch
    {
        StatusCodes.Status400BadRequest => Results.BadRequest(error.Message),
        StatusCodes.Status401Unauthorized => Results.Unauthorized(),
        StatusCodes.Status404NotFound => Results.NotFound(error.Message),
        _ => Results.StatusCode(error.StatusCode)
    };
}

// Request Dto
public class StudentRequest 
{
    public string StudentId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public sealed record RegistrationFormData(RegistrationRequest Registration, IFormFile? StudentPhoto, List<IFormFile> GuardianPhotos);
