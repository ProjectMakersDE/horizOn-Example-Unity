# horizOn Example — Unity

> **Status: Under Construction**
> This project is actively being developed. Screenshots and a playable demo will be added soon.

**Seagull Storm** is a mini Vampire Survivors-style roguelike built with Unity 6. It serves as a comprehensive example project demonstrating all 9 [horizOn](https://horizon.pm) SDK features in a real, playable game.

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

## About the Game

You play as a seagull on a beach, surviving waves of crabs, jellyfish, and pirate seagulls. Auto-attack with upgradeable weapons, collect XP shells to level up, and try to survive the final boss — a giant octopus.

- **Genre:** Vampire Survivors-style auto-attack roguelike
- **Session Length:** 3–5 minutes
- **Art Style:** Pixel art (32x32 sprites), placeholder graphics included
- **Font:** Press Start 2P

## Getting Started

1. Clone this repository
2. Open the project in Unity 6 (2025 LTS)
3. Configure your horizOn API key in the SDK settings
4. Enter Play mode

## Project Structure

```
Assets/
  Fonts/               # Press Start 2P
  Audio/               # Music and SFX (placeholder)
  Sprites/             # Sprite sheets (placeholder)
  Scripts/
    Core/              # GameManager, AudioManager, ConfigCache
    Horizon/           # HorizonManager facade, SDK integration
    Player/            # PlayerController, PlayerHealth
    Enemies/           # EnemyBase, Crab, Jellyfish, Pirate, Boss
    Weapons/           # WeaponBase, Feather, Screech, Dive, Gust
    Spawning/          # WaveSpawner, XPShell, Pickup
    UI/                # All screen controllers and panels
    Data/              # GameData, RunState models
  Scenes/              # BootScene, TitleScene, GameScene
Packages/              # Package manifest
ProjectSettings/       # Unity project settings
```

## Requirements

- [Unity 6](https://unity.com/) (2025 LTS)
- [horizOn Account](https://horizon.pm) (free tier works)
- [horizOn SDK for Unity](https://github.com/ProjectMakersDE/horizOn-SDK-Unity)

## Related Projects

- [horizOn-SDK-Unity](https://github.com/ProjectMakersDE/horizOn-SDK-Unity) — The SDK this example uses
- [horizOn-Example-Godot](https://github.com/ProjectMakersDE/horizOn-Example-Godot) — Same game in Godot
- [horizOn-Example-Unreal](https://github.com/ProjectMakersDE/horizOn-Example-Unreal) — Same game in Unreal Engine

## License

MIT
