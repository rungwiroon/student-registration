using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CsrApi.Models;
using Dapper;
using LanguageExt;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace CsrApi.Repositories;

public interface IVenueVoteRepository
{
    Task<Either<AppError, Option<VenueVoteRecord>>> GetVoteByStudentAsync(Guid studentId);
    Task<Either<AppError, Unit>> UpsertAsync(Guid studentId, string venueId, int attendeeCount, string lineUserId);
    /// <summary>Per venue: (families, total attendees). Venues with no votes are absent.</summary>
    /// <param name="excludedStudentCodes">Test students whose votes must not be counted.</param>
    Task<Either<AppError, Dictionary<string, (int Count, int People)>>> GetCountsAsync(IReadOnlyCollection<string> excludedStudentCodes);
    Task InitializeDatabaseAsync();
}

public class VenueVoteRepository : IVenueVoteRepository
{
    private readonly string _connectionString;

    public VenueVoteRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
        }
    }

    private SqliteConnection GetConnection() => new(_connectionString);

    public async Task InitializeDatabaseAsync()
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        // One row per family (student). Re-voting overwrites the row.
        await connection.ExecuteAsync(@"
            CREATE TABLE IF NOT EXISTS VenueVotes (
                StudentId TEXT PRIMARY KEY,
                VenueId TEXT NOT NULL,
                VotedByLineUserId TEXT NOT NULL,
                CreatedAtUtc TEXT NOT NULL,
                UpdatedAtUtc TEXT NOT NULL
            );");

        // Added after the first release. Deliberately nullable with no default:
        // votes cast before it existed have no attendee count, and we must not invent one.
        try { await connection.ExecuteAsync("ALTER TABLE VenueVotes ADD COLUMN AttendeeCount INTEGER"); } catch { }
    }

    public async Task<Either<AppError, Option<VenueVoteRecord>>> GetVoteByStudentAsync(Guid studentId)
    {
        try
        {
            using var connection = GetConnection();
            var row = await connection.QuerySingleOrDefaultAsync<VenueVoteRecord>(
                "SELECT VenueId, AttendeeCount FROM VenueVotes WHERE StudentId = @StudentId",
                new { StudentId = ToKey(studentId) });

            return row is null ? Option<VenueVoteRecord>.None : Option<VenueVoteRecord>.Some(row);
        }
        catch (Exception ex)
        {
            return AppError.Internal($"Database error: {ex.Message}");
        }
    }

    public async Task<Either<AppError, Unit>> UpsertAsync(Guid studentId, string venueId, int attendeeCount, string lineUserId)
    {
        try
        {
            using var connection = GetConnection();
            var now = DateTime.UtcNow.ToString("O");

            await connection.ExecuteAsync(@"
                INSERT INTO VenueVotes (StudentId, VenueId, AttendeeCount, VotedByLineUserId, CreatedAtUtc, UpdatedAtUtc)
                VALUES (@StudentId, @VenueId, @AttendeeCount, @LineUserId, @Now, @Now)
                ON CONFLICT(StudentId) DO UPDATE SET
                    VenueId = excluded.VenueId,
                    AttendeeCount = excluded.AttendeeCount,
                    VotedByLineUserId = excluded.VotedByLineUserId,
                    UpdatedAtUtc = excluded.UpdatedAtUtc;",
                new { StudentId = ToKey(studentId), VenueId = venueId, AttendeeCount = attendeeCount, LineUserId = lineUserId, Now = now });

            return Unit.Default;
        }
        catch (Exception ex)
        {
            return AppError.Internal($"Database error: {ex.Message}");
        }
    }

    public async Task<Either<AppError, Dictionary<string, (int Count, int People)>>> GetCountsAsync(IReadOnlyCollection<string> excludedStudentCodes)
    {
        try
        {
            using var connection = GetConnection();
            // SUM ignores NULLs, so pre-existing votes count as a family but add no people.
            // Vote keys are lowercase Guid text; Students.Id may be stored upper-case, hence LOWER().
            // A vote whose student row is missing is still counted (LEFT JOIN, NULL is not "excluded").
            var rows = await connection.QueryAsync<(string VenueId, long Count, long People)>(
                @"SELECT v.VenueId, COUNT(*) AS Count, COALESCE(SUM(v.AttendeeCount), 0) AS People
                  FROM VenueVotes v
                  LEFT JOIN Students s ON LOWER(s.Id) = v.StudentId
                  WHERE s.StudentId IS NULL OR s.StudentId NOT IN @Excluded
                  GROUP BY v.VenueId",
                new { Excluded = excludedStudentCodes.Count == 0 ? new[] { "" } : excludedStudentCodes });

            return rows.ToDictionary(r => r.VenueId, r => ((int)r.Count, (int)r.People));
        }
        catch (Exception ex)
        {
            return AppError.Internal($"Database error: {ex.Message}");
        }
    }

    // Stored as plain text so the key does not depend on the Guid type handler.
    private static string ToKey(Guid studentId) => studentId.ToString("D");
}
