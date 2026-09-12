using System.Reflection;
using System.Runtime.CompilerServices;
using Engine;
using Game;
using HarmonyLib;
using Jint;

namespace SCUnity.Probe;

public static class Entry
{
    static int gamePatchCalls;

    public static Dictionary<string, bool> Run(string payloadDirectory)
    {
        var checks = new Dictionary<string, bool>();
        void Check(string name, bool passed)
        {
            checks.Add(name, passed);
            if (!passed) throw new InvalidOperationException("Failed assertion: " + name);
        }

        string pluginName = "SCUnity.Probe.Plugin";
        string dependencyName = "SCUnity.Probe.Dependency";
        Check("payload-not-preloaded", !AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == pluginName || a.GetName().Name == dependencyName));
        bool loadEvent = false, resolved = false;
        AssemblyLoadEventHandler observe = (_, e) => { if (e.LoadedAssembly.GetName().Name == pluginName) loadEvent = true; };
        ResolveEventHandler resolver = (_, e) =>
        {
            if (new AssemblyName(e.Name).Name != dependencyName) return null;
            resolved = true;
            return Assembly.Load(File.ReadAllBytes(Path.Combine(payloadDirectory, dependencyName + ".dll.bytes")));
        };
        AppDomain.CurrentDomain.AssemblyLoad += observe;
        AppDomain.CurrentDomain.AssemblyResolve += resolver;
        try
        {
            var plugin = Assembly.Load(File.ReadAllBytes(Path.Combine(payloadDirectory, pluginName + ".dll.bytes")));
            Check("assembly-load-bytes", plugin.GetName().Name == pluginName && string.IsNullOrEmpty(plugin.Location));
            Check("assembly-load-event", loadEvent);
            Check("assembly-enumeration", AppDomain.CurrentDomain.GetAssemblies().Contains(plugin));
            Type[] extensions = plugin.GetTypes().Where(t => !t.IsAbstract && typeof(ProbeExtension).IsAssignableFrom(t)).ToArray();
            Check("derived-type-discovery", extensions.Length == 1);
            var extension = (ProbeExtension)Activator.CreateInstance(extensions[0]);
            Check("dynamic-initialization", extension.Initialize() == "resolved-from-bytes");
            Check("assembly-resolve-bytes", resolved);
            Check("dynamic-type-lookup", plugin.GetType(extensions[0].FullName) == extensions[0]);
        }
        finally
        {
            AppDomain.CurrentDomain.AssemblyLoad -= observe;
            AppDomain.CurrentDomain.AssemblyResolve -= resolver;
        }

        var js = new Jint.Engine(options => options.AllowClr(typeof(StateMachine).Assembly));
        Check("jint-evaluate", js.Evaluate("const f = x => x * 2; f(8) + 1").AsNumber() == 17);
        js.SetValue("hostAdd", new Func<int, int, int>((a, b) => a + b));
        Check("jint-host-callback", js.Evaluate("hostAdd(4, 5)").AsNumber() == 9);
        Check("jint-clr-interop", js.Evaluate("var g = importNamespace('Game'); var s = new g.StateMachine(); s.CurrentState === null").AsBoolean());
        js.Execute("var frames = 0; function frame() { frames++; }");
        for (int i = 0; i < 3; ++i) js.Invoke("frame");
        Check("jint-repeated-callback", js.GetValue("frames").AsNumber() == 3);

        var list = new List<int> { 3, 1, 2 };
        var readOnly = new ReadOnlyList<int>(list);
        Check("upstream-readonly-enumeration", readOnly.SequenceEqual(list));
        bool rejected = false;
        try { readOnly.Add(4); } catch (NotSupportedException) { rejected = true; }
        Check("upstream-readonly-mutation-rejected", rejected);
        Check("upstream-collection-select", CollectionUtils.SelectNth(list, 1, Comparer<int>.Default) == 2);
        var events = new List<string>();
        var state = new StateMachine();
        state.AddState("a", () => events.Add("enter-a"), () => events.Add("update-a"), () => events.Add("leave-a"));
        state.AddState("b", () => events.Add("enter-b"), null, null);
        state.OnTransitionTo += name => events.Add("transition-" + name);
        state.TransitionTo("a"); state.Update(); state.TransitionTo("b");
        Check("upstream-state-machine-order", string.Join(",", events) == "enter-a,transition-a,update-a,leave-a,enter-b,transition-b");
        Check("upstream-state-machine-history", state.CurrentState == "b" && state.PreviousState == "a");

        var harmony = new Harmony("scunity.mono.capability-probe");
        var constructor = AccessTools.Constructor(typeof(PatchTarget), new[] { typeof(int) });
        var virtualMethod = AccessTools.Method(typeof(PatchTarget), nameof(PatchTarget.Read));
        var genericMethod = AccessTools.Method(typeof(PatchTarget), nameof(PatchTarget.Echo)).MakeGenericMethod(typeof(int));
        // The whole StateMachine source is upstream bytes; no NoInlining edits to the game.
        var gameMethod = AccessTools.Method(typeof(StateMachine), nameof(StateMachine.Update));
        try
        {
            harmony.Patch(constructor, postfix: new HarmonyMethod(typeof(Entry), nameof(ConstructorPostfix)));
            harmony.Patch(virtualMethod, postfix: new HarmonyMethod(typeof(Entry), nameof(VirtualPostfix)));
            harmony.Patch(genericMethod, postfix: new HarmonyMethod(typeof(Entry), nameof(GenericPostfix)));
            harmony.Patch(gameMethod, postfix: new HarmonyMethod(typeof(Entry), nameof(GamePostfix)));
            Check("harmony-constructor", new PatchTarget(3).Value == 7);
            Check("harmony-virtual", CallVirtual(new PatchTarget(3)) == 12);
            Check("harmony-closed-generic", new PatchTarget(0).Echo(2) == 22);
            gamePatchCalls = 0;
            state.Update();
            Check("harmony-upstream-game-method", gamePatchCalls == 1);
        }
        finally { harmony.UnpatchSelf(); }
        Check("harmony-unpatch-constructor", new PatchTarget(3).Value == 3);
        Check("harmony-unpatch-virtual", CallVirtual(new PatchTarget(3)) == 3);
        Check("harmony-unpatch-generic", new PatchTarget(0).Echo(2) == 2);
        gamePatchCalls = 0;
        state.Update();
        Check("harmony-unpatch-game-method", gamePatchCalls == 0);
        return checks;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int CallVirtual(PatchTarget target) => target.Read();
    static void ConstructorPostfix(PatchTarget __instance) => __instance.Value += 4;
    static void VirtualPostfix(ref int __result) => __result += 5;
    static void GenericPostfix(ref int __result) => __result += 20;
    static void GamePostfix() => ++gamePatchCalls;
}

public class PatchTarget
{
    public int Value;
    [MethodImpl(MethodImplOptions.NoInlining)] public PatchTarget(int value) => Value = value;
    [MethodImpl(MethodImplOptions.NoInlining)] public virtual int Read() => Value;
    [MethodImpl(MethodImplOptions.NoInlining)] public T Echo<T>(T value) => value;
}
