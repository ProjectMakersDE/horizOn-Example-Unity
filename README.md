# horizOn Example — Unity

**Seagull Storm** is a compact survival game and a runnable example of the complete horizOn Unity SDK. Move around the island, collect shells, choose one of three level-up upgrades, and defeat the octopus after three minutes. Coins buy permanent upgrades between runs. Runs, upgrades and personal bests persist locally without requiring an account.

The shared square-headed seagull and island artwork use the horizOn charcoal, cream, orange and coastal teal palette. Readable menus replace the old pixel-font title and hub. Contact damage has a short grace period, the island has movement bounds, wave sizes are capped, and victory, quitting, retrying and finishing a run use a single guarded lifecycle.

## Open and play

1. Clone the repository and open it in **Unity 6000.6.3f1**.
2. Open `Assets/Scenes/BootScene.unity` and press Play.
3. Choose **PLAY OFFLINE** for a fully playable practice game, or sign in for online features.

**Controls:** WASD / arrow keys move; attacks fire automatically; collect shells to gain XP; Escape pauses. In menus, arrow keys navigate and Return activates the outlined control; level-up cards also support mouse selection. Play Offline, Start Run, Resume, the first level-up choice and Retry receive initial keyboard focus. The SDK panel keeps navigation within its visible controls and returns focus to Start Run when ready, or SDK Demos while loading, when closed. The hub offers Speed, Damage, Health and Magnet upgrades.

The title loads immediately. If a secret SDK config exists, the connection is prepared in the background. Practice mode stays separate from cached signed-in sessions, remote balancing, cloud saves and leaderboard writes. Its progress uses a separate device-local key.

## Exact SDK dependency

The complete official Unity package is installed through Unity Package Manager:

```text
com.projectmakers.horizon
https://github.com/ProjectMakersDE/horizOn-SDK-Unity.git#c8af809820199fb0cf561d2ee4834a498c7b0810
resolved commit c8af809820199fb0cf561d2ee4834a498c7b0810
```

Pinned to the verified Develop commit for TASK-1004, including Player Values, Statistics and Realtime. This is a development integration, not a released SDK version. `Packages/packages-lock.json` records the exact official package hash. The example targets the installed Unity 6000.6.3f1 and its resolved package/import versions.

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
| Player Values | Explicit state/offers read, selected value/offer, purchase or permitted +1 adjustment, durable replay of the same pending action |
| Statistics | Completed online-run batch, explicit snapshot/report, exact original-ID retry and queued later runs |
| Localization | EN/DE backend catalog; `ui.play` changes the hub Start Run label when supplied |
| Email Sending | Explicit recipient user ID and template slug; **SEND EMAIL** queues a real configured template |

Email Sending never runs automatically. Local QA does not sign in, redeem gifts, send feedback/email or write any production API data.

The pinned leaderboard API supports board selection, Top, Around and Rank; it has no page-number/date-filter query API. These examples use the capabilities actually shipped by the package.

## Player values demo

Open **SDK DEMOS > Player values** after signing in. **Read balances** loads state and offers explicitly. **Next value** and **Next offer** select server-configured entries; **Buy offer** uses the server's costs/rewards. **Adjust +1** requests one unit of the selected value only when the studio has enabled and bounded its adjustment rule. Configure definitions and offers in the Dashboard first. No timer, automatic purchase or automatic grant runs.

The displayed backend balances are separate from the game's practice/upgrade coins. The example never adds returned rewards to local progress. It displays the current server state, including balances returned with a business failure. The studio can enable validated-run value rewards using the existing configuration described below; this is not a migration of previous local coins.

One immutable action is saved in PlayerPrefs before HTTP, scoped by a SHA256 of the actual project API key and player ID; credentials are never stored in the pending record. Regional host changes keep the same pending action. Legacy host-scoped records are migrated from the active and configured hosts; conflicting copies remain protected for reconciliation. Keep previously used hosts in the configuration during migration. A restart or lost response uses **Retry pending** with the same operation ID and payload. An unresolved or unreadable intent blocks another action. `OPERATION_EXPIRED` requires explicit studio/player reconciliation; the example does not replace its ID. Sign-out, project or regional host changes and closed/reopened panels reject late UI callbacks. Leaving the panel may keep a confirmed-but-unobserved intent, which safely replays the same server receipt next time.

Before a new purchase, the example reloads offers. Changed costs, rewards or catalog versions require a fresh explicit confirmation; retrying a pending purchase preserves the original key and operation ID. A persisted send-series counter and SDK transport events distinguish the first single HTTP attempt from retries. Only a first-attempt `ADJUST_NOT_ALLOWED` or `INSUFFICIENT_BALANCE` with authoritative server state releases the slot for another explicit action. Older, restarted, internally retried or ambiguous attempts stay protected. The offer refresh does not create a server-side price quote: a concurrent studio edit after the refresh is governed by the normal purchase API.

## Statistics demo and Hello

Configure `rounds` (`sum`), `kills` (`sum`) and `best_score` (`max`) in the Dashboard, for example with the statistics presets. Choose the UTC reset rules and report bounds your game needs. Open **SDK DEMOS > Statistics** after signing in. Every completed online run queues one round, its kills and score locally. **Read snapshot** loads the server clock, definitions, values, `hasValue` and UTC periods. **Report completed runs** sends one batch; no request runs per kill, when a run starts, or just because this panel opens. Offline practice does not queue or send online metrics.

The local record has a bounded shape: one immutable pending request and one aggregate of later completed runs. A SHA256 scope binds it to the actual project key and player without storing credentials; regional host changes preserve that record. The original operation ID and full batch are saved before HTTP and survive restarts. A lost response, rate limit, catalog change or `STATE_CONFLICT` keeps the original request; **Retry original batch** sends it unchanged. A confirmed replay replaces the displayed snapshot and clears only that exact request. Later queued runs remain intact. `OPERATION_EXPIRED` or unreadable local storage requires reconciliation; no automatic replacement ID is created. Statistics never alter spendable balances, local coins or Cloud Save.

For the standalone Hello variant, open `Assets/Scenes/StatisticsHello.unity` and press Play. Import your own SDK config first. **Connect and restore existing identity** preserves a cached guest after restore failure. Creating a new guest is a separate explicit action; an ambiguous signup remains protected across restarts. Queue the simulated three-kill, score-250 round, read the snapshot, send once and retry the original batch. The Hello example has its own local intent namespace. All network actions are explicit buttons.

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

`Assets/Resources/SeagullArt/Cutout/` contains the accepted gull and crab animation parts, shared contact shadow and `rig.json` provenance. The gull head is clipped from the shared hero image. The moving torso, wings, feet, carapace, legs and claws are complete rebuilt vector surfaces exported to RGBA. `SeagullCutoutRig` reproduces the mockup's pivots and idle/walk/attack/hurt poses while keeping the original centered 32-unit sprite canvases and physics. A SortingGroup keeps each actor's parts together. Gameplay movement, weapon fire, contact attacks and damage drive the poses. Moving feet/legs and hop continue during upper-body attack poses. Overlapping auto-fire keeps an existing attack pulse running instead of restarting it. Pause freezes poses, and pooled crabs reset their effects on reuse. The original gull/crab renderer and legacy Animator are disabled once all parts load.

## Local validation

```bash
Unity -batchmode -nographics -projectPath . -executeMethod SeagullSetup.ValidateSetup -logFile -
Unity -batchmode -nographics -projectPath . -executeMethod SeagullBuild.Build -logFile -
"Builds/Seagull Storm.app/Contents/MacOS/Seagull Storm" -seagull-smoke -screen-width 1280 -screen-height 720
```

`SeagullBuild.Build` creates a Development macOS player using the installed Mono backend and restores the project's configured backend afterward. IL2CPP is not installed on the test Mac. The opt-in smoke player forces practice, uses a separate QA progress namespace, captures title/hub/SDK panel/pause/level-up/run/game-over screenshots in an ignored `tmp/` folder, and checks meta-upgrade affordability, pause, XP/level-up, weapons, completion, duplicate-end protection, replay reset and boss victory. It never connects a backend.

The same native smoke checks the seven gull and nine crab joints, movement and facing, actual feather/contact attack poses, damage tint across parts, pause and pool reuse. `[CUTOUT SMOKE] PASS` confirms those runtime checks separately from the general gameplay smoke.

Native QA results and remaining integration checks are recorded in `QA.md`. Real session authentication, validated rejection/evidence, cloud roundtrips, cosmetics, localization templates and email delivery require a dedicated project configuration and are not asserted from offline checks. This work does not close the broader TASK-615 integration gate.

## License

[MIT](LICENSE). The bundled Press Start 2P font remains under the SIL Open Font License; see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md). Shared generated art is documented in `ART-PROVENANCE.json`.
