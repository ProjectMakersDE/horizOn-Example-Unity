# Seagull Storm Unity — Issues

These issues were found during a design-doc audit. All of them are now **RESOLVED**; the notes below
describe what was actually done. The only remaining manual step is an in-editor play test
(see README, "Getting Started").

---

## General Issues (shared across all engines)

### G1: Google OAuth is a stub — RESOLVED
`HorizonManager` gained `SignInGoogle`/`SignUpGoogle` wrappers around the SDK's
`UserManager.SignInGoogle`/`SignUpGoogle`. `TitleScreenController.OnGoogleClicked()` now runs the
real SDK flow; on platforms without a Google Sign-In plugin, `GetGoogleAuthorizationCode()` throws
`PlatformNotSupportedException` and the catch shows a proper "requires platform-specific setup"
status instead of a hardcoded "not available" string (sanctioned by unity-plan §11). The Google
button is wired in TitleScene. Residual: the SDK call only becomes reachable once a Google Sign-In
plugin supplies an authorization code.

### G2: Pause Menu News not displayed — RESOLVED
`PauseController.OnNews()` uses `HubController.CachedNews` (or a fresh `LoadNews()`), clears
`newsContentContainer`, and instantiates `newsEntryPrefab` with `NewsEntry.Setup(title, releaseDate)`.
The pause canvas, news panel, content container, and the NewsEntry prefab reference are all wired in
GameScene now, so the items actually render.

### G3: Remote Config re-fetched on every hub visit — RESOLVED
`HorizonManager.LoadAllConfigs(useCache: true)` caches configs in `_cachedConfigs` for the session;
`HubController` passes `useCache: true`. Network fetch happens once per app session.

---

## Unity-Specific Issues

### U1: WeaponManager.InitializeDefaultWeapon() never called — RESOLVED
`GameManager.StartRun()` calls `WeaponManager.Instance?.ClearWeapons()` and
`InitializeDefaultWeapon(player)` before `ChangeState(GameState.Run)`. The serialized `player`
field is wired in GameScene, so the call no longer no-ops.

### U2: ApplyStatBoost missing move_speed and xp_magnet — RESOLVED
`LevelUpManager.ApplyStatBoost()` handles `max_hp`, `move_speed`
(`RunState.moveSpeedMultiplier += 0.1`) and `xp_magnet` (`RunState.pickupRadiusMultiplier += 0.25`);
`PlayerController` and `XPPickup` consume both multipliers.

### U3: Pause Menu News panel never populated — RESOLVED
Same fix as G2; the panel hierarchy exists and is wired in GameScene.

### U4: Remote Config useCache:false on every hub visit — RESOLVED
Same fix as G3 (`useCache: true` + session-level `_cachedConfigs` guard).

### U5: Score submit / getRank race condition — RESOLVED
`GameManager.EndRun()` awaits `SubmitScore` and then sets `ScoreSubmitted = true`;
`GameOverController.ShowStats()` awaits that flag before calling `GetRank()`. If submit throws, the
flag is still set and the rank display degrades to "Rank: --".

### U6: Feedback missing email and deviceInfo — RESOLVED
`HorizonManager.SubmitFeedback(title, msg, category, email)` forwards to
`FeedbackManager.Submit(..., includeDeviceInfo: true)` (deviceInfo auto-collected from
`SystemInfo`). Both FeedbackForm instances (hub + pause) have an optional email input wired.

### U7: GameBootstrap bypasses HorizonManager facade — RESOLVED
`GameBootstrap` only calls the facade (`HorizonManager.Instance.StartCrashCapture()` /
`RestoreSession()`), and creates a `[HorizonManager]` GameObject at runtime if none exists.
BootScene additionally contains a `[HorizonManager]` node, so the facade exists before the first
facade call even without the runtime fallback.

### U8: All Unity Prefabs missing — RESOLVED (Stage B)
11 text-YAML prefabs authored with stable GUIDs:
`Enemies/CrabEnemy|JellyfishEnemy|PirateEnemy|BossEnemy` (sprite, kinematic Rigidbody2D with full
kinematic contacts, trigger collider, Enemy tag/layer/sorting layer, per-type stats;
Jellyfish wired to `Pickups/PoisonZone`), `Weapons/FeatherProjectile`, `Pickups/XPShell`,
`Pickups/PoisonZone` (green-tinted trigger zone with a `PoisonZone` damage-on-stay behaviour,
5 HP per second), `UI/UpgradeSlot`, `UI/LeaderboardEntry`, `UI/NewsEntry`,
`UI/LevelUpChoiceCard` (root Button + exact-named `NameText`/`DescText` children). Colliders were
subsequently resized to the pixel-unit world scale (see U9 notes).

### U9: Scene wiring incomplete — RESOLVED (Stage A + C + D)
The scenes were rebuilt from scratch as text YAML and are fully wired:

- **Stage A (foundation):** deterministic `.meta` GUIDs for all 118 non-Plugins assets (grid-sliced
  sprite sheets, audio, font, scripts, scenes), `ProjectVersion.txt` pinned to 6000.5.0f1, package
  manifest fixed (TMP merged into ugui 2.x, Input System 1.19.0), the fabricated scene/script GUIDs
  in `BootScene`/`EditorBuildSettings` replaced with the real generated ones, and the one compile
  error fixed (`HubController` long->int cast).
- **Stage C (scenes):** `BootScene` = `[GameBootstrap]` + `[HorizonManager]`. `TitleScene` = camera
  (orthographic + URP Pixel Perfect), EventSystem, full auth UI (logo, title, name input, 5 auth
  buttons incl. Apple, hidden email panel, status text, horizon.pm footer) with all 11
  `TitleScreenController` fields wired, plus `[AudioManager]` with all 14 clips. `GameScene` =
  camera + `CameraFollow(target=Player)`, `[GameManager]` (5 canvases + player + tilemap),
  `[HorizonManager]`, `[SpawnManager]`, `[WeaponManager]`, `[LevelUpManager]`, `[AudioManager]`
  (14 clips), the three pools with prefab references, the Player (seagull sprite, kinematic RB2D,
  BoxCollider2D, `PlayerController`, tag `Player`), a `Ground` Grid + painted Tilemap, and all five
  canvases (Hub / RunHUD / Pause / LevelUp / GameOver) with every controller SerializeField wired —
  HubController 13, PauseController 8, GameOverController 8, RunHUDController 7, LevelUpController 3,
  FeedbackForm 6 (x2 instances), GiftCodeForm 3, SettingsPanel 2, HPBar/XPBar fill images.
- **Stage D (editor automation, `Assets/Editor/SeagullSetup.cs`):** generated the
  `PressStart2P SDF` TMP font asset (every TMP text references it), created and assigned a URP
  2D pipeline (`Assets/Settings/Renderer2D.asset` + `URP-2D.asset` bound to GraphicsSettings and
  all quality levels; both scene cameras carry URP `PixelPerfectCamera`, 480x270), painted the
  72x56-tile beach island (sand interior with sparse decor, water-edge ring, deep-water border per
  the tilemap region doc), and ships `SeagullSetup.ValidateSetup`, which asserts every
  SerializeField above is non-null — current result: **158 checks, 0 missing**.
- **World scale:** the game code works in pixel units (speeds 40–400, radii 50–120), so the
  gameplay sprite sheets import at 1 pixel-per-unit and prefab colliders were sized accordingly
  (28x28 enemies, 56x56 boss, r=8 projectile/XP, r=16 poison zone). `ui.png` stays at PPU 16 with
  matching `CanvasScaler.referencePixelsPerUnit = 16`.

Re-run the validation anytime:

```
Unity -batchmode -nographics -projectPath . -executeMethod SeagullSetup.ValidateSetup -logFile -
```

---

## Remaining manual step

- In-editor play test (press Play, run through auth -> hub -> run -> level up -> game over) with a
  real horizOn API key imported via **Window > horizOn > Config Importer**.
