namespace CsrApi.Models;

public sealed class ProfileResponse
{
    public StudentProfileResponse Student { get; set; } = new();
    public GuardianProfileResponse Guardian { get; set; } = new();
}

public sealed class StudentProfileResponse
{
    public Guid Id { get; set; }
    public string? StudentId { get; set; }
    public string? Name { get; set; }
    public string? Room { get; set; }
    public string? NewRoom { get; set; }
    public int? NewNo { get; set; }
    public string? Phone { get; set; }
    public bool HasPhoto { get; set; }
    public string? PhotoUrl { get; set; }
}

public sealed class GuardianProfileResponse
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? RelationType { get; set; }
    public string? Occupation { get; set; }
    public string? Email { get; set; }
    public bool HasPhoto { get; set; }
    public string? PhotoUrl { get; set; }
}
