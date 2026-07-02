using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.Tilemaps;
using SeagullStorm;
using Object = UnityEngine.Object;

// Global namespace on purpose so -executeMethod SeagullSetup.<Method> works verbatim.
    /// <summary>
    /// Batch-mode project setup and validation for the Seagull Storm example.
    ///
    /// Usage (from the repo root):
    ///   Unity -batchmode -nographics -quit -projectPath . -executeMethod SeagullSetup.ImportEssentials
    ///   Unity -batchmode -nographics -quit -projectPath . -executeMethod SeagullSetup.Bootstrap
    ///   Unity -batchmode -nographics -quit -projectPath . -executeMethod SeagullSetup.ValidateSetup
    ///
    /// ImportEssentials: imports the TMP Essential Resources (TMP Settings + default style sheet).
    /// Bootstrap: generates the Press Start 2P TMP font asset, creates + assigns the URP 2D
    ///            pipeline assets, and paints the GameScene beach-island tilemap.
    /// ValidateSetup: loads every scene/prefab and asserts each SerializeField that the game
    ///            code depends on is wired; exits 0 on success, 1 when anything is missing.
    /// </summary>
    public static class SeagullSetup
    {
        private const string FontAssetPath = "Assets/Art/Fonts/PressStart2P SDF.asset";
        private const string TtfPath = "Assets/Art/Fonts/PressStart2P-Regular.ttf";
        private const string RendererDataPath = "Assets/Settings/Renderer2D.asset";
        private const string PipelinePath = "Assets/Settings/URP-2D.asset";
        private const string TilemapPngPath = "Assets/Art/Sprites/tilemap.png";
        private const string TilesFolder = "Assets/Art/Tiles";
        private const string BootScenePath = "Assets/Scenes/BootScene.unity";
        private const string TitleScenePath = "Assets/Scenes/TitleScene.unity";
        private const string GameScenePath = "Assets/Scenes/GameScene.unity";

        // ------------------------------------------------------------------ step 1
        public static void ImportEssentials()
        {
            if (TMP_Settings.instance != null)
            {
                Debug.Log("[SETUP] TMP Essential Resources already imported.");
                return;
            }

            string package = Path.GetFullPath(
                "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
            Debug.Log($"[SETUP] Importing TMP Essential Resources from {package}");
            AssetDatabase.ImportPackage(package, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[SETUP] TMP Essential Resources import requested.");
        }

        // ------------------------------------------------------------------ step 2
        public static void Bootstrap()
        {
            try
            {
                GenerateFontAsset();
                SetupUrp();
                PaintTilemap();
                AssetDatabase.SaveAssets();
                Debug.Log("[SETUP] Bootstrap complete.");
                EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SETUP] Bootstrap failed: {ex}");
                EditorApplication.Exit(1);
            }
        }

        private static void GenerateFontAsset()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(TtfPath);
            if (font == null) throw new Exception($"Source font not found at {TtfPath}");

            var dst = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (dst != null && dst.sourceFontFile != null && dst.material != null)
            {
                Debug.Log("[SETUP] TMP font asset already generated.");
                return;
            }

            var generated = TMP_FontAsset.CreateFontAsset(
                font, 32, 4, GlyphRenderMode.SDFAA, 256, 256, AtlasPopulationMode.Dynamic, true);
            if (generated == null) throw new Exception("TMP_FontAsset.CreateFontAsset returned null");

            if (dst == null)
            {
                // No placeholder on disk: create fresh (GUID will differ from the promised one).
                AssetDatabase.CreateAsset(generated, FontAssetPath);
                generated.material.name = "PressStart2P SDF Material";
                generated.atlasTextures[0].name = "PressStart2P SDF Atlas";
                AssetDatabase.AddObjectToAsset(generated.material, generated);
                AssetDatabase.AddObjectToAsset(generated.atlasTextures[0], generated);
                dst = generated;
            }
            else
            {
                // Keep the placeholder's GUID (the scenes/prefabs reference it):
                // parent atlas + material to the existing asset, then copy the data over.
                var tex = generated.atlasTextures[0];
                tex.name = "PressStart2P SDF Atlas";
                var mat = generated.material;
                mat.name = "PressStart2P SDF Material";
                AssetDatabase.AddObjectToAsset(tex, FontAssetPath);
                AssetDatabase.AddObjectToAsset(mat, FontAssetPath);
                EditorUtility.CopySerialized(generated, dst);
                dst.name = "PressStart2P SDF";
                Object.DestroyImmediate(generated);
            }

            EditorUtility.SetDirty(dst);

            // Make it the project-wide TMP default as well.
            var settings = TMP_Settings.instance;
            if (settings != null)
            {
                var so = new SerializedObject(settings);
                var prop = so.FindProperty("m_defaultFontAsset");
                if (prop != null)
                {
                    prop.objectReferenceValue = dst;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(settings);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[SETUP] Generated TMP font asset at {FontAssetPath} " +
                      $"(guid {AssetDatabase.AssetPathToGUID(FontAssetPath)})");
        }

        private static void SetupUrp()
        {
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                var rendererData = ScriptableObject.CreateInstance<Renderer2DData>();
                AssetDatabase.CreateAsset(rendererData, RendererDataPath);
                ResourceReloader.ReloadAllNullIn(rendererData, "Packages/com.unity.render-pipelines.universal");
                EditorUtility.SetDirty(rendererData);

                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            int activeLevel = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }
            QualitySettings.SetQualityLevel(activeLevel, false);

            AssetDatabase.SaveAssets();
            Debug.Log($"[SETUP] URP 2D pipeline assigned ({PipelinePath}).");
        }

        private static void PaintTilemap()
        {
            var sprites = AssetDatabase.LoadAllAssetsAtPath(TilemapPngPath)
                .OfType<Sprite>()
                .ToDictionary(s => s.name, s => s);
            if (sprites.Count == 0) throw new Exception($"No sliced sprites found in {TilemapPngPath}");

            var tileCache = new Dictionary<string, Tile>();
            Tile GetTile(string name)
            {
                if (tileCache.TryGetValue(name, out var cached)) return cached;
                string path = $"{TilesFolder}/{name}.asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (tile == null)
                {
                    if (!sprites.TryGetValue(name, out var sprite))
                        throw new Exception($"tilemap.png has no sprite named {name}");
                    tile = ScriptableObject.CreateInstance<Tile>();
                    tile.sprite = sprite;
                    tile.colliderType = Tile.ColliderType.None;
                    AssetDatabase.CreateAsset(tile, path);
                }
                tileCache[name] = tile;
                return tile;
            }

            var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var tilemap = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault();
            if (tilemap == null) throw new Exception("GameScene has no Tilemap component");

            tilemap.ClearAllTiles();

            // Beach island per docs/plans/2026-07-02-seagull-storm-tilemap-ui-regions.md:
            // 2 rings of deep water, then the water-edge ring, sand interior with sparse decor.
            var rng = new System.Random(42);
            string[] deepWater = { "deep_water_v1", "deep_water_v2", "deep_water_v3", "deep_water_v4" };
            string[] sandPlain = { "sand_v1", "sand_v1", "sand_v1", "sand_v2", "sand_v3", "sand_v4" };
            string[] sandDecor = { "sand_shell", "sand_starfish", "sand_seaweed", "sand_rock" };

            const int xMin = -36, xMax = 36, yMin = -28, yMax = 28; // 72x56 tiles = 1152x896 px
            for (int x = xMin; x < xMax; x++)
            {
                for (int y = yMin; y < yMax; y++)
                {
                    int d = Mathf.Min(Mathf.Min(x - xMin, xMax - 1 - x), Mathf.Min(y - yMin, yMax - 1 - y));
                    string name;
                    if (d <= 1)
                    {
                        name = deepWater[rng.Next(deepWater.Length)];
                    }
                    else if (d == 2)
                    {
                        bool n = y == yMax - 3, s = y == yMin + 2, w = x == xMin + 2, e = x == xMax - 3;
                        if (n && w) name = "water_corner_nw";
                        else if (n && e) name = "water_corner_ne";
                        else if (s && w) name = "water_corner_sw";
                        else if (s && e) name = "water_corner_se";
                        else if (n) name = "water_edge_n";
                        else if (s) name = "water_edge_s";
                        else if (w) name = "water_edge_w";
                        else name = "water_edge_e";
                    }
                    else
                    {
                        name = rng.NextDouble() < 0.03
                            ? sandDecor[rng.Next(sandDecor.Length)]
                            : sandPlain[rng.Next(sandPlain.Length)];
                    }
                    tilemap.SetTile(new Vector3Int(x, y, 0), GetTile(name));
                }
            }

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[SETUP] Painted island tilemap ({(xMax - xMin) * (yMax - yMin)} tiles).");
        }

        // ------------------------------------------------------------------ step 3
        public static void ValidateSetup()
        {
            int missing = 0;

            void Check(bool ok, string what)
            {
                if (ok) Debug.Log($"[VALIDATE] OK {what}");
                else { missing++; Debug.LogError($"[VALIDATE] MISSING {what}"); }
            }

            void CheckFields(string sceneName, Component comp, params string[] fields)
            {
                var so = new SerializedObject(comp);
                foreach (var field in fields)
                {
                    var prop = so.FindProperty(field);
                    bool ok = prop != null && prop.objectReferenceValue != null;
                    Check(ok, $"{sceneName} {comp.GetType().Name}.{field}");
                }
            }

            T[] FindAll<T>() where T : Component =>
                Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            T FindOne<T>(string sceneName) where T : Component
            {
                var all = FindAll<T>();
                Check(all.Length > 0, $"{sceneName} component {typeof(T).Name}");
                return all.FirstOrDefault();
            }

            // ---------- BootScene
            EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);
            FindOne<GameBootstrap>("BootScene");
            FindOne<HorizonManager>("BootScene");

            // ---------- TitleScene
            EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
            var title = FindOne<TitleScreenController>("TitleScene");
            if (title != null)
                CheckFields("TitleScene", title, "nameInput", "emailInput", "passwordInput",
                    "guestButton", "googleButton", "appleButton", "emailSignInButton",
                    "createAccountButton", "titlePanel", "emailPanel", "statusText");
            var titleAudio = FindOne<AudioManager>("TitleScene");
            string[] audioFields =
            {
                "musicMenu", "musicBattle", "musicBoss", "sfxFeather", "sfxScreech", "sfxDive",
                "sfxGust", "sfxPlayerHit", "sfxEnemyHit", "sfxEnemyAttack", "sfxPickupXp",
                "sfxLevelup", "sfxUpgradeSelect", "sfxGameOver"
            };
            if (titleAudio != null) CheckFields("TitleScene", titleAudio, audioFields);
            FindOne<EventSystem>("TitleScene");
            var titleCam = FindOne<Camera>("TitleScene");
            if (titleCam != null)
                Check(titleCam.GetComponent<PixelPerfectCamera>() != null, "TitleScene Camera.PixelPerfectCamera");

            // ---------- GameScene
            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            var gm = FindOne<GameManager>("GameScene");
            if (gm != null)
                CheckFields("GameScene", gm, "hubCanvas", "runHUDCanvas", "levelUpCanvas",
                    "pauseCanvas", "gameOverCanvas", "player", "tilemap");
            var gameAudio = FindOne<AudioManager>("GameScene");
            if (gameAudio != null) CheckFields("GameScene", gameAudio, audioFields);
            var hub = FindOne<HubController>("GameScene");
            if (hub != null)
                CheckFields("GameScene", hub, "coinsText", "highscoreText", "playButton",
                    "upgradeContainer", "upgradeSlotPrefab", "leaderboardContainer",
                    "leaderboardEntryPrefab", "newsContainer", "newsEntryPrefab",
                    "giftCodeForm", "feedbackForm", "settingsPanel", "settingsButton");
            var pause = FindOne<PauseController>("GameScene");
            if (pause != null)
                CheckFields("GameScene", pause, "resumeButton", "newsButton", "feedbackButton",
                    "quitButton", "newsPanel", "feedbackForm", "newsContentContainer", "newsEntryPrefab");
            var over = FindOne<GameOverController>("GameScene");
            if (over != null)
                CheckFields("GameScene", over, "scoreText", "wavesText", "levelText",
                    "coinsEarnedText", "rankText", "bestScoreText", "playAgainButton", "hubButton");
            var hud = FindOne<RunHUDController>("GameScene");
            if (hud != null)
                CheckFields("GameScene", hud, "waveText", "timerText", "scoreText", "levelText",
                    "hpBar", "xpBar", "pauseButton");
            var levelUp = FindOne<LevelUpController>("GameScene");
            if (levelUp != null)
                CheckFields("GameScene", levelUp, "titleText", "choiceContainer", "choiceCardPrefab");
            var forms = FindAll<FeedbackForm>();
            Check(forms.Length >= 2, "GameScene FeedbackForm x2 (hub + pause)");
            foreach (var form in forms)
                CheckFields($"GameScene({form.transform.parent?.name}/{form.name})", form,
                    "titleInput", "messageInput", "categoryDropdown", "emailInput", "submitButton", "statusText");
            var gift = FindOne<GiftCodeForm>("GameScene");
            if (gift != null) CheckFields("GameScene", gift, "codeInput", "redeemButton", "statusText");
            var settingsPanel = FindOne<SettingsPanel>("GameScene");
            if (settingsPanel != null) CheckFields("GameScene", settingsPanel, "signOutButton", "closeButton");
            var hp = FindOne<HPBar>("GameScene");
            if (hp != null) CheckFields("GameScene", hp, "fillImage");
            var xp = FindOne<XPBar>("GameScene");
            if (xp != null) CheckFields("GameScene", xp, "fillImage");
            var follow = FindOne<CameraFollow>("GameScene");
            if (follow != null) CheckFields("GameScene", follow, "target");
            var enemyPool = FindOne<EnemyPool>("GameScene");
            if (enemyPool != null)
                CheckFields("GameScene", enemyPool, "crabPrefab", "jellyfishPrefab", "piratePrefab", "bossPrefab");
            var projPool = FindOne<ProjectilePool>("GameScene");
            if (projPool != null) CheckFields("GameScene", projPool, "projectilePrefab");
            var pickPool = FindOne<PickupPool>("GameScene");
            if (pickPool != null) CheckFields("GameScene", pickPool, "xpPrefab");
            FindOne<HorizonManager>("GameScene");
            FindOne<SpawnManager>("GameScene");
            FindOne<WeaponManager>("GameScene");
            FindOne<LevelUpManager>("GameScene");
            FindOne<EventSystem>("GameScene");
            var player = FindOne<PlayerController>("GameScene");
            if (player != null)
                Check(player.CompareTag("Player"), "GameScene Player tag=Player");
            var gameCam = FindOne<Camera>("GameScene");
            if (gameCam != null)
                Check(gameCam.GetComponent<PixelPerfectCamera>() != null, "GameScene Camera.PixelPerfectCamera");
            var paintedMap = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault();
            Check(paintedMap != null && paintedMap.GetUsedTilesCount() > 0, "GameScene Tilemap painted tiles");

            // ---------- prefabs
            var jelly = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/JellyfishEnemy.prefab");
            Check(jelly != null, "prefab JellyfishEnemy");
            if (jelly != null)
                CheckFields("prefab", jelly.GetComponent<EnemyJellyfish>(), "poisonZonePrefab");
            var slot = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/UpgradeSlot.prefab");
            if (slot != null)
                CheckFields("prefab", slot.GetComponent<UpgradeSlot>(), "labelText", "levelText", "costText", "buyButton");
            var lbe = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/LeaderboardEntry.prefab");
            if (lbe != null)
                CheckFields("prefab", lbe.GetComponent<LeaderboardEntry>(), "rankText", "nameText", "scoreText");
            var news = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/NewsEntry.prefab");
            if (news != null)
                CheckFields("prefab", news.GetComponent<NewsEntry>(), "titleText", "dateText");
            var card = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/LevelUpChoiceCard.prefab");
            Check(card != null && card.GetComponent<UnityEngine.UI.Button>() != null
                  && card.transform.Find("NameText") != null && card.transform.Find("DescText") != null,
                "prefab LevelUpChoiceCard Button + NameText/DescText");
            var poison = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Pickups/PoisonZone.prefab");
            Check(poison != null && poison.GetComponent<PoisonZone>() != null,
                "prefab PoisonZone damage behaviour (PoisonZone component)");

            // ---------- global assets
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            Check(fontAsset != null && fontAsset.sourceFontFile != null && fontAsset.material != null
                  && fontAsset.material.shader != null && fontAsset.material.shader.name.Contains("TextMeshPro"),
                "asset PressStart2P SDF (generated TMP font incl. TMP shader)");
            Check(GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset,
                "settings URP pipeline assigned");

            Debug.Log(missing == 0
                ? "[VALIDATE] ALL CHECKS PASSED"
                : $"[VALIDATE] {missing} MISSING entries");
            EditorApplication.Exit(missing == 0 ? 0 : 1);
        }
    }
