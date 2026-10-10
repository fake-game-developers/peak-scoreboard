using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PeakScoreboard.UI;

namespace PeakScoreboard;

[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    internal static Plugin? Instance { get; private set; }
    internal static ManualLogSource Log { get; private set; } = null!;

    private Harmony? _harmony;
    private ModConfig _config = null!;
    private ScoreboardController? _controller;

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        _config = new ModConfig(Config);

        ScoreboardAction.Initialize(Log, _config);

        _controller = new ScoreboardController(_config, Log);
        _harmony = new Harmony(Id);

        try
        {
            _harmony.PatchAll(typeof(GuiManagerAttachPatch));
            _harmony.PatchAll(typeof(CharacterInputInitPatch));
            RebindControlsPatch.Install(_harmony, Log);
            Log.LogInfo($"{Name} v{Version} loaded. Press Scoreboard (default Z) to toggle the player list.");
        }
        catch (Exception exception)
        {
            Log.LogError($"Failed to patch: {exception}");
        }
    }

    private void Update()
    {
        PlayerStatsTracker.Tick();
        _controller?.Tick();
    }

    internal void AttachUi() => _controller?.Attach();

    private void OnDestroy()
    {
        _controller?.Dispose();
        _controller = null;
        _harmony?.UnpatchSelf();
        Instance = null;
    }
}

[HarmonyPatch(typeof(GUIManager), "Start")]
internal static class GuiManagerAttachPatch
{
    private static void Postfix() => Plugin.Instance?.AttachUi();
}

[HarmonyPatch(typeof(CharacterInput), nameof(CharacterInput.Init))]
internal static class CharacterInputInitPatch
{
    // Register before LoadRebindingsFromFile so Scoreboard overrides in PlayerPrefs apply.
    private static void Prefix() => ScoreboardAction.EnsureRegistered();

    private static void Postfix() => ScoreboardAction.OnGameBindingsLoaded();
}
