using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SCUnity.Editor
{
    // Invoked only by an explicit CLI method in a disposable main-project copy.
    // SessionState survives the Domain reload used by normal Play/Stop.
    [InitializeOnLoad]
    public static class CoreLoopEditorTest
    {
        const string Key = "SCUnity.CoreLoopEditorTest.";
        static CoreLoopEditorTest() { EditorApplication.update += Tick; }

        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs();
            string output = args[Array.IndexOf(args, "-scunity-validation-output") + 1];
            Directory.CreateDirectory(output);
            SessionState.SetString(Key + "output", output);
            SessionState.SetInt(Key + "runs", 0);
            SessionState.SetString(Key + "phase", "waiting");
            SessionState.SetString(Key + "nextPlay", (EditorApplication.timeSinceStartup + 5).ToString("R", System.Globalization.CultureInfo.InvariantCulture));
            SessionState.SetString(Key + "started", DateTime.UtcNow.ToString("O"));
            EditorSettings.enterPlayModeOptionsEnabled = false;
            EditorSceneManager.OpenScene("Assets/SCUnity/Scenes/Survivalcraft.unity");
        }

        static void Tick()
        {
            string phase = SessionState.GetString(Key + "phase", "");
            if (phase.Length == 0) return;
            try
            {
                if ((DateTime.UtcNow - DateTime.Parse(SessionState.GetString(Key + "started", ""))).TotalSeconds > 180)
                    throw new TimeoutException("Editor Play/Stop validation timed out.");
                if (phase == "waiting")
                {
                    if (!EditorApplication.isCompiling && !EditorApplication.isUpdating &&
                        EditorApplication.timeSinceStartup >= double.Parse(SessionState.GetString(Key + "nextPlay", "0"), System.Globalization.CultureInfo.InvariantCulture))
                    {
                        SessionState.SetString(Key + "phase", "starting");
                        EditorApplication.isPlaying = true;
                    }
                    return;
                }
                if (EditorApplication.isPlaying)
                {
                    var game = UnityEngine.Object.FindFirstObjectByType<Runtime.SurvivalcraftGame>();
                    if (game == null) return;
                    if (game.Errors.Count > 0) throw new Exception(string.Join("\n", game.Errors));
                    if (game.Frames >= 60 && game.CurrentScreenName == "Game.MainMenuScreen")
                    {
                        SessionState.SetString(Key + "phase", "stopping");
                        EditorApplication.isPlaying = false;
                    }
                }
                else if (phase == "stopping" && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    int runs = SessionState.GetInt(Key + "runs", 0) + 1;
                    SessionState.SetInt(Key + "runs", runs);
                    if (runs == 2) Finish(null);
                    else
                    {
                        SessionState.SetString(Key + "phase", "waiting");
                        SessionState.SetString(Key + "nextPlay", (EditorApplication.timeSinceStartup + 5).ToString("R", System.Globalization.CultureInfo.InvariantCulture));
                    }
                }
            }
            catch (Exception error) { Finish(error); }
        }

        static void Finish(Exception error)
        {
            SessionState.SetString(Key + "phase", "");
            File.WriteAllText(Path.Combine(SessionState.GetString(Key + "output", ""), "editor-result.json"),
                JsonUtility.ToJson(new Result { passed = error == null, playStopCycles = SessionState.GetInt(Key + "runs", 0),
                    error = error?.ToString(), unityVersion = Application.unityVersion }, true));
            if (error != null) Debug.LogException(error);
            EditorApplication.Exit(error == null ? 0 : 1);
        }

        [Serializable] sealed class Result { public bool passed; public int playStopCycles; public string error, unityVersion; }
    }
}
