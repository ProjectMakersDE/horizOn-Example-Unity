# Unity refresh QA — 2026-10-02

The final source is tested with Unity 6000.5.0f1. The complete official UPM package is v1.9.0 at `ef9e80626133bb2b40928f2a5a7f6e25f47a63b8`; `git merge-base --is-ancestor 1a23308 v1.9.0` confirms that the published package contains the session-bound Cloud Save fix. All nine PNG SHA256 values match `ART-PROVENANCE.json`.

The native test project has no `Assets/Plugins/` secret config. The opt-in `-seagull-smoke` path skips SDK initialization completely and resets/uses only `seagull-storm.progress.qa.practice`. It never reads ordinary practice progress or a signed-in identity. Ordinary practice preferences were snapshotted and restored; SDK cached/session preferences were untouched. Exact QA progress keys were removed after the test players exited.

## Completed checks

| Check | Result |
|---|---|
| Full native import/compile | Passed in Unity 6000.5.0f1 |
| Final macOS Mono Development player | Passed; pueue job 1241, exit 0 |
| Scene/prefab wiring validation | 159 checks passed; job 1227, exit 0 |
| Ticket-keyed evidence status fixture | Passed; preflight failure survives pending assignment, stale callbacks are ignored, completion survives, new run resets |
| Native player at 1280 × 720 | Passed; job 1244, exit 0, 20.5 seconds |
| Native player at 960 × 600 | Passed; job 1242, exit 0, 22.4 seconds |
| Asset provenance and diff whitespace | Passed |

The native smoke checks offline boot and loaded hub, SDK panel open/close, affordable and unaffordable permanent upgrades, start, paused timer freeze, shell XP collection, level-up selection, weapon unlocks, completion/reward persistence, duplicate-end protection, replay state reset, boss spawning and boss death/victory. Each resolution captures title, hub, SDK panel, pause, level-up, active run and results. It uses the real standalone renderer and game components, with accelerated/scripted state transitions. It does not prove three-minute survival balance or physical keyboard/mouse behavior.

Visual review fixed title overlap, aspect-ratio clipping, low-contrast HUD text, status/XP overlap, oversized level-up text and old pixel-beveled/navy menus. Final captures at both sizes show readable HUD plates, separated cards, flat charcoal/orange menus and current reward/best values. Screenshots live under ignored `Builds/qa-*.png`; the task retains both resolution sets separately.

The normal standalone player was launched for manual CUA review, but macOS was locked and the computer-use tool could not unlock it. That player was stopped without claiming a manual-input pass. Only this task's native jobs were stopped; the foreign Unity editor remained untouched.

The first IL2CPP build attempt failed because that native build module is absent on the Mac. `SeagullBuild.Build` temporarily selects the installed Mono backend and restores the project setting after the build. The retained URP/profile assets include the tested Unity 6000.5 import migration.

## Backend checks still required

A dedicated QA project is required to test real guest/email/Apple sessions, board list/top/around/rank, a positive and rejected validated run, evidence upload failure/success, cloud absence/read/write and expired sessions, catalog/avatar updates, localization strings, gift redemption, feedback/log/crash collection and template email status. No production data was written and no email was sent during this task.

The SDK panel uses real APIs, so an empty backend catalog, missing rules, missing templates and absent news are shown as setup/empty states. A successful offline smoke does not prove these backend roundtrips or the broader TASK-615 gate.
