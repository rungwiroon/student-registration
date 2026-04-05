using System;

namespace CsrApi.Models;

public class Committee
{
    public Guid Id { get; set; }
    public string? Position { get; set; }
    public string? EncryptedName { get; set; }
    public string? EncryptedPhone { get; set; }
    public string? LineIdDisplay { get; set; }
}
