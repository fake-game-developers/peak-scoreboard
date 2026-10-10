using BepInEx.Configuration;

namespace PeakScoreboard;

internal sealed class ModConfig
{
    public ModConfig(ConfigFile config)
    {
        KeyboardHoldPath = config.Bind(
            "Controls",
            "KeyboardHoldPath",
            "<Keyboard>/z",
            "Default / config keyboard path for the scoreboard toggle. " +
            "Examples: <Keyboard>/z, <Keyboard>/tab, <Keyboard>/capsLock, <Mouse>/forwardButton. " +
            "Leave empty to disable the keyboard binding. " +
            "In-game rebinds (Pause → CONTROLS → Scoreboard) take priority and sync back here.");

        ControllerHoldPath = config.Bind(
            "Controls",
            "ControllerHoldPath",
            "<Gamepad>/select",
            "Default / config gamepad path for the scoreboard toggle. " +
            "Examples: <Gamepad>/select, <Gamepad>/start, <Gamepad>/leftShoulder. " +
            "Leave empty to disable the controller binding. " +
            "In-game rebinds (Pause → CONTROLS → Scoreboard) take priority and sync back here.");

        ApplyRevivePenalty = config.Bind(
            "Respawn",
            "ApplyRevivePenalty",
            true,
            "If true, revived scouts receive the vanilla revive curse and hunger. If false, they come back clean.");

        RespawnBesideLocal = config.Bind(
            "Respawn",
            "RespawnBesideLocal",
            true,
            "If true, dead scouts respawn next to you (when you are alive). Otherwise they revive at their current position.");

        RespawnVerticalOffset = config.Bind(
            "Respawn",
            "RespawnVerticalOffset",
            2f,
            "Meters above the revive anchor used when warping a revived scout (helps avoid physics flings).");
    }

    public ConfigEntry<string> KeyboardHoldPath { get; }
    public ConfigEntry<string> ControllerHoldPath { get; }
    public ConfigEntry<bool> ApplyRevivePenalty { get; }
    public ConfigEntry<bool> RespawnBesideLocal { get; }
    public ConfigEntry<float> RespawnVerticalOffset { get; }
}
