using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using UnityEngine.InputSystem;

namespace PeakScoreboard;

/// <summary>
/// Registers a native <see cref="InputAction"/> named <see cref="ActionName"/> so PEAK's
/// Rebind controls page can remap it, and provides toggle press detection for the scoreboard.
/// Default paths come from <see cref="ModConfig"/> only when the action is first created, or when
/// the config entries change. In-game rebinds are owned by PEAK's PlayerPrefs "rebinds" blob.
/// </summary>
internal static class ScoreboardAction
{
    public const string ActionName = "Scoreboard";
    public const string LocalizationId = "SCOREBOARD";
    public const string DisplayLabel = "Scoreboard";

    private static ManualLogSource? _logger;
    private static ModConfig? _config;
    private static InputAction? _action;
    private static bool _registered;
    private static bool _listeningForConfigChanges;
    private static bool _gameBindingsLoaded;
    private static bool _suppressConfigApply;

    public static InputAction? Action => _action;

    public static void Initialize(ManualLogSource logger, ModConfig config)
    {
        _logger = logger;
        _config = config;
        HookConfigChanges();
        // Register early so CharacterInput.Init's LoadRebindingsFromFile can apply Scoreboard overrides.
        // Do NOT save rebinds here — PlayerPrefs may not be loaded yet and Save would wipe them.
        EnsureRegistered();
    }

    /// <summary>
    /// Called after PEAK has loaded PlayerPrefs rebinds. Safe point to sync config ↔ live bindings.
    /// </summary>
    public static void OnGameBindingsLoaded()
    {
        _gameBindingsLoaded = true;
        EnsureRegistered();
        SyncConfigFromLiveBindings();
    }

    public static bool EnsureRegistered()
    {
        if (_registered && _action != null && _action.actionMap != null)
        {
            return true;
        }

        InputActionAsset? asset = InputSystem.actions;
        if (asset == null)
        {
            return false;
        }

        InputAction? existing = asset.FindAction(ActionName, throwIfNotFound: false);
        if (existing != null)
        {
            _action = existing;
            _registered = true;
            EnsureLocalizationEntry();
            return true;
        }

        InputActionMap? map = asset.actionMaps.FirstOrDefault(candidate => candidate.FindAction("Jump") != null)
            ?? asset.actionMaps.FirstOrDefault();
        if (map == null)
        {
            _logger?.LogWarning("Could not find an InputActionMap to host the Scoreboard action.");
            return false;
        }

        try
        {
            bool wasEnabled = map.enabled;
            if (wasEnabled)
            {
                map.Disable();
            }

            string keyboardPath = NormalizePath(_config?.KeyboardHoldPath.Value, "<Keyboard>/z");
            string controllerPath = NormalizePath(_config?.ControllerHoldPath.Value, "<Gamepad>/select");

            InputAction action = map.AddAction(ActionName, InputActionType.Button);
            if (keyboardPath.Length > 0)
            {
                action.AddBinding(keyboardPath, groups: "Keyboard&Mouse");
            }

            if (controllerPath.Length > 0)
            {
                action.AddBinding(controllerPath, groups: "Gamepad");
            }

            // Always keep at least one binding so Rebind controls has something to edit.
            if (action.bindings.Count == 0)
            {
                action.AddBinding("<Keyboard>/z", groups: "Keyboard&Mouse");
                _logger?.LogWarning("Both scoreboard bindings were empty; fell back to <Keyboard>/z.");
            }

            _action = action;

            if (wasEnabled)
            {
                map.Enable();
            }
            else
            {
                action.Enable();
            }

            EnsureLocalizationEntry();
            _registered = true;
            _logger?.LogInfo(
                $"Registered InputAction '{ActionName}' (default {DescribeBinding(keyboardPath, controllerPath)}).");
            return true;
        }
        catch (Exception exception)
        {
            _logger?.LogError($"Failed to register Scoreboard input action: {exception}");
            return false;
        }
    }

    public static bool WasPressedThisFrame()
    {
        EnsureRegistered();
        if (_action != null)
        {
            return _action.WasPressedThisFrame();
        }

        // Last-resort fallback before the Input System action exists.
        Keyboard? keyboard = Keyboard.current;
        return keyboard != null && keyboard.zKey.wasPressedThisFrame;
    }

    /// <summary>
    /// Apply BepInEx config paths as binding overrides. Only call after game rebinds are loaded,
    /// or when the user explicitly changes the config.
    /// </summary>
    public static void ApplyConfiguredBindings(bool save)
    {
        if (_config == null || !EnsureRegistered() || _action == null)
        {
            return;
        }

        try
        {
            string keyboardPath = NormalizePath(_config.KeyboardHoldPath.Value, string.Empty);
            string controllerPath = NormalizePath(_config.ControllerHoldPath.Value, string.Empty);

            ApplyOrClearGroupBinding("Keyboard&Mouse", keyboardPath);
            ApplyOrClearGroupBinding("Gamepad", controllerPath);

            if (save && _gameBindingsLoaded)
            {
                Rebinding.SaveRebindingsToFile();
            }

            _logger?.LogInfo(
                $"Scoreboard toggle binding set from config to {DescribeBinding(keyboardPath, controllerPath)}.");
        }
        catch (Exception exception)
        {
            _logger?.LogWarning($"Failed to apply scoreboard binding from config: {exception.Message}");
        }
    }

    public static string GetEffectiveKeyboardPath()
    {
        EnsureRegistered();
        return GetEffectivePathForGroup("Keyboard&Mouse");
    }

    public static string GetEffectiveGamepadPath()
    {
        EnsureRegistered();
        return GetEffectivePathForGroup("Gamepad");
    }

    private static void SyncConfigFromLiveBindings()
    {
        if (_config == null || _action == null)
        {
            return;
        }

        try
        {
            string keyboard = GetEffectivePathForGroup("Keyboard&Mouse");
            string gamepad = GetEffectivePathForGroup("Gamepad");

            // Write without ApplyConfiguredBindings (avoid feedback loop / wiping rebinds).
            _suppressConfigApply = true;
            if (!string.IsNullOrEmpty(keyboard) && _config.KeyboardHoldPath.Value != keyboard)
            {
                _config.KeyboardHoldPath.Value = keyboard;
            }

            if (!string.IsNullOrEmpty(gamepad) && _config.ControllerHoldPath.Value != gamepad)
            {
                _config.ControllerHoldPath.Value = gamepad;
            }
        }
        catch (Exception exception)
        {
            _logger?.LogWarning($"Failed to sync config from live scoreboard bindings: {exception.Message}");
        }
        finally
        {
            _suppressConfigApply = false;
        }
    }

    private static string GetEffectivePathForGroup(string group)
    {
        if (_action == null)
        {
            return string.Empty;
        }

        for (int i = 0; i < _action.bindings.Count; i++)
        {
            InputBinding binding = _action.bindings[i];
            if (!string.IsNullOrEmpty(binding.groups) && binding.groups.Contains(group))
            {
                return binding.effectivePath ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static void ApplyOrClearGroupBinding(string group, string path)
    {
        if (_action == null)
        {
            return;
        }

        int index = FindBindingIndex(group);
        if (index < 0)
        {
            if (path.Length == 0)
            {
                return;
            }

            _action.AddBinding(path, groups: group);
            return;
        }

        if (path.Length == 0)
        {
            // Disable this device's binding without removing the slot Rebind controls expects.
            _action.ApplyBindingOverride(index, string.Empty);
            return;
        }

        _action.ApplyBindingOverride(index, path);
    }

    private static int FindBindingIndex(string group)
    {
        if (_action == null)
        {
            return -1;
        }

        for (int i = 0; i < _action.bindings.Count; i++)
        {
            InputBinding binding = _action.bindings[i];
            if (!string.IsNullOrEmpty(binding.groups) && binding.groups.Contains(group))
            {
                return i;
            }
        }

        return -1;
    }

    private static void HookConfigChanges()
    {
        if (_listeningForConfigChanges || _config == null)
        {
            return;
        }

        _config.KeyboardHoldPath.SettingChanged += OnControlPathChanged;
        _config.ControllerHoldPath.SettingChanged += OnControlPathChanged;
        _listeningForConfigChanges = true;
    }

    private static void OnControlPathChanged(object sender, EventArgs e)
    {
        // Only push config → game after PEAK has loaded existing rebinds; otherwise Save would wipe them.
        if (_suppressConfigApply || !_gameBindingsLoaded)
        {
            return;
        }

        ApplyConfiguredBindings(save: true);
    }

    private static string NormalizePath(string? value, string fallback)
    {
        string trimmed = value?.Trim() ?? string.Empty;
        return trimmed.Length > 0 ? trimmed : fallback;
    }

    private static string DescribeBinding(string keyboardPath, string controllerPath)
    {
        string keyboard = keyboardPath.Length > 0 ? keyboardPath : "(keyboard off)";
        string controller = controllerPath.Length > 0 ? controllerPath : "(controller off)";
        return $"{keyboard} / {controller}";
    }

    private static void EnsureLocalizationEntry()
    {
        try
        {
            Dictionary<string, List<string>> table = LocalizedText.mainTable;
            if (table.ContainsKey(LocalizationId))
            {
                return;
            }

            var translations = new List<string>(LocalizedText.LANGUAGE_COUNT);
            for (int i = 0; i < LocalizedText.LANGUAGE_COUNT; i++)
            {
                translations.Add(DisplayLabel);
            }

            table[LocalizationId] = translations;
        }
        catch (Exception exception)
        {
            _logger?.LogWarning($"Could not register Scoreboard localization entry: {exception.Message}");
        }
    }
}
