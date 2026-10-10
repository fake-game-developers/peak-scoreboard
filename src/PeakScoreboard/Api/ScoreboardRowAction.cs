using System;

namespace PeakScoreboard.Api;

/// <summary>
/// A per-row button contributed by another mod (shown next to Respawn).
/// </summary>
public sealed class ScoreboardRowAction
{
    public ScoreboardRowAction(string id, string label, Action<ScoreboardPlayer> onClick)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Action id is required.", nameof(id));
        }

        Id = id.Trim();
        Label = string.IsNullOrWhiteSpace(label) ? Id : label.Trim();
        OnClick = onClick ?? throw new ArgumentNullException(nameof(onClick));
    }

    /// <summary>Stable unique id (e.g. <c>MyMod.Spectate</c>). Re-registering the same id replaces the previous action.</summary>
    public string Id { get; }

    public string Label { get; }

    public Action<ScoreboardPlayer> OnClick { get; }

    /// <summary>When set, the button is shown only when this returns true. Default: always visible.</summary>
    public Func<ScoreboardPlayer, bool>? IsVisible { get; set; }

    /// <summary>When set, controls interactable state. Default: enabled when visible.</summary>
    public Func<ScoreboardPlayer, bool>? IsEnabled { get; set; }

    /// <summary>Sort key among custom actions (lower = further left). Default 0.</summary>
    public int Order { get; set; }
}
