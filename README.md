# horizOn Example — Unity

**Seagull Storm** is a compact survival game and a runnable example of the complete horizOn Unity SDK. Move around the island, collect shells, choose one of three level-up upgrades, and defeat the octopus after three minutes. Coins buy permanent upgrades between runs. Runs, upgrades and personal bests persist locally without requiring an account.

The shared square-headed seagull and island artwork use the horizOn charcoal, cream, orange and coastal teal palette. Readable menus replace the old pixel-font title and hub. Contact damage has a short grace period, the island has movement bounds, wave sizes are capped, and victory, quitting, retrying and finishing a run use a single guarded lifecycle.

## Open and play

1. Clone the repository and open it in **Unity 6000.5.0f1**.
2. Open `Assets/Scenes/BootScene.unity` and press Play.
3. Choose **PLAY OFFLINE** for a fully playable practice game, or sign in for online features.

**Controls:** WASD / arrow keys move; attacks fire automatically; collect shells to gain XP; click a level-up card; Escape pauses. The hub offers Speed, Damage, Health and Magnet upgrades.

The title loads immediately. If a secret SDK config exists, the connection is prepared in the background. Practice mode stays separate from cached signed-in sessions, remote balancing, cloud saves and leaderboard writes. Its progress uses a separate device-local key.

## Exact SDK dependency

The complete official Unity package is installed through Unity Package Manager:

```text
com.projectmakers.horizon
https://github.com/ProjectMakersDE/horizOn-SDK-Unity.git#v1.9.0
resolved commit ef9e80626133bb2b40928f2a5a7f6e25f47a63b8
```

Verified against the published tag on 2026-10-02. This release contains Validated Actions, Player Profile, leaderboard board metadata and the session-bound Cloud Save fix from `1a23308`. No example-only SDK fork or unpublished package is required. `Packages/packages-lock.json` records the resolved official package hash.

## Connect your own project

Create a project and download its config JSON from [horizon.pm](https://horizon.pm). In Unity use **Window > horizOn > Config Importer**. The personal config is stored in `Assets/Plugins/ProjectMakers/horizOn/CloudSDK/Resources/horizOn/HorizonConfig.asset`; the entire personal `Assets/Plugins/` folder is ignored by Git.

Guest, email sign-in/registration, Apple sign-in and session restore are on the title. Apple requires platform OAuth configuration. Google requires a platform-specific authorization-code plugin; it is not falsely presented as a working standalone sign-in button.

## SDK coverage

Choose **SDK DEMOS** in the hub. Operations show loaded data, empty states or failures. Optional backend configuration never blocks offline practice.

| SDK manager | Example flow |
|---|---|
| User | Guest, email, Apple and explicit session restore; sign-out |
| Leaderboard | List and switch boards, Top 10, Around me, own rank, `validatedOnly` metadata and profile-aware rows |
| Cloud Save | Session-bound load and save of signed-in progress; explicit save/readback demo |
| Remote Config | Load all game balancing and validation settings when signed in |
| News | Published in-project news through the demo panel and pause menu |
| Gift Code | Validate then redeem, apply local reward, persist progress |
| Feedback | Explicit user-written in-game feedback |
| User Log | Run summaries and a repeated wave-one-death balancing warning |
| Crash | Signed-in capture, breadcrumbs, state keys and caught exceptions |
| Validated Actions | Board-bound run ticket, seed, input evidence, validated submit, accepted/rejected results, evidence upload state, server-owned state read |
| Player Profile | Read profile/catalog, cycle available avatars, explicitly apply avatar while retaining frame and badges |
| Localization | EN/DE backend catalog; `ui.play` changes the hub Start Run label when supplied |
| Email Sending | Explicit recipient user ID and template slug; **SEND EMAIL** queues a real configured template |

Email Sending never runs automatically. Local QA does not sign in, redeem gifts, send feedback/email or write any production API data.

The pinned leaderboard API supports board selection, Top, Around and Rank; it has no page-number/date-filter query API. These examples use the capabilities actually shipped by the package.

## Cloud progress boundaries

Session generations and captured owners reject stale auth, config, gift and cloud callbacks. Authentication and whole-save writes are serialized. Upgrade purchases wait for progress loading. A failed or ambiguous cloud load cannot replace an existing save. The SDK's `Load()` returns null for both missing saves and failures; the facade uses a typed read of the same existing endpoint to confirm `found:false` before permitting a first write. Automatic cloud writes require either a loaded save or confirmed absence. Local progress remains available after failures.

A run is frozen and rewards are applied once before network work begins. New runs, upgrade purchases and sign-out stay blocked while score, save and log completion are in progress. Returning to the hub preserves the current session's progress rather than reloading defaults.

## Validated Actions

Set these Remote Config keys in your dashboard:

| Key | Default | Purpose |
|---|---|---|
| `validated_runs_enabled` | `false` | Request validated tickets for runs |
| `validated_runs_board` | `default` | Initial board; SDK demo board selection changes the active board |
| `validated_runs_send_coins` | `false` | Also submit earned `coins` when that value exists in your rules |

Configure Validated Actions rules and the chosen leaderboard in the dashboard. Selecting a validated-only board enables validated runs. The run sends score, reached stage `wave_<n>` and the compact input log. If no ticket can be issued, the player gets a practice run and **no normal score fallback**. A rejection never writes a plain leaderboard entry. Accepted results show the exact server-returned score and rank; evidence upload pending/failure is independent of score acceptance.

The log is review evidence, **not authoritative gameplay replay verification**. Unity physics/frame timing is not claimed deterministic. The bounded format remains shared with the other examples: version byte `1`, little-endian server seed, then tick delta (u16) and event code (u8); movement `0x00–0x0F`, choice `0x10–0x1F`, run end `0xFF`. Unity uses 50 physics ticks/second at its default timestep. The maximum is 32 KiB; exceeding it refuses submission and discards the ticket instead of ranking incomplete evidence.

Default survival time is 180 seconds, wave interval 15 seconds, first-wave count 5, growth 1.3 (capped to 60 per wave). Upgrade costs are `[10,25,60,100]` for speed/damage, `[15,40,80]` for health, `[20,50]` for magnet. Remote keys follow the existing `enemy_<type>_<stat>`, `weapon_<type>_<stat>`, `upgrade_<type>_costs/values` and `levelup_pool/choices` patterns in `Assets/Scripts/Core/GameConfig.cs`.

## Shared art and provenance

`Assets/Resources/SeagullArt/` contains nine shared PNG assets. Sprites have real RGBA transparency. The requested `google/gemini-2.5-flash-image` model was called through OpenRouter; its magenta backgrounds were keyed, cropped and padded for alpha. `ART-PROVENANCE.json` records model, prompts, processing, dimensions and final file hashes. The old atlases remain fallback assets. No API key is bundled.

## Local validation

```bash
Unity -batchmode -nographics -projectPath . -executeMethod SeagullSetup.ValidateSetup -logFile -
Unity -batchmode -nographics -projectPath . -executeMethod SeagullBuild.Build -logFile -
"Builds/Seagull Storm.app/Contents/MacOS/Seagull Storm" -seagull-smoke -screen-width 1280 -screen-height 720
```

`SeagullBuild.Build` creates a Development macOS player using the installed Mono backend and restores the project's configured backend afterward. IL2CPP is not installed on the test Mac. The opt-in smoke player forces practice, uses a separate QA progress namespace, captures title/hub/SDK panel/pause/level-up/run/game-over screenshots under ignored `Builds/`, and checks meta-upgrade affordability, pause, XP/level-up, weapons, completion, duplicate-end protection, replay reset and boss victory. It never connects a backend.

Native QA results and remaining integration checks are recorded in `QA.md`. Real session authentication, validated rejection/evidence, cloud roundtrips, cosmetics, localization templates and email delivery require a dedicated project configuration and are not asserted from offline checks. This work does not close the broader TASK-615 integration gate.

## License

[MIT](LICENSE). The bundled Press Start 2P font remains under the SIL Open Font License; see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md). Shared generated art is documented in `ART-PROVENANCE.json`.
