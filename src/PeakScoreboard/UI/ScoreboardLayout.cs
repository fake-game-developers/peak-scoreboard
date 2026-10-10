using System;
using System.Collections.Generic;
using PeakScoreboard.Api;
using UnityEngine;

namespace PeakScoreboard.UI;

/// <summary>
/// Computes normalized [0,1] column slots so built-in columns, extensions, and actions share the row.
/// With no extensions the layout matches the original 90% content / 10% action split.
/// </summary>
internal sealed class ScoreboardLayout
{
    private const float BuiltInTotalWeight = 90f;
    private const float ActionBaseWeight = 10f;
    private const float ActionExtraWeight = 8f;
    private const float ExtensionUnitWeight = 8f;

    public readonly struct Slot
    {
        public Slot(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float Min { get; }
        public float Max { get; }
        public float Mid => (Min + Max) * 0.5f;
    }

    public Slot Scout { get; private set; }
    public Slot State { get; private set; }
    public Slot Stamina { get; private set; }
    public Slot Effects { get; private set; }
    public Slot Height { get; private set; }
    public Slot Travel { get; private set; }
    public Slot Deaths { get; private set; }
    public Slot Actions { get; private set; }

    public IReadOnlyList<ScoreboardColumn> Columns { get; private set; } = Array.Empty<ScoreboardColumn>();
    public IReadOnlyList<ScoreboardRowAction> RowActions { get; private set; } = Array.Empty<ScoreboardRowAction>();
    public IReadOnlyList<Slot> ColumnSlots { get; private set; } = Array.Empty<Slot>();

    public int RegistryVersion { get; private set; } = -1;

    public static ScoreboardLayout Capture()
    {
        var layout = new ScoreboardLayout();
        layout.Rebuild(
            ScoreboardRegistry.SnapshotColumns(),
            ScoreboardRegistry.SnapshotActions(),
            ScoreboardRegistry.Version);
        return layout;
    }

    public bool NeedsRebuild(int registryVersion) => RegistryVersion != registryVersion;

    public void Rebuild(
        IReadOnlyList<ScoreboardColumn> columns,
        IReadOnlyList<ScoreboardRowAction> rowActions,
        int registryVersion)
    {
        Columns = columns;
        RowActions = rowActions;
        RegistryVersion = registryVersion;

        float extensionWeight = 0f;
        for (int i = 0; i < columns.Count; i++)
        {
            extensionWeight += Mathf.Max(0.25f, columns[i].WidthWeight) * ExtensionUnitWeight;
        }

        float actionWeight = ActionBaseWeight + rowActions.Count * ActionExtraWeight;
        float total = BuiltInTotalWeight + extensionWeight + actionWeight;
        if (total <= 0f)
        {
            total = 100f;
        }

        float cursor = 0f;
        Scout = Take(ref cursor, 22f / total);
        State = Take(ref cursor, 8f / total);
        Stamina = Take(ref cursor, 18f / total);
        Effects = Take(ref cursor, 18f / total);
        Height = Take(ref cursor, 9f / total);
        Travel = Take(ref cursor, 9f / total);
        Deaths = Take(ref cursor, 6f / total);

        var columnSlots = new Slot[columns.Count];
        for (int i = 0; i < columns.Count; i++)
        {
            float weight = Mathf.Max(0.25f, columns[i].WidthWeight) * ExtensionUnitWeight / total;
            columnSlots[i] = Take(ref cursor, weight);
        }

        ColumnSlots = columnSlots;
        Actions = new Slot(cursor, 1f);
    }

    private static Slot Take(ref float cursor, float width)
    {
        float min = cursor;
        cursor = Mathf.Min(1f, cursor + width);
        return new Slot(min, cursor);
    }
}
