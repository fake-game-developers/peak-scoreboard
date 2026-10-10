using System;
using System.Collections.Generic;
using System.Linq;

namespace PeakScoreboard.Api;

/// <summary>
/// Internal store for columns and actions registered through <see cref="ScoreboardApi"/>.
/// </summary>
internal static class ScoreboardRegistry
{
    private static readonly object Gate = new();
    private static readonly Dictionary<string, ScoreboardColumn> Columns =
        new(StringComparer.Ordinal);
    private static readonly Dictionary<string, ScoreboardRowAction> Actions =
        new(StringComparer.Ordinal);

    private static int _version;

    public static int Version
    {
        get
        {
            lock (Gate)
            {
                return _version;
            }
        }
    }

    public static event Action? Changed;

    public static ScoreboardRegistration AddColumn(ScoreboardColumn column)
    {
        lock (Gate)
        {
            Columns[column.Id] = column;
            _version++;
        }

        Changed?.Invoke();
        return new ScoreboardRegistration(() => RemoveColumn(column.Id));
    }

    public static ScoreboardRegistration AddAction(ScoreboardRowAction action)
    {
        lock (Gate)
        {
            Actions[action.Id] = action;
            _version++;
        }

        Changed?.Invoke();
        return new ScoreboardRegistration(() => RemoveAction(action.Id));
    }

    public static void RemoveColumn(string id)
    {
        bool removed;
        lock (Gate)
        {
            removed = Columns.Remove(id);
            if (removed)
            {
                _version++;
            }
        }

        if (removed)
        {
            Changed?.Invoke();
        }
    }

    public static void RemoveAction(string id)
    {
        bool removed;
        lock (Gate)
        {
            removed = Actions.Remove(id);
            if (removed)
            {
                _version++;
            }
        }

        if (removed)
        {
            Changed?.Invoke();
        }
    }

    public static IReadOnlyList<ScoreboardColumn> SnapshotColumns()
    {
        lock (Gate)
        {
            return Columns.Values
                .OrderBy(column => column.Order)
                .ThenBy(column => column.Id, StringComparer.Ordinal)
                .ToArray();
        }
    }

    public static IReadOnlyList<ScoreboardRowAction> SnapshotActions()
    {
        lock (Gate)
        {
            return Actions.Values
                .OrderBy(action => action.Order)
                .ThenBy(action => action.Id, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
