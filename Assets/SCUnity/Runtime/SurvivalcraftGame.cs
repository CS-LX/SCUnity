using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using H = Engine.UnityRuntime.Host;

namespace SCUnity.Runtime
{
    /// <summary>Unity owns the lifetime; the original game owns each frame's update and drawing.</summary>
    public sealed class SurvivalcraftGame : MonoBehaviour
    {
        [SerializeField] UniversalRenderPipelineAsset pipeline;
        PortRenderer portRenderer;
        PortInput input;
        CoreLoopSmokeTest smoke;
        Harmony desktopServices;
        RenderPipelineAsset previousGraphicsPipeline, previousQualityPipeline;
        bool stopped;
        public int Frames { get; private set; }
        public long ExecutedDraws => portRenderer?.ExecutedDraws ?? 0;
        public readonly List<string> Errors = new List<string>();
        public string DataDirectory { get; private set; }
        public string CurrentScreenName => Game.ScreensManager.CurrentScreen?.GetType().FullName;

        void Start()
        {
            Application.logMessageReceived += UnityLog;
            try
            {
                if (pipeline == null) throw new InvalidOperationException("Assign the Survivalcraft URP pipeline to this scene.");
                previousGraphicsPipeline = GraphicsSettings.defaultRenderPipeline;
                previousQualityPipeline = QualitySettings.renderPipeline;
                GraphicsSettings.defaultRenderPipeline = pipeline;
                QualitySettings.renderPipeline = pipeline;
                string[] args = Environment.GetCommandLineArgs();
                int index = Array.IndexOf(args, "-scunity-smoke-output");
                string smokeOutput = index >= 0 ? Path.GetFullPath(args[index + 1]) : null;
                DataDirectory = smokeOutput == null
                    ? Path.Combine(Application.persistentDataPath, "Survivalcraft")
                    : Path.Combine(smokeOutput, "data");
                int dataIndex = Array.IndexOf(args, "-scunity-data-path");
                if (dataIndex >= 0) DataDirectory = Path.GetFullPath(args[dataIndex + 1]);
                H.Configure(Path.Combine(Application.streamingAssetsPath, "Survivalcraft"), DataDirectory, Screen.width, Screen.height);
                Engine.Log.AddLogSink(new UnityGameLog(this));

                // The original updater/registry integration target the legacy executable.
                // Keep them disabled until their Unity desktop implementations are ready.
                desktopServices = new Harmony("scunity.desktop.services");
                foreach (var item in new[] {
                    new[] { "Game.APIUpdateManager", "Initialize" },
                    new[] { "Game.MotdManager", "Update" },
                    new[] { "Game.FileAssociationManager", "Initialize" },
                    new[] { "Game.Program", "ToRestartHandler" }
                })
                    desktopServices.Patch(AccessTools.Method(typeof(Game.Program).Assembly.GetType(item[0], true), item[1]),
                        new HarmonyMethod(typeof(SurvivalcraftGame), nameof(SkipLegacyService)));

                portRenderer = new PortRenderer();
                Game.Program.EntryPoint();
                input = new PortInput();
                if (smokeOutput != null) smoke = new CoreLoopSmokeTest(this, smokeOutput);
                Debug.Log("Survivalcraft core loop started. Data: " + DataDirectory);
            }
            catch (Exception error) { Fail(error); }
        }

        static bool SkipLegacyService() => false;

        void Update()
        {
            if (stopped) return;
            try
            {
                smoke?.Before();
                input.Sample(smoke != null);
                portRenderer.Begin();
                H.Step(UnityEngine.Time.unscaledDeltaTime);
                Frames++;
                smoke?.After();
                if (!Engine.Window.IsCreated) ExitGame();
            }
            catch (Exception error) { Fail(error); }
        }

        void UnityLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                Errors.Add(message + "\n" + stackTrace);
        }

        void Fail(Exception error)
        {
            Debug.LogException(error);
            smoke?.Fail(error);
            Stop();
        }

        void ExitGame()
        {
            Stop();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void Stop()
        {
            if (stopped) return;
            stopped = true;
            try { H.Shutdown(); }
            finally
            {
                input?.Dispose();
                portRenderer?.Dispose();
                smoke?.Dispose();
                desktopServices?.UnpatchSelf();
                GraphicsSettings.defaultRenderPipeline = previousGraphicsPipeline;
                QualitySettings.renderPipeline = previousQualityPipeline;
                Application.logMessageReceived -= UnityLog;
            }
        }

        void OnApplicationQuit() => Stop();
        void OnDestroy() => Stop();

        sealed class UnityGameLog : Engine.ILogSink
        {
            readonly SurvivalcraftGame owner;
            public UnityGameLog(SurvivalcraftGame owner) { this.owner = owner; }
            public void Dispose() { }
            public void Log(Engine.LogType type, string message)
            {
                if (type == Engine.LogType.Error) owner.Errors.Add(message);
                if (type == Engine.LogType.Error) Debug.LogError(message);
                else Debug.Log(message);
            }
        }
    }
}
