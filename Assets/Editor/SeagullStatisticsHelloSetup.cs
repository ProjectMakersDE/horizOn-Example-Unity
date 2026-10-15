using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class SeagullStatisticsHelloSetup
{
    public static void CreateScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Camera").AddComponent<Camera>();
        camera.backgroundColor = new Color(.11f, .11f, .11f); camera.clearFlags = CameraClearFlags.SolidColor;
        new GameObject("Statistics Hello").AddComponent<SeagullStorm.StatisticsHello>();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/StatisticsHello.unity");
        AssetDatabase.SaveAssets();
        Debug.Log("[STATISTICS HELLO] Created standalone scene");
    }
    public static void ValidateIntent()
    {
        SeagullStorm.PlayerValueIntentSmoke.Check();
        SeagullStorm.StatisticsIntentSmoke.Check();
        Debug.Log("[STATISTICS HELLO] PASS standalone component compiled; network remains explicit");
    }
    public static void BuildHello()
    {
        ValidateIntent();
        var target = UnityEditor.Build.NamedBuildTarget.Standalone;
        var original = PlayerSettings.GetScriptingBackend(target);
        BuildReport result = null;
        try
        {
            PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.Mono2x);
            result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/Scenes/StatisticsHello.unity" }, locationPathName = "Builds/Statistics Hello.app",
                target = BuildTarget.StandaloneOSX, options = BuildOptions.Development });
        }
        finally { PlayerSettings.SetScriptingBackend(target, original); }
        EditorApplication.Exit(result.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
