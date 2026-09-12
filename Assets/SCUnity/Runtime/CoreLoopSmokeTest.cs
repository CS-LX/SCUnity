using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using H = Engine.UnityRuntime.Host;

namespace SCUnity.Runtime {
    // Enabled only by -scunity-smoke-output. Runs the same scene/entry used for normal play.
    internal sealed class CoreLoopSmokeTest : IDisposable {
        readonly SurvivalcraftGame game;
        readonly string output;
        readonly Mouse mouse;
        readonly Keyboard keyboard;
        readonly DateTime started = DateTime.UtcNow;
        int phase, ticks;
        bool settingsEntered, returned, finished, disposed;
        AudioSmokeTest audio;
        WorldSmokeTest world;
        CommunitySearchSmokeTest community;
        string[] sourceChecks;
        readonly bool worldRequested = Array.IndexOf(Environment.GetCommandLineArgs(), "-scunity-world-test") >= 0;

        public CoreLoopSmokeTest(SurvivalcraftGame game, string output) {
            this.game = game;
            this.output = output;
            Directory.CreateDirectory(output);
            mouse = InputSystem.AddDevice<Mouse>();
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        public void Before() {
            mouse.MakeCurrent();
            keyboard.MakeCurrent();
        }

        void Click(bool pressed) => InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(Screen.width * .36f, Screen.height * .253f), buttons = (ushort)(pressed ? 1 : 0) });

        public void After() {
            if (finished) return;
            ticks++;
            if (phase == 0
                && Game.ScreensManager.CurrentScreen is Game.MainMenuScreen
                && !Game.ScreensManager.IsAnimating
                && ticks > 40) {
                sourceChecks = SourceCompilationSmokeTest.Run();
                Click(true);
                phase = 1;
                ticks = 0;
            }
            else if (phase == 1
                && ticks == 2)
                Click(false);
            else if (phase == 1
                && Game.ScreensManager.CurrentScreen is Game.SettingsScreen
                && !Game.ScreensManager.IsAnimating) {
                settingsEntered = true;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape));
                phase = 2;
                ticks = 0;
            }
            else if (phase == 2
                && ticks == 2)
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            else if (phase == 2
                && Game.ScreensManager.CurrentScreen is Game.MainMenuScreen
                && !Game.ScreensManager.IsAnimating) {
                returned = true;
                phase = 3;
                ticks = 0;
            }
            else if (phase == 3
                && ticks >= 30
                && game.ExecutedDraws > 0) {
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "-scunity-audio-test") >= 0) {
                    audio = new AudioSmokeTest(game);
                    phase = 4;
                }
                else
                    NextScenario();
            }
            else if (phase == 4) {
                audio.Tick();
                if (audio.Complete) NextScenario();
            }
            else if (phase == 5) {
                world.Tick();
                if (world.Complete) {
                    finished = true;
                    game.StartCoroutine(Capture());
                }
            }
            else if (phase == 6) {
                community.Tick();
                if (community.Complete) NextScenario();
            }
            if ((DateTime.UtcNow - started).TotalSeconds > (worldRequested ? 240 : 90)) throw new TimeoutException("Core loop smoke test timed out. Phase: " + phase);
        }

        void NextScenario() {
            if (community == null && Array.IndexOf(Environment.GetCommandLineArgs(), "-scunity-community-test") >= 0) {
                community = new CommunitySearchSmokeTest(game, output, mouse);
                phase = 6;
                return;
            }
            if (worldRequested) {
                world = new WorldSmokeTest(game, output);
                phase = 5;
            }
            else {
                finished = true;
                game.StartCoroutine(Capture());
            }
        }

        IEnumerator Capture() {
            yield return new WaitForEndOfFrame();
            Exception failure = null;
            try {
                var image = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(output, "main-menu.png"), image.EncodeToPNG());
                UnityEngine.Object.Destroy(image);
                if (!settingsEntered
                    || !returned
                    || game.Errors.Count > 0
                    || game.ExecutedDraws == 0)
                    throw new Exception("Core loop assertions failed: " + string.Join("\n", game.Errors));
                string settings = Engine.Storage.GetSystemPath(ModsManager.SettingPath);
                if (!Path.GetFullPath(settings).StartsWith(Path.GetFullPath(game.DataDirectory) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new Exception("Settings escaped the persistent data directory: " + settings);
            }
            catch (Exception error) {
                failure = error;
            }
            Complete(failure);
        }

        public void Fail(Exception error) {
            if (!disposed) Complete(error);
        }

        void Complete(Exception error) {
            finished = true;
            var result = new Result {
                passed = error == null,
                enteredSettingsWithMouse = settingsEntered,
                returnedWithEscape = returned,
                frames = game.Frames,
                executedDraws = game.ExecutedDraws,
                draws = H.DrawCount,
                uploads = H.UploadCount,
                screen = Game.ScreensManager.CurrentScreen?.GetType().FullName,
                isMono = Type.GetType("Mono.Runtime") != null,
                isEditor = Application.isEditor,
                unityVersion = Application.unityVersion,
                platform = Application.platform.ToString(),
                pointerSize = IntPtr.Size,
                error = error?.ToString(),
                entryPoint = typeof(SurvivalcraftGame).FullName
            };
            result.worldChecks = world?.Checks.ToArray();
            result.communityChecks = community?.Checks.ToArray();
            result.sourceChecks = sourceChecks;
            result.audioBlocks = game.AudioBlocks;
            result.audioChecks = audio?.Checks.ToArray();
            try {
                game.Stop();
            }
            catch (Exception stopError) {
                result.passed = false;
                result.error += stopError.ToString();
            }
            if (Engine.Window.IsCreated) {
                result.passed = false;
                result.error += "Window remained created after shutdown.";
            }
            File.WriteAllText(Path.Combine(output, "result.json"), JsonUtility.ToJson(result, true));
            Application.Quit(result.passed ? 0 : 1);
        }

        public void Dispose() {
            if (disposed) return;
            disposed = true;
            audio?.Dispose();
            world?.Dispose();
            community?.Dispose();
            InputSystem.RemoveDevice(mouse);
            InputSystem.RemoveDevice(keyboard);
        }

        [Serializable]
        sealed class Result {
            public bool passed, enteredSettingsWithMouse, returnedWithEscape, isMono, isEditor;
            public int frames, pointerSize;
            public long executedDraws, draws, uploads;
            public long audioBlocks;
            public string[] audioChecks, worldChecks;
            public string[] communityChecks;
            public string[] sourceChecks;
            public string screen, unityVersion, platform, entryPoint, error;
        }
    }
}
