using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SCUnity.Editor
{
    public static class SurvivalcraftProject
    {
        const string Scene = "Assets/SCUnity/Scenes/Survivalcraft.unity";

        [MenuItem("Survivalcraft/Open Game Scene")]
        public static void OpenScene()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(Scene);
        }

        // Used on an isolated copy of the main project; never closes the user's Editor.
        public static void BuildCoreLoop()
        {
            try
            {
                var target = NamedBuildTarget.Standalone;
                if (PlayerSettings.GetScriptingBackend(target) != ScriptingImplementation.Mono2x ||
                    PlayerSettings.GetApiCompatibilityLevel(target) != ApiCompatibilityLevel.NET_Unity_4_8 ||
                    PlayerSettings.colorSpace != ColorSpace.Gamma)
                    throw new InvalidOperationException("Core loop requires Windows Mono, .NET Framework and Gamma color space.");
                PlayerSettings.runInBackground = true;
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                PlayerSettings.defaultScreenWidth = 960;
                PlayerSettings.defaultScreenHeight = 540;
                PlayerSettings.SplashScreen.show = false;
                var scene = EditorSceneManager.OpenScene(Scene);
                int hosts = 0;
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (component == null) throw new InvalidOperationException("Missing script in game scene: " + root.name);
                        if (component is Runtime.SurvivalcraftGame) hosts++;
                    }
                }
                if (hosts != 1) throw new InvalidOperationException("Game scene must contain one SurvivalcraftGame entry.");
                string[] args = Environment.GetCommandLineArgs();
                string player = args[Array.IndexOf(args, "-scunity-player") + 1];
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { Scene }, target = BuildTarget.StandaloneWindows64,
                    locationPathName = player, options = BuildOptions.StrictMode
                });
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(player), "build.json"),
                    "{\"result\":\"" + report.summary.result + "\",\"errors\":" + report.summary.totalErrors +
                    ",\"warnings\":" + report.summary.totalWarnings + "}");
                if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0)
                    throw new InvalidOperationException("Main project core-loop build failed.");
                EditorApplication.Exit(0);
            }
            catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
        }
    }
}
