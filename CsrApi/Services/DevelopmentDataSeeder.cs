using System;
using System.Threading.Tasks;
using CsrApi.Models;
using CsrApi.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CsrApi.Services;

public interface IDevelopmentDataSeeder
{
    Task SeedAsync();
}

public sealed class DevelopmentDataSeeder : IDevelopmentDataSeeder
{
    private static readonly Guid MockStudentId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid MockGuardianId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ClassmateOneId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid ClassmateTwoId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly IEncryptionService _encryptionService;
    private readonly IStudentRepository _studentRepository;
    private readonly IStaffRepository _staffRepository;

    public DevelopmentDataSeeder(
        IConfiguration configuration,
        IHostEnvironment environment,
        IEncryptionService encryptionService,
        IStudentRepository studentRepository,
        IStaffRepository staffRepository)
    {
        _configuration = configuration;
        _environment = environment;
        _encryptionService = encryptionService;
        _studentRepository = studentRepository;
        _staffRepository = staffRepository;
    }

    public async Task SeedAsync()
    {
        if (!_environment.IsDevelopment()) return;
        if (!_configuration.GetValue<bool>("SeedData:Enabled")) return;

        var mockUserId = _configuration["Line:MockUserId"];
        if (string.IsNullOrWhiteSpace(mockUserId)) return;

        await UpsertStudentAsync(new Student
        {
            Id = MockStudentId,
            StudentId = "30558",
            OldRoom = "ป.6/1",
            OldNo = 12,
            NewRoom = "ม.1/2",
            NewNo = 1,
            Nickname = "บีม",
            BloodType = "O",
            DOB = "2012-05-14",
            EncryptedName = _encryptionService.Encrypt("ทดสอบ ระบบ"),
            EncryptedPhone = _encryptionService.Encrypt("0811111111"),
            Status = "Pending"
        });

        await UpsertStudentAsync(new Student
        {
            Id = ClassmateOneId,
            StudentId = "30559",
            OldRoom = "ป.6/1",
            OldNo = 13,
            NewRoom = "ม.1/2",
            NewNo = 2,
            Nickname = "ฟ้า",
            BloodType = "A",
            DOB = "2012-08-09",
            EncryptedName = _encryptionService.Encrypt("สมชาย ใจดี"),
            EncryptedPhone = _encryptionService.Encrypt("0822222222"),
            Status = "Pending"
        });

        await UpsertStudentAsync(new Student
        {
            Id = ClassmateTwoId,
            StudentId = "30560",
            OldRoom = "ป.6/2",
            OldNo = 21,
            NewRoom = "ม.1/2",
            NewNo = 3,
            Nickname = "ต้น",
            BloodType = "B",
            DOB = "2012-11-02",
            EncryptedName = _encryptionService.Encrypt("สุดา มีสุข"),
            EncryptedPhone = _encryptionService.Encrypt("0833333333"),
            Status = "Pending"
        });

        await UpsertGuardianAsync(new Guardian
        {
            Id = MockGuardianId,
            StudentId = MockStudentId,
            RelationType = "Father",
            Occupation = "Tester",
            Email = "mock.parent@example.com",
            LineUserId = mockUserId,
            EncryptedName = _encryptionService.Encrypt("ผู้ปกครอง ทดสอบ"),
            EncryptedPhone = _encryptionService.Encrypt("0899999999")
        });

        await UpsertStaffUserAsync(new StaffUser
        {
            Id = Guid.NewGuid().ToString(),
            LineUserId = mockUserId,
            Role = "Teacher",
            Name = "ครูสมหญิง ใจดี"
        });
    }

    private async Task UpsertStudentAsync(Student student)
    {
        var result = await _studentRepository.UpsertStudentAsync(student);
        if (result.IsRight) return;

        throw new InvalidOperationException(result.Match(
            Right: _ => string.Empty,
            Left: err => err.Message));
    }

    private async Task UpsertGuardianAsync(Guardian guardian)
    {
        var result = await _studentRepository.UpsertGuardianAsync(guardian);
        if (result.IsRight) return;

        throw new InvalidOperationException(result.Match(
            Right: _ => string.Empty,
            Left: err => err.Message));
    }

    private async Task UpsertStaffUserAsync(StaffUser staffUser)
    {
        var result = await _staffRepository.UpsertStaffUserAsync(staffUser);
        if (result.IsRight) return;

        throw new InvalidOperationException(result.Match(
            Right: _ => string.Empty,
            Left: err => err.Message));
    }
}
