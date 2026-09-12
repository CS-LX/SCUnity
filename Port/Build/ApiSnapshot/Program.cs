using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.Json;

// Read PE metadata only. Never load game assemblies, resolve their dependencies,
// initialize modules, or call game/native code while establishing the reference API.
if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: ApiSnapshot <output.txt> <assembly.dll> [...]");
    return 2;
}
try
{
    var lines = new List<string> { "# Survivalcraft Windows API metadata snapshot v1",
        "# Includes externally visible types, public/protected members, attributes, constraints and layout.",
        "# Assembly scopes are retained; net48 retargeting will require a separate reviewed comparison policy." };
    var summaries = new List<object>();
    foreach (string file in args.Skip(1).Order(StringComparer.Ordinal))
    {
        using var stream = File.OpenRead(file);
        using var pe = new PEReader(stream);
        var reader = pe.GetMetadataReader();
        var snapshot = new Snapshot(reader);
        lines.AddRange(snapshot.Read());
        summaries.Add(snapshot.Summary());
    }
    File.WriteAllText(args[0], string.Join('\n', lines) + "\n", new UTF8Encoding(false));
    string summaryFile = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[0]))!, "PublicApi.Summary.json");
    File.WriteAllText(summaryFile, JsonSerializer.Serialize(new { schemaVersion = 1, assemblies = summaries },
        new JsonSerializerOptions { WriteIndented = true }) + "\n", new UTF8Encoding(false));
    Console.WriteLine($"Wrote {lines.Count} API metadata records.");
    return 0;
}
catch (Exception error)
{
    Console.Error.WriteLine(error);
    return 1;
}

sealed class Snapshot(MetadataReader reader)
{
    readonly SignatureNames names = new(reader);
    readonly List<string> lines = [];
    int types, methods, fields, properties, events, layoutFields;
    string Str(StringHandle h) => reader.GetString(h);
    string Blob(BlobHandle h) => h.IsNil ? "-" : Convert.ToHexString(reader.GetBlobBytes(h));
    static string Text(string value) => JsonSerializer.Serialize(value);
    public object Summary() => new { name = Str(reader.GetAssemblyDefinition().Name),
        version = reader.GetAssemblyDefinition().Version.ToString(), externallyVisibleTypes = types,
        methods, fields, properties, events, nonPublicLayoutFields = layoutFields, metadataRecords = lines.Count };

    bool Visible(TypeDefinitionHandle handle)
    {
        var type = reader.GetTypeDefinition(handle);
        var visibility = type.Attributes & TypeAttributes.VisibilityMask;
        return visibility == TypeAttributes.Public ||
            ((visibility is TypeAttributes.NestedPublic or TypeAttributes.NestedFamily or TypeAttributes.NestedFamORAssem)
             && Visible(type.GetDeclaringType()));
    }
    static bool Visible(MethodAttributes attributes) => (attributes & MethodAttributes.MemberAccessMask)
        is MethodAttributes.Public or MethodAttributes.Family or MethodAttributes.FamORAssem;
    static bool Visible(FieldAttributes attributes) => (attributes & FieldAttributes.FieldAccessMask)
        is FieldAttributes.Public or FieldAttributes.Family or FieldAttributes.FamORAssem;
    bool Visible(MethodDefinitionHandle handle) => !handle.IsNil && Visible(reader.GetMethodDefinition(handle).Attributes);

    string Constant(ConstantHandle handle)
    {
        if (handle.IsNil) return "-";
        var value = reader.GetConstant(handle);
        return $"{value.TypeCode}:{Blob(value.Value)}";
    }
    string MethodName(EntityHandle handle) => handle.Kind switch
    {
        HandleKind.MethodDefinition => DefinitionName((MethodDefinitionHandle)handle),
        HandleKind.MemberReference => ReferenceName((MemberReferenceHandle)handle),
        _ => throw new NotSupportedException($"Method handle kind {handle.Kind}")
    };
    string DefinitionName(MethodDefinitionHandle handle)
    {
        var method = reader.GetMethodDefinition(handle);
        return $"{names.Type(method.GetDeclaringType())}::{Str(method.Name)} {SignatureNames.Method(method.DecodeSignature(names, (object?)null))}";
    }
    string ReferenceName(MemberReferenceHandle handle)
    {
        var member = reader.GetMemberReference(handle);
        return $"{names.Type(member.Parent)}::{Str(member.Name)} {SignatureNames.Method(member.DecodeMethodSignature(names, (object?)null))}";
    }
    void Attributes(string owner, CustomAttributeHandleCollection handles)
    {
        foreach (var handle in handles)
        {
            var attribute = reader.GetCustomAttribute(handle);
            lines.Add($"attribute {owner} {MethodName(attribute.Constructor)} value={Blob(attribute.Value)}");
        }
    }
    void Generics(string owner, GenericParameterHandleCollection handles)
    {
        foreach (var handle in handles)
        {
            var parameter = reader.GetGenericParameter(handle);
            string key = $"{owner} generic[{parameter.Index}]";
            var constraints = parameter.GetConstraints().Select(h => names.Type(reader.GetGenericParameterConstraint(h).Type))
                .Order(StringComparer.Ordinal);
            lines.Add($"generic {key} name={Text(Str(parameter.Name))} flags={parameter.Attributes} constraints={string.Join(';', constraints)}");
            Attributes(key, parameter.GetCustomAttributes());
        }
    }
    void Security(string owner, DeclarativeSecurityAttributeHandleCollection handles)
    {
        foreach (var handle in handles)
        {
            var security = reader.GetDeclarativeSecurityAttribute(handle);
            lines.Add($"security {owner} action={security.Action} permissions={Blob(security.PermissionSet)}");
        }
    }
    public IEnumerable<string> Read()
    {
        var assembly = reader.GetAssemblyDefinition();
        string identity = Str(assembly.Name);
        lines.Add($"assembly {identity} version={assembly.Version} culture={Text(Str(assembly.Culture))} publicKey={Blob(assembly.PublicKey)} flags={assembly.Flags}");
        Attributes(identity, assembly.GetCustomAttributes());
        Security(identity, assembly.GetDeclarativeSecurityAttributes());
        foreach (var handle in reader.ExportedTypes)
        {
            var exported = reader.GetExportedType(handle);
            lines.Add($"exported {Str(exported.Namespace)}.{Str(exported.Name)} flags={exported.Attributes} implementation={names.Scope(exported.Implementation)}");
        }
        foreach (var handle in reader.TypeDefinitions)
        {
            if (!Visible(handle)) continue;
            types++;
            var type = reader.GetTypeDefinition(handle);
            string owner = names.Type(handle);
            var layout = type.GetLayout();
            var interfaces = type.GetInterfaceImplementations().Select(h => names.Type(reader.GetInterfaceImplementation(h).Interface))
                .Order(StringComparer.Ordinal);
            lines.Add($"type {owner} flags={type.Attributes} base={names.Type(type.BaseType)} interfaces={string.Join(';', interfaces)} pack={layout.PackingSize} size={layout.Size}");
            Attributes(owner, type.GetCustomAttributes());
            Generics(owner, type.GetGenericParameters());
            Security(owner, type.GetDeclarativeSecurityAttributes());
            bool hasLayout = (type.Attributes & TypeAttributes.LayoutMask) != TypeAttributes.AutoLayout;
            bool sequential = (type.Attributes & TypeAttributes.LayoutMask) == TypeAttributes.SequentialLayout;
            int instanceFieldIndex = 0;
            foreach (var fieldHandle in type.GetFields())
            {
                var field = reader.GetFieldDefinition(fieldHandle);
                bool instance = (field.Attributes & FieldAttributes.Static) == 0;
                // Sequential fields have no explicit offsets in metadata. Their declaration
                // order must survive sorting, otherwise swapped fields would evade the gate.
                string order = sequential && instance ? $" layoutIndex={instanceFieldIndex++}" : "";
                bool visible = Visible(field.Attributes);
                // Private instance fields also affect a sequential/explicit type's binary layout.
                if (!visible && (!hasLayout || (field.Attributes & FieldAttributes.Static) != 0)) continue;
                if (visible) fields++; else layoutFields++;
                string key = $"{owner}::{Str(field.Name)}";
                lines.Add($"{(visible ? "field" : "layout-field")} {key} type={field.DecodeSignature(names, (object?)null)} flags={field.Attributes} offset={field.GetOffset()}{order} constant={Constant(field.GetDefaultValue())} marshal={Blob(field.GetMarshallingDescriptor())}");
                Attributes(key, field.GetCustomAttributes());
            }
            foreach (var methodHandle in type.GetMethods())
            {
                var method = reader.GetMethodDefinition(methodHandle);
                if (!Visible(method.Attributes)) continue;
                methods++;
                string key = DefinitionName(methodHandle);
                lines.Add($"method {key} flags={method.Attributes} impl={method.ImplAttributes}");
                Attributes(key, method.GetCustomAttributes());
                Generics(key, method.GetGenericParameters());
                Security(key, method.GetDeclarativeSecurityAttributes());
                foreach (var parameterHandle in method.GetParameters())
                {
                    var parameter = reader.GetParameter(parameterHandle);
                    string parameterKey = $"{key} parameter[{parameter.SequenceNumber}]";
                    lines.Add($"parameter {parameterKey} name={Text(Str(parameter.Name))} flags={parameter.Attributes} default={Constant(parameter.GetDefaultValue())} marshal={Blob(parameter.GetMarshallingDescriptor())}");
                    Attributes(parameterKey, parameter.GetCustomAttributes());
                }
                if ((method.Attributes & MethodAttributes.PinvokeImpl) != 0)
                {
                    var import = method.GetImport();
                    lines.Add($"pinvoke {key} module={Text(Str(reader.GetModuleReference(import.Module).Name))} name={Text(Str(import.Name))} flags={import.Attributes}");
                }
            }
            foreach (var propertyHandle in type.GetProperties())
            {
                var property = reader.GetPropertyDefinition(propertyHandle);
                var access = property.GetAccessors();
                if (!Visible(access.Getter) && !Visible(access.Setter) && !access.Others.Any(Visible)) continue;
                properties++;
                string key = $"{owner}::{Str(property.Name)} {SignatureNames.Method(property.DecodeSignature(names, (object?)null))}";
                lines.Add($"property {key} flags={property.Attributes} default={Constant(property.GetDefaultValue())} get={Access(access.Getter)} set={Access(access.Setter)} others={string.Join(';', access.Others.Select(Access).Order(StringComparer.Ordinal))}");
                Attributes(key, property.GetCustomAttributes());
            }
            foreach (var eventHandle in type.GetEvents())
            {
                var evt = reader.GetEventDefinition(eventHandle);
                var access = evt.GetAccessors();
                if (!Visible(access.Adder) && !Visible(access.Remover) && !Visible(access.Raiser) && !access.Others.Any(Visible)) continue;
                events++;
                string key = $"{owner}::{Str(evt.Name)}";
                lines.Add($"event {key} type={names.Type(evt.Type)} flags={evt.Attributes} add={Access(access.Adder)} remove={Access(access.Remover)} raise={Access(access.Raiser)} others={string.Join(';', access.Others.Select(Access).Order(StringComparer.Ordinal))}");
                Attributes(key, evt.GetCustomAttributes());
            }
            foreach (var implementationHandle in type.GetMethodImplementations())
            {
                var impl = reader.GetMethodImplementation(implementationHandle);
                lines.Add($"override {owner} declaration={MethodName(impl.MethodDeclaration)} body={MethodName(impl.MethodBody)}");
            }
        }
        lines.Sort(StringComparer.Ordinal);
        return lines;
    }
    string Access(MethodDefinitionHandle handle) => handle.IsNil ? "-" :
        $"{Str(reader.GetMethodDefinition(handle).Name)}:{reader.GetMethodDefinition(handle).Attributes}";
}

sealed class SignatureNames(MetadataReader reader) : ISignatureTypeProvider<string, object?>
{
    public string Type(EntityHandle handle)
    {
        if (handle.IsNil) return "-";
        return handle.Kind switch
        {
            HandleKind.TypeDefinition => GetTypeFromDefinition(reader, (TypeDefinitionHandle)handle, 0),
            HandleKind.TypeReference => GetTypeFromReference(reader, (TypeReferenceHandle)handle, 0),
            HandleKind.TypeSpecification => GetTypeFromSpecification(reader, null, (TypeSpecificationHandle)handle, 0),
            _ => throw new NotSupportedException($"Type handle kind {handle.Kind}")
        };
    }
    public string Scope(EntityHandle handle) => handle.Kind switch
    {
        HandleKind.AssemblyReference => "[" + reader.GetString(reader.GetAssemblyReference((AssemblyReferenceHandle)handle).Name) + "]",
        HandleKind.ModuleReference => "[module:" + reader.GetString(reader.GetModuleReference((ModuleReferenceHandle)handle).Name) + "]",
        HandleKind.ModuleDefinition => "[" + reader.GetString(reader.GetAssemblyDefinition().Name) + "]",
        HandleKind.TypeReference => Type(handle) + "+",
        HandleKind.ExportedType => "export:" + reader.GetString(reader.GetExportedType((ExportedTypeHandle)handle).Name),
        HandleKind.AssemblyFile => "file:" + reader.GetString(reader.GetAssemblyFile((AssemblyFileHandle)handle).Name),
        _ => throw new NotSupportedException($"Scope handle kind {handle.Kind}")
    };
    static string Qualified(MetadataReader r, StringHandle ns, StringHandle name)
        => (ns.IsNil || r.GetString(ns).Length == 0 ? "" : r.GetString(ns) + ".") + r.GetString(name);
    public string GetTypeFromDefinition(MetadataReader r, TypeDefinitionHandle handle, byte rawTypeKind)
    {
        var type = r.GetTypeDefinition(handle);
        return type.IsNested ? Type(type.GetDeclaringType()) + "+" + r.GetString(type.Name)
            : "[" + r.GetString(r.GetAssemblyDefinition().Name) + "]" + Qualified(r, type.Namespace, type.Name);
    }
    public string GetTypeFromReference(MetadataReader r, TypeReferenceHandle handle, byte rawTypeKind)
    {
        var type = r.GetTypeReference(handle);
        return Scope(type.ResolutionScope) + Qualified(r, type.Namespace, type.Name);
    }
    public string GetTypeFromSpecification(MetadataReader r, object? context, TypeSpecificationHandle h, byte kind)
        => r.GetTypeSpecification(h).DecodeSignature(this, context);
    public static string Method(MethodSignature<string> s)
        => $"{s.ReturnType} ({string.Join(',', s.ParameterTypes)}) header={s.Header.RawValue:X2} genericArity={s.GenericParameterCount} requiredParameters={s.RequiredParameterCount}";
    public string GetArrayType(string element, ArrayShape shape)
        => $"{element}[rank={shape.Rank};sizes={string.Join(',', shape.Sizes)};lower={string.Join(',', shape.LowerBounds)}]";
    public string GetByReferenceType(string element) => element + "&";
    public string GetFunctionPointerType(MethodSignature<string> signature) => "fnptr " + Method(signature);
    public string GetGenericInstantiation(string type, ImmutableArray<string> args) => $"{type}<{string.Join(',', args)}>";
    public string GetGenericMethodParameter(object? context, int index) => "!!" + index;
    public string GetGenericTypeParameter(object? context, int index) => "!" + index;
    public string GetModifiedType(string modifier, string type, bool required) => $"{type} mod{(required ? "req" : "opt")}({modifier})";
    public string GetPinnedType(string element) => element + " pinned";
    public string GetPointerType(string element) => element + "*";
    public string GetPrimitiveType(PrimitiveTypeCode type) => type.ToString();
    public string GetSZArrayType(string element) => element + "[]";
}
