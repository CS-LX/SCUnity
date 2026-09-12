using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Game;
using HarmonyLib;
using UnityEngine;

namespace SCUnity.Runtime {
    // Opt-in scenario using the same world creation/loading/save code as normal play.
    internal sealed class WorldSmokeTest : IDisposable {
        readonly SurvivalcraftGame game;
        readonly string output;
        readonly Harmony observer = new Harmony("scunity.world.smoke");
        WorldInfo world;
        int phase, playingFrames;
        float playingSince;
        bool captured;
        bool fixturesPlaced;
        readonly RenderSmokeTest rendering = new RenderSmokeTest();
        public readonly List<string> Checks = new List<string>();
        public bool Complete { get; private set; }

        public WorldSmokeTest(SurvivalcraftGame game, string output) {
            this.game = game;
            this.output = output;
            SettingsManager.VisibilityRange = 32;
            observer.Patch(AccessTools.Method(typeof(GameManager), nameof(GameManager.LoadProject)), postfix: new HarmonyMethod(typeof(WorldSmokeTest), nameof(AddTestPlayer)));
            foreach (var type in typeof(Game.Program).Assembly.GetTypes())
                if (!type.IsAbstract
                    && typeof(IDrawable).IsAssignableFrom(type))
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                        if (method.Name == "Draw"
                            && !method.IsAbstract)
                            observer.Patch(method, finalizer: new HarmonyMethod(typeof(WorldSmokeTest), nameof(DrawingFailure)));
            world = WorldsManager.CreateWorld(
                new WorldSettings {
                    Name = "SCUnity world validation",
                    CustomWorldSeed = true,
                    WorldSeed = 123456,
                    Seed = "123456",
                    OriginalSerializationVersion = VersionsManager.SerializationVersion,
                    GameMode = GameMode.Creative,
                    TerrainGenerationMode = TerrainGenerationMode.FlatContinent,
                    EnvironmentBehaviorMode = EnvironmentBehaviorMode.Static,
                    AreWeatherEffectsEnabled = false,
                    AreSupernaturalCreaturesEnabled = false,
                    AreSeasonsChanging = false
                }
            );
            ScreensManager.SwitchScreen("GameLoading", world, null);
        }

        static void AddTestPlayer() {
            var players = GameManager.Project.FindSubsystem<SubsystemPlayers>(true);
            if (players.PlayersData.Count == 0) players.AddPlayerData(new PlayerData(GameManager.Project) { Name = "UnityPlayer", InputDevice = WidgetInputDevice.None, SpawnPosition = new Engine.Vector3(0, 66, 0), CharacterSkinName = "$Male1" });
        }

        // The upstream SubsystemDrawing catches per-draw exceptions. Validation
        // must observe them or a missing model could be mistaken for a pass.
        static Exception DrawingFailure(Exception __exception) {
            if (__exception != null) Engine.Log.Error(__exception.ToString());
            return __exception;
        }

        public void Tick() {
            if (game.Errors.Count > 0) throw new InvalidOperationException(game.Errors[0]);
            if (phase == 4
                && rendering.Complete) {
                if (rendering.Failure != null) throw rendering.Failure;
                Checks.AddRange(rendering.Checks);
                Complete = true;
            }
            if (Complete || GameManager.Project == null) return;
            var players = GameManager.Project.FindSubsystem<SubsystemPlayers>(true);
            if (players.PlayersData.Count == 0
                || !players.PlayersData[0].IsReadyForPlaying)
                return;
            if (playingFrames == 0) playingSince = UnityEngine.Time.realtimeSinceStartup;
            playingFrames++;
            if (!fixturesPlaced) {
                var camera = players.PlayersData[0].GameWidget.ActiveCamera;
                var position = camera.ViewPosition + camera.ViewDirection * 6;
                var terrain = GameManager.Project.FindSubsystem<SubsystemTerrain>(true);
                int x = (int)Math.Floor(position.X), z = (int)Math.Floor(position.Z);
                for (int y = 65; y <= 68; y++) terrain.ChangeCell(x, y, z, 3);
                var boat = DatabaseManager.CreateEntity(GameManager.Project, "Boat", true);
                boat.FindComponent<ComponentBody>(true).Position = new Engine.Vector3(x + 2, 66, z);
                GameManager.Project.AddEntity(boat);
                fixturesPlaced = true;
            }
            // Terrain fades use elapsed time, so frame counts alone are insufficient
            // on an uncapped Player. Wait for the real chunk fade to settle.
            if (phase == 0
                && playingFrames >= 30
                && UnityEngine.Time.realtimeSinceStartup - playingSince >= 3) {
                Checks.Add("Original world creation and player spawn reached Game screen");
                Require(GameManager.Project.FindSubsystem<SubsystemModelsRenderer>(true).ModelsDrawn > 0, "Original boat model reaches drawing");
                game.StartCoroutine(Capture("world-before-reload.png"));
                phase = 1;
            }
            else if (phase == 1 && captured) {
                captured = false;
                GameManager.Project.FindSubsystem<SubsystemTerrain>(true).ChangeCell(0, 70, 0, 3);
                GameManager.SaveProject(true, false);
                Checks.Add("Original terrain edit and synchronous save completed");
                GameManager.DisposeProject();
                using (var stream = File.Create(Path.Combine(output, "world.scworld"))) WorldsManager.ExportWorld(world.DirectoryName, stream);
                var games = ScreensManager.FindScreen<GameScreen>("Game").Children.Find<ContainerWidget>("GamesWidget");
                GameManager.LoadProject(WorldsManager.GetWorldInfo(world.DirectoryName), games);
                Require(GameManager.Project.FindSubsystem<SubsystemGameInfo>(true).WorldSeed == 123456, "World seed restored");
                Require(GameManager.Project.FindSubsystem<SubsystemPlayers>(true).PlayersData.Count == 1, "Player restored");
                phase = 2;
                playingFrames = 0;
            }
            else if (phase == 2
                && playingFrames >= 30
                && UnityEngine.Time.realtimeSinceStartup - playingSince >= 3) {
                Require(GameManager.Project.FindSubsystem<SubsystemModelsRenderer>(true).ModelsDrawn > 0, "Saved boat model is drawn after reload");
                Require(GameManager.Project.FindSubsystem<SubsystemTerrain>(true).Terrain.GetCellValue(0, 70, 0) == 3, "Placed block survived project disposal and reload");
                game.StartCoroutine(Capture("world-after-reload.png"));
                phase = 3;
            }
            else if (phase == 3 && captured) {
                Checks.Add("Reloaded world rendered through Unity GPU");
                rendering.Draw(game, output);
                phase = 4;
            }
        }

        void Require(bool condition, string check) {
            if (!condition) throw new InvalidOperationException(check);
            Checks.Add(check);
        }

        IEnumerator Capture(string name) {
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(output, name), texture.EncodeToPNG());
            UnityEngine.Object.Destroy(texture);
            captured = true;
        }

        public void Dispose() {
            observer.UnpatchSelf();
            rendering.Dispose();
        }
    }
}