# Unity refresh QA — 2026-10-02

The final source is tested with Unity 6000.5.0f1. The complete official UPM package is v1.9.0 at `ef9e80626133bb2b40928f2a5a7f6e25f47a63b8`; `git merge-base --is-ancestor 1a23308 v1.9.0` confirms that the published package contains the session-bound Cloud Save fix. All nine PNG SHA256 values match `ART-PROVENANCE.json`.

The native test project has no `Assets/Plugins/` secret config. The opt-in `-seagull-smoke` path skips SDK initialization completely and resets/uses only `seagull-storm.progress.qa.practice`. It never reads ordinary practice progress or a signed-in identity. Ordinary practice preferences were snapshotted and restored; SDK cached/session preferences were untouched. Exact QA progress keys were removed after the test players exited.

## Completed checks

| Check | Result |
|---|---|
| Full native import/compile | Passed in Unity 6000.5.0f1 |
| Final macOS Mono Development player | Passed; final keyboard UX build job 1279, exit 0 |
| Scene/prefab wiring validation | 159 checks passed; job 1227, exit 0 |
| Ticket-keyed evidence status fixture | Passed; preflight failure survives pending assignment, stale callbacks are ignored, completion survives, new run resets |
| Native player at 1280 × 720 | Passed; final focus/raycast job 1280, exit 0, 21.8 seconds |
| Native player at 960 × 600 | Passed; final focus/raycast job 1281, exit 0, 20.0 seconds |
| Native CUA keyboard review | Passed; title/hub/start/natural level-up/pause/quit/results/replay on job 1273; final selected styling on job 1283 |
| Asset provenance and diff whitespace | Passed |

The native smoke checks offline boot and loaded hub, SDK panel open/close, affordable and unaffordable permanent upgrades, start, paused timer freeze, shell XP collection, level-up selection, weapon unlocks, completion/reward persistence, duplicate-end protection, replay state reset, boss spawning and boss death/victory. Each resolution captures title, hub, SDK panel, pause, level-up, active run and results. It uses the real standalone renderer and game components, with accelerated/scripted state transitions. It does not prove three-minute survival balance or physical keyboard/mouse behavior.

Visual review fixed title overlap, aspect-ratio clipping, low-contrast HUD text, status/XP overlap, oversized level-up text and old pixel-beveled/navy menus. Final captures at both sizes show readable HUD plates, separated cards, flat charcoal/orange menus and current reward/best values. Screenshots live under ignored `Builds/qa-*.png`; the task retains both resolution sets separately.

After macOS was unlocked, native CUA review exposed a missing initial menu selection and an independent pointer-coordinate delivery limitation. A temporary opt-in probe was built from the exact original refresh source (`357bfa7`) in an isolated project without SDK config (build 1257, player 1260). Both application and EventSystem were focused. The Play Offline button was interactable and its center raycast hit the button first; there was no transparent graphic blocking it. Four actual input markers arrived: three keyboard `anyKeyDown` markers and one left down/up pair. The polled pointer stayed at `(1515, -421)`, outside the `1280 × 720` game window, across all 33 polls and all input markers. This establishes event arrival without a matching in-window pointer position; successful physical mouse control is not claimed. Probe/build logs and a precise evidence note are retained in the parent task's dated Unity report. The diagnostic source is absent from the delivered project.

The keyboard UX change assigns visible focus to Play Offline, ready Start Run, Resume, the first newly created level-up card and Retry. Hub readiness restores focus after its loading lock clears; subsequent user selection is retained. The SDK panel selects Top 10, excludes obscured hub controls from navigation and restores Start Run when ready, or the active SDK Demos button while loading, when closed. Hidden legacy canvases no longer contribute selectable controls. The native smoke checks each selected control and its unobstructed center raycast, separately from scripted activation. It also closes the SDK panel while Start is locked, verifies focus remains on an active home control and verifies Start receives focus when loading completes. Extra naturally earned level-ups are selected before requesting completion, so random enemy XP cannot leave that request in a paused choice state.

Actual native CUA keyboard review of the normal player (job 1273) confirms Return from title to hub, Return to start a run, a naturally reached level-up after about nine seconds with score 13, directional navigation to Magnet+, Return accepting it (score 53), and Escape opening pause with Resume selected. The timer stayed at 2:51 during approximately three minutes of pause. Arrow navigation reached News, Feedback and Quit Run; Return opened results showing score 53, wave 1, level 2, five coins and best 53. Return on Play Again started a fresh level-1 run with score zero, full HP and timer 2:59; Escape paused it. Brief key pulses and the centered camera did not establish a precise WASD movement measurement.

This test also exposed a legacy SpriteSwap selection graphic, which the final control styling replaces with ColorTint, an empty SpriteState and a cleared override sprite. The final focused smoke checks the selected control is interactable, uses no state sprite, has its focus outline enabled and is the first center raycast hit. The final normal player (build 1279, job 1283) was operated with Return from title to hub to run and Escape to pause; Resume rendered as flat orange with the cream selection outline. A stale second app instance, automatically relaunched by CUA after the earlier Quit, initially showed an old loaded SpriteSwap graphic. Read-only process inspection identified it by its earlier start time and missing QA launch flags. Only that task-owned older process was stopped, CUA was rebound to the actual new process, and the final styling passed live review. No further source or build change was required. The final player exited successfully; process inventory confirmed no owned native player remained. Only this task's native jobs were stopped; the foreign Unity editor remained untouched.

The first IL2CPP build attempt failed because that native build module is absent on the Mac. `SeagullBuild.Build` temporarily selects the installed Mono backend and restores the project setting after the build. The retained URP/profile assets include the tested Unity 6000.5 import migration.

## Cutout animation validation (2026-10-03)

The accepted shared gull/crab cutout assets replace the two gameplay creatures with seven and nine independently rendered parts. Full-canvas center anchors remain 512/16 PPU and 256/8 PPU, both 32 world units. Original colliders, movement, health and SDK flows remain on the actor. One SortingGroup preserves each rig's internal order. Transparent edges use bilinear/clamp FullRect imports. All sixteen delivered PNG hashes match `Cutout/rig.json`.

| Check | Result |
|---|---|
| Scene/prefab validation | All 159 checks passed in Unity 6000.5.0f1, pueue 1742 |
| Final blended macOS Mono Development player | Build succeeded, pueue 1744 |
| Native 1280 × 720 | Cutout and general gameplay smoke passed, pueue 1748, exit 0, 29.3 seconds |
| Native 960 × 600 | Cutout and general gameplay smoke passed, pueue 1749, exit 0, 23.9 seconds |
| Shared asset distribution/import settings | Sixteen hashes, 16/8 PPU, bilinear/clamp checked |

`[CUTOUT SMOKE] PASS` checks all parts and the disabled monolithic renderer/legacy Animator, actual movement changing feet/legs, correct art direction, actual feather and crab-contact attacks, moving feet during auto-fire, actual damage and whole-rig tint, frozen poses during pause and crab pool reuse without lingering attack/hurt state or duplicate visuals. These checks run in the real standalone player with the offline QA namespace and no SDK configuration. The general gameplay smoke also passes its existing menu, upgrades, level-up, completion and replay gates. Screenshots are in ignored `tmp/native1280/` and `tmp/native960/`; logs and the player are in ignored `Builds/`. A `-seagull-smoke-output <folder>` argument selects the capture folder.

The first native launch command failed before the process started because the pueue shell command did not preserve the executable path's spaces. The quoted replacement launch passed. Exact QA progress keys were restored and read back after both players exited. No task-owned editor/player remains. A separate TASK-909 editor and its import workers were left running. Local player checks do not add live-backend or physical-input evidence.

## Backend checks still required

A dedicated QA project is required to test real guest/email/Apple sessions, board list/top/around/rank, a positive and rejected validated run, evidence upload failure/success, cloud absence/read/write and expired sessions, catalog/avatar updates, localization strings, gift redemption, feedback/log/crash collection and template email status. No production data was written and no email was sent during this task.

The SDK panel uses real APIs, so an empty backend catalog, missing rules, missing templates and absent news are shown as setup/empty states. A successful offline smoke does not prove these backend roundtrips or the broader TASK-615 gate.
