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

public interface IStudentRepository
{
    Task<Either<AppError, Student>> GetStudentByIdAsync(Guid id);
    Task<Either<AppError, Guardian>> GetGuardianByLineIdAsync(string lineUserId);
    Task<Either<AppError, Unit>> AddStudentAsync(Student student);
    Task<Either<AppError, Unit>> UpsertStudentAsync(Student student);
    Task<Either<AppError, Unit>> UpdateStudentAsync(Student student);
    Task<Either<AppError, Unit>> UpsertGuardianAsync(Guardian guardian);
    Task<Either<AppError, IEnumerable<Student>>> GetStudentsAsync();
    Task InitializeDatabaseAsync();
}

public class StudentRepository : IStudentRepository
{
    private readonly string _connectionString;

    public StudentRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? throw new ArgumentNullException("DefaultConnection missing");
    }

    private SqliteConnection GetConnection()
    {
        // SQLCipher injection point: The connection string handles the password if SQLitePCLRaw.bundle_e_sqlcipher is configured.
        return new SqliteConnection(_connectionString);
    }

    public async Task InitializeDatabaseAsync()
    {
        using var connection = GetConnection();
        await connection.OpenAsync();

        // Enable WAL mode
        using var walCmd = connection.CreateCommand();
        walCmd.CommandText = "PRAGMA journal_mode=WAL;";
        await walCmd.ExecuteNonQueryAsync();

        var sql = @"
            CREATE TABLE IF NOT EXISTS Students (
                Id TEXT PRIMARY KEY,
                StudentId TEXT,
                OldRoom TEXT,
                OldNo INTEGER,
                NewRoom TEXT,
                NewNo INTEGER,
                EncryptedName TEXT NOT NULL,
                Nickname TEXT,
                BloodType TEXT,
                DOB TEXT,
                EncryptedPhone TEXT,
                PhotoFileName TEXT,
                PhotoContentType TEXT,
                PhotoUploadedAtUtc TEXT,
                Status TEXT NOT NULL
            );
            
            CREATE TABLE IF NOT EXISTS Guardians (
                Id TEXT PRIMARY KEY,
                StudentId TEXT,
                RelationType TEXT,
                EncryptedName TEXT,
                EncryptedPhone TEXT,
                Occupation TEXT,
                Email TEXT,
                LineUserId TEXT UNIQUE,
                PhotoFileName TEXT,
                PhotoContentType TEXT,
                PhotoUploadedAtUtc TEXT,
                FOREIGN KEY(StudentId) REFERENCES Students(Id)
            );
            
            CREATE TABLE IF NOT EXISTS Committees (
                Id TEXT PRIMARY KEY,
                Position TEXT,
                EncryptedName TEXT,
                EncryptedPhone TEXT,
                LineIdDisplay TEXT
            );";

        await connection.ExecuteAsync(sql);
        await EnsureColumnAsync(connection, "Students", "PhotoFileName", "TEXT");
        await EnsureColumnAsync(connection, "Students", "PhotoContentType", "TEXT");
        await EnsureColumnAsync(connection, "Students", "PhotoUploadedAtUtc", "TEXT");
        await EnsureColumnAsync(connection, "Guardians", "PhotoFileName", "TEXT");
        await EnsureColumnAsync(connection, "Guardians", "PhotoContentType", "TEXT");
        await EnsureColumnAsync(connection, "Guardians", "PhotoUploadedAtUtc", "TEXT");
    }

    private static async Task EnsureColumnAsync(SqliteConnection connection, string tableName, string columnName, string columnDefinition)
    {
        var columns = await connection.QueryAsync<TableColumnInfo>($"PRAGMA table_info({tableName});");
        if (columns.Any(column => string.Equals(column.Name, columnName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        await connection.ExecuteAsync($"ALTER TABLE {tableName} ADD COLUMN {columnName} {columnDefinition};");
    }

    public async Task<Either<AppError, Student>> GetStudentByIdAsync(Guid id)
    {
        try
        {
            using var connection = GetConnection();
            var student = await connection.QuerySingleOrDefaultAsync<Student>(
                "SELECT * FROM Students WHERE Id = @Id", new { Id = id.ToString() });

            if (student == null)
            {
                return AppError.NotFound($"Student with ID {id} not found.");
            }

            return student;
        }
        catch (Exception ex)
        {
            return AppError.Internal($"Database error: {ex.Message}");
        }
    }

    public async Task<Either<AppError, IEnumerable<Student>>> GetStudentsAsync()
    {
        try
        {
            using var connection = GetConnection();
            var students = await connection.QueryAsync<Student>("SELECT * FROM Students");
            return students.ToList();
        }
        catch (Exception ex)
        {
            return AppError.Internal($"Database error: {ex.Message}");
        }
    }

    public async Task<Either<AppError, Guardian>> GetGuardianByLineIdAsync(string lineUserId)
    {
        try
        {
            using var connection = GetConnection();
            var guardian = await connection.QuerySingleOrDefaultAsync<Guardian>(
                "SELECT * FROM Guardians WHERE LineUserId = @LineUserId", new { LineUserId = lineUserId });

            if (guardian == null)
            {
                return AppError.NotFound($"Guardian with LineUserId {lineUserId} not found.");
            }

            return guardian;
        }
        catch (Exception ex)
        {
            return AppError.Internal($"Database error: {ex.Message}");
        }
    }

    public async Task<Either<AppError, Unit>> AddStudentAsync(Student student)
    {
        try
        {
            using var connection = GetConnection();
            var sql = @"
                INSERT INTO Students (
                    Id, StudentId, OldRoom, OldNo, NewRoom, NewNo, 
                    EncryptedName, Nickname, BloodType, DOB, 
                    EncryptedPhone, PhotoFileName, PhotoContentType, PhotoUploadedAtUtc, Status
                ) 
                VALUES (
                    @Id, @StudentId, @OldRoom, @OldNo, @NewRoom, @NewNo, 
                    @EncryptedName, @Nickname, @BloodType, @DOB, 
                    @EncryptedPhone, @PhotoFileName, @PhotoContentType, @PhotoUploadedAtUtc, @Status
                )";

            var result = await connection.ExecuteAsync(sql, new 
            { 
                Id = student.Id.ToString(),
                student.StudentId,
                student.OldRoom,
                student.OldNo,
                student.NewRoom,
                student.NewNo,
                student.EncryptedName,
                student.Nickname,
                student.BloodType,
                student.DOB,
                student.EncryptedPhone,
                student.PhotoFileName,
                student.PhotoContentType,
                student.PhotoUploadedAtUtc,
                student.Status
            });

            if (result > 0)
                return Unit.Default;
            
            return AppError.Internal("Failed to insert student.");
        }
        catch (Exception ex)
        {
            return AppError.Internal($"Database error: {ex.Message}");
        }
    }

    public async Task<Either<AppError, Unit>> UpsertStudentAsync(Student student)
    {
        try
        {
            using var connection = GetConnection();
            var sql = @"
                INSERT INTO Students (
                    Id, StudentId, OldRoom, OldNo, NewRoom, NewNo,
                    EncryptedName, Nickname, BloodType, DOB,
                    EncryptedPhone, PhotoFileName, PhotoContentType, PhotoUploadedAtUtc, Status
                ) VALUES (
                    @Id, @StudentId, @OldRoom, @OldNo, @NewRoom, @NewNo,
                    @EncryptedName, @Nickname, @BloodType, @DOB,
                    @EncryptedPhone, @PhotoFileName, @PhotoContentType, @PhotoUploadedAtUtc, @Status
                )
                ON CONFLICT(Id) DO UPDATE SET
                    StudentId = excluded.StudentId,
                    OldRoom = excluded.OldRoom,
                    OldNo = excluded.OldNo,
                    NewRoom = excluded.NewRoom,
                    NewNo = excluded.NewNo,
                    EncryptedName = excluded.EncryptedName,
                    Nickname = excluded.Nickname,
                    BloodType = excluded.BloodType,
                    DOB = excluded.DOB,
                    EncryptedPhone = excluded.EncryptedPhone,
                    PhotoFileName = excluded.PhotoFileName,
                    PhotoContentType = excluded.PhotoContentType,
                    PhotoUploadedAtUtc = excluded.PhotoUploadedAtUtc,
                    Status = excluded.Status;";

            var result = await connection.ExecuteAsync(sql, new
            {
                Id = student.Id.ToString(),
                student.StudentId,
                student.OldRoom,
                student.OldNo,
                student.NewRoom,
                student.NewNo,
                student.EncryptedName,
                student.Nickname,
                student.BloodType,
                student.DOB,
                student.EncryptedPhone,
                student.PhotoFileName,
                student.PhotoContentType,
                student.PhotoUploadedAtUtc,
                student.Status
            });

            if (result > 0)
                return Unit.Default;

            return AppError.Internal("Failed to upsert student.");
        }
        catch (Exception ex)
        {
            return AppError.Internal($"Database error: {ex.Message}");
        }
    }

    public async Task<Either<AppError, Unit>> UpdateStudentAsync(Student student)
    {
        try
        {
            using var connection = GetConnection();
            var sql = @"
                UPDATE Students SET 
                    StudentId = @StudentId,
                    OldRoom = @OldRoom,
                    OldNo = @OldNo,
                    NewRoom = @NewRoom,
                    NewNo = @NewNo,
                    EncryptedName = @EncryptedName,
                    Nickname = @Nickname,
                    BloodType = @BloodType,
                    DOB = @DOB,
                    EncryptedPhone = @EncryptedPhone,
                    PhotoFileName = @PhotoFileName,
                    PhotoContentType = @PhotoContentType,
                    PhotoUploadedAtUtc = @PhotoUploadedAtUtc,
                    Status = @Status
                WHERE Id = @Id";

            var result = await connection.ExecuteAsync(sql, new 
            { 
                Id = student.Id.ToString(),
                student.StudentId,
                student.OldRoom,
                student.OldNo,
                student.NewRoom,
                student.NewNo,
                student.EncryptedName,
                student.Nickname,
                student.BloodType,
                student.DOB,
                student.EncryptedPhone,
                student.PhotoFileName,
                student.PhotoContentType,
                student.PhotoUploadedAtUtc,
                student.Status
            });

            if (result > 0)
                return Unit.Default;
            
            return AppError.Internal("Failed to update student.");
        }
        catch (Exception ex)
        {
            return AppError.Internal($"Database error: {ex.Message}");
        }
    }

    public async Task<Either<AppError, Unit>> UpsertGuardianAsync(Guardian guardian)
    {
        try
        {
            using var connection = GetConnection();
            var sql = @"
                INSERT INTO Guardians (
                    Id, StudentId, RelationType, EncryptedName, EncryptedPhone, Occupation, Email, LineUserId, PhotoFileName, PhotoContentType, PhotoUploadedAtUtc
                ) VALUES (
                    @Id, @StudentId, @RelationType, @EncryptedName, @EncryptedPhone, @Occupation, @Email, @LineUserId, @PhotoFileName, @PhotoContentType, @PhotoUploadedAtUtc
                )
                ON CONFLICT(LineUserId) DO UPDATE SET
                    RelationType = excluded.RelationType,
                    StudentId = excluded.StudentId,
                    EncryptedName = excluded.EncryptedName,
                    EncryptedPhone = excluded.EncryptedPhone,
                    Occupation = excluded.Occupation,
                    Email = excluded.Email,
                    PhotoFileName = excluded.PhotoFileName,
                    PhotoContentType = excluded.PhotoContentType,
                    PhotoUploadedAtUtc = excluded.PhotoUploadedAtUtc;";

            var result = await connection.ExecuteAsync(sql, new 
            { 
                Id = guardian.Id.ToString(),
                StudentId = guardian.StudentId.ToString(),
                guardian.RelationType,
                guardian.EncryptedName,
                guardian.EncryptedPhone,
                guardian.Occupation,
                guardian.Email,
                guardian.LineUserId,
                guardian.PhotoFileName,
                guardian.PhotoContentType,
                guardian.PhotoUploadedAtUtc
            });

            if (result > 0)
                return Unit.Default;
            
            return AppError.Internal("Failed to upsert guardian.");
        }
        catch (Exception ex)
        {
            return AppError.Internal($"Database error: {ex.Message}");
        }
    }
}

file sealed class TableColumnInfo
{
    public string Name { get; set; } = string.Empty;
}
