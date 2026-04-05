using System;
using System.Collections.Generic;

namespace CsrApi.Models;

public class RegistrationRequest
{
    public StudentInfo Student { get; set; } = new();
    public List<GuardianInfo> Guardians { get; set; } = new();
}

public class StudentInfo
{
    public string? StudentId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? OldRoom { get; set; }
    public int? OldNo { get; set; }
    public string? NewRoom { get; set; }
    public int? NewNo { get; set; }
    public string? Nickname { get; set; }
    public string? BloodType { get; set; }
    public string? DOB { get; set; }
}

public class GuardianInfo
{
    public int Order { get; set; } = 1; // 1 = primary, 2 = secondary
    public string? RelationType { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Occupation { get; set; }
    public string? Email { get; set; }
}
