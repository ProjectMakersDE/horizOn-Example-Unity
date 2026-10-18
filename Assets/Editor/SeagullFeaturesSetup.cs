using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using SeagullStorm;

public static class SeagullFeaturesSetup
{
    [MenuItem("Seagull Storm/Create current-feature Hello scene")]
    public static void CreateScene()
    {
        if (File.Exists("Assets/Scenes/HelloFeatures.unity")) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Camera").AddComponent<Camera>(); camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.11f, .11f, .11f);
        new GameObject("Current feature Hello").AddComponent<SeagullHelloFeatures>();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/HelloFeatures.unity"); AssetDatabase.SaveAssets();
    }
    public static void BuildQa()
    {
        Preflight();
        string output = Environment.GetEnvironmentVariable("SEAGULL_QA_OUT");
        string tag = Environment.GetEnvironmentVariable("SEAGULL_QA_TAG");
        if (string.IsNullOrEmpty(output) || string.IsNullOrEmpty(tag) || !System.Text.RegularExpressions.Regex.IsMatch(tag, "^[a-f0-9]{12}$")) throw new Exception("Explicit owned QA output/tag required");
        var target = UnityEditor.Build.NamedBuildTarget.Standalone;
        string product = PlayerSettings.productName, company = PlayerSettings.companyName, identifier = PlayerSettings.GetApplicationIdentifier(target);
        var backend = PlayerSettings.GetScriptingBackend(target);
        try
        {
            PlayerSettings.companyName = "ProjectMakers QA"; PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.Mono2x);
            PlayerSettings.SetApplicationIdentifier(target, "de.projectmakers.seagull.qa." + tag);
            PlayerSettings.productName = "Seagull QA " + tag;
            var game = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { "Assets/Scenes/BootScene.unity", "Assets/Scenes/TitleScene.unity", "Assets/Scenes/GameScene.unity" }, locationPathName = Path.Combine(output, "Game.app"), target = BuildTarget.StandaloneOSX, options = BuildOptions.Development });
            if (game.summary.result != BuildResult.Succeeded) throw new Exception("Native QA game build failed");
            PlayerSettings.productName = "Seagull QA Hello " + tag;
            PlayerSettings.SetApplicationIdentifier(target, "de.projectmakers.seagull.hello.qa." + tag);
            var hello = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = new[] { "Assets/Scenes/HelloFeatures.unity" }, locationPathName = Path.Combine(output, "Hello.app"), target = BuildTarget.StandaloneOSX, options = BuildOptions.Development });
            if (hello.summary.result != BuildResult.Succeeded) throw new Exception("Native QA Hello build failed");
            File.WriteAllText(Path.Combine(output, "qa-products.txt"), "ProjectMakers QA/Seagull QA " + tag + "\nProjectMakers QA/Seagull QA Hello " + tag + "\n");
            Debug.Log("[CURRENT FEATURES BUILD] PASS isolated game and Hello native players; no real config or backend used");
        }
        finally { PlayerSettings.productName = product; PlayerSettings.companyName = company; PlayerSettings.SetApplicationIdentifier(target, identifier); PlayerSettings.SetScriptingBackend(target, backend); AssetDatabase.SaveAssets(); }
    }

    public static void Preflight()
    {
        string temp = Path.Combine(Path.GetTempPath(), "seagull-originals-" + Guid.NewGuid().ToString("N"));
        try
        {
            using (var store = new FeatureOriginalStore("fixture-project", "fixture-player", "Friends", temp))
            {
                if (store.Read() != null) throw new Exception("Unexpected original");
                const string original = "{\"expectedRevision\":9007199254740991,\"id\":\"unchanged\"}";
                store.SaveNew(original);
                bool locked = false; try { using var second = new FeatureOriginalStore("fixture-project", "fixture-player", "Friends", temp); } catch (IOException) { locked = true; }
                if (!locked || store.Read() != original) throw new Exception("Exclusive original lease failed");
                bool replacement = false; try { store.SaveNew("replacement"); } catch (IOException) { replacement = true; }
                if (!replacement || store.Read() != original) throw new Exception("Original was replaced");
                store.Confirm(original); if (store.Read() != null) throw new Exception("Matching ACK failed");
            }
            using (var store = new FeatureOriginalStore("fixture-project", "fixture-player", "Chat", temp)) store.SaveNew("original-NUL-\0-tail");
            using (var store = new FeatureOriginalStore("fixture-project", "fixture-player", "Chat", temp))
            { if (store.Read() != "original-NUL-\0-tail") throw new Exception("Restart changed original token"); }
            CreateScene();
            Debug.Log("[CURRENT FEATURES] PASS exact original/lease/restart/ACK, all official recipes and unchanged public guide compiled; Hello scene created without backend work");
        }
        finally { if (Directory.Exists(temp)) Directory.Delete(temp, true); }
    }
}
