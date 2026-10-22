using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;
using System.Text.RegularExpressions;

// Run from the repository root. Parse declarations, never method bodies as members.
var root = Path.GetFullPath(args.FirstOrDefault() ?? ".");
var trees = Directory.GetFiles(Path.Combine(root, "src"), "*.cs", SearchOption.AllDirectories)
    .Where(p => !Path.GetRelativePath(Path.Combine(root, "src"), p).Split(Path.DirectorySeparatorChar).Any(s => s is "obj" or "bin"))
    .OrderBy(p => p, StringComparer.Ordinal)
    .Select(p => CSharpSyntaxTree.ParseText(File.ReadAllText(p), path: p)).ToArray();
foreach (var tree in trees)
    if (tree.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
        throw new InvalidOperationException($"Cannot parse {tree.FilePath}");
var types = trees.SelectMany(t => t.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
    .GroupBy(t => Namespace(t) + "." + t.Identifier.Text)
    .OrderBy(g => g.Key, StringComparer.Ordinal).ToArray();
if (types.Length == 0) throw new InvalidOperationException("No project types found.");
var byName = types.ToDictionary(g => g.First().Identifier.Text, g => g.Key);
var edges = new SortedSet<string>(StringComparer.Ordinal);
var external = new SortedSet<string>(StringComparer.Ordinal);
var output = new StringBuilder("""
@startuml
title Zombie Shooter — complete source-declared class model
top to bottom direction
set namespaceSeparator none
skinparam classAttributeIconSize 0
skinparam packageStyle rectangle
skinparam shadowing false
skinparam backgroundColor #FFFFFF
skinparam defaultFontName DejaVu Sans
skinparam defaultFontSize 14
skinparam defaultFontColor #172033
skinparam classBackgroundColor #FFFFFF
skinparam classHeaderBackgroundColor #E8F0FA
skinparam classBorderColor #52657A
skinparam classFontColor #172033
skinparam classAttributeFontColor #172033
skinparam packageBackgroundColor #F5F8FC
skinparam packageBorderColor #94A3B8
skinparam packageFontColor #172033
skinparam arrowColor #475569
skinparam arrowFontColor #172033
skinparam arrowFontSize 13
skinparam legendBackgroundColor #F1F5F9
skinparam legendBorderColor #94A3B8
skinparam legendFontColor #172033
skinparam titleFontColor #172033
skinparam titleFontSize 22
skinparam nodesep 55
skinparam ranksep 90
hide empty members

""");
int memberCount = 0;
foreach (var package in types.GroupBy(g => Namespace(g.First())))
{
    output.AppendLine($"package \"{package.Key}\" {{");
    foreach (var group in package)
    {
        var first = group.First();
        string id = Alias(group.Key);
        bool isInterface = first is InterfaceDeclarationSyntax;
        string kind = first is EnumDeclarationSyntax ? "enum" : isInterface ? "interface" :
            first.Modifiers.Any(SyntaxKind.AbstractKeyword) ? "abstract class" : "class";
        string generic = first is TypeDeclarationSyntax td ? Clean(td.TypeParameterList) : "";
        var stereotypes = new List<string>();
        if (first is RecordDeclarationSyntax record) stereotypes.Add(record.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) ? "record struct" : "record");
        else if (first is StructDeclarationSyntax) stereotypes.Add("struct");
        stereotypes.AddRange(first.Modifiers.Where(m => m.IsKind(SyntaxKind.StaticKeyword) || m.IsKind(SyntaxKind.SealedKeyword) || m.IsKind(SyntaxKind.ReadOnlyKeyword)).Select(m => m.Text));
        if (!first.Modifiers.Any(SyntaxKind.PublicKeyword)) stereotypes.Add("internal");
        output.AppendLine($"  {kind} \"{first.Identifier.Text}{generic}\" as {id}" +
            (stereotypes.Count == 0 ? "" : $" <<{string.Join(", ", stereotypes)}>>") + " {");
        void Member(string line) { output.AppendLine("    " + line); memberCount++; }
        void Reference(TypeSyntax? type, bool held)
        {
            if (type is null) return;
            foreach (var name in type.DescendantNodesAndSelf().OfType<SimpleNameSyntax>().Select(n => n.Identifier.Text).Distinct())
                if (byName.TryGetValue(name, out var key) && key != group.Key)
                    edges.Add($"{id} {(held ? "-->" : "..>")} {Alias(key)} : {(held ? "member type" : "signature")}");
        }
        void Parameters(ParameterListSyntax? parameters)
        {
            if (parameters is not null)
                foreach (var p in parameters.Parameters) Reference(p.Type, false);
        }
        foreach (var declaration in group)
        {
            if (declaration.BaseList is not null)
                foreach (var b in declaration.BaseList.Types)
                {
                    var name = b.Type.DescendantNodesAndSelf().OfType<SimpleNameSyntax>().First().Identifier.Text;
                    bool iface = byName.TryGetValue(name, out var key)
                        ? types.First(g => g.Key == key).First() is InterfaceDeclarationSyntax
                        : name.StartsWith("I", StringComparison.Ordinal);
                    var target = key is null ? "External_" + name : Alias(key);
                    if (key is null) external.Add($"{(iface ? "interface" : "class")} \"{Clean(b.Type)}\" as {target} <<external>>");
                    edges.Add($"{target} {(iface && !isInterface ? "<|.." : "<|--")} {id}" +
                        (b.Type is GenericNameSyntax g ? " : " + Clean(g) : ""));
                }
            if (declaration is EnumDeclarationSyntax en)
            {
                foreach (var value in en.Members) Member(value.Identifier.Text + (value.EqualsValue is null ? "" : " = " + Clean(value.EqualsValue.Value)));
                continue;
            }
            var type = (TypeDeclarationSyntax)declaration;
            if (type.ParameterList is not null)
            {
                Member("{method} +" + type.Identifier.Text + Clean(type.ParameterList) + " <<primary constructor>>");
                Parameters(type.ParameterList);
                if (type is not RecordDeclarationSyntax)
                    foreach (var p in type.ParameterList.Parameters)
                    {
                        // Parameters referenced by instance method/accessor bodies require captured storage.
                        var bodies = type.Members.SelectMany(m => m switch
                        {
                            MethodDeclarationSyntax method => new SyntaxNode?[] { method.Body, method.ExpressionBody },
                            PropertyDeclarationSyntax property => new SyntaxNode?[] { property.AccessorList, property.ExpressionBody },
                            _ => Array.Empty<SyntaxNode?>()
                        });
                        if (bodies.Where(b => b is not null).Any(b => b!.DescendantNodesAndSelf()
                            .OfType<IdentifierNameSyntax>().Any(n => n.Identifier.Text == p.Identifier.Text)))
                        {
                            Member("{field} -" + p.Identifier.Text + " : " + Clean(p.Type) + " <<captured parameter>>");
                            Reference(p.Type, true);
                        }
                    }
                if (type is RecordDeclarationSyntax)
                    foreach (var p in type.ParameterList.Parameters)
                    {
                        Member("{field} +" + p.Identifier.Text + " : " + Clean(p.Type) +
                            (type.Modifiers.Any(SyntaxKind.ReadOnlyKeyword) || !((RecordDeclarationSyntax)type).ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) ? " { get; init; }" : " { get; set; }"));
                        Reference(p.Type, true);
                    }
            }
            foreach (var m in type.Members)
            {
                string prefix = Visibility(m.Modifiers, isInterface) + Modifiers(m.Modifiers);
                switch (m)
                {
                    case FieldDeclarationSyntax f:
                        foreach (var v in f.Declaration.Variables)
                            Member("{field} " + prefix + v.Identifier.Text + " : " + Clean(f.Declaration.Type) +
                                (f.Modifiers.Any(SyntaxKind.ConstKeyword) ? " = " + Clean(v.Initializer?.Value) : ""));
                        Reference(f.Declaration.Type, true);
                        break;
                    case EventFieldDeclarationSyntax e:
                        foreach (var v in e.Declaration.Variables)
                            Member("{field} " + prefix + v.Identifier.Text + " : " + Clean(e.Declaration.Type) + " <<event>>");
                        Reference(e.Declaration.Type, true);
                        break;
                    case PropertyDeclarationSyntax p:
                        var accessors = p.AccessorList is null ? "get; computed" : string.Join(" ", p.AccessorList.Accessors.Select(a => Clean(a.Modifiers) + (a.Modifiers.Count == 0 ? "" : " ") + a.Keyword.Text + ";"));
                        Member("{field} " + prefix + Clean(p.ExplicitInterfaceSpecifier) + p.Identifier.Text + " : " + Clean(p.Type) + " { " + accessors + " }");
                        Reference(p.Type, true);
                        break;
                    case ConstructorDeclarationSyntax c:
                        Member("{method} " + prefix + c.Identifier.Text + Clean(c.ParameterList));
                        Parameters(c.ParameterList);
                        break;
                    case MethodDeclarationSyntax method:
                        Member("{method} " + prefix + Clean(method.ExplicitInterfaceSpecifier) + method.Identifier.Text + Clean(method.TypeParameterList) + Clean(method.ParameterList) + " : " + Clean(method.ReturnType) +
                            (method.ConstraintClauses.Count == 0 ? "" : " " + string.Join(" ", method.ConstraintClauses.Select(Clean))));
                        Reference(method.ReturnType, false);
                        Parameters(method.ParameterList);
                        break;
                    default:
                        throw new NotSupportedException($"Unhandled member {m.Kind()} in {group.Key}");
                }
            }
        }
        output.AppendLine("  }");
    }
    output.AppendLine("}");
}
output.AppendLine("package \"External base types (members omitted)\" {");
foreach (var e in external) output.AppendLine("  " + e);
output.AppendLine("}");
// Keep explicit behavioral/ownership relationships separate from generated declarations.
var relations = Path.Combine(root, "docs/uml/class-relationships.puml.inc");
var explicitLinks = File.Exists(relations) ? File.ReadAllText(relations) : "";
output.AppendLine(explicitLinks);
var explicitPairs = Regex.Matches(explicitLinks, @"(?m)^(\w+) (?:""[^""]+"" )?(?:-->|\.\.>|\*--) (?:""[^""]+"" )?(\w+)")
    .Select(m => m.Groups[1].Value + " " + m.Groups[2].Value).ToHashSet();
foreach (var edge in edges)
{
    var endpoints = edge.Split(' ');
    if (endpoints[1] is "-->" or "..>" && explicitPairs.Contains(endpoints[0] + " " + endpoints[2])) continue;
    // Signatures already display parameter/return types; enum attributes display their type.
    // Keep the diagram focused on object structure and the explicit pattern relationships.
    if (edge.EndsWith(" : signature", StringComparison.Ordinal)) continue;
    if (edge.EndsWith(" : member type", StringComparison.Ordinal) &&
        types.Any(g => Alias(g.Key) == endpoints[2] && g.First() is EnumDeclarationSyntax)) continue;
    output.AppendLine(edge.Replace(" : member type", ""));
}
output.AppendLine("""
legend bottom
  All source-declared types and members under src/; partial declarations merged.
  + public, - private, # protected, ~ internal; C# signatures and accessor visibility retained.
  Static/abstract members are marked; readonly, const, virtual and override modifiers are retained.
  Record positional properties and captured primary-constructor parameters are included.
  Inherited members appear on base types. Compiler-generated plumbing and implicit constructors are omitted.
  Solid association arrows describe object references, not exclusive ownership. Dotted arrows show selected behavioral dependencies.
  Parameter/return-type dependencies and enum attribute arrows are omitted; those types remain visible in member signatures.
  Multiplicities describe initialized valid states; repeated field slots (e.g. pistol/shotgun) count separately.
  0..* at a referring end permits sharing; 0..1 at a composition owner end permits a standalone part.
  Inheritance, interface implementations and dependency arrows do not carry multiplicities. External base types show names only.
  Top-level Program.cs entry-point statements, local variables/functions and tests are outside this class model.
endlegend
@enduml
""");
var destination = Path.Combine(root, "docs/uml/01-current-classes.puml");
File.WriteAllText(destination, output.ToString());
Console.WriteLine($"Generated {types.Length} project types, {memberCount} member entries, filtered structural relationships: {destination}");

static string Namespace(BaseTypeDeclarationSyntax t) => string.Join(".", t.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(n => n.Name.ToString()));
static string Alias(string text) => text.Replace('.', '_');
static string Clean(object? node) => Regex.Replace(node?.ToString() ?? "", @"\s+", " ").Trim();
static string Visibility(SyntaxTokenList mods, bool iface) => mods.Any(SyntaxKind.PublicKeyword) ? "+" : mods.Any(SyntaxKind.PrivateKeyword) ? "-" : mods.Any(SyntaxKind.ProtectedKeyword) ? "#" : mods.Any(SyntaxKind.InternalKeyword) ? "~" : iface ? "+" : "-";
static string Modifiers(SyntaxTokenList mods) => string.Concat(mods.Where(m => m.IsKind(SyntaxKind.StaticKeyword) || m.IsKind(SyntaxKind.ConstKeyword) || m.IsKind(SyntaxKind.AbstractKeyword)).Select(m => "{" + (m.IsKind(SyntaxKind.ConstKeyword) ? "static" : m.Text) + "} ")) +
    string.Concat(mods.Where(m => m.IsKind(SyntaxKind.ReadOnlyKeyword) || m.IsKind(SyntaxKind.ConstKeyword) || m.IsKind(SyntaxKind.VirtualKeyword) || m.IsKind(SyntaxKind.OverrideKeyword) || m.IsKind(SyntaxKind.AsyncKeyword)).Select(m => m.Text + " "));
