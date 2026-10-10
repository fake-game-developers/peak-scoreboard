using System;

namespace PeakScoreboard.Api;

/// <summary>
/// Public entry point for other PEAK mods to extend the scoreboard.
/// Soft-depend with <c>[BepInDependency(ScoreboardApi.PluginGuid, SoftDependency)]</c>
/// and only call this API when PeakScoreboard is loaded.
/// </summary>
public static class ScoreboardApi
{
    /// <summary>BepInEx plugin GUID — use this with <c>BepInDependency</c>.</summary>
    public const string PluginGuid = "PeakScoreboard";

    /// <summary>
    /// Register (or replace) a text column. Dispose the returned handle to remove it.
    /// Safe to call from your plugin's <c>Awake</c> even before the scoreboard UI exists.
    /// </summary>
    public static ScoreboardRegistration RegisterColumn(ScoreboardColumn column)
    {
        if (column == null)
        {
            throw new ArgumentNullException(nameof(column));
        }

        return ScoreboardRegistry.AddColumn(column);
    }

    /// <summary>
    /// Register (or replace) a per-row action button. Dispose the returned handle to remove it.
    /// </summary>
    public static ScoreboardRegistration RegisterAction(ScoreboardRowAction action)
    {
        if (action == null)
        {
            throw new ArgumentNullException(nameof(action));
        }

        return ScoreboardRegistry.AddAction(action);
    }

    /// <summary>Remove a previously registered column by id.</summary>
    public static void UnregisterColumn(string id)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            ScoreboardRegistry.RemoveColumn(id.Trim());
        }
    }

    /// <summary>Remove a previously registered action by id.</summary>
    public static void UnregisterAction(string id)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            ScoreboardRegistry.RemoveAction(id.Trim());
        }
    }
}
