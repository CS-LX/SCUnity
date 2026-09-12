using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;

// Reproduce the bounds check without creating a graphics context or uploading data.
var assembly = Assembly.LoadFrom(Path.GetFullPath(args[0]));
var type = assembly.GetType("Engine.Graphics.IndexBuffer", true)!;
var format = assembly.GetType("Engine.Graphics.IndexFormat", true)!;
var buffer = RuntimeHelpers.GetUninitializedObject(type);
GC.SuppressFinalize(buffer);
type.GetProperty("IndicesCount")!.SetValue(buffer, 6);
type.GetProperty("IndexFormat")!.SetValue(buffer, Enum.Parse(format, "SixteenBits"));
var method = type.GetMethod("VerifyParametersSetData", BindingFlags.NonPublic | BindingFlags.Instance)!;
var checks = new Dictionary<string, string>();
foreach (var (name, values, count, offset) in new (string, Array, int, int)[] {
    ("six-int-indices-into-six-ushort-slots", new int[] { 0, 1, 2, 2, 1, 3 }, 6, 0),
    ("six-uint-indices-into-six-ushort-slots", new uint[] { 0, 1, 2, 2, 1, 3 }, 6, 0),
    ("six-ushort-indices-into-six-ushort-slots", new ushort[] { 0, 1, 2, 2, 1, 3 }, 6, 0),
    ("seven-indices-overflow-six-slots", new int[7], 7, 0),
    ("offset-overflow", new int[2], 2, 5)
}) {
    try {
        method.MakeGenericMethod(values.GetType().GetElementType()!).Invoke(buffer, new object[] { values, 0, count, offset });
        checks[name] = "accepted";
    }
    catch (TargetInvocationException error) { checks[name] = error.InnerException!.GetType().Name; }
}
File.WriteAllText(args[1], JsonSerializer.Serialize(checks, new JsonSerializerOptions { WriteIndented = true }));
bool fixedBehavior = args.Length > 2 && args[2] == "fixed";
if (checks["six-int-indices-into-six-ushort-slots"] != (fixedBehavior ? "accepted" : "ArgumentException") ||
    checks["six-uint-indices-into-six-ushort-slots"] != (fixedBehavior ? "accepted" : "ArgumentException") ||
    checks["six-ushort-indices-into-six-ushort-slots"] != "accepted" ||
    checks["seven-indices-overflow-six-slots"] != "ArgumentException" || checks["offset-overflow"] != "ArgumentException")
    throw new Exception("Index buffer regression did not reproduce the expected behavior.");
