using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CsrApi.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using LanguageExt;
using Microsoft.Extensions.Options;

namespace CsrApi.Services;

public sealed class GoogleSheetsOptions
{
    public string CredentialsPath { get; set; } = string.Empty;
    public string SpreadsheetId { get; set; } = string.Empty;
    public string SheetName { get; set; } = "Orders";
}

public interface IGoogleSheetsService
{
    Task<Either<AppError, string>> AppendOrderRowAsync(
        DateTime orderDateUtc,
        string lineDisplayName,
        string studentName,
        string studentNumber,
        string guardianPhone,
        string orderSummary,
        decimal totalAmount,
        string slipUrl,
        string status,
        CancellationToken cancellationToken);
}

public sealed class GoogleSheetsService : IGoogleSheetsService
{
    private readonly SheetsService _sheetsService;
    private readonly string _spreadsheetId;
    private readonly string _sheetName;
    private readonly ILogger<GoogleSheetsService> _logger;

    public GoogleSheetsService(IOptions<GoogleSheetsOptions> options, ILogger<GoogleSheetsService> logger)
    {
        var opts = options.Value;
        _spreadsheetId = opts.SpreadsheetId;
        _sheetName = opts.SheetName;
        _logger = logger;

        logger.LogInformation("[GoogleSheets] CredentialsPath={Path}, Exists={Exists}, SpreadsheetId={SpreadsheetId}",
            opts.CredentialsPath,
            !string.IsNullOrWhiteSpace(opts.CredentialsPath) && File.Exists(opts.CredentialsPath),
            _spreadsheetId);

        if (!string.IsNullOrWhiteSpace(opts.CredentialsPath) && File.Exists(opts.CredentialsPath))
        {
            using var stream = new FileStream(opts.CredentialsPath, FileMode.Open, FileAccess.Read);
#pragma warning disable CS0618 // GoogleCredential.FromStream is deprecated; suppressing until CredentialFactory API stabilizes in our target version
            var credential = GoogleCredential.FromStream(stream)
                .CreateScoped(SheetsService.Scope.Spreadsheets);
#pragma warning restore CS0618

            _sheetsService = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "CSR School Shirt Order"
            });
            logger.LogInformation("[GoogleSheets] Service initialized with credentials from {Path}", opts.CredentialsPath);
        }
        else
        {
            _sheetsService = new SheetsService(new BaseClientService.Initializer
            {
                ApplicationName = "CSR School Shirt Order"
            });
            logger.LogWarning("[GoogleSheets] Credentials file not found at {Path}. Service initialized WITHOUT credentials.", opts.CredentialsPath);
        }
    }

    public async Task<Either<AppError, string>> AppendOrderRowAsync(
        DateTime orderDateUtc,
        string lineDisplayName,
        string studentName,
        string studentNumber,
        string guardianPhone,
        string orderSummary,
        decimal totalAmount,
        string slipUrl,
        string status,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_spreadsheetId))
            {
                return AppError.Internal("Google Sheets SpreadsheetId is not configured.");
            }

            // Count existing rows by reading column A
            int nextRow;
            try
            {
                var getReq = _sheetsService.Spreadsheets.Values.Get(_spreadsheetId, $"{_sheetName}!A:A");
                var getResp = await getReq.ExecuteAsync(cancellationToken);
                var existingRows = getResp.Values?.Count ?? 0;
                nextRow = existingRows + 1;
            }
            catch (Google.GoogleApiException gex) when (gex.HttpStatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                // Sheet might be empty or name mismatch; start at row 1
                nextRow = 1;
            }

            var range = $"{_sheetName}!A{nextRow}:I{nextRow}";
            var valueRange = new ValueRange
            {
                Values = new List<IList<object>>
                {
                    new List<object>
                    {
                        ConvertToBangkokTime(orderDateUtc).ToString("yyyy-MM-dd HH:mm:ss"),
                        lineDisplayName ?? string.Empty,
                        studentName ?? string.Empty,
                        studentNumber ?? string.Empty,
                        $"'{guardianPhone ?? string.Empty}",
                        orderSummary ?? string.Empty,
                        totalAmount,
                        slipUrl ?? string.Empty,
                        status ?? "Pending"
                    }
                }
            };

            var request = _sheetsService.Spreadsheets.Values.Update(valueRange, _spreadsheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

            var response = await request.ExecuteAsync(cancellationToken);
            var updatedRange = response?.UpdatedRange ?? "unknown";

            return updatedRange;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to append order to Google Sheets");
            return AppError.Internal("Failed to append order to Google Sheets.");
        }
    }

    private static DateTime ConvertToBangkokTime(DateTime utcDateTime)
    {
        try
        {
            var bangkokTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Bangkok");
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, bangkokTimeZone);
        }
        catch
        {
            // Fallback: add 7 hours manually if timezone not found
            return utcDateTime.AddHours(7);
        }
    }
}
