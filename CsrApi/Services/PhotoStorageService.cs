using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CsrApi.Models;
using LanguageExt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace CsrApi.Services;

public enum ProtectedPhotoSubject
{
    Student,
    Guardian
}

public sealed class PhotoStorageOptions
{
    public string RootPath { get; set; } = "App_Data/ProtectedUploads";
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;
    public List<string> AllowedContentTypes { get; set; } = new()
    {
        "image/jpeg",
        "image/png",
        "image/webp"
    };
}

public sealed record StoredPhoto(string FileName, string ContentType, DateTime UploadedAtUtc);

public sealed record PhotoReadResult(Stream Stream, string ContentType) : IAsyncDisposable
{
    public ValueTask DisposeAsync() => Stream.DisposeAsync();
}

public interface IPhotoStorageService
{
    Task<Either<AppError, StoredPhoto>> SaveAsync(IFormFile file, ProtectedPhotoSubject subject, string ownerKey, CancellationToken cancellationToken);
    Task<Either<AppError, PhotoReadResult>> OpenReadAsync(ProtectedPhotoSubject subject, string ownerKey, string? fileName, string? contentType, CancellationToken cancellationToken);
}

public sealed class PhotoStorageService : IPhotoStorageService
{
    private static readonly Dictionary<string, string> ContentTypeExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp"
    };

    private readonly PhotoStorageOptions _options;
    private readonly string _rootPath;

    public PhotoStorageService(IOptions<PhotoStorageOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        _rootPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, _options.RootPath));
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<Either<AppError, StoredPhoto>> SaveAsync(IFormFile file, ProtectedPhotoSubject subject, string ownerKey, CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
        {
            return AppError.BadRequest("Uploaded photo is empty.");
        }

        if (file.Length > _options.MaxFileSizeBytes)
        {
            return AppError.BadRequest($"Photo size exceeds the {_options.MaxFileSizeBytes} byte limit.");
        }

        if (!_options.AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return AppError.BadRequest("Unsupported photo type. Allowed types: JPEG, PNG, WEBP.");
        }

        try
        {
            var ownerFolderPath = GetOwnerFolderPath(subject, ownerKey);
            Directory.CreateDirectory(ownerFolderPath);
            DeleteExistingFiles(ownerFolderPath);

            var extension = ResolveExtension(file);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(ownerFolderPath, fileName);

            await using var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await file.CopyToAsync(stream, cancellationToken);

            return new StoredPhoto(fileName, file.ContentType, DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            return AppError.Internal($"Failed to store photo securely: {ex.Message}");
        }
    }

    public Task<Either<AppError, PhotoReadResult>> OpenReadAsync(ProtectedPhotoSubject subject, string ownerKey, string? fileName, string? contentType, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Task.FromResult<Either<AppError, PhotoReadResult>>(AppError.NotFound("Photo not found."));
        }

        try
        {
            var ownerFolderPath = GetOwnerFolderPath(subject, ownerKey);
            var safeFileName = Path.GetFileName(fileName);
            var filePath = Path.Combine(ownerFolderPath, safeFileName);

            if (!File.Exists(filePath))
            {
                return Task.FromResult<Either<AppError, PhotoReadResult>>(AppError.NotFound("Photo file does not exist."));
            }

            Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var resolvedContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
            return Task.FromResult<Either<AppError, PhotoReadResult>>(new PhotoReadResult(stream, resolvedContentType));
        }
        catch (Exception ex)
        {
            return Task.FromResult<Either<AppError, PhotoReadResult>>(AppError.Internal($"Failed to read protected photo: {ex.Message}"));
        }
    }

    private string GetOwnerFolderPath(ProtectedPhotoSubject subject, string ownerKey)
    {
        var subjectFolder = subject == ProtectedPhotoSubject.Student ? "students" : "guardians";
        var ownerHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ownerKey)));
        return Path.Combine(_rootPath, subjectFolder, ownerHash);
    }

    private static void DeleteExistingFiles(string ownerFolderPath)
    {
        foreach (var existingFile in Directory.EnumerateFiles(ownerFolderPath))
        {
            File.Delete(existingFile);
        }
    }

    private static string ResolveExtension(IFormFile file)
    {
        if (ContentTypeExtensions.TryGetValue(file.ContentType, out var extension))
        {
            return extension;
        }

        var originalExtension = Path.GetExtension(file.FileName);
        return string.IsNullOrWhiteSpace(originalExtension) ? ".bin" : originalExtension.ToLowerInvariant();
    }
}
