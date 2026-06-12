using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UI-agnostic progression rules: stat-click bookkeeping, XP spend/refund, and
/// per-item reset. Operates on the active <see cref="SaveManager.Current"/> save.
/// Effective stat values are computed by the UI as (SO base value) + clicks*step;
/// this class only owns the clicks and the XP economy.
/// </summary>
public static class Progression
{
    // Balance constants (placeholders, tune later or move to a ProgressionConfig SO).
    public const int XP_PER_CLICK = 50;   // cost of one stat-stepper click
    public const int XP_PER_KILL = 25;    // meta-XP awarded per enemy killed at level end
    public const float RESET_REFUND = 0.8f; // 80% of spent XP returned on reset (20% penalty)

    // ── Click bookkeeping on a stat list ─────────────────────────
    public static int GetClicks(List<StatPoint> stats, string key)
    {
        if (stats == null) return 0;
        for (int i = 0; i < stats.Count; i++)
            if (stats[i].key == key) return stats[i].clicks;
        return 0;
    }

    public static void SetClicks(List<StatPoint> stats, string key, int clicks)
    {
        if (stats == null) return;
        for (int i = 0; i < stats.Count; i++)
        {
            if (stats[i].key == key)
            {
                if (clicks <= 0) stats.RemoveAt(i);
                else stats[i] = new StatPoint { key = key, clicks = clicks };
                return;
            }
        }
        if (clicks > 0) stats.Add(new StatPoint { key = key, clicks = clicks });
    }

    // ── XP economy ───────────────────────────────────────────────
    public static bool CanAfford(int clicks = 1)
    {
        var save = SaveManager.Current;
        return save != null && save.globalXP >= clicks * XP_PER_CLICK;
    }

    /// <summary>
    /// Commit a batch of pending clicks onto an item: deduct XP, add clicks,
    /// track xpSpent for refund math. Returns false (and changes nothing) if the
    /// player cannot afford the full batch.
    /// </summary>
    public static bool Commit(IProgressItem item, IReadOnlyDictionary<string, int> pendingClicks)
    {
        var save = SaveManager.Current;
        if (save == null || item == null) return false;

        int totalClicks = 0;
        foreach (var kv in pendingClicks) totalClicks += kv.Value;
        if (totalClicks <= 0) return true;

        int cost = totalClicks * XP_PER_CLICK;
        if (save.globalXP < cost) return false;

        foreach (var kv in pendingClicks)
        {
            if (kv.Value <= 0) continue;
            SetClicks(item.Stats, kv.Key, GetClicks(item.Stats, kv.Key) + kv.Value);
        }
        save.globalXP -= cost;
        item.XpSpent += cost;
        return true;
    }

    /// <summary>
    /// Reset an item back to naked defaults: clear all clicks and refund
    /// floor(xpSpent * 0.8) to the global pool. Returns the XP refunded.
    /// </summary>
    public static int ResetItem(IProgressItem item)
    {
        var save = SaveManager.Current;
        if (save == null || item == null) return 0;

        int refund = Mathf.FloorToInt(item.XpSpent * RESET_REFUND);
        save.globalXP += refund;
        item.XpSpent = 0;
        item.Stats.Clear();
        return refund;
    }
}
