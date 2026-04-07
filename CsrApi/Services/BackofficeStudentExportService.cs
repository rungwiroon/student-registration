using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;
using CsrApi.Models;
using CsrApi.Repositories;
using LanguageExt;

namespace CsrApi.Services;

public interface IBackofficeStudentExportService
{
    Task<Either<AppError, StudentExportResult>> ExportStudentListAsync(
        string role,
        string? search,
        CancellationToken cancellationToken);
}

public class StudentExportResult
{
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required byte[] Content { get; init; }
}

public class BackofficeStudentExportService : IBackofficeStudentExportService
{
    private readonly IStudentRepository _repo;
    private readonly IEncryptionService _encryption;

    public BackofficeStudentExportService(IStudentRepository repo, IEncryptionService encryption)
    {
        _repo = repo;
        _encryption = encryption;
    }

    public async Task<Either<AppError, StudentExportResult>> ExportStudentListAsync(
        string role,
        string? search,
        CancellationToken cancellationToken)
    {
        var result = await _repo.GetStudentsAsync();

        return result.Match<Either<AppError, StudentExportResult>>(
            Right: students =>
            {
                var rows = MapStudentRows(students, role, search);
                var (content, fileName) = GenerateExcel(rows, role, search);
                return new StudentExportResult
                {
                    FileName = fileName,
                    ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    Content = content
                };
            },
            Left: err => err
        );
    }

    private List<StudentExportRow> MapStudentRows(IEnumerable<Student> students, string role, string? search)
    {
        var canViewFull = role == "Teacher";

        var query = students.AsEnumerable();

        // Server-side search matching the frontend filter behavior
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s =>
            {
                var studentId = s.StudentId ?? "";
                var name = _encryption.Decrypt(s.EncryptedName);
                var nickname = s.Nickname ?? "";
                return studentId.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                       name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                       nickname.Contains(search, StringComparison.OrdinalIgnoreCase);
            });
        }

        return query.Select(s =>
        {
            var plainName = _encryption.Decrypt(s.EncryptedName);
            return new StudentExportRow
            {
                StudentId = s.StudentId ?? "",
                Name = canViewFull ? plainName : MaskLastName(plainName),
                Nickname = s.Nickname ?? "",
                NewRoom = s.NewRoom ?? "",
                NewNo = s.NewNo?.ToString() ?? "",
                Status = s.Status ?? ""
            };
        }).ToList();
    }

    private (byte[] content, string fileName) GenerateExcel(List<StudentExportRow> rows, string role, string? search)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Students");

        // Header row
        var headers = new[] { "รหัสนักเรียน", "ชื่อ-นามสกุล", "ชื่อเล่น", "ห้อง", "เลขที่", "สถานะ" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Data rows
        for (int r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            ws.Cell(r + 2, 1).Value = row.StudentId;
            ws.Cell(r + 2, 2).Value = row.Name;
            ws.Cell(r + 2, 3).Value = row.Nickname;
            ws.Cell(r + 2, 4).Value = row.NewRoom;
            ws.Cell(r + 2, 5).Value = row.NewNo;
            ws.Cell(r + 2, 6).Value = row.Status;
        }

        // Auto-fit columns
        ws.Columns().AdjustToContents();

        // Build filename
        var datePart = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var safeSearch = string.IsNullOrWhiteSpace(search)
            ? null
            : new string(search.Where(char.IsLetterOrDigit).Take(20).ToArray());
        var fileName = safeSearch != null && safeSearch.Length > 0
            ? $"students-search-{safeSearch}-{datePart}.xlsx"
            : $"students-{datePart}.xlsx";

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return (ms.ToArray(), fileName);
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

    private class StudentExportRow
    {
        public required string StudentId { get; init; }
        public required string Name { get; init; }
        public required string Nickname { get; init; }
        public required string NewRoom { get; init; }
        public required string NewNo { get; init; }
        public required string Status { get; init; }
    }
}
