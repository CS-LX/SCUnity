using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.Json;

if (args[0] == "--format-primary")
{
    using var document = JsonDocument.Parse(File.ReadAllText(args[2]));
    var primary = document.RootElement.EnumerateArray().Where(e => e.GetProperty("kind").GetString() is "ClassDeclaration" or "StructDeclaration")
        .GroupBy(e => e.GetProperty("file").GetString());
    foreach (var group in primary)
    {
        string file = Path.Combine(args[1], group.Key);
        var root = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot();
        foreach (var entry in group)
        {
            string name = CSharpSyntaxTree.ParseText(entry.GetProperty("text").GetString()).GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().First().Identifier.ValueText;
            var type = root.DescendantNodes().OfType<TypeDeclarationSyntax>().Single(t => t.Identifier.ValueText == name);
            SyntaxNode original = name == "Matrix" ? type.Members.OfType<ConstructorDeclarationSyntax>().First() : type;
            int indentation = original.GetLocation().GetLineSpan().StartLinePosition.Character;
            string formatted = original.WithoutTrivia().NormalizeWhitespace().ToFullString().Replace("\r\n", "\n")
                .Replace("\n", "\n" + new string(' ', indentation));
            root = root.ReplaceNode(original, SyntaxFactory.ParseMemberDeclaration(formatted).WithTriviaFrom(original));
        }
        File.WriteAllText(file, root.ToFullString());
    }
    return;
}
var source = Path.GetFullPath(args[0]);
var plugins = Path.GetFullPath(args[1]);
var framework = Path.GetFullPath(args[2]);
var report = new List<object>();
foreach (var assembly in new[] { "Engine", "EntitySystem", "Survivalcraft" })
{
    var options = new CSharpParseOptions(LanguageVersion.Preview, preprocessorSymbols: new[] { "WINDOWS", "SCUNITY", "TRACE" });
    var trees = Directory.GetFiles(Path.Combine(source, assembly), "*.cs", SearchOption.AllDirectories)
        .Where(p => !p.Contains("/obj/") && !p.Contains("\\obj\\") && !p.EndsWith("EGL.cs"))
        .Select(p => CSharpSyntaxTree.ParseText(File.ReadAllText(p), options, p)).ToArray();
    var files = Directory.GetFiles(framework, "*.dll", SearchOption.AllDirectories)
        .Concat(Directory.GetFiles(plugins, "*.dll").Where(p => Path.GetFileNameWithoutExtension(p) != assembly
            && !new[] { "wrap_oal.dll", "MonoMod.Backports.dll", "Microsoft.Bcl.HashCode.dll", "System.Runtime.InteropServices.RuntimeInformation.dll" }.Contains(Path.GetFileName(p))))
        .GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).Select(g => g.First());
    var references = files.Select(p => MetadataReference.CreateFromFile(p));
    var compilation = CSharpCompilation.Create(assembly, trees, references,
        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
    Console.Error.WriteLine(assembly + ": " + string.Join("\n", compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).Take(20)));
    foreach (var tree in trees)
    {
        var model = compilation.GetSemanticModel(tree);
        foreach (var node in tree.GetRoot().DescendantNodes())
        {
            if (node is CollectionExpressionSyntax || node is TypeDeclarationSyntax { ParameterList: not null }
                || node is FileScopedNamespaceDeclarationSyntax || node.IsKind(SyntaxKind.FieldExpression)
                || node is ListPatternSyntax || node is LiteralExpressionSyntax literal && literal.Token.Kind().ToString().Contains("RawString"))
                report.Add(new { file = Path.GetRelativePath(source, tree.FilePath), line = node.GetLocation().GetLineSpan().StartLinePosition.Line + 1,
                    kind = node.Kind().ToString(), text = node.ToString(), type = node is ExpressionSyntax expr ? model.GetTypeInfo(expr).ConvertedType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) : null });
        }
        if (args.Length > 4)
        {
            var root = (CompilationUnitSyntax)new Downlevel(model).Visit(tree.GetRoot());
            // The pinned Unity Roslyn supports C# 10: keep upstream global imports.
            string output = Path.Combine(args[4], Path.GetRelativePath(source, tree.FilePath));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, root.ToFullString().Replace("\r\n", "\n"));
        }
    }
}
File.WriteAllText(args[3], JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
