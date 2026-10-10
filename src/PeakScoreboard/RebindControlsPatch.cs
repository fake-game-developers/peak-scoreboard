using System;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using PeakScoreboard.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zorro.ControllerSupport;

namespace PeakScoreboard;

/// <summary>
/// Injects a Scoreboard row into PEAK's Pause → CONTROLS page by cloning an existing
/// <see cref="PauseMenuRebindButton"/> into the same layout group as the vanilla rows.
/// </summary>
internal static class RebindControlsPatch
{
    private const string InjectedObjectName = "PeakScoreboard_RebindButton";
    private const string InjectedKeyboardName = InjectedObjectName + "_KB";
    private const string InjectedGamepadName = InjectedObjectName + "_GP";

    private static ManualLogSource? _logger;
    private static bool _patched;

    public static void Install(Harmony harmony, ManualLogSource logger)
    {
        _logger = logger;
        if (_patched)
        {
            return;
        }

        MethodInfo? initButtons = AccessTools.Method(typeof(PauseMenuControlsPage), "InitButtons");
        if (initButtons == null)
        {
            logger.LogWarning("PauseMenuControlsPage.InitButtons not found; Scoreboard will not appear in Rebind controls.");
            return;
        }

        harmony.Patch(
            initButtons,
            prefix: new HarmonyMethod(typeof(RebindControlsPatch), nameof(InitButtonsPrefix)));
        _patched = true;
    }

    private static void InitButtonsPrefix(PauseMenuControlsPage __instance)
    {
        try
        {
            ScoreboardAction.EnsureRegistered();
            EnsureButtons(__instance);
        }
        catch (Exception exception)
        {
            _logger?.LogWarning($"Failed to inject Scoreboard rebind control: {exception}");
        }
    }

    private static void EnsureButtons(PauseMenuControlsPage page)
    {
        Transform? root = page.controlsMenuButtonsParent;
        if (root == null)
        {
            return;
        }

        PauseMenuRebindButton[] templates = root.GetComponentsInChildren<PauseMenuRebindButton>(includeInactive: true);
        if (templates.Length == 0)
        {
            _logger?.LogWarning("No PauseMenuRebindButton template found to clone for Scoreboard.");
            return;
        }

        PauseMenuRebindButton? keyboardTemplate = FindTemplate(templates, preferKeyboard: true);
        PauseMenuRebindButton? gamepadTemplate = FindTemplate(templates, preferKeyboard: false);

        // Remove the legacy clone that was parented under controlsMenuButtonsParent (wrong layout).
        DestroyLegacyMisplacedClone(root, keyboardTemplate?.transform.parent, gamepadTemplate?.transform.parent);

        bool added = false;
        if (keyboardTemplate != null)
        {
            added |= EnsureClone(keyboardTemplate, InjectedKeyboardName);
        }

        // Separate gamepad hierarchy (PEAK toggles keyboardOnlyObjects / controllerOnlyObjects).
        if (gamepadTemplate != null
            && keyboardTemplate != null
            && gamepadTemplate.transform.parent != keyboardTemplate.transform.parent)
        {
            added |= EnsureClone(gamepadTemplate, InjectedGamepadName);
        }
        else if (keyboardTemplate == null && gamepadTemplate != null)
        {
            added |= EnsureClone(gamepadTemplate, InjectedGamepadName);
        }

        if (!added)
        {
            return;
        }

        // Force InitButtons to rescan children (it caches once).
        FieldInfo? initializedField = AccessTools.Field(typeof(PauseMenuControlsPage), "initializedButtons");
        initializedField?.SetValue(page, false);

        RectTransform? layoutRoot = root as RectTransform;
        if (layoutRoot != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
        }

        _logger?.LogInfo("Added Scoreboard to Rebind controls.");
    }

    private static void DestroyLegacyMisplacedClone(Transform root, Transform? keyboardParent, Transform? gamepadParent)
    {
        PauseMenuRebindButton[] all = root.GetComponentsInChildren<PauseMenuRebindButton>(includeInactive: true);
        foreach (PauseMenuRebindButton button in all)
        {
            if (button == null)
            {
                continue;
            }

            string name = button.gameObject.name;
            bool isOurs = name == InjectedObjectName
                || name.StartsWith(InjectedObjectName, StringComparison.Ordinal);
            if (!isOurs)
            {
                continue;
            }

            Transform parent = button.transform.parent;
            bool inCorrectLayout = parent != null
                && (parent == keyboardParent || parent == gamepadParent)
                && (name == InjectedKeyboardName || name == InjectedGamepadName);
            if (!inCorrectLayout)
            {
                UnityEngine.Object.DestroyImmediate(button.gameObject);
            }
        }
    }

    private static PauseMenuRebindButton? FindTemplate(PauseMenuRebindButton[] templates, bool preferKeyboard)
    {
        PauseMenuRebindButton? fallback = null;
        foreach (PauseMenuRebindButton candidate in templates)
        {
            if (candidate == null
                || string.Equals(candidate.inputActionName, "Pause", StringComparison.OrdinalIgnoreCase)
                || candidate.gameObject.name.StartsWith(InjectedObjectName, StringComparison.Ordinal))
            {
                continue;
            }

            fallback ??= candidate;

            string name = candidate.gameObject.name;
            bool looksKeyboard = name.IndexOf("KB", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Keyboard", StringComparison.OrdinalIgnoreCase) >= 0;
            bool looksGamepad = name.IndexOf("GP", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Gamepad", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Controller", StringComparison.OrdinalIgnoreCase) >= 0;

            if (preferKeyboard && looksKeyboard)
            {
                return candidate;
            }

            if (!preferKeyboard && looksGamepad)
            {
                return candidate;
            }
        }

        // Fall back to any active non-pause button when naming conventions differ.
        foreach (PauseMenuRebindButton candidate in templates)
        {
            if (candidate == null
                || string.Equals(candidate.inputActionName, "Pause", StringComparison.OrdinalIgnoreCase)
                || candidate.gameObject.name.StartsWith(InjectedObjectName, StringComparison.Ordinal))
            {
                continue;
            }

            if (candidate.gameObject.activeInHierarchy)
            {
                return candidate;
            }
        }

        return fallback;
    }

    private static bool EnsureClone(PauseMenuRebindButton template, string objectName)
    {
        Transform parent = template.transform.parent;
        if (parent == null)
        {
            return false;
        }

        Transform? existing = parent.Find(objectName);
        if (existing != null)
        {
            return false;
        }

        GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, parent);
        clone.name = objectName;
        clone.SetActive(template.gameObject.activeSelf);

        // Keep layout sibling order with the rest of the list.
        clone.transform.SetAsLastSibling();

        RectTransform? cloneRect = clone.transform as RectTransform;
        RectTransform? templateRect = template.transform as RectTransform;
        if (cloneRect != null && templateRect != null)
        {
            cloneRect.localScale = templateRect.localScale;
            cloneRect.anchorMin = templateRect.anchorMin;
            cloneRect.anchorMax = templateRect.anchorMax;
            cloneRect.pivot = templateRect.pivot;
            cloneRect.sizeDelta = templateRect.sizeDelta;
            cloneRect.anchoredPosition = templateRect.anchoredPosition;
        }

        PauseMenuRebindButton button = clone.GetComponent<PauseMenuRebindButton>();
        button.inputActionName = ScoreboardAction.ActionName;

        // Force the button to re-resolve the action after we change the name.
        FieldInfo? inputActionField = AccessTools.Field(typeof(PauseMenuRebindButton), "inputAction");
        inputActionField?.SetValue(button, ScoreboardAction.Action);

        if (button.inputDescriptionText != null)
        {
            button.inputDescriptionText.index = ScoreboardAction.LocalizationId;
            button.inputDescriptionText.autoSet = false;
            if (button.inputDescriptionText.tmp != null)
            {
                button.inputDescriptionText.tmp.text = ScoreboardAction.DisplayLabel;
            }
        }
        else
        {
            TextMeshProUGUI? tmp = clone.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                tmp.text = ScoreboardAction.DisplayLabel;
            }
        }

        // Cloned InputIcons keep the template action enum (e.g. MoveForward → W). Replace them.
        ReplaceBindingGlyphs(clone);

        LayoutGroup? layout = parent.GetComponent<LayoutGroup>();
        if (layout != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parent as RectTransform);
        }

        return true;
    }

    private static void ReplaceBindingGlyphs(GameObject clone)
    {
        InputIcon[] icons = clone.GetComponentsInChildren<InputIcon>(includeInactive: true);
        foreach (InputIcon icon in icons)
        {
            if (icon == null)
            {
                continue;
            }

            TMP_Text? text = icon.text;
            if (text == null)
            {
                text = icon.GetComponent<TMP_Text>();
            }

            UnityEngine.Object.Destroy(icon);

            if (text == null)
            {
                continue;
            }

            ScoreboardBindingGlyph glyph = text.gameObject.GetComponent<ScoreboardBindingGlyph>()
                ?? text.gameObject.AddComponent<ScoreboardBindingGlyph>();
            glyph.Bind(text);
            glyph.Refresh(InputHandler.GetCurrentUsedInputScheme());
        }
    }
}
