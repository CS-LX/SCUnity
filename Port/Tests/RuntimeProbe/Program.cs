using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Security.Cryptography;
using System.Xml.Linq;
using Engine;
using Engine.Graphics;
using Engine.Input;
using Engine.Audio;
using Game;
using HarmonyLib;
using Silk.NET.Windowing;
using GameProgram = Game.Program;
using Window = Engine.Window;

internal static class RuntimeProbe
{
    static StreamWriter trace;
    static string evidence;
    static readonly object gate = new();
    static readonly Stopwatch elapsed = Stopwatch.StartNew();
    static readonly List<string> errors = [];
    static readonly List<string> expectedErrors = [];
    static string expectedErrorSubstring;
    static readonly List<string> patched = [];
    static readonly Harmony harmony = new("scunity.baseline.runtime-probe");
    static int frame = -1, menuFrames;
    static string previousScreen;
    static bool completed;
    static string scenario;
    static string mods;
    static int phase, playingFrames;
    static WorldInfo world;
    static readonly List<object> checks = [];
    static bool sampleEcs;

    [STAThread]
    static int Main(string[] args)
    {
        evidence = Path.GetFullPath(args[0]);
        scenario = args.Length > 1 ? args[1] : "menu";
        mods = args.Length > 2 ? args[2] : "none";
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Directory.CreateDirectory(evidence);
        trace = new StreamWriter(Path.Combine(evidence, "trace.jsonl"));
        try
        {
            Engine.Log.AddLogSink(new ProbeLog());
            Patch(typeof(Storage), "GetDataDirectory", nameof(DataPath));
            Patch(typeof(Silk.NET.Windowing.Window), "Create", nameof(HiddenWindow));
            Patch(typeof(APIUpdateManager), "Initialize", nameof(SkipSideService));
            Patch(typeof(MotdManager), "Update", nameof(SkipSideService));
            Patch(typeof(FileAssociationManager), "Initialize", nameof(SkipSideService));
            Patch(typeof(VrManager), "Initialize", nameof(SkipSideService));
            Patch(typeof(SettingsManager), "Initialize", after: nameof(Settings));
            Patch(typeof(LoadingScreen), "AddLoadAction", nameof(LoadingAction));
            Patch(typeof(GameManager), "LoadProject", after: nameof(Player));
            foreach (Type type in new[] { typeof(Window), typeof(Engine.Time), typeof(Dispatcher), typeof(Display),
                         typeof(Keyboard), typeof(Mouse), typeof(Touch), typeof(GamePad), typeof(Mixer) })
            {
                foreach (string method in new[] { "BeforeFrame", "AfterFrame", "BeforeFrameAll", "AfterFrameAll", "LoadHandler" })
                    if (AccessTools.Method(type, method) is { } target) Trace(target);
            }
            foreach (Type type in new[] { typeof(GameProgram), typeof(PerformanceManager), typeof(MusicManager),
                         typeof(ScreensManager), typeof(DialogsManager), typeof(JsInterface), typeof(ModsManager),
                         typeof(LoadingScreen), typeof(GameManager), typeof(SubsystemUpdate), typeof(SubsystemDrawing) })
            {
                foreach (string method in new[] { "Initialize", "FrameHandler", "Run", "Update", "Draw", "LoadProject", "SaveProject", "DisposeProject" })
                    foreach (var target in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                 .Where(m => m.Name == method && !m.IsAbstract && !m.ContainsGenericParameters)) Trace(target);
            }
            Patch(typeof(ModsManager), "HookAction", nameof(Hook));
            Patch(typeof(ModsManager), "HookActionReverse", nameof(Hook));
            Patch(typeof(Display), "set_RenderTarget", nameof(Target));
            Patch(typeof(Window), "RenderFrameHandler", nameof(FrameBegin), nameof(FrameEnd));
            foreach (Type type in typeof(GameProgram).Assembly.GetTypes().Where(t => !t.IsAbstract && !t.ContainsGenericParameters))
            {
                foreach (string name in new[] { "Update", "Draw" })
                {
                    bool applies = name == "Update" ? typeof(IUpdateable).IsAssignableFrom(type) : typeof(IDrawable).IsAssignableFrom(type);
                    if (!applies) continue;
                    foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => m.Name == name && !m.IsAbstract))
                    {
                        harmony.Patch(method, new HarmonyMethod(typeof(RuntimeProbe), nameof(Ecs)));
                        patched.Add($"{type.FullName}.{method.Name} (ECS sample)");
                    }
                }
            }
            Write("harness", new { scenario, mods, window = "hidden 960x540", dataDirectory = "evidence/data; app:/doc within isolated bin",
                disabledServices = new[] { "APIUpdateManager.Initialize", "MotdManager.Update", "FileAssociationManager.Initialize", "VrManager.Initialize" },
                time = "original wall clock", input = "no injected input; hidden window", entry = "Game.Program.EntryPoint (OS CLI/mutex/IME bootstrap not exercised)",
                patchedMethods = patched });
            GameProgram.EntryPoint();
            if (!completed) throw new InvalidOperationException("Window exited before scenario completed.");
            if (errors.Count > 0) throw new InvalidOperationException("The reference game logged errors; see trace.jsonl.");
            Result("passed", null);
            return 0;
        }
        catch (Exception error)
        {
            Result("failed", error.ToString());
            Console.Error.WriteLine(error);
            return 1;
        }
        finally { trace.Dispose(); }
    }

    static void Patch(Type type, string method, string before = null, string after = null)
    {
        var target = AccessTools.Method(type, method) ?? throw new MissingMethodException(type.FullName, method);
        harmony.Patch(target, before == null ? null : new HarmonyMethod(typeof(RuntimeProbe), before),
            after == null ? null : new HarmonyMethod(typeof(RuntimeProbe), after));
        patched.Add($"{type.FullName}.{method}");
    }
    static void Trace(MethodBase target)
    {
        harmony.Patch(target, new HarmonyMethod(typeof(RuntimeProbe), nameof(Before)), new HarmonyMethod(typeof(RuntimeProbe), nameof(After)));
        patched.Add($"{target.DeclaringType.FullName}.{target.Name}");
    }
    static void Before(MethodBase __originalMethod) => Write("begin", $"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}");
    static void After(MethodBase __originalMethod) => Write("end", $"{__originalMethod.DeclaringType.FullName}.{__originalMethod.Name}");
    static void Hook(string __0, MethodBase __originalMethod) => Write("hook", new { name = __0, reverse = __originalMethod.Name.EndsWith("Reverse") });
    static void Ecs(object __instance, MethodBase __originalMethod, object[] __args)
    {
        if (sampleEcs) Write("ecs-call", new { type = __instance.GetType().FullName, method = __originalMethod.Name,
            entityId = (__instance as GameEntitySystem.Component)?.Entity?.Id,
            updateOrder = (__instance as IUpdateable)?.FloatUpdateOrder,
            drawOrder = __originalMethod.Name == "Draw" && __args.Length > 1 ? __args[1] : null });
    }
    static void LoadingAction(ref Action __0)
    {
        Action original = __0;
        string name = $"{original.Method.DeclaringType.FullName}.{original.Method.Name}";
        __0 = () => { Write("loading-action-begin", name); original(); Write("loading-action-end", name); };
    }
    static void Target(RenderTarget2D __0) => Write("render-target", __0 == null ? "backbuffer" : $"{__0.Width}x{__0.Height}");
    static bool DataPath(bool writeAccess, ref string __result)
    {
        __result = Path.Combine(evidence, "data");
        if (writeAccess) Directory.CreateDirectory(__result);
        return false;
    }
    static void HiddenWindow(ref WindowOptions __0)
    {
        __0.IsVisible = false;
        __0.Size = new Silk.NET.Maths.Vector2D<int>(960, 540);
        __0.FramesPerSecond = 60;
    }
    static bool SkipSideService() => false;
    static void Settings()
    {
        SettingsManager.FileAssociationEnabled = false;
        SettingsManager.UseVr = false;
        SettingsManager.VisibilityRange = 32;
        SettingsManager.SoundsVolume = 0;
        SettingsManager.MusicVolume = 0;
        SettingsManager.MultithreadedTerrainUpdate = false;
        if (mods == "generated") SettingsManager.ModLoadAfters = "scunity.probe.late;scunity.probe.code";
    }
    static void FrameBegin()
    {
        frame++;
        Write("frame", new { frame, engineFrame = Engine.Time.FrameIndex });
    }
    static void FrameEnd()
    {
        string screen = ScreensManager.CurrentScreen?.GetType().Name;
        if (screen != previousScreen)
        {
            Write("screen", screen);
            previousScreen = screen;
            Console.WriteLine($"Frame {frame}: {screen}");
        }
        if (screen == nameof(MainMenuScreen) && phase == 0 && ++menuFrames == 10)
        {
            Write("checkpoint", "main-menu-ready");
            CheckMods();
            if (scenario == "menu") { completed = true; Window.Close(); }
            else
            {
                FileSampleChecks();
                world = WorldsManager.CreateWorld(new WorldSettings { Name = "SCUnity baseline", CustomWorldSeed = true, WorldSeed = 123456,
                    Seed = "123456", GameMode = GameMode.Creative, TerrainGenerationMode = TerrainGenerationMode.FlatContinent,
                    EnvironmentBehaviorMode = EnvironmentBehaviorMode.Static, AreWeatherEffectsEnabled = false,
                    AreSupernaturalCreaturesEnabled = false, AreSeasonsChanging = false });
                Export("new-world", world.DirectoryName);
                phase = 1;
                ScreensManager.SwitchScreen("GameLoading", world, null);
            }
        }
        if (phase == 1 && GameManager.Project != null)
        {
            var players = GameManager.Project.FindSubsystem<SubsystemPlayers>(true);
            if (players.PlayersData.Count > 0 && players.PlayersData[0].IsReadyForPlaying)
            {
                sampleEcs = playingFrames < 3;
                if (++playingFrames == 12)
                {
                    sampleEcs = false;
                    var terrain = GameManager.Project.FindSubsystem<SubsystemTerrain>(true);
                    terrain.ChangeCell(0, 70, 0, 3);
                    GameManager.SaveProject(true, false);
                    string before = PersistentState();
                    File.WriteAllText(Path.Combine(evidence, "before-reload.json"), before);
                    string directory = world.DirectoryName;
                    GameManager.DisposeProject();
                    Export("populated-world", directory);
                    var games = ScreensManager.FindScreen<GameScreen>("Game").Children.Find<ContainerWidget>("GamesWidget");
                    GameManager.LoadProject(WorldsManager.GetWorldInfo(directory), games);
                    string after = PersistentState();
                    File.WriteAllText(Path.Combine(evidence, "after-reload.json"), after);
                    // Terrain chunks load on demand; check persisted cell after the normal updater runs.
                    Require(XElement.Load(Storage.GetSystemPath(Storage.CombinePaths(directory, "Project.xml"))).Name == "Project", "valid-project-after-save");
                    Require(GameManager.Project.FindSubsystem<SubsystemPlayers>(true).PlayersData.Count == 1, "player-restored");
                    Require(GameManager.Project.FindSubsystem<SubsystemGameInfo>(true).WorldSeed == 123456, "seed-restored");
                    phase = 2;
                    playingFrames = 0;
                }
            }
        }
        else if (phase == 2 && GameManager.Project.FindSubsystem<SubsystemPlayers>(true).PlayersData[0].IsReadyForPlaying && ++playingFrames == 5)
        {
            Require(GameManager.Project.FindSubsystem<SubsystemTerrain>(true).Terrain.GetCellValue(0, 70, 0) == 3, "placed-block-restored");
            GameManager.SaveProject(true, false);
            GameManager.DisposeProject();
            Export("reloaded-world", world.DirectoryName);
            completed = true;
            phase = 3;
            Write("checkpoint", "world-save-reload-complete");
            Window.Close();
        }
        if (elapsed.Elapsed.TotalSeconds > 150)
            throw new TimeoutException($"Scenario timed out at {screen}.");
        lock (gate) trace.Flush();
    }
    static void Write(string kind, object data)
    {
        if (kind == "hook" && phase != 0 && !sampleEcs) return;
        lock (gate) trace.WriteLine(JsonSerializer.Serialize(new { frame, kind, data }));
    }
    static void Result(string status, string error)
    {
        var result = new { status, scenario, mods, frameCount = frame + 1,
            engineFrameCount = Engine.Time.FrameIndex, reachedMainMenu = menuFrames >= 10, completed, checks,
            loggedErrors = errors.ToArray(), expectedErrors = expectedErrors.ToArray(), error };
        File.WriteAllText(Path.Combine(evidence, "result.json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
    }
    static void Player()
    {
        if (scenario == "menu") return;
        var players = GameManager.Project.FindSubsystem<SubsystemPlayers>(true);
        if (players.PlayersData.Count == 0)
            players.AddPlayerData(new PlayerData(GameManager.Project) { Name = "BaselinePlayer", InputDevice = WidgetInputDevice.None,
                SpawnPosition = new Engine.Vector3(0, 66, 0), CharacterSkinName = "$Male1" });
    }
    static void CheckMods()
    {
        Write("mods", ModsManager.ModListAll.Select(m => new { name = m.modInfo?.PackageName, m.IsDisabled, reason = m.DisableReason.ToString(),
            blockTypes = m.BlockTypes.Select(t => t.FullName), loaders = m.Loaders.Select(l => l.GetType().FullName) }));
        if (mods == "community")
        {
            int expected = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Mods"), "*.scmod", SearchOption.AllDirectories).Length;
            Require(ModsManager.ModListAll.Count(m => m.modInfo?.PackageName is not ("survivalcraft" or "fastdebug")) == expected,
                "all-community-mods-discovered");
            Require(ModsManager.ModListAll.All(m => !m.IsDisabled), "community-mods-enabled");
            string vertex = ContentManager.Get<string>("Shaders/RealmPonderBlocks", ".vsh");
            string pixel = ContentManager.Get<string>("Shaders/RealmPonderBlocks", ".psh");
            using var opaque = new Shader(vertex, pixel);
            using var alpha = new Shader(vertex, pixel, new ShaderMacro("ALPHATESTED"));
            Require(true, "realm-custom-shader-opaque-and-alpha-compiled");
        }
        if (mods != "generated") return;
        Type probe = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Game.PortProbeLoader")).FirstOrDefault(t => t != null);
        Require(probe != null, "mod-assembly-loaded-from-scmod");
        var results = (Dictionary<string, bool>)probe.GetField("Results").GetValue(null);
        string[] required = ["modloader-initialized", "game-method-before-patch", "harmony-game-method", "harmony-constructor",
            "harmony-virtual", "harmony-generic", "loading-finished-hook", "resource-override", "custom-reader",
            "block-discovered", "typecache", "language-merge", "database-xml-merge", "dependency-order", "load-after-order"];
        foreach (string key in required) Require(results.TryGetValue(key, out bool passed) && passed, key);
        object engine = typeof(JsInterface).GetField("engine").GetValue(null);
        var getValue = engine.GetType().GetMethod("GetValue", [typeof(string)]);
        Require(getValue.Invoke(engine, ["portProbeScript"]).ToString() == "17", "javascript-initialized");
        Require(int.Parse(getValue.Invoke(engine, ["portProbeFrames"]).ToString()) > 0, "javascript-frame-handler");
        Require(expectedErrors.Count(e => e.Contains("BrokenForBaseline.dll") && e.Contains("BadImageFormatException")) == 1,
            "invalid-dll-isolated-from-valid-mod");
        GraphChecks();
    }
    static void GraphChecks()
    {
        var saved = ModsManager.ModListAll.ToArray();
        var cache = ModsManager.PackageNameToModEntity.ToArray();
        ModEntity Mod(string name, int order = 0, string dependency = null)
        {
            var metadata = new Dictionary<string, object> { ["Name"] = name, ["Version"] = "1.0.0", ["ApiVersion"] = "1.9",
                ["PackageName"] = "graph." + name, ["LoadOrder"] = order };
            if (dependency != null) metadata["Dependencies"] = new Dictionary<string, string> { [dependency] = "[2.0.0,3.0.0)" };
            return new ModEntity { modInfo = ModsManager.DeserializeJson(JsonSerializer.Serialize(metadata)) };
        }
        try
        {
            var a = Mod("a"); var b = Mod("b"); var c = Mod("c");
            ModsManager.ModListAll.Clear(); ModsManager.ModListAll.AddRange([a, b, c]);
            ModsManager.SortModListAll();
            Require(ModsManager.ModListAll.SequenceEqual(new[] { a, b, c }), "stable-equal-priority-order");
            a.LoadAfter = "graph.b"; b.LoadAfter = "graph.a";
            ModsManager.SortModListAll();
            Require(ReferenceEquals(ModsManager.ModListAll[0], c) && ReferenceEquals(ModsManager.ModListAll[1], a)
                && ReferenceEquals(ModsManager.ModListAll[2], b), "cycle-falls-back-without-dropping-mods");
            var duplicate = Mod("a");
            a.LoadAfter = null;
            ModsManager.ModListAll.Clear(); ModsManager.ModListAll.AddRange([a, duplicate]);
            ModsManager.SortModListAll();
            Require(ModsManager.ModListAll.Count == 2 && ReferenceEquals(ModsManager.ModListAll[1], duplicate), "same-package-distinct-instances-retained");
            foreach (string dependency in new[] { "graph.absent", "graph.a" })
            {
                var invalid = Mod("invalid", dependency: dependency);
                ModsManager.ModListAll.Clear(); ModsManager.ModListAll.AddRange([a, invalid]);
                int count = expectedErrors.Count;
                expectedErrorSubstring = "Failed to find dependency: Package Name";
                try { invalid.CheckDependencies(new List<ModEntity>()); }
                finally { expectedErrorSubstring = null; }
                Require(invalid.IsDisabled && invalid.DisableReason == ModDisableReason.DependencyError && expectedErrors.Count == count + 1,
                    dependency.EndsWith("absent") ? "missing-dependency-disabled" : "incompatible-version-disabled");
            }
        }
        finally
        {
            ModsManager.ModListAll.Clear(); ModsManager.ModListAll.AddRange(saved);
            ModsManager.PackageNameToModEntity.Clear();
            foreach (var pair in cache) ModsManager.PackageNameToModEntity.Add(pair.Key, pair.Value);
        }
    }
    static void Require(bool value, string check)
    {
        checks.Add(new { check, passed = value });
        if (!value) throw new InvalidOperationException($"Failed: {check}");
    }
    static void Export(string name, string directory)
    {
        using var stream = File.Create(Path.Combine(evidence, name + ".scworld"));
        WorldsManager.ExportWorld(directory, stream);
        Write("checkpoint", name);
    }
    static string PersistentState()
    {
        var project = GameManager.Project;
        var info = project.FindSubsystem<SubsystemGameInfo>(true);
        var players = project.FindSubsystem<SubsystemPlayers>(true);
        return JsonSerializer.Serialize(new { seed = info.WorldSeed, world = info.WorldSettings.Name,
            players = players.PlayersData.Select(p => new { p.Name, p.PlayerIndex, p.Level, p.SpawnsCount, p.CharacterSkinName }),
            entities = project.Entities.Count, allocatedChunks = project.FindSubsystem<SubsystemTerrain>(true).Terrain.AllocatedChunks.Length },
            new JsonSerializerOptions { WriteIndented = true });
    }
    static void FileSampleChecks()
    {
        string input = Path.Combine(evidence, "input");
        if (!Directory.Exists(input)) return;
        foreach (string archive in Directory.GetFiles(input, "*.scworld", SearchOption.AllDirectories))
        {
            using var stream = File.OpenRead(archive);
            string directory = WorldsManager.ImportWorld(stream);
            string path = Storage.GetSystemPath(Storage.CombinePaths(directory, "Project.xml"));
            string version = XElement.Load(path).Attribute("Version")?.Value;
            VersionsManager.UpgradeWorld(directory);
            Require(XElement.Load(path).Attribute("Version")?.Value == VersionsManager.SerializationVersion, $"upgrade:{Path.GetFileName(archive)}:{version}");
            WorldsManager.MakeQuickWorldBackup(directory);
            string original = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
            File.WriteAllText(path, "<deliberately-corrupted");
            WorldsManager.RepairWorldIfNeeded(directory);
            Require(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) == original, $"backup-recovery:{Path.GetFileName(archive)}");
            Export("upgraded-" + Path.GetFileNameWithoutExtension(archive), directory);
        }
    }
    sealed class ProbeLog : ILogSink
    {
        public void Log(LogType type, string message)
        {
            Write("log", new { type = type.ToString(), message });
            if (type == LogType.Error)
            {
                bool expected = (expectedErrorSubstring != null && message.Contains(expectedErrorSubstring)) ||
                    (mods == "generated" && message.Contains("BrokenForBaseline.dll") && message.Contains("BadImageFormatException"));
                lock (gate) (expected ? expectedErrors : errors).Add(message);
                Console.WriteLine(message);
            }
        }
    }
}
