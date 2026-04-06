using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CsrApi.Models;
using Dapper;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace CsrApi.Repositories;

public interface IStaffRepository
{
    Task<Either<AppError, StaffUser>> GetStaffByLineIdAsync(string lineUserId);
    Task<Either<AppError, StaffUser>> GetStaffByIdAsync(string id);
    Task<Either<AppError, List<StaffUser>>> GetAllStaffAsync();
    Task<Either<AppError, Unit>> UpsertStaffUserAsync(StaffUser staff);
    Task<Either<AppError, Unit>> DeleteStaffAsync(string id);
    Task InitializeDatabaseAsync();
}

public class StaffRepository : IStaffRepository
{
    private readonly string _connectionString;

    public StaffRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException("DefaultConnection missing");
    }

    private SqliteConnection GetConnection()
    {
        return new SqliteConnection(_connectionString);
    }

    public async Task InitializeDatabaseAsync()
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        var sql = @"
            CREATE TABLE IF NOT EXISTS StaffUsers (
                Id TEXT PRIMARY KEY,
                LineUserId TEXT UNIQUE,
                Role TEXT NOT NULL,
                Name TEXT,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))
            );";

        await connection.ExecuteAsync(sql);

        // Migration: add columns if they don't exist (for existing databases)
        try { await connection.ExecuteAsync("ALTER TABLE StaffUsers ADD COLUMN IsActive INTEGER NOT NULL DEFAULT 1"); } catch { }
        try { await connection.ExecuteAsync("ALTER TABLE StaffUsers ADD COLUMN CreatedAt TEXT NOT NULL DEFAULT (datetime('now'))"); } catch { }
    }

    public async Task<Either<AppError, StaffUser>> GetStaffByLineIdAsync(string lineUserId)
    {
        try
        {
            using var connection = GetConnection();
            var staff = await connection.QuerySingleOrDefaultAsync<StaffUser>(
                "SELECT * FROM StaffUsers WHERE LineUserId = @LineUserId AND IsActive = 1", new { LineUserId = lineUserId });

            if (staff == null)
            {
                return AppError.NotFound($"StaffUser with LineUserId {lineUserId} not found.");
            }

            return staff;
        }
        catch (Exception ex)
        {
            return AppError.Internal($"Database error: {ex.Message}");
        }
    }

    public async Task<Either<AppError, StaffUser>> GetStaffByIdAsync(string id)
    {
        try
        {
            using var connection = GetConnection();
            var staff = await connection.QuerySingleOrDefaultAsync<StaffUser>(
                "SELECT * FROM StaffUsers WHERE Id = @Id", new { Id = id });

            if (staff == null)
            {
                return AppError.NotFound($"StaffUser with Id {id} not found.");
            }

            return staff;
        }
        catch (Exception ex)
        {
            return AppError.Internal($"Database error: {ex.Message}");
        }
    }

    public async Task<Either<AppError, List<StaffUser>>> GetAllStaffAsync()
    {
        try
        {
            using var connection = GetConnection();
            var staff = await connection.QueryAsync<StaffUser>(
                "SELECT * FROM StaffUsers ORDER BY CreatedAt DESC");
            return staff.ToList();
        }
        catch (Exception ex)
        {
            return AppError.Internal($"Database error: {ex.Message}");
        }
    }

    public async Task<Either<AppError, Unit>> UpsertStaffUserAsync(StaffUser staff)
    {
        try
        {
            using var connection = GetConnection();

            var sql = @"
                INSERT INTO StaffUsers (Id, LineUserId, Role, Name, IsActive, CreatedAt)
                VALUES (@Id, @LineUserId, @Role, @Name, @IsActive, @CreatedAt)
                ON CONFLICT(LineUserId) DO UPDATE SET
                    Role = excluded.Role,
                    Name = excluded.Name,
                    IsActive = excluded.IsActive;";

            var result = await connection.ExecuteAsync(sql, new
            {
                Id = staff.Id,
                staff.LineUserId,
                staff.Role,
                staff.Name,
                staff.IsActive,
                staff.CreatedAt
            });

            if (result > 0)
                return Unit.Default;

            return AppError.Internal("Failed to upsert staff user.");
        }
        catch (Exception ex)
        {
            return AppError.Internal($"Database error: {ex.Message}");
        }
    }

    public async Task<Either<AppError, Unit>> DeleteStaffAsync(string id)
    {
        try
        {
            using var connection = GetConnection();
            var result = await connection.ExecuteAsync(
                "UPDATE StaffUsers SET IsActive = 0 WHERE Id = @Id", new { Id = id });

            if (result > 0)
                return Unit.Default;

            return AppError.NotFound($"StaffUser with Id {id} not found.");
        }
        catch (Exception ex)
        {
            return AppError.Internal($"Database error: {ex.Message}");
        }
    }
}
