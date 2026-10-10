# PeakScoreboard

A PEAK mod that adds a Counter-Strike-style **toggle scoreboard**: a popup list of connected scouts with **Alive / Down / Dead** status, run stats, plus **Respawn** for dead players. Other mods can add columns and row buttons through a public API.

Inspired by the overlay style of [ItemSpawnerEnhanced](https://github.com/llleixx/ItemSpawnerEnhanced).

## Usage

- Press the scoreboard key (default **Z**) to open; press it again or click **Close** to dismiss.
- Scroll the list when there are more scouts than fit on the panel.
- Each scout shows an **avatar**, **[HOST]** tag, **stamina** (with bonus stripe), active **effect icons** (same glyphs as the stamina bar), plus **Height**, **Travel**, and **Deaths**.
- Dead scouts show a **Respawn** button (**host only** — clients can view the board but cannot respawn).
- Change the hotkey in `PeakScoreboard.cfg`, or in **Pause → Rebind controls** (entry: **Scoreboard**).

### Stats notes

- **Height** comes from PEAK's built-in scout altitude tracking.
- **Travel** is estimated locally from horizontal movement (warps / huge teleports are ignored).
- **Deaths** counts dead/ghost transitions and PEAK timeline death markers.
- Stats reset when a new run starts.

## Configuration

After one launch, edit:

`BepInEx/config/PeakScoreboard.cfg`

| Key | Default | Meaning |
|-----|---------|---------|
| `KeyboardHoldPath` | `<Keyboard>/z` | Keyboard/mouse path that toggles the scoreboard |
| `ControllerHoldPath` | `<Gamepad>/select` | Gamepad path that toggles the scoreboard |
| `ApplyRevivePenalty` | `true` | Apply vanilla revive curse + hunger |
| `RespawnBesideLocal` | `true` | Warp revived scouts next to you (or nearest alive scout) |
| `RespawnVerticalOffset` | `2` | Vertical offset (meters) used when warping |

Respawn is always host-only and is not configurable.

Control paths use Unity Input System syntax, for example `<Keyboard>/tab`, `<Keyboard>/capsLock`, `<Mouse>/forwardButton`, `<Gamepad>/leftShoulder`. Leave a path empty to disable that device. Config values are applied on launch and whenever the setting changes (including live reload); they also sync into **Rebind controls**.

## Install

1. Install `BepInExPack_PEAK`.
2. Drop `PeakScoreboard.dll` into `BepInEx/plugins/` (or use r2modman / Gale).

Clients that want the scoreboard UI should install the mod. Respawn RPCs use PEAK's built-in `RPCA_ReviveAtPosition`.

## Extending from other mods

PeakScoreboard exposes a small public API under `PeakScoreboard.Api`. Soft-depend on GUID `PeakScoreboard`, reference the DLL, and register columns or row buttons from your plugin `Awake`.

```csharp
using BepInEx;
using PeakScoreboard.Api;
using UnityEngine;

[BepInPlugin("MyTeam.MyMod", "MyMod", "1.0.0")]
[BepInDependency(ScoreboardApi.PluginGuid, BepInDependency.DependencyFlags.SoftDependency)]
public class MyPlugin : BaseUnityPlugin
{
    private ScoreboardRegistration? _column;

    private void Awake()
    {
        if (!BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(ScoreboardApi.PluginGuid))
        {
            return;
        }

        _column = ScoreboardApi.RegisterColumn(new ScoreboardColumn(
            id: "MyMod.Beans",
            header: "Beans",
            getValue: player => GetBeanCount(player.ActorId).ToString())
        {
            Order = 10,
            Alignment = ScoreboardTextAlign.Center,
            GetColor = player => player.IsDead ? Color.gray : null,
        });

        ScoreboardApi.RegisterAction(new ScoreboardRowAction(
            id: "MyMod.Ping",
            label: "Ping",
            onClick: player => Logger.LogInfo($"Ping {player.Name}"))
        {
            IsVisible = player => !player.IsLocal,
        });
    }

    private void OnDestroy() => _column?.Dispose();

    private static int GetBeanCount(int actorId) => actorId; // your data
}
```

- **Columns** — text cells with optional color, order, and width weight
- **Actions** — buttons next to Respawn; gate with `IsVisible` / `IsEnabled`
- Registrations persist across UI rebuilds; dispose the handle (or call `Unregister*`) to remove them
- Value getters run while the board is open (~4 times per second) — keep them cheap

## Build

Requires [PEAK](https://store.steampowered.com/app/3527290/PEAK/) installed (auto-detected under common Steam paths), or set `PEAK_GAME_DIR` / `-p:PeakGameRootDir=`.

```bash
dotnet build -c Release
```

Optional deploy: `-p:DeployToPeak=true -p:PeakPluginsDir="/path/to/BepInEx/plugins/PeakScoreboard"`

## License

[MIT with Attribution](LICENSE). Credit **Arman Ossi Loko** and **Fake Game Developers** when you reuse this work.

## Credits

- Original author: **Arman Ossi Loko**
- This mod belongs to **Fake Game Developers**
- Overlay approach informed by [ItemSpawnerEnhanced](https://github.com/llleixx/ItemSpawnerEnhanced)
