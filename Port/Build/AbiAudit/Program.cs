using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;

// Metadata inspection only: do not create a GL context or invoke the native API.
static string NativeType(Type type) => type.IsPointer ? "IntPtr"
    : type.IsEnum ? NativeType(Enum.GetUnderlyingType(type))
    : type == typeof(void) ? "void" : type == typeof(bool) ? "byte" : type.FullName!;
static bool Native(Type type) => type.IsPointer || type.IsEnum || type.IsPrimitive
    || type == typeof(IntPtr) || type == typeof(UIntPtr) || type == typeof(void);

var names = File.ReadAllLines(args[0]).ToHashSet();
var inspectedAssembly = Assembly.LoadFile(Path.GetFullPath(args[3]));
AssemblyLoadContext.GetLoadContext(inspectedAssembly)!.Resolving += (context, name) => {
    string dependency = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[3]))!, name.Name + ".dll");
    return File.Exists(dependency) ? context.LoadFromAssemblyPath(dependency) : null;
};
var inspectedType = inspectedAssembly.GetType("Silk.NET.OpenGLES.GL", true)!;
var signatures = inspectedType.GetMethods()
    .Where(method => !method.ContainsGenericParameters && Native(method.ReturnType)
        && method.GetParameters().All(parameter => Native(parameter.ParameterType)))
    .Select(method => (method, attribute: method.GetCustomAttributesData()
        .FirstOrDefault(attribute => attribute.AttributeType.Name == "NativeApiAttribute")))
    .Where(item => item.attribute != null)
    .Select(item => (item.method, entry: (string)item.attribute!.NamedArguments
        .First(argument => argument.MemberName == "EntryPoint").TypedValue.Value!))
    .Where(item => names.Contains(item.entry))
    .GroupBy(item => item.entry)
    .ToDictionary(group => group.Key, group => {
        var method = group.First().method;
        return new[] { NativeType(method.ReturnType) }.Concat(method.GetParameters()
            .Select(parameter => NativeType(parameter.ParameterType))).ToArray();
    });
File.WriteAllText(args[1], JsonSerializer.Serialize(new {
    assemblyVersion = inspectedAssembly.GetName().Version!.ToString(),
    alAssemblyVersion = AssemblyName.GetAssemblyName(args[2]).Version!.ToString(), signatures
}, new JsonSerializerOptions { WriteIndented = true }));
