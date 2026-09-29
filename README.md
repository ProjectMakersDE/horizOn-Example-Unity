# horizOn Example — Unity

> **Status: Content complete, full play test pending**
> Scenes, prefabs, sprites, audio, URP 2D pipeline and SDK wiring are in place. The project opens
> and compiles in Unity 6000.5 and passes the batch validation (`SeagullSetup.ValidateSetup`).
> A full in-editor play test with a real horizOn API key is still pending. Screenshots will be
> added after that.

**Seagull Storm** is a mini Vampire Survivors-style roguelike built with Unity 6. It serves as a comprehensive example project demonstrating 9 [horizOn](https://horizon.pm) SDK features in a real, playable game.

## Features Demonstrated

| # | horizOn Feature | In-Game Usage |
|---|----------------|---------------|
| 1 | **Authentication** | Guest, Google, Email sign-in/sign-up on title screen |
| 2 | **Leaderboards** | Score submission, Top 10 display, player rank |
| 3 | **Cloud Save** | Persistent coins, upgrades, highscore across sessions |
| 4 | **Remote Config** | All game balancing (enemies, weapons, upgrades, wave timing) |
| 5 | **News** | In-game news feed in hub and pause menu |
| 6 | **Gift Codes** | Code redemption for coin rewards |
| 7 | **Feedback** | Bug reports and feature requests from in-game |
| 8 | **User Logs** | Aggregated run summary logged at game over |
| 9 | **Crash Reporting** | Session tracking, breadcrumbs, exception capture |
| + | **Validated Actions** (opt-in) | Run ticket and server seed at run start, input log, validated score submit, rejection reason on the game over screen (see [Validated Actions](#validated-actions)) |

## About the Game

You play as a seagull on a beach, surviving waves of crabs, jellyfish, and pirate seagulls. Auto-attack with upgradeable weapons, collect XP shells to level up, and try to survive the final boss — a giant octopus.

- **Genre:** Vampire Survivors-style auto-attack roguelike
- **Session Length:** 3–5 minutes
- **Art Style:** Pixel art (32x32 sprites), placeholder graphics included
- **Font:** Press Start 2P

## Getting Started

### Step 1 — Clone and Open

1. Clone this repository
2. Open the project in **Unity 6000.5** (the project is pinned to 6000.5.0f1)

The Unity Package Manager installs the
[horizOn SDK for Unity](https://github.com/ProjectMakersDE/horizOn-SDK-Unity) (`com.projectmakers.horizon`)
from Git when the project opens, so Git must be installed. The SDK release is pinned in
`Packages/manifest.json` and is updated automatically when a new SDK version is released.

### Step 2 — Create a horizOn Account and API Key

1. Go to [horizon.pm](https://horizon.pm) and create a free account
2. Open the **Dashboard** and create a new project
3. Navigate to **Settings > API Keys** and generate an API key
4. Download the config JSON file — it contains your `apiKey` and `backendUrl`

### Step 3 — Import the Config into the SDK

1. In Unity, go to **Window > horizOn > Config Importer**
2. Select the config JSON file you downloaded from the dashboard
3. The SDK saves the config to
   `Assets/Plugins/ProjectMakers/horizOn/CloudSDK/Resources/horizOn/HorizonConfig.asset`
   (loaded at runtime from the `horizOn/HorizonConfig` Resources path). This file holds your
   API key and is ignored by Git.

### Step 4 — Set Up Remote Config (Optional)

The game works out of the box with built-in defaults. To customize the game balance, set up Remote Config variables in the horizOn Dashboard under **Remote Config**. See the [Remote Config Reference](#remote-config-reference) below for all available keys.

### Step 5 — Run

Press **Play** in the Unity editor.

## Validated Actions

Validated Actions lets the server check a run before the score reaches the leaderboard.
At run start the game asks for a single-use run ticket bound to the leaderboard and seeds
Unity's random numbers with the server seed. During the run it records a compact input log.
At game over it submits score, reached wave and the SHA-256 hash of that log. The server
checks the ticket and your rules (score limits, minimum duration, score per second, stage
rules) before it writes anything, and asks for the log itself when the run lands in the
top N. The SDK then uploads the log in the background (`AutoUploadEvidence`).

The SDK calls live in `Assets/Scripts/Horizon/ValidatedActions/` (`ValidatedRunService`,
`RunInputLog`). `GameManager.StartRun()` and `GameManager.EndRun()` use them through the
`HorizonManager` facade, and `GameOverController` shows the result.

**Availability.** The folder is its own assembly (`SeagullStorm.ValidatedActions.asmdef`).
Its version define turns on `HORIZON_VALIDATED_ACTIONS` when the installed
`com.projectmakers.horizon` package is 1.9.0 or newer, the first SDK release that ships
Validated Actions (TASK-883). The pinned SDK in `Packages/manifest.json` is bumped
automatically after that release, so the integration switches itself on. With an older SDK
the project still compiles and the game submits scores the normal way. To try it earlier
with an SDK build that already has Validated Actions, add `HORIZON_VALIDATED_ACTIONS` under
**Project Settings > Player > Scripting Define Symbols**.

### Dashboard Setup

1. **Rules:** open **Validated Actions** in the horizOn Dashboard, pick your API key and
   set the rules. Seagull Storm's score is kills + collected XP + survived seconds, so a
   starting point is `minDurationSeconds: 20`, `maxScorePerSecond: 50` and a `maxScore`
   that fits your balancing. Rules stay on the server and never reach the game.
2. **Validated-only board:** open **Leaderboards**, edit the board the game uses
   (`default` unless you set `validated_runs_board`) and turn on **Validated submissions
   only**. From then on the board refuses normal submits (`VALIDATED_SUBMIT_REQUIRED`).
3. **Top N evidence:** on the same board set **Evidence top N**, the number of top places
   that need the input log. Runs that land there upload their log automatically, and you
   can review them in the dashboard.
4. **Stages (optional):** the game sends the reached wave as stage key `wave_<n>`
   (for example `wave_4`). Add stage rules with these keys if you want limits per wave.
5. **Coins (optional):** to let the server own the coins of a run, add a value `coins` to
   the rules (for example `"values": {"coins": {"maxPerRun": 1000}}`) and set
   `validated_runs_send_coins` to `true`.

### Enabling It in the Game

Set these Remote Config keys in the dashboard:

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `validated_runs_enabled` | bool | `false` | Start every run as a validated run. When `false`, the game uses the normal leaderboard submit |
| `validated_runs_board` | string | `default` | Leaderboard key the run ticket is bound to (the hub shows `default`) |
| `validated_runs_send_coins` | bool | `false` | Also send the coins of the run as earned value `coins`. Turn on only when your rules define a `coins` value, otherwise the server rejects the run (`UNKNOWN_VALUE_KEY`) |

If the run ticket cannot be issued (for example offline or `RUN_RATE_LIMITED`), the run
still starts and the score is submitted the normal way. A rejected validated run does not
fall back: the game over screen shows a short reason instead of the rank, for example
"Not ranked: that run was too short to count", and the error code is logged to the
console. `GameOverController` has an optional `validationText` field for a separate line;
without it the rank line shows the result.

### Input Log Format

The log is at most 32 KB (a full run uses a few kilobytes). All numbers are little endian,
the layout is the same as in the Godot and Unreal versions of the game.

| Part | Bytes | Content |
|------|-------|---------|
| Header | 5 | format version `1` (u8), run seed (u32) |
| Event | 3 | physics ticks (`FixedUpdate` calls while the run plays, 50 per second at the default fixed timestep; Godot and Unreal use 60) since the previous event (u16), event code (u8) |

Event codes: `0x00` to `0x0F` movement (bit 0 left, bit 1 right, bit 2 up, bit 3 down,
written only when the direction changes), `0x10` to `0x1F` level-up choice (low 4 bits =
button index), `0xFF` run end.

## Remote Config Reference

All values are optional — the game ships with sensible built-in defaults. Set these in the horizOn Dashboard under **Remote Config** to customize the game balance without updating the client.

### General

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `run_duration_seconds` | float | `180.0` | Duration of a survival run in seconds before the boss spawns |
| `boss_wave_enabled` | bool | `true` | Whether a boss wave spawns when the timer runs out |
| `coin_divisor` | int | `10` | Score is divided by this value to calculate coins earned |
| `xp_level_curve` | float | `1.4` | XP-to-next-level scaling exponent (higher = steeper curve) |

### Wave Spawning

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `wave_interval_seconds` | float | `15.0` | Seconds between enemy waves |
| `wave_enemy_count_base` | int | `5` | Number of enemies in the first wave |
| `wave_enemy_count_growth` | float | `1.3` | Enemy count multiplier per wave (e.g. 1.3 = +30% each wave) |
| `wave_boss_hp` | float | `500.0` | Boss hit points (overrides `enemy_boss_hp`) |

### Enemy Stats

Each enemy type (`crab`, `jellyfish`, `pirate`) has four config keys following the pattern `enemy_{type}_{stat}`:

| Key Pattern | Type | Default | Description |
|-------------|------|---------|-------------|
| `enemy_{type}_hp` | int | `30` | Hit points |
| `enemy_{type}_speed` | float | `40.0` | Movement speed (units/sec) |
| `enemy_{type}_damage` | int | `10` | Melee attack damage |
| `enemy_{type}_xp` | int | `10` | XP dropped on death |

**Example keys:** `enemy_crab_hp`, `enemy_jellyfish_speed`, `enemy_pirate_damage`

### Weapon Stats

Each weapon type (`feather`, `screech`, `dive`, `gust`) has config keys following the pattern `weapon_{type}_{stat}`:

| Key Pattern | Type | Default | Description |
|-------------|------|---------|-------------|
| `weapon_{type}_damage` | float | `20.0` | Base damage per hit |
| `weapon_{type}_cooldown` | float | `1.0` | Seconds between attacks |
| `weapon_{type}_projectiles` | int | `1` | Number of projectiles (feather only) |
| `weapon_{type}_radius` | float | `80.0` | AoE radius (screech only) |
| `weapon_{type}_range` | float | `120.0` | Dash range (dive only) |
| `weapon_{type}_knockback` | float | `60.0` | Knockback force (gust only) |

**Example keys:** `weapon_feather_damage`, `weapon_screech_cooldown`, `weapon_dive_range`

### Upgrade System

Each upgrade type (`speed`, `damage`, `hp`, `magnet`) has three config keys:

| Key Pattern | Type | Default | Description |
|-------------|------|---------|-------------|
| `upgrade_{type}_max` | int | `5` | Maximum upgrade level |
| `upgrade_{type}_costs` | JSON array | `[10, 25, 50, 100, 200]` | Coin cost per level (array index = level) |
| `upgrade_{type}_values` | JSON array | *(see below)* | Stat value at each level (array index = level) |

**Default upgrade values:**

| Upgrade | Values (level 0–5) |
|---------|---------------------|
| `speed` | `[1.0, 1.1, 1.2, 1.3, 1.4, 1.5]` (multiplier) |
| `damage` | `[1.0, 1.15, 1.3, 1.5, 1.75, 2.0]` (multiplier) |
| `hp` | `[100, 120, 140, 170, 200, 250]` (max HP) |
| `magnet` | `[50, 65, 80, 100, 120, 150]` (pickup radius) |

**Example keys:** `upgrade_speed_max`, `upgrade_damage_costs`, `upgrade_hp_values`

### Level-Up Choices

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `levelup_choices` | int | `3` | Number of choices shown on level up |
| `levelup_pool` | JSON array | *(built-in pool)* | Pool of available upgrades with weighted random selection |

**`levelup_pool` format** — each entry is an object with `id`, `type`, and `weight`:
```json
[
  {"id": "feather_dmg",   "type": "weapon_upgrade", "weight": 3},
  {"id": "feather_speed", "type": "weapon_upgrade", "weight": 2},
  {"id": "screech_new",   "type": "weapon_new",     "weight": 1},
  {"id": "dive_new",      "type": "weapon_new",     "weight": 1},
  {"id": "gust_new",      "type": "weapon_new",     "weight": 1},
  {"id": "move_speed",    "type": "stat_boost",     "weight": 2},
  {"id": "max_hp",        "type": "stat_boost",     "weight": 2},
  {"id": "xp_magnet",     "type": "stat_boost",     "weight": 1}
]
```

## Project Structure

```
Assets/
  Art/
    Sprites/           # Sprite sheets (seagull, enemies, weapons, pickups, tilemap, ui, logo)
    Fonts/             # Press Start 2P + generated "PressStart2P SDF" TMP font asset
    Audio/             # Music (3) and SFX (11)
    Tiles/             # Tile assets generated from tilemap.png (used by the GameScene island)
  Editor/              # SeagullSetup.cs (batch setup + SerializeField validation)
  Prefabs/
    Enemies/           # CrabEnemy, JellyfishEnemy, PirateEnemy, BossEnemy
    Weapons/           # FeatherProjectile
    Pickups/           # XPShell, PoisonZone
    UI/                # UpgradeSlot, LeaderboardEntry, NewsEntry, LevelUpChoiceCard
  Scenes/              # BootScene, TitleScene, GameScene
  Scripts/
    Bootstrap/         # GameBootstrap (SDK init, session restore)
    Core/              # GameManager, GameConfig, SaveData, RunState, GameColors
    Horizon/           # HorizonManager facade (all SDK features the game uses)
      ValidatedActions/  # Validated Actions assembly (ValidatedRunService, RunInputLog)
    Player/            # PlayerController, PlayerAnimator, PlayerStats
    Enemies/           # EnemyBase + Crab/Jellyfish/Pirate/Boss, EnemyPool, SpawnManager
    Weapons/           # WeaponBase + Feather/Screech/Dive/Gust, Projectile(+Pool), WeaponManager
    Pickups/           # XPPickup, PickupPool
    LevelUp/           # LevelUpManager, LevelUpChoice
    Audio/             # AudioManager (crossfade + SFX polyphony)
    UI/                # Screen controllers and reusable components
    Camera/            # CameraFollow
  Settings/            # URP-2D.asset + Renderer2D.asset (URP 2D pipeline)
Packages/              # Package manifest (horizOn SDK as a Git dependency, pinned to a release tag)
ProjectSettings/       # Unity project settings (pinned to 6000.5.0f1)
```

## Batch Validation

`Assets/Editor/SeagullSetup.cs` contains the batch-mode setup used to build the project and a
validation pass that asserts every SerializeField the game code depends on is wired:

```
Unity -batchmode -nographics -projectPath . -executeMethod SeagullSetup.ValidateSetup -logFile -
```

It exits 0 when all checks pass and logs `[VALIDATE] OK` / `[VALIDATE] MISSING` per field.

## Requirements

- [Unity 6](https://unity.com/) (6000.5)
- [Git](https://git-scm.com/) (the Package Manager uses it to download the SDK)
- [horizOn Account](https://horizon.pm) (free tier works)
- [horizOn SDK for Unity](https://github.com/ProjectMakersDE/horizOn-SDK-Unity) (installed by the Package Manager, see Step 1)

## Related Projects

- [horizOn-SDK-Unity](https://github.com/ProjectMakersDE/horizOn-SDK-Unity) — The SDK this example uses
- [horizOn-Example-Godot](https://github.com/ProjectMakersDE/horizOn-Example-Godot) — Same game in Godot
- [horizOn-Example-Unreal](https://github.com/ProjectMakersDE/horizOn-Example-Unreal) — Same game in Unreal Engine

## License

[MIT](LICENSE). The bundled Press Start 2P font is licensed under the SIL Open Font License 1.1,
see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
