using System;
using UnityEngine;

namespace PeakScoreboard.Api;

/// <summary>
/// A custom scoreboard column contributed by another mod.
/// </summary>
public sealed class ScoreboardColumn
{
    public ScoreboardColumn(string id, string header, Func<ScoreboardPlayer, string> getValue)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Column id is required.", nameof(id));
        }

        Id = id.Trim();
        Header = header ?? string.Empty;
        GetValue = getValue ?? throw new ArgumentNullException(nameof(getValue));
    }

    /// <summary>Stable unique id (e.g. <c>MyMod.Kills</c>). Re-registering the same id replaces the previous column.</summary>
    public string Id { get; }

    public string Header { get; }

    /// <summary>Returns the cell text for a scout. Called on each scoreboard refresh (~4 Hz while open).</summary>
    public Func<ScoreboardPlayer, string> GetValue { get; }

    /// <summary>Optional per-cell color. Return null to use the default scoreboard text color.</summary>
    public Func<ScoreboardPlayer, Color?>? GetColor { get; set; }

    /// <summary>Sort key among custom columns (lower = further left). Default 0.</summary>
    public int Order { get; set; }

    /// <summary>Relative width versus other custom columns. Default 1.</summary>
    public float WidthWeight { get; set; } = 1f;

    public ScoreboardTextAlign Alignment { get; set; } = ScoreboardTextAlign.Center;
}
