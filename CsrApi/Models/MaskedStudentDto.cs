using System;

namespace CsrApi.Models;

public class MaskedStudentDto
{
    public Guid Id { get; set; }
    public string MaskedStudentId { get; set; } = string.Empty;
    public string MaskedName { get; set; } = string.Empty;
    public string MaskedPhone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
