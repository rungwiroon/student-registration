using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CsrApi.Services;

/// <summary>
/// Throttles failed "join as guardian" attempts per LINE account. Student codes and phone numbers
/// are guessable, so failures are capped; successful joins are never counted.
/// </summary>
public interface IJoinAttemptLimiter
{
    bool IsBlocked(string lineUserId);
    void RecordFailure(string lineUserId);
}

public sealed class JoinAttemptLimiter : IJoinAttemptLimiter
{
    public const int MaxFailures = 5;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<string, List<DateTimeOffset>> _failures = new();

    public bool IsBlocked(string lineUserId) => RecentCount(lineUserId) >= MaxFailures;

    public void RecordFailure(string lineUserId)
    {
        var list = _failures.GetOrAdd(lineUserId, _ => new List<DateTimeOffset>());
        lock (list)
        {
            Prune(list);
            list.Add(DateTimeOffset.UtcNow);
        }
    }

    private int RecentCount(string lineUserId)
    {
        if (!_failures.TryGetValue(lineUserId, out var list))
        {
            return 0;
        }

        lock (list)
        {
            Prune(list);
            return list.Count;
        }
    }

    private static void Prune(List<DateTimeOffset> list)
    {
        var cutoff = DateTimeOffset.UtcNow - Window;
        list.RemoveAll(t => t < cutoff);
    }
}
