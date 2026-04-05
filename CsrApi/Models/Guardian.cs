using System;

namespace CsrApi.Models;

public class Guardian
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; } // FK
    public string? RelationType { get; set; } // Father, Mother, Other
    public int GuardianOrder { get; set; } = 1; // 1 = primary guardian, 2 = secondary guardian
    public string? EncryptedName { get; set; } // Legacy: kept for migration compatibility
    public string? EncryptedFirstName { get; set; }
    public string? EncryptedLastName { get; set; }
    public string? EncryptedPhone { get; set; }
    public string? Occupation { get; set; }
    public string? Email { get; set; }
    public string? LineUserId { get; set; }
    public string? PhotoFileName { get; set; }
    public string? PhotoContentType { get; set; }
    public DateTime? PhotoUploadedAtUtc { get; set; }
}
