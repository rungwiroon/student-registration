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

    public GoogleSheetsService(IOptions<GoogleSheetsOptions> options)
    {
        var opts = options.Value;
        _spreadsheetId = opts.SpreadsheetId;
        _sheetName = opts.SheetName;

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
        }
        else
        {
            // Fallback: create uninitialized service for environments without credentials
            // The AppendOrderRowAsync will return an error if credentials are missing.
            _sheetsService = new SheetsService(new BaseClientService.Initializer
            {
                ApplicationName = "CSR School Shirt Order"
            });
        }
    }

    public async Task<Either<AppError, string>> AppendOrderRowAsync(
        DateTime orderDateUtc,
        string lineDisplayName,
        string studentName,
        string studentNumber,
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

            var range = $"{_sheetName}!A:H";
            var valueRange = new ValueRange
            {
                Values = new List<IList<object>>
                {
                    new List<object>
                    {
                        orderDateUtc.ToString("yyyy-MM-dd HH:mm:ss"),
                        lineDisplayName ?? string.Empty,
                        studentName ?? string.Empty,
                        studentNumber ?? string.Empty,
                        orderSummary ?? string.Empty,
                        totalAmount,
                        slipUrl ?? string.Empty,
                        status ?? "Pending"
                    }
                }
            };

            var request = _sheetsService.Spreadsheets.Values.Append(valueRange, _spreadsheetId, range);
            request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
            request.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;

            var response = await request.ExecuteAsync(cancellationToken);
            var updatedRange = response?.Updates?.UpdatedRange ?? "unknown";

            return updatedRange;
        }
        catch (Exception ex)
        {
            return AppError.Internal($"Failed to append order to Google Sheets: {ex.Message}");
        }
    }
}
