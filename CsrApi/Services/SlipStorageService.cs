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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CsrApi.Services;

public sealed class SlipStorageOptions
{
    public string RootPath { get; set; } = "App_Data/ShirtOrderSlips";
    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;
    public List<string> AllowedContentTypes { get; set; } = new()
    {
        "image/jpeg",
        "image/png"
    };
}

public sealed record StoredSlip(string FileName, string ContentType, DateTime UploadedAtUtc, string PublicUrl);

public sealed record SlipReadResult(Stream Stream, string ContentType) : IAsyncDisposable
{
    public ValueTask DisposeAsync() => Stream.DisposeAsync();
}

public interface ISlipStorageService
{
    Task<Either<AppError, StoredSlip>> SaveAsync(IFormFile file, string ownerKey, CancellationToken cancellationToken);
    Task<Either<AppError, SlipReadResult>> OpenReadAsync(string ownerKey, string fileName, CancellationToken cancellationToken);
}

public sealed class SlipStorageService : ISlipStorageService
{
    private static readonly Dictionary<string, string> ContentTypeExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png"
    };

    private readonly SlipStorageOptions _options;
    private readonly string _rootPath;
    private readonly string _publicBaseUrl;
    private readonly ILogger<SlipStorageService> _logger;

    public SlipStorageService(IOptions<SlipStorageOptions> options, IHostEnvironment environment, IConfiguration config, ILogger<SlipStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _rootPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, _options.RootPath));
        Directory.CreateDirectory(_rootPath);
        _publicBaseUrl = config["PublicBaseUrl"]?.TrimEnd('/') ?? "";
    }

    public async Task<Either<AppError, StoredSlip>> SaveAsync(IFormFile file, string ownerKey, CancellationToken cancellationToken)
    {
        if (file.Length <= 0)
        {
            return AppError.BadRequest("Uploaded slip is empty.");
        }

        if (file.Length > _options.MaxFileSizeBytes)
        {
            return AppError.BadRequest($"Slip size exceeds the {_options.MaxFileSizeBytes} byte limit.");
        }

        if (!_options.AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
        {
            return AppError.BadRequest("Unsupported slip type. Allowed types: JPEG, PNG.");
        }

        try
        {
            var ownerFolderPath = GetOwnerFolderPath(ownerKey);
            Directory.CreateDirectory(ownerFolderPath);

            var extension = ResolveExtension(file);
            var fileName = $"{Guid.NewGuid():N}{extension}";
            var filePath = Path.Combine(ownerFolderPath, fileName);

            await using var stream = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await file.CopyToAsync(stream, cancellationToken);

            var relativeUrl = $"/api/v1/school-shirt/slips/{Uri.EscapeDataString(ownerKey)}/{Uri.EscapeDataString(fileName)}";
            var publicUrl = string.IsNullOrEmpty(_publicBaseUrl) ? relativeUrl : $"{_publicBaseUrl}{relativeUrl}";
            return new StoredSlip(fileName, file.ContentType, DateTime.UtcNow, publicUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store slip");
            return AppError.Internal("Failed to store slip.");
        }
    }

    public Task<Either<AppError, SlipReadResult>> OpenReadAsync(string ownerKey, string fileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Task.FromResult<Either<AppError, SlipReadResult>>(AppError.NotFound("Slip not found."));
        }

        try
        {
            var ownerFolderPath = GetOwnerFolderPath(ownerKey);
            var safeFileName = Path.GetFileName(fileName);
            var filePath = Path.Combine(ownerFolderPath, safeFileName);

            if (!File.Exists(filePath))
            {
                return Task.FromResult<Either<AppError, SlipReadResult>>(AppError.NotFound("Slip file does not exist."));
            }

            Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var contentType = GetContentTypeFromExtension(safeFileName);
            return Task.FromResult<Either<AppError, SlipReadResult>>(new SlipReadResult(stream, contentType));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read protected slip");
            return Task.FromResult<Either<AppError, SlipReadResult>>(AppError.Internal("Failed to read protected slip."));
        }
    }

    private string GetOwnerFolderPath(string ownerKey)
    {
        var ownerHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(ownerKey)));
        return Path.Combine(_rootPath, ownerHash);
    }

    private static string ResolveExtension(IFormFile file)
    {
        if (ContentTypeExtensions.TryGetValue(file.ContentType, out var extension))
        {
            return extension;
        }

        var originalExtension = Path.GetExtension(file.FileName);
        return string.IsNullOrWhiteSpace(originalExtension) ? ".jpg" : originalExtension.ToLowerInvariant();
    }

    private static string GetContentTypeFromExtension(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => "application/octet-stream"
        };
    }
}
