# PeakScoreboard

A Counter-Strike-style **toggle scoreboard** for PEAK: connected scouts with **Alive / Down / Dead** status, run stats, and **Respawn** for dead players (host only).

## Usage

- Press the scoreboard key (default **Z**) to open; press it again or click **Close** to dismiss.
- Scroll the list when there are more scouts than fit on the panel.
- Each scout shows an **avatar**, **[HOST]** tag, **stamina** (with bonus stripe), active **effect icons**, plus **Height**, **Travel**, and **Deaths**.
- Dead scouts show a **Respawn** button (**host only** — clients can view the board but cannot respawn).
- Change the hotkey in `PeakScoreboard.cfg`, or in **Pause → Rebind controls** (entry: **Scoreboard**).

### Stats notes

- **Height** comes from PEAK's built-in scout altitude tracking.
- **Travel** is estimated from horizontal movement (warps / huge teleports are ignored).
- **Deaths** counts deaths during the run.
- Stats reset when a new run starts.

## Configuration

After one launch, edit:

`BepInEx/config/PeakScoreboard.cfg`

| Key | Default | Meaning |
|-----|---------|---------|
| `KeyboardHoldPath` | `<Keyboard>/z` | Keyboard/mouse binding that toggles the scoreboard |
| `ControllerHoldPath` | `<Gamepad>/select` | Gamepad binding that toggles the scoreboard |
| `ApplyRevivePenalty` | `true` | Apply vanilla revive curse + hunger |
| `RespawnBesideLocal` | `true` | Warp revived scouts next to you (or nearest alive scout) |
| `RespawnVerticalOffset` | `2` | Vertical offset (meters) used when warping |

Respawn is always host-only and is not configurable.

Leave a binding path empty to disable that device. Config values apply on launch and when the file changes; they also sync into **Rebind controls**.

## Install

Install with **r2modman** or **Gale** (requires `BepInExPack_PEAK`). Clients that want the scoreboard UI should install the mod.

## Credits

- Original author: **Arman Ossi Loko**
- This mod belongs to **Fake Game Developers**
