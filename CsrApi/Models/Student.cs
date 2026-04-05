using System;

namespace CsrApi.Models;

public class Student
{
    public Guid Id { get; set; }
    public string? StudentId { get; set; }
    public string? OldRoom { get; set; }
    public int? OldNo { get; set; }
    public string? NewRoom { get; set; }
    public int? NewNo { get; set; }
    public string EncryptedName { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public string? BloodType { get; set; }
    public string? DOB { get; set; }
    public string? EncryptedPhone { get; set; }
    public string? PhotoFileName { get; set; }
    public string? PhotoContentType { get; set; }
    public DateTime? PhotoUploadedAtUtc { get; set; }
    public string Status { get; set; } = "Pending"; 
}
