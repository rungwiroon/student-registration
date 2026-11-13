using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CsrApi.Models;
using CsrApi.Repositories;
using LanguageExt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CsrApi;

public static class VenueVoteEndpoints
{
    public static void MapVenueVoteEndpoints(this WebApplication app)
    {
        // GET /api/venue-vote — venues, deadline, my vote, and (once voted or closed) the tally
        app.MapGet("/api/venue-vote", async (
            HttpContext context,
            IStudentRepository students,
            IVenueVoteRepository votes,
            IOptions<VenueVoteOptions> options) =>
        {
            var lineUserId = GetLineUserId(context);
            if (lineUserId is null)
            {
                return Results.Unauthorized();
            }

            var studentIdResult = await FindStudentIdAsync(students, lineUserId);
            if (studentIdResult.IsLeft)
            {
                return studentIdResult.Match<IResult>(Right: _ => Results.Ok(), Left: ToErrorResult);
            }

            var studentId = studentIdResult.MatchUnsafe(Right: ToNullable, Left: _ => (Guid?)null);
            var state = await BuildStateAsync(votes, options.Value, studentId);
            return state.Match(Right: s => Results.Ok(s), Left: ToErrorResult);
        });

        // PUT /api/venue-vote — cast or change this family's vote (venue + how many will attend)
        app.MapPut("/api/venue-vote", async (
            HttpContext context,
            VenueVoteRequest request,
            IStudentRepository students,
            IVenueVoteRepository votes,
            IOptions<VenueVoteOptions> options) =>
        {
            var lineUserId = GetLineUserId(context);
            if (lineUserId is null)
            {
                return Results.Unauthorized();
            }

            if (options.Value.IsClosed(DateTimeOffset.UtcNow))
            {
                return ToErrorResult(AppError.BadRequest("ปิดโหวตแล้ว"));
            }

            if (!VenueCatalog.Contains(request.VenueId))
            {
                return ToErrorResult(AppError.BadRequest("ไม่พบสถานที่ที่เลือก"));
            }

            // double? so a fractional value (2.5) reaches this check and gets a 400 instead of a body-binding failure.
            if (request.AttendeeCount is not { } rawAttendees
                || rawAttendees != Math.Floor(rawAttendees)
                || rawAttendees < 1
                || rawAttendees > VenueCatalog.MaxAttendees)
            {
                return ToErrorResult(AppError.BadRequest($"กรุณาระบุจำนวนคนที่จะไป (1-{VenueCatalog.MaxAttendees} คน)"));
            }

            var attendees = (int)rawAttendees;

            var studentIdResult = await FindStudentIdAsync(students, lineUserId);
            if (studentIdResult.IsLeft)
            {
                return studentIdResult.Match<IResult>(Right: _ => Results.Ok(), Left: ToErrorResult);
            }

            var studentId = studentIdResult.MatchUnsafe(Right: ToNullable, Left: _ => (Guid?)null);
            if (studentId is null)
            {
                return ToErrorResult(new AppError("กรุณาลงทะเบียนนักเรียนก่อนโหวต", StatusCodes.Status403Forbidden));
            }

            var upsert = await votes.UpsertAsync(studentId.Value, request.VenueId!, attendees, lineUserId);
            if (upsert.IsLeft)
            {
                return upsert.Match<IResult>(Right: _ => Results.Ok(), Left: ToErrorResult);
            }

            var state = await BuildStateAsync(votes, options.Value, studentId);
            return state.Match(Right: s => Results.Ok(s), Left: ToErrorResult);
        });
    }

    /// <summary>Tally for every venue (zero-filled), in catalog order.</summary>
    public static async Task<Either<AppError, IReadOnlyList<VenueVoteCount>>> GetTallyAsync(IVenueVoteRepository votes)
    {
        var counts = await votes.GetCountsAsync();
        return counts.Map<IReadOnlyList<VenueVoteCount>>(byVenue => VenueCatalog.All
            .Select(v => byVenue.TryGetValue(v.Id, out var c)
                ? new VenueVoteCount(v.Id, c.Count, c.People)
                : new VenueVoteCount(v.Id, 0, 0))
            .ToList());
    }

    /// <summary>
    /// Right(None) when the LINE user has no registered student. Not Right(null):
    /// LanguageExt's Either throws ValueIsNullException on a null Right, which made
    /// every unregistered parent get a 500.
    /// </summary>
    private static async Task<Either<AppError, Option<Guid>>> FindStudentIdAsync(IStudentRepository students, string lineUserId)
    {
        var guardian = await students.GetGuardianByLineIdAsync(lineUserId);
        return guardian.Match<Either<AppError, Option<Guid>>>(
            Right: g => Option<Guid>.Some(g.StudentId),
            Left: err =>
            {
                if (err.StatusCode == StatusCodes.Status404NotFound)
                {
                    return Option<Guid>.None;
                }

                return err;
            });
    }

    // MatchUnsafe, not Match: LanguageExt's Match throws ResultIsNullException when a branch returns null.
    private static Guid? ToNullable(Option<Guid> studentId) =>
        studentId.MatchUnsafe(Some: id => (Guid?)id, None: () => (Guid?)null);

    private static async Task<Either<AppError, VenueVoteState>> BuildStateAsync(
        IVenueVoteRepository votes,
        VenueVoteOptions options,
        Guid? studentId)
    {
        var isClosed = options.IsClosed(DateTimeOffset.UtcNow);

        string? myVenueId = null;
        int? myAttendeeCount = null;
        if (studentId is { } id)
        {
            var mine = await votes.GetVoteByStudentAsync(id);
            if (mine.IsLeft)
            {
                return mine.Match<Either<AppError, VenueVoteState>>(Right: _ => throw new InvalidOperationException(), Left: err => err);
            }

            mine.Match(Right: v => v, Left: _ => Option<VenueVoteRecord>.None)
                .IfSome(v =>
                {
                    myVenueId = v.VenueId;
                    myAttendeeCount = v.AttendeeCount;
                });
        }

        // The tally stays hidden until the family has voted, so it cannot sway their choice.
        IReadOnlyList<VenueVoteCount>? tally = null;
        if (isClosed || myVenueId is not null)
        {
            var tallyResult = await GetTallyAsync(votes);
            if (tallyResult.IsLeft)
            {
                return tallyResult.Match<Either<AppError, VenueVoteState>>(Right: _ => throw new InvalidOperationException(), Left: err => err);
            }

            tally = tallyResult.Match(Right: t => t, Left: _ => Array.Empty<VenueVoteCount>());
        }

        return new VenueVoteState(
            VenueCatalog.All,
            options.ClosesAtUtc,
            isClosed,
            IsRegistered: studentId is not null,
            VenueCatalog.MaxAttendees,
            myVenueId,
            myAttendeeCount,
            tally,
            tally?.Sum(c => c.Count),
            tally?.Sum(c => c.People));
    }

    private static string? GetLineUserId(HttpContext context)
    {
        return context.Items["LineUserId"]?.ToString();
    }

    // Plain-text bodies: the frontend shows the response text as the error message.
    private static IResult ToErrorResult(AppError error)
    {
        return error.StatusCode switch
        {
            StatusCodes.Status401Unauthorized => Results.Unauthorized(),
            StatusCodes.Status500InternalServerError => Results.StatusCode(error.StatusCode),
            _ => Results.Text(error.Message, "text/plain; charset=utf-8", statusCode: error.StatusCode)
        };
    }
}
