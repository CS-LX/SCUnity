using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Engine;
using Engine.Serialization;
using GameEntitySystem;
using TemplatesDatabase;

namespace SCUnity.Foundation
{
    public interface IDefaultMethod { int Value() => 42; }
    public sealed class DefaultMethod : IDefaultMethod { }

    // Compiled twice: original net10 assemblies and migrated net48 assemblies.
    // Values are recorded as raw IEEE-754 bytes for an independent comparison.
    public static class Entry
    {
        public static readonly List<string> Events = new();
        static readonly Dictionary<string, bool> Checks = new();
        static uint state;
        static float Next()
        {
            state = unchecked(state * 1664525u + 1013904223u);
            return ((int)(state >> 8) - 8388608) / 4096f;
        }
        static Matrix NextMatrix() => new(Next(), Next(), Next(), Next(), Next(), Next(), Next(), Next(),
            Next(), Next(), Next(), Next(), Next(), Next(), Next(), Next());
        static void Require(string name, bool passed)
        {
            Checks.Add(name, passed);
            if (!passed) throw new InvalidOperationException("Failed assertion: " + name);
        }
        static void Write(BinaryWriter writer, Vector2 v) { writer.Write(v.X); writer.Write(v.Y); }
        static void Write(BinaryWriter writer, Vector3 v) { writer.Write(v.X); writer.Write(v.Y); writer.Write(v.Z); }
        static void Write(BinaryWriter writer, Vector4 v) { writer.Write(v.X); writer.Write(v.Y); writer.Write(v.Z); writer.Write(v.W); }
        static void Write(BinaryWriter writer, Matrix m)
        {
            writer.Write(m.M11); writer.Write(m.M21); writer.Write(m.M31); writer.Write(m.M41);
            writer.Write(m.M12); writer.Write(m.M22); writer.Write(m.M32); writer.Write(m.M42);
            writer.Write(m.M13); writer.Write(m.M23); writer.Write(m.M33); writer.Write(m.M43);
            writer.Write(m.M14); writer.Write(m.M24); writer.Write(m.M34); writer.Write(m.M44);
        }
        static void MathCorpus(string output)
        {
            state = 0x5c001234;
            using (var writer = new BinaryWriter(File.Create(Path.Combine(output, "math.bin"))))
            {
                for (int i = 0; i < 4096; i++)
                {
                    Matrix a = NextMatrix(), b = NextMatrix();
                    float f = Next();
                    Write(writer, a + b); Write(writer, a - b); Write(writer, -a);
                    Write(writer, a * f); Write(writer, a / (f == 0 ? 1 : f));
                    Write(writer, a * b); Write(writer, a / b); Write(writer, Matrix.Lerp(a, b, f));
                    Matrix.MultiplyRestricted(ref a, ref b, out Matrix restricted); Write(writer, restricted);
                    Vector2 v2 = new(Next(), Next()); Vector3 v3 = new(Next(), Next(), Next());
                    Vector4 v4 = new(Next(), Next(), Next(), Next());
                    Write(writer, Vector2.Transform(v2, a)); Write(writer, Vector3.Transform(v3, a));
                    Write(writer, Vector4.Transform(v4, a));
                    Vector2.Transform(ref v2, ref a, out Vector2 r2); Write(writer, r2);
                    Vector3.Transform(ref v3, ref a, out Vector3 r3); Write(writer, r3);
                    Vector4.Transform(ref v4, ref a, out Vector4 r4); Write(writer, r4);
                    Write(writer, Vector2.TransformNormal(v2, a)); Write(writer, Vector3.TransformNormal(v3, a));
                    Vector2.TransformNormal(ref v2, ref a, out r2); Write(writer, r2);
                    Vector3.TransformNormal(ref v3, ref a, out r3); Write(writer, r3);
                    var s2 = new[] { v2, -v2, v2 * 2 }; var d2 = new Vector2[5];
                    var s3 = new[] { v3, -v3, v3 * 2 }; var d3 = new Vector3[5];
                    var s4 = new[] { v4, -v4, v4 * 2 }; var d4 = new Vector4[5];
                    Vector2.Transform(s2, 1, ref a, d2, 2, 2);
                    Vector3.Transform(s3, 1, ref a, d3, 2, 2);
                    Vector4.Transform(s4, 1, ref a, d4, 2, 2);
                    foreach (var v in d2) Write(writer, v);
                    foreach (var v in d3) Write(writer, v);
                    foreach (var v in d4) Write(writer, v);
                    Vector2.TransformNormal(s2, 1, ref a, d2, 2, 2);
                    Vector3.TransformNormal(s3, 1, ref a, d3, 2, 2);
                    foreach (var v in d2) Write(writer, v);
                    foreach (var v in d3) Write(writer, v);
                    Write(writer, Vector4.Floor(v4)); Write(writer, Vector4.Ceiling(v4)); Write(writer, Vector4.Round(v4));
                    System.Numerics.Vector2 n2 = v2; System.Numerics.Vector3 n3 = v3; System.Numerics.Vector4 n4 = v4;
                    Write(writer, (Vector2)n2); Write(writer, (Vector3)n3); Write(writer, (Vector4)n4);
                }
            }
            using (var writer = new BinaryWriter(File.Create(Path.Combine(output, "rounding-edges.bin"))))
            {
                // Include negative zero, subnormals, halfway values, infinities and NaN payloads.
                int[] bits = { 0, unchecked((int)0x80000000), 1, unchecked((int)0x80000001), 0x3f000000,
                    unchecked((int)0xbf000000), 0x3fc00000, unchecked((int)0xbfc00000), 0x40200000,
                    unchecked((int)0xc0200000), 0x7f800000, unchecked((int)0xff800000), 0x7fc12345, 0x7f812345 };
                foreach (int value in bits)
                {
                    float f = BitConverter.ToSingle(BitConverter.GetBytes(value), 0);
                    Write(writer, Vector4.Floor(new Vector4(f))); Write(writer, Vector4.Ceiling(new Vector4(f)));
                    Write(writer, Vector4.Round(new Vector4(f)));
                }
            }
            Require("math-corpus-completed", true);
            Require("matrix-column-layout", Marshal.SizeOf(typeof(Matrix)) == 64
                && Marshal.OffsetOf(typeof(Matrix), "M21").ToInt32() == 4
                && Marshal.OffsetOf(typeof(Matrix), "M12").ToInt32() == 16
                && Marshal.OffsetOf(typeof(Matrix), "M44").ToInt32() == 60);
        }
        static void Serialization(string output)
        {
            byte[] bytes;
            using (var stream = new MemoryStream())
            {
                var archive = new BinaryOutputArchive(stream, 17) { UseObjectInfos = false };
                foreach (int value in new[] { int.MinValue, -1, 0, 127, 128, 16384, int.MaxValue }) archive.Serialize("int", value);
                archive.Serialize("null", (string)null); archive.Serialize("text", "旧存档 Δ"); archive.Serialize("repeat", "旧存档 Δ");
                archive.Serialize("vector", new Vector3(1.25f, -2.5f, 73f)); archive.Serialize("matrix", Matrix.Identity);
                archive.Serialize("array", new[] { 1, -2, 73 });
                bytes = stream.ToArray();
            }
            File.WriteAllBytes(Path.Combine(output, "archive.bin"), bytes);
            using (var archive = new BinaryInputArchive(new MemoryStream(bytes), 17) { UseObjectInfos = false })
            {
                var ints = new List<int>();
                foreach (int expected in new[] { int.MinValue, -1, 0, 127, 128, 16384, int.MaxValue })
                { int value = 0; archive.Serialize("int", ref value); ints.Add(value); }
                Require("binary-integer-boundaries", ints.SequenceEqual(new[] { int.MinValue, -1, 0, 127, 128, 16384, int.MaxValue }));
                string empty = "x", text = null, repeat = null;
                archive.Serialize("null", ref empty); archive.Serialize("text", ref text); archive.Serialize("repeat", ref repeat);
                Require("binary-string-interning", empty == null && text == "旧存档 Δ" && ReferenceEquals(text, repeat));
                Vector3 v = default; Matrix m = default; int[] array = null;
                archive.Serialize("vector", ref v); archive.Serialize("matrix", ref m); archive.Serialize("array", ref array);
                Require("binary-serializer-discovery", v == new Vector3(1.25f, -2.5f, 73) && m == Matrix.Identity && array.SequenceEqual(new[] { 1, -2, 73 }));
                Require("binary-consumed-exactly", archive.Stream.Position == bytes.Length);
            }
            var values = new ValuesDictionary();
            values.SetValue("Position", new Vector3(1.25f, -2.5f, 73)); values.SetValue("Health", 73);
            var child = new ValuesDictionary(); child.SetValue("Name", "旧存档 Δ"); values.SetValue("Player", child);
            var node = new XElement("Values"); values.Save(node);
            File.WriteAllText(Path.Combine(output, "values.xml"), node.ToString(SaveOptions.DisableFormatting), new System.Text.UTF8Encoding(false));
            var restored = new ValuesDictionary(); restored.ApplyOverrides(node);
            Require("values-xml-roundtrip", restored.GetValue<Vector3>("Position") == values.GetValue<Vector3>("Position")
                && restored.GetValue<ValuesDictionary>("Player").GetValue<string>("Name") == "旧存档 Δ");
            Require("human-readable-null-guard", NullGuard());
            Require("human-readable-invalid-input", !HumanReadableConverter.TryConvertFromString<Vector3>("invalid", out _));
            values.EnsureCapacity(100); values.TryAdd("Temporary", 12); values.Remove("Temporary", out object removed);
            values.TrimExcess();
            Require("unity-dictionary-apis", (int)removed == 12 && !values.ContainsKey("Temporary"));
            // Record the unchanged upstream default object-info behavior separately.
            // The original currently reads the first int[] back as null; do not fix it in a port.
            using (var stream = new MemoryStream())
            {
                var writer = new BinaryOutputArchive(stream); writer.Serialize("array", new[] { 1, -2, 73 });
                stream.Position = 0; var reader = new BinaryInputArchive(stream); int[] array = null;
                reader.Serialize("array", ref array);
                File.WriteAllText(Path.Combine(output, "object-info.txt"), (array == null ? "null" : string.Join(",", array))
                    + ";position=" + stream.Position + ";length=" + stream.Length, new System.Text.UTF8Encoding(false));
                Require("default-object-info-behavior-recorded", true);
            }
        }
        static bool NullGuard()
        {
            try { HumanReadableConverter.IsTypeSupported(null); return false; }
            catch (ArgumentNullException e) { return e.ParamName == "type"; }
        }
        static void EntityLifecycle(string payload)
        {
            const string name = "SCUnity.Foundation.Dynamic.ProbeComponent";
            Require("typecache-payload-not-preloaded", TypeCache.FindType(name, false, false) == null);
            int before = TypeCache.LoadedAssemblies.Count;
            Assembly assembly = Assembly.Load(File.ReadAllBytes(payload));
            Type type = TypeCache.FindType(name, false, true);
            Require("typecache-assembly-load-rescan", type.Assembly == assembly && TypeCache.LoadedAssemblies.Count > before);
            Require("typecache-short-names", TypeCache.FindType("Vector3", false, true) == typeof(Vector3)
                && TypeCache.GetShortTypeName(typeof(Matrix).FullName) == "Matrix");
            Require("typecache-cache-identity", ReferenceEquals(type, TypeCache.FindType(name, false, true)));
            bool missing = false;
            try { TypeCache.FindType("SCUnity.Foundation.Missing", false, true); } catch (InvalidOperationException) { missing = true; }
            Require("typecache-required-missing-rejected", missing);
            string[] names = { "Folder", "ProjectTemplate", "MemberSubsystemTemplate", "SubsystemTemplate", "EntityTemplate",
                "MemberComponentTemplate", "ComponentTemplate", "ParameterSet", "Parameter" };
            var types = names.Select(n => new DatabaseObjectType(n, n, "", 0, false, false, 100, false)).ToArray();
            foreach (var t in types) t.InitializeRelations(types, types, null);
            var db = new GameDatabase(new Database(new DatabaseObject(types[0], "Root"), types));
            using (var project = new Project { m_gameDatabase = db, PostponeFireEntityAddedEvents = false })
            {
                var values = new ValuesDictionary { DatabaseObject = new DatabaseObject(db.EntityTemplateType, "TestEntity") };
                var component = new ValuesDictionary { DatabaseObject = new DatabaseObject(db.MemberComponentTemplateType, "Probe") };
                component.SetValue("IsOptional", false); component.SetValue("Class", name); component.SetValue("LoadOrder", 0);
                component.SetValue("Number", 73); values.SetValue("Probe", component);
                Events.Clear();
                var entity = project.CreateEntity(values);
                Require("entity-component-reflection-load", entity.Components.Count == 1 && entity.Components[0].GetType() == type
                    && Events.SequenceEqual(new[] { "load:73" }));
                Project.EntityAdded += (_, _) => Events.Add("project-added");
                Project.EntityRemoved += (_, _) => Events.Add("project-removed");
                entity.EntityAdded += (_, _) => Events.Add("entity-added");
                entity.EntityRemoved += (_, _) => Events.Add("entity-removed");
                project.AddEntity(entity); project.AddEntity(entity);
                Require("entity-add-id-and-find", entity.Id == 1 && project.NextEntityID == 2 && project.Entities.Count == 1
                    && ReferenceEquals(project.FindEntity(1), entity) && project.FindEntity(999) == null);
                var saved = new ValuesDictionary(); entity.InternalSaveEntity(saved, new EntityToIdMap(project.Entities.ToDictionary(e => e, e => e.Id)));
                Require("entity-component-save", saved.GetValue<ValuesDictionary>("Probe").GetValue<int>("Number") == 73);
                project.RemoveEntity(entity, true);
                Require("entity-remove-and-dispose", !entity.IsAddedToProject && project.FindEntity(1) == null);
                Require("entity-event-order", string.Join(",", Events) == "load:73,component-added,project-added,entity-added,component-removed,project-removed,entity-removed,component-disposed");
            }
        }
        public static Dictionary<string, bool> Run(string output, string payload)
        {
            Directory.CreateDirectory(output); Checks.Clear();
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            Require("default-interface-method", ((IDefaultMethod)new DefaultMethod()).Value() == 42);
            MathCorpus(output); EntityLifecycle(payload); Serialization(output);
            return new Dictionary<string, bool>(Checks);
        }
    }
}
