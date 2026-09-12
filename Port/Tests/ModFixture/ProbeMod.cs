using System.Reflection;
using System.Runtime.CompilerServices;
using Engine.Serialization;
using HarmonyLib;

namespace Game;

// Test-only ModLoader follows the official template's initialization/hook pattern.
// This assembly is packed into a real .scmod and must be discovered by the original loader.
public class PortProbeLoader : ModLoader
{
    public static readonly Dictionary<string, bool> Results = [];
    public override void __ModInitialize()
    {
        Results["modloader-initialized"] = true;
        ModsManager.RegisterHook("OnLoadingFinished", this);
        var harmony = new Harmony("scunity.probe.mod");
        harmony.Patch(AccessTools.Constructor(typeof(PortPatchTarget), [typeof(int)]), postfix: new HarmonyMethod(typeof(PortProbeLoader), nameof(Constructor)));
        harmony.Patch(AccessTools.Method(typeof(PortPatchTarget), nameof(PortPatchTarget.Read)), postfix: new HarmonyMethod(typeof(PortProbeLoader), nameof(Virtual)));
        harmony.Patch(AccessTools.Method(typeof(PortPatchTarget), nameof(PortPatchTarget.Echo)).MakeGenericMethod(typeof(int)), postfix: new HarmonyMethod(typeof(PortProbeLoader), nameof(Generic)));
        MethodInfo gameMethod = AccessTools.Method(typeof(WorldsManager), nameof(WorldsManager.ValidateWorldName));
        Results["game-method-before-patch"] = (bool)gameMethod.Invoke(null, ["__scunity_probe__"]);
        harmony.Patch(gameMethod, postfix: new HarmonyMethod(typeof(PortProbeLoader), nameof(GameMethod)));
        Results["harmony-game-method"] = !(bool)gameMethod.Invoke(null, ["__scunity_probe__"]);
        harmony.Unpatch(gameMethod, HarmonyPatchType.All, harmony.Id);
        var target = new PortPatchTarget(3);
        Results["harmony-constructor"] = target.Value == 7;
        Results["harmony-virtual"] = target.Read() == 12;
        Results["harmony-generic"] = target.Echo(2) == 22;
    }
    public override void OnLoadingFinished(List<Action> actions)
    {
        Results["loading-finished-hook"] = true;
        actions.Add(() =>
        {
            Results["resource-override"] = ContentManager.Get<string>("PortProbe/winner") == "late";
            Results["custom-reader"] = ContentManager.Get<PortProbePayload>("PortProbe/payload").Value == "decoded:payload";
            Results["block-discovered"] = BlocksManager.GetBlockIndex<PortProbeBlock>() > 0;
            Results["typecache"] = TypeCache.FindType(typeof(PortProbeBlock).FullName, false, true) == typeof(PortProbeBlock);
            Results["language-merge"] = LanguageControl.Get("PortProbe", "Value") == "late";
            Results["database-xml-merge"] = (Engine.Color)DatabaseManager.GameDatabase.Database.FindDatabaseObject(
                new Guid("480fe1d2-2474-4fa9-a984-9a999d61b1c9"), null, true).Value == Engine.Color.Black;
            var order = ModsManager.ModList.Select(m => m.modInfo.PackageName).ToList();
            Results["dependency-order"] = order.IndexOf("scunity.probe.resources") < order.IndexOf("scunity.probe.code");
            Results["load-after-order"] = order.IndexOf("scunity.probe.code") < order.IndexOf("scunity.probe.late");
        });
    }
    static void Constructor(PortPatchTarget __instance) => __instance.Value += 4;
    static void Virtual(ref int __result) => __result += 5;
    static void Generic(ref int __result) => __result += 20;
    static void GameMethod(string name, ref bool __result) { if (name == "__scunity_probe__") __result = false; }
}

public class PortProbePayload { public string Value; }
public class PortProbeReader : IContentReader.IContentReader
{
    public override string Type => typeof(PortProbePayload).FullName;
    public override string[] DefaultSuffix => ["probe"];
    public override object Get(ContentInfo[] contents)
        => new PortProbePayload { Value = "decoded:" + new StreamReader(contents[0].Duplicate()).ReadToEnd() };
}
public class PortProbeBlock : CubeBlock { }
public class PortPatchTarget
{
    public int Value;
    [MethodImpl(MethodImplOptions.NoInlining)] public PortPatchTarget(int value) => Value = value;
    [MethodImpl(MethodImplOptions.NoInlining)] public virtual int Read() => Value;
    [MethodImpl(MethodImplOptions.NoInlining)] public T Echo<T>(T value) => value;
}
