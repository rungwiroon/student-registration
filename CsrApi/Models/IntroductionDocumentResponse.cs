namespace CsrApi.Models;

public sealed class IntroductionDocumentResponse
{
    public StudentDocumentData Student { get; set; } = new();
    public List<GuardianDocumentData> Guardians { get; set; } = new();
}

public sealed class StudentDocumentData
{
    public string? StudentId { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Nickname { get; set; }
    public string? Room { get; set; }
    public int? NewNo { get; set; }
    public string? Phone { get; set; }
    public string? BloodType { get; set; }
    public string? DOB { get; set; }
    public bool HasPhoto { get; set; }
    public string? PhotoUrl { get; set; }
}

public sealed class GuardianDocumentData
{
    public int Order { get; set; } = 1;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? RelationType { get; set; }
    public string? Occupation { get; set; }
    public string? Email { get; set; }
    public string? LineUserId { get; set; }
    public bool HasPhoto { get; set; }
    public string? PhotoUrl { get; set; }
}
