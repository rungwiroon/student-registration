using System;
using System.Collections.Generic;
using System.Linq;

namespace CsrApi.Models;

public sealed record VenueOption(string Id, string Name, string Area, string Url);

/// <summary>
/// Fixed list of venues for the parents' gathering vote. One-off poll, so the
/// options live in code rather than in the database.
/// </summary>
public static class VenueCatalog
{
    /// <summary>Most people one family can register (including the voter). Also sent to the frontend.</summary>
    public const int MaxAttendees = 10;

    public static readonly IReadOnlyList<VenueOption> All = new List<VenueOption>
    {
        new("big-tree", "The Big Tree Café (เดอะ บิ๊กทรี คาเฟ่)", "ท่าอิฐ นนทบุรี",
            "https://www.facebook.com/thebigtreethai/?locale=th_TH"),
        new("lets-chill", "LET'S CHILL - เลทซ์ชิลล์ Music & Restaurant", "ถนนชัยพฤกษ์ นนทบุรี",
            "https://www.facebook.com/letschill.mr/?locale=th_TH"),
        new("blu-o", "Blu-O Bowling", "เอสพลานาด แคราย",
            "https://www.bluofriends.com/home/platinumroom/"),
        new("suan-lung-mong", "สวนลุงหมง", "ต.ราษฎร์นิยม อ.ไทรน้อย",
            "https://www.facebook.com/SuanLungMong/?locale=th_TH"),
    };

    public static bool Contains(string? venueId) =>
        !string.IsNullOrWhiteSpace(venueId) && All.Any(v => v.Id == venueId);
}

public sealed class VenueVoteOptions
{
    /// <summary>Voting deadline. Null means the vote stays open.</summary>
    public DateTimeOffset? ClosesAtUtc { get; set; }

    public bool IsClosed(DateTimeOffset now) => ClosesAtUtc is { } closesAt && now >= closesAt;

    /// <summary>
    /// Comma-separated student codes (รหัสประจำตัว) of test records. Their votes are kept
    /// but left out of every tally and out of the "families" denominator.
    /// </summary>
    public string ExcludedStudentCodes { get; set; } = string.Empty;

    public IReadOnlyList<string> ExcludedCodes() =>
        ExcludedStudentCodes
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
}

/// <summary>A family's stored vote. AttendeeCount is null for votes cast before the field existed.</summary>
public sealed class VenueVoteRecord
{
    public string VenueId { get; set; } = string.Empty;
    public int? AttendeeCount { get; set; }
}

public sealed record VenueVoteRequest(string? VenueId, double? AttendeeCount);

/// <summary>Count = families that voted for the venue; People = total attendees those families registered.</summary>
public sealed record VenueVoteCount(string VenueId, int Count, int People);

public sealed record VenueVoteState(
    IReadOnlyList<VenueOption> Venues,
    DateTimeOffset? ClosesAtUtc,
    bool IsClosed,
    bool IsRegistered,
    int MaxAttendees,
    string? MyVenueId,
    int? MyAttendeeCount,
    IReadOnlyList<VenueVoteCount>? Counts,
    int? TotalVotes,
    int? TotalPeople);
