using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SCUnity.Validation
{
    public static class BuildFoundation
    {
        public static void Configure()
        {
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            PlayerSettings.SetApiCompatibilityLevel(NamedBuildTarget.Standalone, ApiCompatibilityLevel.NET_Unity_4_8);
            AssetDatabase.SaveAssets();
            // Persist and restart before BuildPlayer so imports use the selected profile.
            EditorApplication.Exit(0);
        }

        public static void Build()
        {
            try
            {
                var target = NamedBuildTarget.Standalone;
                if (PlayerSettings.GetScriptingBackend(target) != ScriptingImplementation.Mono2x
                    || PlayerSettings.GetApiCompatibilityLevel(target) != ApiCompatibilityLevel.NET_Unity_4_8)
                    throw new InvalidOperationException("Probe requires explicit Mono and .NET Framework API settings before import.");
                PlayerSettings.SetManagedStrippingLevel(target, ManagedStrippingLevel.Disabled);
                PlayerSettings.runInBackground = true;
                PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
                PlayerSettings.defaultScreenWidth = 640;
                PlayerSettings.defaultScreenHeight = 360;
                PlayerSettings.companyName = "SCUnity";
                PlayerSettings.productName = "SCUnity Foundation Validation";
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("Foundation assertions").AddComponent<FoundationBehaviour>();
                EditorSceneManager.SaveScene(scene, "Assets/Foundation.unity");
                string[] args = Environment.GetCommandLineArgs();
                int index = Array.IndexOf(args, "-portProbePlayer");
                if (index < 0 || index + 1 >= args.Length) throw new InvalidOperationException("Missing -portProbePlayer.");
                BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/Foundation.unity" },
                    target = BuildTarget.StandaloneWindows64,
                    locationPathName = args[index + 1],
                    options = BuildOptions.StrictMode
                });
                File.WriteAllText(Path.Combine(Path.GetDirectoryName(args[index + 1]), "build-summary.json"),
                    "{\"result\":\"" + report.summary.result + "\",\"errors\":" + report.summary.totalErrors
                    + ",\"warnings\":" + report.summary.totalWarnings
                    + ",\"backend\":\"Mono2x\",\"apiCompatibility\":\"NET_Unity_4_8\",\"development\":false}");
                if (report.summary.result != BuildResult.Succeeded || report.summary.totalErrors != 0)
                    throw new InvalidOperationException("Unity Player build failed.");
                EditorApplication.Exit(0);
            }
            catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }
    }
}
