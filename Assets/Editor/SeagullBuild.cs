using UnityEditor;
using UnityEditor.Build.Reporting;
public static class SeagullBuild
{
    public static void Build()
    {
        SeagullEvidenceFixture.Check();
        var target = UnityEditor.Build.NamedBuildTarget.Standalone;
        var original = PlayerSettings.GetScriptingBackend(target);
        BuildReport result = null;
        try
        {
            PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.Mono2x);
            result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[]{"Assets/Scenes/BootScene.unity","Assets/Scenes/TitleScene.unity","Assets/Scenes/GameScene.unity"},
                locationPathName = "Builds/Seagull Storm.app",
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.Development
            });
        }
        finally { PlayerSettings.SetScriptingBackend(target, original); }
        UnityEngine.Debug.Log("[SEAGULL BUILD] " + result.summary.result);
        EditorApplication.Exit(result.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
