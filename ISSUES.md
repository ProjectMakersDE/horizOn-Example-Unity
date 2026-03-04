# Seagull Storm Unity — Open Issues

These issues were found during a design-doc audit and must be fixed before the project is considered complete.

---

## General Issues (shared across all engines)

### G1: Google OAuth is a stub
**Expected:** Functional Google OAuth flow using `UserManager.Instance.SignUpGoogle()` / `SignInGoogle()`.
**Actual:** `OnGoogleClicked()` shows "Google Sign-In is not available in this example." `HorizonManager` has no Google methods.
**Acceptance:** Add `SignInGoogle` and `SignUpGoogle` wrapper methods to `HorizonManager`. The Google button must trigger the SDK's Google OAuth methods. If OAuth is not available on the platform, show a proper error (not a hardcoded string).

### G2: Pause Menu News not displayed
**Expected:** News data shown in pause menu (title + date for each item).
**Actual:** `PauseController.OnNews()` calls `LoadNews()` but discards the result. The news panel shows as empty.
**Acceptance:** The pause menu news panel must display the loaded news items. Either cache the hub-loaded news and display it, or populate the panel from the fresh fetch. Items must show title and date.

### G3: Remote Config re-fetched on every hub visit
**Expected:** `getAllConfigs()` called once per app session with `useCache: true`.
**Actual:** `HubController.OnEnable()` calls `LoadAllConfigs(useCache: false)` every time the hub canvas activates.
**Acceptance:** Add a guard so Remote Config is only loaded once per app session. On subsequent hub visits, use the cached config. Change `useCache: false` to `true` or check if config is already loaded.

---

## Unity-Specific Issues

### U1: WeaponManager.InitializeDefaultWeapon() never called [HIGH]
**Expected:** Player starts each run with FeatherThrow weapon active.
**Actual:** `GameManager.StartRun()` does not call `WeaponManager.Instance.InitializeDefaultWeapon(player)`. The method exists but is never invoked.
**Acceptance:** `StartRun()` must call `InitializeDefaultWeapon()` before `ChangeState(Run)`. The player must have FeatherThrow active at run start regardless of scene/prefab setup.

### U2: ApplyStatBoost missing move_speed and xp_magnet cases [HIGH]
**Expected:** Levelup choices for move_speed and xp_magnet have gameplay effects.
**Actual:** `LevelUpManager.ApplyStatBoost()` only handles `"max_hp"`. Selecting move_speed or xp_magnet does nothing.
**Acceptance:** Add cases for `"move_speed"` (increase RunState movement speed multiplier) and `"xp_magnet"` (increase RunState pickup radius). Both must have visible gameplay effect.

### U3: Pause Menu News panel never populated [MEDIUM]
**Expected:** News items displayed in the pause menu news panel.
**Actual:** `PauseController.OnNews()` fetches news but never populates the panel content.
**Acceptance:** The news panel in the pause menu must show news entries with title and date, either from a fresh fetch or from cached hub data.

### U4: Remote Config useCache:false on every hub visit [MEDIUM]
**Expected:** Config loaded once, cached for session.
**Actual:** `HubController.OnEnable()` line 52 uses `useCache: false`.
**Acceptance:** Change to only load once per session. Guard with a null check on cached config.

### U5: Score submit / getRank race condition [LOW]
**Expected:** Score is submitted before rank is queried.
**Actual:** `EndRun()` fires submitScore and `GameOverController.ShowStats()` calls getRank without waiting for submission to complete.
**Acceptance:** Ensure `submitScore` completes before `getRank` is called. Either await submitScore in EndRun before changing state, or have ShowStats await a completion signal.

### U6: Feedback missing email and deviceInfo parameters [LOW]
**Expected:** `submit(title, message, category, email, deviceInfo)`.
**Actual:** `HorizonManager.SubmitFeedback(title, msg, category)` — no email or deviceInfo.
**Acceptance:** Pass email (from an optional field in the form) and deviceInfo (auto-collected via `SystemInfo`) to the feedback SDK call.

### U7: GameBootstrap bypasses HorizonManager facade [LOW]
**Expected:** All SDK calls go through HorizonManager.
**Actual:** `GameBootstrap.cs` calls `CrashManager.Instance.StartCapture()` directly.
**Acceptance:** Replace with `HorizonManager.Instance.StartCrashCapture()`. Ensure HorizonManager is initialized before GameBootstrap runs.

### U8: All Unity Prefabs missing [CRITICAL]
**Expected:** Prefab files for enemies, projectiles, pickups, UI elements.
**Actual:** Zero `.prefab` files exist in the project.
**Acceptance:** Create all required prefabs: CrabEnemy, JellyfishEnemy, PirateEnemy, BossEnemy, Projectile, XPPickup, UpgradeSlot, LeaderboardEntry, NewsEntry, LevelUpChoiceCard. Each must have correct components (Rigidbody2D, Collider2D, SpriteRenderer, scripts).

### U9: Scene wiring incomplete [CRITICAL]
**Expected:** All SerializeField references assigned in scenes.
**Actual:** Scenes exist but Inspector references are not wired (cannot be done from code alone).
**Acceptance:** All SerializeField references in all MonoBehaviours must be assigned in the Unity Editor. This includes: GameManager canvas references, AudioManager clip references, all UI controller references, pool prefab references.
