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
    Task<Either<AppError, Unit>> UpsertStaffUserAsync(StaffUser staff);
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
                Name TEXT
            );";

        await connection.ExecuteAsync(sql);
    }

    public async Task<Either<AppError, StaffUser>> GetStaffByLineIdAsync(string lineUserId)
    {
        try
        {
            using var connection = GetConnection();
            var staff = await connection.QuerySingleOrDefaultAsync<StaffUser>(
                "SELECT * FROM StaffUsers WHERE LineUserId = @LineUserId", new { LineUserId = lineUserId });

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

    public async Task<Either<AppError, Unit>> UpsertStaffUserAsync(StaffUser staff)
    {
        try
        {
            using var connection = GetConnection();
            
            // Fallback for LineUserId conflict if Id wasn't the collision
            var sql = @"
                INSERT INTO StaffUsers (Id, LineUserId, Role, Name) 
                VALUES (@Id, @LineUserId, @Role, @Name)
                ON CONFLICT(LineUserId) DO UPDATE SET
                    Role = excluded.Role,
                    Name = excluded.Name;";

            var result = await connection.ExecuteAsync(sql, new 
            { 
                Id = staff.Id,
                staff.LineUserId,
                staff.Role,
                staff.Name
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
}
