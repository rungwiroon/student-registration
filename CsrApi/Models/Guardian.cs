using System;

namespace CsrApi.Models;

public class Guardian
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; } // FK
    public string? RelationType { get; set; } // Father, Mother, Other
    public string? EncryptedName { get; set; }
    public string? EncryptedPhone { get; set; }
    public string? Occupation { get; set; }
    public string? Email { get; set; }
    public string? LineUserId { get; set; }
}
