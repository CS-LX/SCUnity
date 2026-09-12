using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

// One-time, semantic migration of the pinned Desktop profile. Never run on build
// or over hand-edited port sources. New upstream constructs must be reviewed.
sealed class Downlevel(SemanticModel model) : CSharpSyntaxRewriter
{
    static string TypeName(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    static ExpressionSyntax Expr(string text, SyntaxNode original) => ParseExpression(text).WithTriviaFrom(original);

    public override SyntaxNode VisitCollectionExpression(CollectionExpressionSyntax node)
    {
        var type = model.GetTypeInfo(node).ConvertedType;
        if (type == null || type.TypeKind == TypeKind.Error) throw new InvalidOperationException("Unresolved collection: " + node.GetLocation());
        var rewritten = (CollectionExpressionSyntax)base.VisitCollectionExpression(node);
        var elements = rewritten.Elements;
        var name = TypeName(type);
        var named = type as INamedTypeSymbol;
        var generic = named?.ConstructedFrom.ToDisplayString();
        var item = type is IArrayTypeSymbol array ? array.ElementType : named?.TypeArguments.FirstOrDefault();
        if (elements.OfType<SpreadElementSyntax>().Any())
        {
            // Only these three forms exist in the pinned source. Keep evaluation
            // order explicit; reject any future form instead of guessing semantics.
            if (elements.Count == 1 && elements[0] is SpreadElementSyntax spread)
                return Expr($"new {name}({spread.Expression})", node);
            if (elements.Count == 2 && elements[0] is SpreadElementSyntax first && elements[1] is ExpressionElementSyntax last)
                return Expr($"new {name}({first.Expression}) {{ {last.Expression} }}", node);
            if (elements.Count == 2 && elements[0] is ExpressionElementSyntax head && head.Expression is LiteralExpressionSyntax
                && elements[1] is SpreadElementSyntax tail && generic == "System.Collections.Generic.IEnumerable<T>")
                return Expr($"global::System.Linq.Enumerable.ToArray(global::System.Linq.Enumerable.Concat(new {TypeName(item)}[] {{ {head.Expression} }}, {tail.Expression}))", node);
            throw new NotSupportedException("Review spread evaluation order: " + node.GetLocation());
        }
        string values = string.Join(", ", elements.Cast<ExpressionElementSyntax>().Select(e => e.Expression.ToFullString()));
        if (values.Contains('#') || values.Contains("//")) values = "\n" + values.Replace(", #", ",\n#") + "\n";
        if (type is IArrayTypeSymbol || generic is "System.Collections.Generic.IEnumerable<T>" or "System.ReadOnlySpan<T>" or "System.Span<T>")
            return Expr($"new {TypeName(item)}[] {{ {values} }}", node);
        if (generic == "System.Collections.Immutable.ImmutableArray<T>")
            return Expr($"global::System.Collections.Immutable.ImmutableArray.Create<{TypeName(item)}>(new {TypeName(item)}[] {{ {values} }})", node);
        return Expr($"new {name}() {{ {values} }}", node);
    }

    public override SyntaxNode VisitFieldExpression(FieldExpressionSyntax node)
    {
        var property = node.Ancestors().OfType<PropertyDeclarationSyntax>().First();
        return IdentifierName("m_unity_" + property.Identifier.ValueText).WithTriviaFrom(node);
    }

    public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node) => Lower(node, (TypeDeclarationSyntax)base.VisitClassDeclaration(node));
    public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node) => Lower(node, (TypeDeclarationSyntax)base.VisitStructDeclaration(node));

    TypeDeclarationSyntax Lower(TypeDeclarationSyntax original, TypeDeclarationSyntax rewritten)
    {
        var members = rewritten.Members.ToList();
        foreach (var property in original.Members.OfType<PropertyDeclarationSyntax>())
        {
            if (!property.DescendantNodes().OfType<FieldExpressionSyntax>().Any()) continue;
            string modifiers = property.Modifiers.Any(SyntaxKind.StaticKeyword) ? "private static" : "private";
            var current = (PropertyDeclarationSyntax)members[original.Members.IndexOf(property)];
            members.Add(ParseMemberDeclaration($"{modifiers} {property.Type} m_unity_{property.Identifier.ValueText}{current.Initializer};")
                .WithLeadingTrivia(ParseLeadingTrivia("\n        ")));
            // Mixed explicit/auto accessors use the same synthesized field.
            if (current.AccessorList != null)
                current = current.WithAccessorList(current.AccessorList.WithAccessors(List(current.AccessorList.Accessors.Select(a =>
                    a.Body != null || a.ExpressionBody != null ? a : a.WithExpressionBody(ArrowExpressionClause(ParseExpression(
                        a.IsKind(SyntaxKind.GetAccessorDeclaration) ? "m_unity_" + property.Identifier.ValueText : "m_unity_" + property.Identifier.ValueText + " = value")))))));
            members[original.Members.IndexOf(property)] = current.WithInitializer(null)
                .WithSemicolonToken(current.ExpressionBody == null ? default : current.SemicolonToken);
        }
        rewritten = rewritten.WithMembers(List(members));
        if (original.ParameterList == null) return rewritten;

        var assignments = new List<string>();
        var captures = new List<MemberDeclarationSyntax>();
        foreach (var parameter in original.ParameterList.Parameters)
        {
            var symbol = model.GetDeclaredSymbol(parameter);
            bool captured = original.Members.SelectMany(m => m.DescendantNodes()).OfType<IdentifierNameSyntax>().Any(n =>
                SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(n).Symbol, symbol)
                && !n.Ancestors().OfType<EqualsValueClauseSyntax>().Any(e => e.Parent is PropertyDeclarationSyntax
                    || e.Parent is VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Parent: FieldDeclarationSyntax } }));
            if (captured)
            {
                captures.Add(ParseMemberDeclaration($"private {parameter.Type} {parameter.Identifier};"));
                assignments.Add($"this.{parameter.Identifier} = {parameter.Identifier};");
            }
        }
        // Reviewed primary types only derive from object/empty TextDrawItem;
        // Geometry has no initializers. Preserve declaration order in the ctor.
        members = rewritten.Members.Select<MemberDeclarationSyntax, MemberDeclarationSyntax>(member =>
        {
            if (member is FieldDeclarationSyntax field && !field.Modifiers.Any(SyntaxKind.StaticKeyword) && !field.Modifiers.Any(SyntaxKind.ConstKeyword))
                return field.WithDeclaration(field.Declaration.WithVariables(SeparatedList(field.Declaration.Variables.Select(v => {
                    if (v.Initializer == null) return v;
                    assignments.Add($"this.{v.Identifier} = {v.Initializer.Value};");
                    return v.WithInitializer(null);
                }))));
            if (member is PropertyDeclarationSyntax property && !property.Modifiers.Any(SyntaxKind.StaticKeyword) && property.Initializer != null)
            {
                assignments.Add($"this.{property.Identifier} = {property.Initializer.Value};");
                return property.WithInitializer(null).WithSemicolonToken(default);
            }
            return member;
        }).ToList();
        var primaryBase = rewritten.BaseList?.Types.OfType<PrimaryConstructorBaseTypeSyntax>().SingleOrDefault();
        string initializer = primaryBase != null ? " : base" + primaryBase.ArgumentList : original is StructDeclarationSyntax ? " : this()" : "";
        var constructor = ParseMemberDeclaration($"public {original.Identifier}{original.ParameterList}{initializer} {{ {string.Join(" ", assignments)} }}")
            .NormalizeWhitespace().WithLeadingTrivia(ParseLeadingTrivia("\n        "));
        members.Insert(0, constructor);
        members.AddRange(captures.Select(c => c.WithLeadingTrivia(ParseLeadingTrivia("\n        "))));
        rewritten = rewritten.WithParameterList(null).WithMembers(List(members));
        if (primaryBase != null)
            rewritten = rewritten.WithBaseList(rewritten.BaseList.WithTypes(SeparatedList(rewritten.BaseList.Types.Select(t =>
                t is PrimaryConstructorBaseTypeSyntax b ? SimpleBaseType(b.Type).WithTriviaFrom(t) : t))));
        if (rewritten.OpenBraceToken.IsMissing || rewritten.OpenBraceToken.RawKind == 0)
            rewritten = rewritten.WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken)).WithCloseBraceToken(Token(SyntaxKind.CloseBraceToken)).WithSemicolonToken(default);
        return rewritten;
    }
}
