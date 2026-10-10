using System;
using System.Collections.Generic;
using PeakScoreboard.Api;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PeakScoreboard.UI;

/// <summary>
/// Dynamic custom columns + action buttons hosted on a header or data row.
/// </summary>
internal sealed class ScoreboardExtensionCells
{
    private readonly RectTransform _parent;
    private readonly TMP_FontAsset _font;
    private readonly bool _header;
    private readonly List<TextMeshProUGUI> _columnLabels = new();
    private readonly List<Button> _actionButtons = new();
    private readonly List<TextMeshProUGUI> _actionLabels = new();

    public ScoreboardExtensionCells(RectTransform parent, TMP_FontAsset font, bool header)
    {
        _parent = parent;
        _font = font;
        _header = header;
    }

    public void SyncStructure(ScoreboardLayout layout)
    {
        EnsureColumnCount(layout.Columns.Count);
        EnsureActionCount(layout.RowActions.Count);

        for (int i = 0; i < _columnLabels.Count; i++)
        {
            bool active = i < layout.Columns.Count;
            _columnLabels[i].gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            ScoreboardColumn column = layout.Columns[i];
            ScoreboardLayout.Slot slot = layout.ColumnSlots[i];
            TextMeshProUGUI label = _columnLabels[i];
            label.text = _header ? column.Header : string.Empty;
            label.alignment = ToTmp(column.Alignment);
            ApplySlot(label.rectTransform, slot, leftPad: 4f);
            if (_header)
            {
                label.color = RuntimeUiFactory.TextMuted;
            }
        }

        for (int i = 0; i < _actionButtons.Count; i++)
        {
            bool active = i < layout.RowActions.Count;
            _actionButtons[i].gameObject.SetActive(active);
            if (!active)
            {
                continue;
            }

            ScoreboardRowAction action = layout.RowActions[i];
            _actionLabels[i].text = _header ? string.Empty : action.Label;
            PlaceActionButton(_actionButtons[i].GetComponent<RectTransform>(), layout, i);
        }
    }

    public void BindHeader(ScoreboardLayout layout)
    {
        SyncStructure(layout);
        for (int i = 0; i < layout.RowActions.Count && i < _actionButtons.Count; i++)
        {
            // Header: hide action chrome; space is still reserved by layout.
            _actionButtons[i].gameObject.SetActive(false);
        }
    }

    public void BindRow(ScoreboardLayout layout, ScoreboardPlayer player)
    {
        SyncStructure(layout);

        for (int i = 0; i < layout.Columns.Count && i < _columnLabels.Count; i++)
        {
            ScoreboardColumn column = layout.Columns[i];
            TextMeshProUGUI label = _columnLabels[i];
            try
            {
                label.text = column.GetValue(player) ?? string.Empty;
            }
            catch (Exception exception)
            {
                label.text = "!";
                Plugin.Log.LogWarning($"Scoreboard column '{column.Id}' failed: {exception.Message}");
            }

            Color? color = null;
            try
            {
                color = column.GetColor?.Invoke(player);
            }
            catch (Exception exception)
            {
                Plugin.Log.LogWarning($"Scoreboard column '{column.Id}' color failed: {exception.Message}");
            }

            label.color = color ?? RuntimeUiFactory.TextPrimary;
        }

        for (int i = 0; i < layout.RowActions.Count && i < _actionButtons.Count; i++)
        {
            ScoreboardRowAction action = layout.RowActions[i];
            Button button = _actionButtons[i];
            bool visible = true;
            bool enabled = true;

            try
            {
                visible = action.IsVisible?.Invoke(player) ?? true;
            }
            catch (Exception exception)
            {
                visible = false;
                Plugin.Log.LogWarning($"Scoreboard action '{action.Id}' IsVisible failed: {exception.Message}");
            }

            if (visible)
            {
                try
                {
                    enabled = action.IsEnabled?.Invoke(player) ?? true;
                }
                catch (Exception exception)
                {
                    enabled = false;
                    Plugin.Log.LogWarning($"Scoreboard action '{action.Id}' IsEnabled failed: {exception.Message}");
                }
            }

            button.gameObject.SetActive(visible);
            button.interactable = enabled;
            _actionLabels[i].text = action.Label;

            button.onClick.RemoveAllListeners();
            if (visible)
            {
                ScoreboardPlayer captured = player;
                ScoreboardRowAction capturedAction = action;
                button.onClick.AddListener(() =>
                {
                    try
                    {
                        capturedAction.OnClick(captured);
                    }
                    catch (Exception exception)
                    {
                        Plugin.Log.LogError($"Scoreboard action '{capturedAction.Id}' failed: {exception}");
                    }
                });
            }

            PlaceActionButton(button.GetComponent<RectTransform>(), layout, i);
        }
    }

    private void EnsureColumnCount(int count)
    {
        while (_columnLabels.Count < count)
        {
            TextMeshProUGUI label = RuntimeUiFactory.CreateText(
                "ExtColumn",
                _parent,
                _font,
                _header ? 16f : 20f,
                _header ? RuntimeUiFactory.TextMuted : RuntimeUiFactory.TextPrimary,
                TextAlignmentOptions.Center);
            label.text = string.Empty;
            label.raycastTarget = false;
            _columnLabels.Add(label);
        }
    }

    private void EnsureActionCount(int count)
    {
        while (_actionButtons.Count < count)
        {
            Button button = RuntimeUiFactory.CreateButton("ExtAction", _parent, _font, "");
            TextMeshProUGUI? label = button.GetComponentInChildren<TextMeshProUGUI>();
            _actionButtons.Add(button);
            _actionLabels.Add(label!);
            if (_header)
            {
                button.gameObject.SetActive(false);
            }
        }
    }

    private static void PlaceActionButton(RectTransform rect, ScoreboardLayout layout, int index)
    {
        int total = Math.Max(1, layout.RowActions.Count + 1); // +1 reserved for built-in Respawn
        float span = layout.Actions.Max - layout.Actions.Min;
        float slotWidth = span / total;
        float min = layout.Actions.Min + index * slotWidth;
        float max = min + slotWidth;

        rect.anchorMin = new Vector2(min, 0.5f);
        rect.anchorMax = new Vector2(max, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0, 36);
        rect.offsetMin = new Vector2(4f, -18f);
        rect.offsetMax = new Vector2(-4f, 18f);
    }

    internal static void PlaceRespawnButton(RectTransform rect, ScoreboardLayout layout)
    {
        int customCount = layout.RowActions.Count;
        int total = Math.Max(1, customCount + 1);
        float span = layout.Actions.Max - layout.Actions.Min;
        float slotWidth = span / total;
        float min = layout.Actions.Min + customCount * slotWidth;
        float max = layout.Actions.Max;

        rect.anchorMin = new Vector2(min, 0.5f);
        rect.anchorMax = new Vector2(max, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0, 36);
        rect.offsetMin = new Vector2(4f, -18f);
        rect.offsetMax = new Vector2(-4f, 18f);
    }

    private static void ApplySlot(RectTransform rect, ScoreboardLayout.Slot slot, float leftPad)
    {
        rect.anchorMin = new Vector2(slot.Min, 0f);
        rect.anchorMax = new Vector2(slot.Max, 1f);
        rect.offsetMin = new Vector2(leftPad, 4f);
        rect.offsetMax = new Vector2(-4f, -4f);
    }

    private static TextAlignmentOptions ToTmp(ScoreboardTextAlign align) =>
        align switch
        {
            ScoreboardTextAlign.Left => TextAlignmentOptions.MidlineLeft,
            ScoreboardTextAlign.Right => TextAlignmentOptions.MidlineRight,
            _ => TextAlignmentOptions.Center,
        };
}
