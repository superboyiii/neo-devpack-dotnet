extern alias scfx;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Neo.IO.Json;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;

namespace Neo.Compiler
{
    public class CompilationContext
    {
        private readonly Compilation compilation;
        private bool scTypeFound;
        private readonly List<Diagnostic> diagnostics = new();
        private readonly List<string> supportedStandards = new();
        private readonly List<AbiMethod> methodsExported = new();
        private readonly List<AbiEvent> eventsExported = new();
        private readonly PermissionBuilder permissions = new();
        private readonly JObject manifestExtra = new();
        private readonly MethodConvertCollection methodsConverted = new();
        private readonly MethodConvertCollection methodsForward = new();
        private readonly List<MethodToken> methodTokens = new();
        private readonly List<IFieldSymbol> staticFields = new();

        public bool Success => diagnostics.All(p => p.Severity != DiagnosticSeverity.Error);
        public IReadOnlyList<Diagnostic> Diagnostics => diagnostics;
        public string ContractName { get; private set; } = "";
        internal Options Options { get; private set; }
        internal IReadOnlyList<IFieldSymbol> StaticFields => staticFields;

        private CompilationContext(Compilation compilation, Options options)
        {
            this.compilation = compilation;
            this.Options = options;
        }

        private void RemoveEmptyInitialize()
        {
            int index = methodsExported.FindIndex(p => p.Name == "_initialize");
            if (index < 0) return;
            AbiMethod method = methodsExported[index];
            if (methodsConverted[method.Symbol].Instructions.Count <= 1)
            {
                methodsExported.RemoveAt(index);
                methodsConverted.Remove(method.Symbol);
            }
        }

        private IEnumerable<Instruction> GetInstructions()
        {
            return methodsConverted.SelectMany(p => p.Instructions).Concat(methodsForward.SelectMany(p => p.Instructions));
        }

        private int GetAbiOffset(IMethodSymbol method)
        {
            if (!methodsForward.TryGetValue(method, out MethodConvert? convert))
                convert = methodsConverted[method];
            return convert.Instructions[0].Offset;
        }

        private void Compile()
        {
            foreach (SyntaxTree tree in compilation.SyntaxTrees)
            {
                SemanticModel model = compilation.GetSemanticModel(tree);
                diagnostics.AddRange(model.GetDiagnostics());
                if (!Success) continue;
                try
                {
                    ProcessCompilationUnit(model, tree.GetCompilationUnitRoot());
                }
                catch (CompilationException ex)
                {
                    diagnostics.Add(ex.Diagnostic);
                }
            }
            if (Success)
            {
                if (!scTypeFound)
                {
                    diagnostics.Add(Diagnostic.Create(DiagnosticId.NoEntryPoint, DiagnosticCategory.Default, "No SmartContract is found in the sources.", DiagnosticSeverity.Error, DiagnosticSeverity.Error, true, 0));
                    return;
                }
                RemoveEmptyInitialize();
                Instruction[] instructions = GetInstructions().ToArray();
                instructions.RebuildOffsets();
                if (!Options.NoOptimize) Optimizer.CompressJumps(instructions);
                instructions.RebuildOperands();
            }
        }

        private static CompilationContext Compile(string? csproj, string[] sourceFiles, Options options)
        {
            IEnumerable<SyntaxTree> syntaxTrees = sourceFiles.Select(p => CSharpSyntaxTree.ParseText(File.ReadAllText(p), path: p));
            string coreDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
            MetadataReference[] references = new[]
            {
                MetadataReference.CreateFromFile(Path.Combine(coreDir, "System.Runtime.dll")),
                MetadataReference.CreateFromFile(Path.Combine(coreDir, "System.Runtime.InteropServices.dll")),
                MetadataReference.CreateFromFile(typeof(string).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(DisplayNameAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(BigInteger).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(scfx.Neo.SmartContract.Framework.SmartContract).Assembly.Location)
            };
            CSharpCompilation compilation = CSharpCompilation.Create(null, syntaxTrees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            if (csproj is not null)
            {
                string path = Path.GetDirectoryName(csproj)!;
                Process.Start(new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = "restore",
                    WorkingDirectory = path
                })!.WaitForExit();
                path = Path.Combine(path, "obj", "project.assets.json");
                JObject assets = JObject.Parse(File.ReadAllBytes(path));
                string packagesPath = assets["project"]["restore"]["packagesPath"].GetString();
                foreach (var (name, package) in assets["targets"][0].Properties)
                {
                    if (name.StartsWith("Neo.SmartContract.Framework")) continue;
                    JObject files = package["compile"] ?? package["runtime"];
                    if (files is null) continue;
                    foreach (var (file, _) in files.Properties)
                    {
                        if (file.EndsWith("_._")) continue;
                        path = Path.Combine(packagesPath, name, file);
                        if (!File.Exists(path)) continue;
                        compilation.AddReferences(MetadataReference.CreateFromFile(path));
                    }
                }
            }
            CompilationContext context = new(compilation, options);
            context.Compile();
            return context;
        }

        public static CompilationContext CompileSources(string[] sourceFiles, Options options)
        {
            return Compile(null, sourceFiles, options);
        }

        public static CompilationContext CompileProject(string csproj, Options options)
        {
            string folder = Path.GetDirectoryName(csproj)!;
            string obj = Path.Combine(folder, "obj");
            string[] sourceFiles = Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories).Where(p => !p.StartsWith(obj)).ToArray();
            return Compile(csproj, sourceFiles, options);
        }

        public NefFile CreateExecutable()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            var titleAttribute = assembly.GetCustomAttribute<AssemblyTitleAttribute>()!;
            var versionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!;
            NefFile nef = new()
            {
                Compiler = $"{titleAttribute.Title} {versionAttribute.InformationalVersion}",
                Tokens = methodTokens.ToArray(),
                Script = GetInstructions().Select(p => p.ToArray()).SelectMany(p => p).ToArray()
            };
            nef.CheckSum = NefFile.ComputeChecksum(nef);
            return nef;
        }

        public string CreateAssembly()
        {
            static void WriteMethod(StringBuilder builder, MethodConvert method)
            {
                foreach (Instruction i in method.Instructions)
                {
                    builder.Append($"{i.Offset:x8}: ");
                    i.ToString(builder);
                    builder.AppendLine();
                }
                builder.AppendLine();
                builder.AppendLine();
            }
            StringBuilder builder = new();
            foreach (MethodConvert method in methodsConverted)
            {
                builder.Append("// ");
                builder.AppendLine(method.Symbol.ToString());
                builder.AppendLine();
                WriteMethod(builder, method);
            }
            foreach (MethodConvert method in methodsForward)
            {
                builder.Append("// ");
                builder.Append(method.Symbol.ToString());
                builder.AppendLine(" (Forward)");
                builder.AppendLine();
                WriteMethod(builder, method);
            }
            return builder.ToString();
        }

        public JObject CreateManifest()
        {
            return new JObject
            {
                ["name"] = ContractName,
                ["groups"] = new JArray(),
                ["supportedstandards"] = supportedStandards.Select(p => (JString)p).ToArray(),
                ["abi"] = new JObject
                {
                    ["methods"] = methodsExported.Select(p => new JObject
                    {
                        ["name"] = p.Name,
                        ["offset"] = GetAbiOffset(p.Symbol),
                        ["safe"] = p.Safe,
                        ["returntype"] = p.ReturnType,
                        ["parameters"] = p.Parameters.Select(p => p.ToJson()).ToArray()
                    }).ToArray(),
                    ["events"] = eventsExported.Select(p => new JObject
                    {
                        ["name"] = p.Name,
                        ["parameters"] = p.Parameters.Select(p => p.ToJson()).ToArray()
                    }).ToArray()
                },
                ["permissions"] = permissions.ToJson(),
                ["trusts"] = new JArray(),
                ["extra"] = manifestExtra
            };
        }

        public JObject CreateDebugInformation()
        {
            SyntaxTree[] trees = compilation.SyntaxTrees.ToArray();
            return new JObject
            {
                ["documents"] = compilation.SyntaxTrees.Select(p => (JString)p.FilePath).ToArray(),
                ["methods"] = methodsConverted.Where(p => p.SyntaxNode is not null).Select(m => new JObject
                {
                    ["id"] = m.Symbol.ToString(),
                    ["name"] = $"{m.Symbol.ContainingType},{m.Symbol.Name}",
                    ["range"] = $"{m.Instructions[0].Offset}-{m.Instructions[^1].Offset}",
                    ["params"] = m.Symbol.Parameters.Select(p => (JString)$"{p.Name},{p.Type.GetContractParameterType()}").ToArray(),
                    ["return"] = m.Symbol.ReturnType.GetContractParameterType().ToString(),
                    ["variables"] = m.Variables.Select(p => (JString)$"{p.Name},{p.Type.GetContractParameterType()}").ToArray(),
                    ["sequence-points"] = m.Instructions.Where(p => p.SourceLocation is not null).Select(p =>
                    {
                        FileLinePositionSpan span = p.SourceLocation!.GetLineSpan();
                        return (JString)$"{p.Offset}[{Array.IndexOf(trees, p.SourceLocation.SourceTree)}]{span.StartLinePosition.Line}:{span.StartLinePosition.Character}-{span.EndLinePosition.Line}:{span.EndLinePosition.Character}";
                    }).ToArray()
                }).ToArray(),
                ["events"] = eventsExported.Select(e => new JObject
                {
                    ["id"] = e.Name,
                    ["name"] = $"{e.Symbol.ContainingType},{e.Symbol.Name}",
                    ["params"] = e.Parameters.Select(p => (JString)$"{p.Name},{p.Type}").ToArray()
                }).ToArray()
            };
        }

        private void ProcessCompilationUnit(SemanticModel model, CompilationUnitSyntax syntax)
        {
            foreach (MemberDeclarationSyntax member in syntax.Members)
                ProcessMemberDeclaration(model, member);
        }

        private void ProcessMemberDeclaration(SemanticModel model, MemberDeclarationSyntax syntax)
        {
            switch (syntax)
            {
                case NamespaceDeclarationSyntax @namespace:
                    foreach (MemberDeclarationSyntax member in @namespace.Members)
                        ProcessMemberDeclaration(model, member);
                    break;
                case ClassDeclarationSyntax @class:
                    ProcessClass(model, model.GetDeclaredSymbol(@class)!);
                    break;
            }
        }

        private void ProcessClass(SemanticModel model, INamedTypeSymbol symbol)
        {
            bool isPublic = symbol.DeclaredAccessibility == Accessibility.Public;
            bool isAbstract = symbol.IsAbstract;
            bool isContractType = symbol.IsSubclassOf(nameof(scfx.Neo.SmartContract.Framework.SmartContract));
            bool isSmartContract = !scTypeFound && isPublic && !isAbstract && isContractType;
            if (isSmartContract)
            {
                scTypeFound = true;
                ContractName = symbol.Name;
                foreach (var attribute in symbol.GetAttributes())
                {
                    switch (attribute.AttributeClass!.Name)
                    {
                        case nameof(DisplayNameAttribute):
                            ContractName = (string)attribute.ConstructorArguments[0].Value!;
                            break;
                        case nameof(scfx.Neo.SmartContract.Framework.ManifestExtraAttribute):
                            manifestExtra[(string)attribute.ConstructorArguments[0].Value!] = (string)attribute.ConstructorArguments[1].Value!;
                            break;
                        case nameof(scfx.Neo.SmartContract.Framework.ContractPermissionAttribute):
                            permissions.Add((string)attribute.ConstructorArguments[0].Value!, attribute.ConstructorArguments[1].Values.Select(p => (string)p.Value!).ToArray());
                            break;
                        case nameof(scfx.Neo.SmartContract.Framework.SupportedStandardsAttribute):
                            supportedStandards.AddRange(attribute.ConstructorArguments[0].Values.Select(p => (string)p.Value!));
                            break;
                    }
                }
            }
            foreach (ISymbol member in symbol.GetMembers())
            {
                switch (member)
                {
                    case IEventSymbol @event when isSmartContract:
                        ProcessEvent(@event);
                        break;
                    case IMethodSymbol method when method.MethodKind != MethodKind.StaticConstructor:
                        ProcessMethod(model, method, isSmartContract);
                        break;
                }
            }
            if (isSmartContract && symbol.StaticConstructors.Length > 0)
            {
                ProcessMethod(model, symbol.StaticConstructors[0], true);
            }
        }

        private void ProcessEvent(IEventSymbol symbol)
        {
            if (symbol.DeclaredAccessibility != Accessibility.Public) return;
            INamedTypeSymbol type = (INamedTypeSymbol)symbol.Type;
            if (!type.DelegateInvokeMethod!.ReturnsVoid)
                throw new CompilationException(symbol, DiagnosticId.EventReturns, $"Event return value is not supported.");
            eventsExported.Add(new AbiEvent(symbol));
        }

        private void ProcessMethod(SemanticModel model, IMethodSymbol symbol, bool export)
        {
            if (symbol.MethodKind != MethodKind.StaticConstructor)
            {
                if (symbol.DeclaredAccessibility != Accessibility.Public)
                    export = false;
                if (symbol.MethodKind != MethodKind.Ordinary && symbol.MethodKind != MethodKind.PropertyGet && symbol.MethodKind != MethodKind.PropertySet)
                    return;
            }
            if (export) methodsExported.Add(new AbiMethod(symbol));
            MethodConvert convert = ConvertMethod(model, symbol);
            if (export && !symbol.IsStatic)
            {
                MethodConvert forward = new(this, symbol);
                forward.ConvertForward(model, convert);
                methodsForward.Add(forward);
            }
        }

        internal MethodConvert ConvertMethod(SemanticModel model, IMethodSymbol symbol)
        {
            if (!methodsConverted.TryGetValue(symbol, out MethodConvert? method))
            {
                method = new MethodConvert(this, symbol);
                methodsConverted.Add(method);
                if (!symbol.DeclaringSyntaxReferences.IsEmpty)
                    model = model.Compilation.GetSemanticModel(symbol.DeclaringSyntaxReferences[0].SyntaxTree);
                method.Convert(model);
            }
            return method;
        }

        internal ushort AddMethodToken(UInt160 hash, string method, ushort parametersCount, bool hasReturnValue, CallFlags callFlags)
        {
            int index = methodTokens.FindIndex(p => p.Hash == hash && p.Method == method && p.ParametersCount == parametersCount && p.HasReturnValue == hasReturnValue && p.CallFlags == callFlags);
            if (index >= 0) return (ushort)index;
            methodTokens.Add(new MethodToken
            {
                Hash = hash,
                Method = method,
                ParametersCount = parametersCount,
                HasReturnValue = hasReturnValue,
                CallFlags = callFlags
            });
            permissions.Add(hash.ToString(), method);
            return (ushort)(methodTokens.Count - 1);
        }

        internal byte AddStaticField(IFieldSymbol symbol)
        {
            int index = staticFields.IndexOf(symbol);
            if (index == -1)
            {
                index = staticFields.Count;
                staticFields.Add(symbol);
            }
            return (byte)index;
        }
    }
}
