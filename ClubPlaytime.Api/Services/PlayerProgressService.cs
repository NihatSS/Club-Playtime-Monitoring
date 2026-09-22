using ClubPlaytime.Api.Data;
using ClubPlaytime.Api.DTOs;
using ClubPlaytime.Api.Models;
using ClubPlaytime.Api.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ClubPlaytime.Api.Services;

/// <summary>
/// Computes and stores player streaks and achievements. All values are derived
/// exclusively from real tracker data (DailyPlaytime rows, Player totals,
/// tournament results, leaderboard positions) — there is no manual award path.
///
/// Streaks are fully recomputed from DailyPlaytime on every update, so:
///  - existing historical playtime produces correct streaks on first run,
///  - rows self-heal if manual adjustments change historical days,
///  - users returning after a gap get the right current streak automatically.
/// </summary>
public sealed class PlayerProgressService(
    ClubPlaytimeDbContext dbContext,
    IDiscordNotifier discordNotifier,
    ILogger<PlayerProgressService> logger)
{
    /// <summary>UTC date used for all "day" boundaries, matching how the tracker records playtime.</summary>
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>
    /// Per-player locks: progress reads happen from several endpoints at once
    /// (stats page, achievements page, detail panel) and both recompute paths
    /// insert the same PlayerStreaks row. Serializing per player keeps the
    /// recompute idempotent without blocking unrelated players.
    /// </summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, SemaphoreSlim> PlayerLocks = new();

    /// <summary>
    /// Read-path recompute throttle, per player. Every stats/achievements/summary
    /// GET used to re-derive streaks and achievements from DailyPlaytime (several
    /// queries + a potential write) before rendering, which made those pages take
    /// seconds whenever a few were opened in a row. Progress only actually changes
    /// when playtime is recorded (monitor / admin adjust call UpdatePlayerProgressAsync
    /// on the write path), so reads recompute at most once per minute per player and
    /// otherwise render straight from the stored PlayerStreaks/PlayerAchievements rows.
    /// </summary>
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<int, DateTime> LastReadRecompute = new();
    private static readonly TimeSpan ReadRecomputeInterval = TimeSpan.FromMinutes(1);

    private static bool ShouldRecomputeOnRead(int playerId)
    {
        var now = DateTime.UtcNow;
        if (LastReadRecompute.TryGetValue(playerId, out var last) && now - last < ReadRecomputeInterval)
        {
            return false;
        }

        // Claim the slot first so N concurrent opens of the same player still run
        // ONE recompute instead of N (TryUpdate keeps the earliest winner).
        var claimed = LastReadRecompute.TryUpdate(playerId, now, last)
                      || LastReadRecompute.TryAdd(playerId, now);
        return claimed;
    }

    /// <summary>Force the next read of this player to recompute (called from write paths).</summary>
    public static void InvalidateProgressCache(int playerId)
    {
        LastReadRecompute.TryRemove(playerId, out _);
    }

    /// <summary>Clears the whole read-recompute throttle (used after bulk imports / recompute-all).</summary>
    public static void InvalidateAllProgressCaches()
    {
        LastReadRecompute.Clear();
    }

    private async Task<T> WithPlayerLockAsync<T>(int playerId, Func<Task<T>> action, CancellationToken cancellationToken)
    {
        var gate = PlayerLocks.GetOrAdd(playerId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            return await action();
        }
        finally
        {
            gate.Release();
        }
    }

    /// <summary>
    /// Recomputes the streak row for one player from their DailyPlaytime rows
    /// (single pass, no loading of playtime rows for other players).
    /// </summary>
    public async Task<PlayerStreak> UpdateStreakAsync(int playerId, CancellationToken cancellationToken = default)
    {
        var dates = await dbContext.DailyPlaytime
            .AsNoTracking()
            .Where(d => d.PlayerId == playerId && d.PlaySeconds > 0)
            .Select(d => d.Date)
            .Distinct()
            .ToListAsync(cancellationToken);

        var streak = await dbContext.PlayerStreaks.FirstOrDefaultAsync(s => s.PlayerId == playerId, cancellationToken);
        var isNewRow = false;
        if (streak is null)
        {
            streak = new PlayerStreak { PlayerId = playerId };
            dbContext.PlayerStreaks.Add(streak);
            isNewRow = true;
        }

        if (dates.Count == 0)
        {
            // New users with no playtime: zeroed row, no error.
            streak.CurrentStreak = 0;
            streak.LongestStreak = 0;
            streak.DaysPlayed = 0;
            streak.LastActiveDate = null;
            streak.UpdatedAt = DateTime.UtcNow;
            if (isNewRow)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            return streak;
        }

        var ordered = dates.Order().ToList();
        var today = Today;

        // Current streak: consecutive days ending at the last active day. The
        // streak is "alive" only if the player played today or yesterday (UTC) —
        // otherwise it has lapsed and counts as 0.
        var current = 1;
        var lastIndex = ordered.Count - 1;
        var last = ordered[lastIndex];
        for (var i = lastIndex; i > 0; i--)
        {
            if (ordered[i - 1].DayNumber == ordered[i].DayNumber - 1)
            {
                current++;
            }
            else
            {
                break;
            }
        }

        if (last.DayNumber < today.DayNumber - 1)
        {
            current = 0; // streak lapsed
        }

        // Longest streak: full scan of the sorted play-days. Runs and their end
        // days are recorded so milestones can be dated to when they were really
        // reached (used to keep historical backfills off Discord).
        var longest = 1;
        var run = 1;
        var runs = new List<(int Length, DateOnly EndDay)>();
        for (var i = 1; i < ordered.Count; i++)
        {
            if (ordered[i].DayNumber == ordered[i - 1].DayNumber + 1)
            {
                run++;
                if (run > longest) longest = run;
            }
            else
            {
                runs.Add((run, ordered[i - 1]));
                run = 1;
            }
        }
        runs.Add((run, ordered[^1]));

        var previousStreak = streak.CurrentStreak;
        var previousLongest = streak.LongestStreak;

        streak.CurrentStreak = current;
        streak.LongestStreak = Math.Max(longest, current);
        streak.DaysPlayed = ordered.Count;
        streak.LastActiveDate = last;
        streak.UpdatedAt = DateTime.UtcNow;

        // Save only when something actually changed. Streaks are recomputed on
        // every stats/achievements GET; writing unconditionally turned every
        // public read into a DB roundtrip (rows updated on Railway = real cost).
        var dirty = previousStreak != streak.CurrentStreak
            || previousLongest != streak.LongestStreak
            || streak.DaysPlayed != ordered.Count
            || streak.LastActiveDate != last
            || isNewRow;
        if (dirty)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Streak milestone announcements: fire once when the live current streak
        // crosses a milestone. Bests reached through history (recompute, import)
        // also announce via the previousLongest comparison. A milestone at or
        // below the previous best has already been announced (or lapsed) and
        // stays silent, so nothing ever fires twice.
        var streakForNotification = Math.Max(current, streak.LongestStreak);
        var previousBest = Math.Max(previousStreak, previousLongest);
        if (streakForNotification > previousBest)
        {
            var milestone = StreakMilestones.Where(m => m <= streakForNotification && m > previousBest)
                .OrderByDescending(m => m)
                .FirstOrDefault();

            if (milestone > 0)
            {
                // Date the milestone to the day it was really reached and
                // announce only recent crossings — historical backfills (first
                // deploy, bulk imports) must not fire stale celebrations.
                var milestoneDay = runs.FirstOrDefault(r => r.Length >= milestone).EndDay;
                if (milestoneDay < today.AddDays(-2))
                {
                    return streak;
                }

                // Load the player only now — notification needs name + avatar,
                // and recompute-all shouldn't fetch players that have no event.
                var milestonePlayer = await dbContext.Players
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == playerId, cancellationToken);
                if (milestonePlayer is not null)
                {
                    try
                    {
                        await discordNotifier.StreakMilestoneAsync(milestonePlayer, milestone, cancellationToken);
                    }
                    catch (Exception notifyEx)
                    {
                        logger.LogWarning(notifyEx, "Streak Discord notification failed for {Username}", milestonePlayer.Username);
                    }
                }
            }
        }

        return streak;
    }

    /// <summary>Milestone day counts that trigger a Discord announcement, ascending.</summary>
    private static readonly int[] StreakMilestones = [3, 7, 14, 30, 50, 100, 365];

    /// <summary>
    /// Evaluates every catalog achievement for one player against real data and
    /// inserts unlock rows for newly satisfied ones. Existing unlocks are never
    /// removed or re-dated. Returns the keys unlocked during this call.
    /// </summary>
    public async Task<List<string>> EvaluateAchievementsAsync(int playerId, CancellationToken cancellationToken = default)
    {
        var unlocked = new List<string>();

        var player = await dbContext.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == playerId, cancellationToken);
        if (player is null)
        {
            return unlocked;
        }

        var streak = await dbContext.PlayerStreaks.FirstOrDefaultAsync(s => s.PlayerId == playerId, cancellationToken)
                     ?? await UpdateStreakAsync(playerId, cancellationToken);

        var existing = await dbContext.PlayerAchievements
            .Where(a => a.PlayerId == playerId)
            .ToDictionaryAsync(a => a.AchievementKey, a => a, cancellationToken);

        // Fast path: everything the catalog defines is already unlocked — there is
        // nothing to evaluate, so skip the rank count, the tournament query and the
        // full play-day history (this is the steady state for established players,
        // and these three queries dominated every stats/achievements read).
        if (existing.Count >= AchievementCatalog.All.Count)
        {
            return unlocked;
        }

        var now = DateTime.UtcNow;

        // ── Time-based achievements: player.TotalPlaySeconds (real tracker total) ──
        // ── Streak-based achievements: recomputed streak state ──
        // ── Rank-based: all-time leaderboard position over real totals ──
        var betterCount = await dbContext.Players.CountAsync(p => p.TotalPlaySeconds > player.TotalPlaySeconds, cancellationToken);
        var totalRank = player.TotalPlaySeconds > 0 ? betterCount + 1 : (int?)null;

        // ── Tournament Winner: completed solo tournaments where this player's
        // participant row is the recorded first place. Real tournament results. ──
        var tournamentWins = await dbContext.Tournaments
            .Where(t => t.Status == TournamentStatus.Completed
                        && t.FirstPlaceParticipantId != null
                        && t.TeamMode == TournamentTeamMode.Solo
                        && dbContext.TournamentParticipants.Any(
                            tp => tp.Id == t.FirstPlaceParticipantId
                                  && tp.PlayerId == playerId))
            .OrderBy(t => t.CompletedAt)
            .Select(t => new { t.Name, t.CompletedAt })
            .ToListAsync(cancellationToken);

        // Unlocks to announce on Discord after the database save succeeds.
        // Only recent crossings are announced — backfilled achievements from
        // a first-deploy backfill must not spam the channel with months-old
        // unlocks dated today.
        var newlyUnlocked = new List<(string Key, string Name, string Icon, DateTime? CrossedAt)>();
        var announcementCutoff = now.AddDays(-2);
        long rankThreshold = 0; // set when Top 3 is being evaluated

        // Real play-day history, used to backdate "time"/"streak" unlocks to
        // the day the requirement was actually crossed instead of "today".
        var playDays = await dbContext.DailyPlaytime
            .AsNoTracking()
            .Where(d => d.PlayerId == playerId && d.PlaySeconds > 0)
            .OrderBy(d => d.Date)
            .Select(d => new { d.Date, d.PlaySeconds })
            .ToListAsync(cancellationToken);
        var dayTotals = playDays
            .GroupBy(d => d.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.PlaySeconds));
        var orderedDays = dayTotals.Keys.Order().ToList();

        // First UTC day on which the cumulative recorded playtime reached
        // "target" seconds. Null when history never shows the crossing
        // (e.g. totals imported outside DailyPlaytime).
        DateOnly? FirstDayReaching(long target)
        {
            long running = 0;
            foreach (var day in orderedDays)
            {
                running += dayTotals[day];
                if (running >= target)
                {
                    return day;
                }
            }

            return null;
        }

        // First UTC day a run of consecutive play-days reached "target" length.
        DateOnly? FirstDayStreakReaching(int target)
        {
            var runLength = 0;
            for (var i = 0; i < orderedDays.Count; i++)
            {
                var consecutive = i > 0
                    && orderedDays[i].DayNumber == orderedDays[i - 1].DayNumber + 1;
                runLength = consecutive ? runLength + 1 : 1;
                if (runLength >= target)
                {
                    return orderedDays[i];
                }
            }

            return null;
        }

        foreach (var def in AchievementCatalog.All)
        {
            if (existing.ContainsKey(def.Key))
            {
                continue;
            }

            long currentValue;
            string? detail;
            if (def.Tier == "time")
            {
                currentValue = player.TotalPlaySeconds;
                detail = null;
            }
            else if (def.Tier == "streak")
            {
                currentValue = def.Unit == "days"
                    ? Math.Max(streak.CurrentStreak, streak.LongestStreak)
                    : streak.CurrentStreak;
                detail = null;
            }
            else if (def.Key == AchievementCatalog.TournamentWinnerKey)
            {
                if (tournamentWins.Count == 0)
                {
                    continue;
                }

                currentValue = tournamentWins.Count;
                var first = tournamentWins[0];
                detail = $"Won \"{first.Name}\"";
            }
            else if (def.Key == AchievementCatalog.Top3PlayerKey)
            {
                if (totalRank is null || totalRank > 3)
                {
                    continue;
                }

                currentValue = 4 - totalRank.Value; // 1st -> 3, 2nd -> 2, 3rd -> 1
                detail = $"All-time rank #{totalRank}";

                // Date the unlock to when the player's cumulative total first
                // reached the current 3rd-place threshold, so historical
                // backfills don't announce a months-old rank as "just now".
                // Top 3 only: count in the database instead of loading every row.
                var thirdPlaceSeconds = await dbContext.Players
                    .AsNoTracking()
                    .OrderByDescending(p => p.TotalPlaySeconds)
                    .Take(3)
                    .Select(p => p.TotalPlaySeconds)
                    .ToListAsync(cancellationToken);
                rankThreshold = thirdPlaceSeconds.Count == 3 ? thirdPlaceSeconds.Min() : 0;
            }
            else
            {
                continue;
            }

            if (currentValue >= def.TargetValue)
            {
                // Backdate the unlock to the day the requirement was really met.
                var historicalDay = def.Tier == "time"
                    ? FirstDayReaching(def.TargetValue)
                    : def.Tier == "streak"
                        ? FirstDayStreakReaching((int)def.TargetValue)
                        : def.Key == AchievementCatalog.Top3PlayerKey && rankThreshold > 0
                            ? FirstDayReaching(rankThreshold)
                            : null;
                // Play-days are UTC calendar days (same basis the tracker records
                // playtime on), so midnight of the crossing day is a UTC instant.
                // Npgsql rejects DateTime.Kind=Unspecified for timestamptz columns,
                // which made every stats/achievements read 500 on PostgreSQL.
                var crossedAt = def.Key == AchievementCatalog.TournamentWinnerKey
                    ? tournamentWins[0].CompletedAt
                    : historicalDay is null
                        ? null
                        : DateTime.SpecifyKind(historicalDay.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

                dbContext.PlayerAchievements.Add(new PlayerAchievement
                {
                    PlayerId = playerId,
                    AchievementKey = def.Key,
                    UnlockedAt = crossedAt ?? now,
                    Source = "Auto",
                    Detail = detail
                });
                unlocked.Add(def.Key);

                // Announce only recent crossings — backfilled history (e.g. the
                // first-deploy backfill or an import of old data) stays silent.
                if (crossedAt is null || crossedAt >= announcementCutoff)
                {
                    newlyUnlocked.Add((def.Key, def.Name, def.Icon, crossedAt));
                }
            }
        }

        if (unlocked.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);

            // Fire-and-forget Discord announcement — must never break the unlock flow.
            try
            {
                await discordNotifier.AchievementUnlockedAsync(player, newlyUnlocked, cancellationToken);
            }
            catch (Exception notifyEx)
            {
                logger.LogWarning(notifyEx, "Achievement Discord notification failed for {Username}", player.Username);
            }
        }

        return unlocked;
    }

    /// <summary>
    /// Convenience wrapper used after playtime changes: refresh streak, then
    /// evaluate achievements. Write paths (monitor scan, admin adjustments, bulk
    /// imports) go through here, so the read-path throttle is reset to force the
    /// next read to pick up the new state.
    /// </summary>
    public Task UpdatePlayerProgressAsync(int playerId, CancellationToken cancellationToken = default) =>
        WithPlayerLockAsync(playerId, async () =>
        {
            InvalidateProgressCache(playerId);
            await UpdateStreakAsync(playerId, cancellationToken);
            await EvaluateAchievementsAsync(playerId, cancellationToken);
            return true;
        }, cancellationToken);

    /// <summary>
    /// Re-evaluates streaks and achievements for every player. Used by a
    /// maintenance endpoint and after bulk data changes; idempotent.
    /// </summary>
    public async Task UpdateAllPlayersProgressAsync(CancellationToken cancellationToken = default)
    {
        var playerIds = await dbContext.Players
            .AsNoTracking()
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        InvalidateAllProgressCaches();
        foreach (var playerId in playerIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await UpdateStreakAsync(playerId, cancellationToken);
            await EvaluateAchievementsAsync(playerId, cancellationToken);
        }
    }

    /// <summary>Achievement list for one player (all catalog entries, unlocked first).</summary>
    public Task<PlayerAchievementsDto?> GetAchievementsAsync(int playerId, CancellationToken cancellationToken = default) =>
        WithPlayerLockAsync(playerId, async () =>
        {
            var player = await dbContext.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == playerId, cancellationToken);
            if (player is null)
            {
                return null;
            }

            // Keep stored state in sync with real data before rendering — but at
            // most once per interval per player (see ShouldRecomputeOnRead). Every
            // badge-page open used to re-derive everything from DailyPlaytime.
            var recompute = ShouldRecomputeOnRead(playerId);
            if (recompute)
            {
                await UpdateStreakAsync(playerId, cancellationToken);
                await EvaluateAchievementsAsync(playerId, cancellationToken);
            }

        var unlocks = await dbContext.PlayerAchievements
            .AsNoTracking()
            .Where(a => a.PlayerId == playerId)
            .ToDictionaryAsync(a => a.AchievementKey, a => a, cancellationToken);

        var streak = await dbContext.PlayerStreaks.AsNoTracking()
            .FirstOrDefaultAsync(s => s.PlayerId == playerId, cancellationToken);

        var dto = new PlayerAchievementsDto
        {
            PlayerId = player.Id,
            Username = player.Username,
            AvatarUrl = player.AvatarUrl,
            TotalCount = AchievementCatalog.All.Count
        };

        foreach (var def in AchievementCatalog.All)
        {
            unlocks.TryGetValue(def.Key, out var unlock);
            var (label, percent) = ComputeProgress(def, player.TotalPlaySeconds, streak);
            dto.Achievements.Add(new AchievementDto
            {
                Key = def.Key,
                Name = def.Name,
                Description = def.Description,
                Icon = def.Icon,
                Tier = def.Tier,
                Unlocked = unlock is not null,
                UnlockedAt = unlock?.UnlockedAt,
                Source = unlock?.Source,
                Detail = unlock?.Detail,
                ProgressLabel = label,
                ProgressPercent = percent
            });
        }

            dto.UnlockedCount = dto.Achievements.Count(a => a.Unlocked);
            // Unlocked first, then by progress (closest to completion first).
            dto.Achievements = dto.Achievements
                .OrderByDescending(a => a.Unlocked)
                .ThenByDescending(a => a.ProgressPercent)
                .ThenBy(a => a.Name)
                .ToList();
            return dto;
        }, cancellationToken);

    /// <summary>
    /// Full statistics for one player, computed from real tracker data. Chart
    /// data is zero-filled so charts show gaps rather than skipping days.
    /// </summary>
    public Task<PlayerStatsDto?> GetStatsAsync(int playerId, CancellationToken cancellationToken = default) =>
        WithPlayerLockAsync(playerId, async () =>
        {
            var player = await dbContext.Players
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == playerId, cancellationToken);
            if (player is null)
            {
                return null;
            }

            var recompute = ShouldRecomputeOnRead(playerId);
            var streak = await dbContext.PlayerStreaks.AsNoTracking()
                .FirstOrDefaultAsync(s => s.PlayerId == playerId, cancellationToken);
            if (recompute || streak is null)
            {
                // Throttle window elapsed, or first-ever view with no streak row yet —
                // recompute now so charts, counts and achievements are real.
                streak = await UpdateStreakAsync(playerId, cancellationToken);
                await EvaluateAchievementsAsync(playerId, cancellationToken);
            }

        var today = Today;
        var weekFrom = today.AddDays(-6);
        var monthFrom = today.AddDays(-29);
        var chartFrom = monthFrom;

        var rows = await dbContext.DailyPlaytime
            .AsNoTracking()
            .Where(d => d.PlayerId == playerId && d.Date >= chartFrom)
            .OrderBy(d => d.Date)
            .Select(d => new { d.Date, d.PlaySeconds })
            .ToListAsync(cancellationToken);

        var byDate = rows.ToDictionary(r => r.Date, r => r.PlaySeconds);

        long SumRange(DateOnly from, DateOnly to) =>
            rows.Where(r => r.Date >= from && r.Date <= to).Sum(r => r.PlaySeconds);

        var todaySeconds = byDate.GetValueOrDefault(today);
        var weekSeconds = SumRange(weekFrom, today);
        var monthSeconds = SumRange(monthFrom, today);
        var average = streak.DaysPlayed > 0 ? player.TotalPlaySeconds / streak.DaysPlayed : 0;

        var betterCount = await dbContext.Players.CountAsync(p => p.TotalPlaySeconds > player.TotalPlaySeconds, cancellationToken);
        var totalRank = player.TotalPlaySeconds > 0 ? betterCount + 1 : (int?)null;

        var achievementsUnlocked = await dbContext.PlayerAchievements
            .CountAsync(a => a.PlayerId == playerId, cancellationToken);

        // ── Daily chart: last 30 days ──
        var dailyChart = Enumerable.Range(0, 30)
            .Select(i =>
            {
                var date = today.AddDays(-(29 - i));
                return new ChartPointDto(date.ToString("MMM d"), byDate.GetValueOrDefault(date));
            })
            .ToList();

        // ── Weekly chart: last 12 weeks, buckets start Monday ──
        var weeklyChart = new List<ChartPointDto>();
        var currentWeekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7)); // most recent Monday
        for (var w = 11; w >= 0; w--)
        {
            var weekStart = currentWeekStart.AddDays(-7 * w);
            var weekEnd = weekStart.AddDays(6);
            var total = rows
                .Where(r => r.Date >= weekStart && r.Date <= weekEnd)
                .Sum(r => r.PlaySeconds);
            weeklyChart.Add(new ChartPointDto($"W/C {weekStart:MMM d}", total));
        }

        // ── Monthly chart: last 12 months ──
        var monthlyChart = new List<ChartPointDto>();
        for (var m = 11; m >= 0; m--)
        {
            var month = today.AddMonths(-m);
            var monthStart = new DateOnly(month.Year, month.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            var total = rows
                .Where(r => r.Date >= monthStart && r.Date <= monthEnd)
                .Sum(r => r.PlaySeconds);
            monthlyChart.Add(new ChartPointDto(monthStart.ToString("MMM yyyy"), total));
        }

        return new PlayerStatsDto
        {
            PlayerId = player.Id,
            Username = player.Username,
            AvatarUrl = player.AvatarUrl,
            Club = player.Club,
            TodayPlaySeconds = todaySeconds,
            WeekPlaySeconds = weekSeconds,
            MonthPlaySeconds = monthSeconds,
            TotalPlaySeconds = player.TotalPlaySeconds,
            CurrentStreak = streak.CurrentStreak,
            LongestStreak = streak.LongestStreak,
            DaysPlayed = streak.DaysPlayed,
            LastActiveDate = streak.LastActiveDate,
            AverageDailyPlaySeconds = average,
            TotalRank = totalRank,
            AchievementsUnlocked = achievementsUnlocked,
            TotalAchievements = AchievementCatalog.All.Count,
            DailyChart = dailyChart,
            WeeklyChart = weeklyChart,
            MonthlyChart = monthlyChart
        };
        }, cancellationToken);

    /// <summary>
    /// Compact summary for embedding in existing responses (player details,
    /// profile). Does not write anything — reads the cached streak row so the
    /// hot dashboard path stays cheap.
    /// </summary>
    /// <summary>
    /// Streaks, achievement count and rank for the dashboard's detail panel.
    /// This surface only READS the stored progress rows, so it must recompute
    /// them first, exactly like <see cref="GetAchievementsAsync"/> does: progress
    /// is derived lazily (the monitor only recomputes when playtime changes), so
    /// without this the panel showed zeros for every player who had not been
    /// scanned since the progress feature shipped — while the badges page, which
    /// does recompute, showed the real numbers for the same player.
    /// </summary>
    public Task<PlayerProgressSummaryDto?> GetSummaryAsync(int playerId, CancellationToken cancellationToken = default) =>
        WithPlayerLockAsync<PlayerProgressSummaryDto?>(playerId, async () =>
        {
            var player = await dbContext.Players
                .AsNoTracking()
                .Select(p => new { p.Id, p.TotalPlaySeconds })
                .FirstOrDefaultAsync(p => p.Id == playerId, cancellationToken);
            if (player is null)
            {
                return null;
            }

            var recompute = ShouldRecomputeOnRead(playerId);
            if (recompute)
            {
                await UpdateStreakAsync(playerId, cancellationToken);
                await EvaluateAchievementsAsync(playerId, cancellationToken);
            }

            var streak = await dbContext.PlayerStreaks.AsNoTracking()
                .FirstOrDefaultAsync(s => s.PlayerId == playerId, cancellationToken);
            var unlocked = await dbContext.PlayerAchievements
                .CountAsync(a => a.PlayerId == playerId, cancellationToken);

            var betterCount = await dbContext.Players.CountAsync(p => p.TotalPlaySeconds > player.TotalPlaySeconds, cancellationToken);
            var totalRank = player.TotalPlaySeconds > 0 ? betterCount + 1 : (int?)null;

            return new PlayerProgressSummaryDto
            {
                CurrentStreak = streak?.CurrentStreak ?? 0,
                LongestStreak = streak?.LongestStreak ?? 0,
                DaysPlayed = streak?.DaysPlayed ?? 0,
                LastActiveDate = streak?.LastActiveDate,
                AchievementsUnlocked = unlocked,
                TotalAchievements = AchievementCatalog.All.Count,
                TotalRank = totalRank
            };
        }, cancellationToken);

    private static (string label, int percent) ComputeProgress(
        AchievementDefinition def, long totalPlaySeconds, PlayerStreak? streak)
    {
        if (def.Tier == "time")
        {
            var pct = def.TargetValue == 0 ? 100 : (int)Math.Min(100, totalPlaySeconds * 100 / def.TargetValue);
            return ($"{FormatShort(totalPlaySeconds)} / {FormatShort(def.TargetValue)}", pct);
        }

        if (def.Tier == "streak")
        {
            var best = Math.Max(streak?.CurrentStreak ?? 0, streak?.LongestStreak ?? 0);
            var pct = (int)Math.Min(100, best * 100 / def.TargetValue);
            return ($"{best} / {def.TargetValue} days", pct);
        }

        if (def.Key == AchievementCatalog.Top3PlayerKey)
        {
            return ("Reach top 3 all-time", 0);
        }

        return ("Win a tournament", 0);
    }

    private static string FormatShort(long seconds)
    {
        var hours = seconds / 3600.0;
        return hours >= 10
            ? $"{Math.Round(hours)}h"
            : $"{Math.Floor(hours)}h {seconds % 3600 / 60}m";
    }
}
