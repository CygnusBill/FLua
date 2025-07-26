using FLua.Ast;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace FLua.Compiler;

/// <summary>
/// Lua compiler backend using Roslyn for C# code generation
/// </summary>
public class RoslynLuaCompiler : ILuaCompiler
{
    public string BackendName => "Roslyn";
    
    public IEnumerable<CompilationTarget> SupportedTargets => new[]
    {
        CompilationTarget.Library,
        CompilationTarget.ConsoleApp
    };

    public CompilationResult Compile(IList<Statement> ast, CompilerOptions options)
    {
        try
        {
            // Generate C# code from Lua AST
            var csharpCode = GenerateCSharpCode(ast, options);
            
            // Compile using Roslyn
            return CompileWithRoslyn(csharpCode, options);
        }
        catch (Exception ex)
        {
            return new CompilationResult(
                Success: false,
                Errors: new[] { $"Compilation failed: {ex.Message}" }
            );
        }
    }

    private string GenerateCSharpCode(IList<Statement> ast, CompilerOptions options)
    {
        var generator = new CSharpCodeGenerator();
        return generator.Generate(ast, options);
    }

    private CompilationResult CompileWithRoslyn(string csharpCode, CompilerOptions options)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(csharpCode);
        
        var references = GetReferences(options);
        
        var compilation = CSharpCompilation.Create(
            assemblyName: options.AssemblyName ?? "LuaScript",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(
                outputKind: options.Target == CompilationTarget.ConsoleApp 
                    ? OutputKind.ConsoleApplication 
                    : OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: options.Optimization == OptimizationLevel.Release 
                    ? Microsoft.CodeAnalysis.OptimizationLevel.Release 
                    : Microsoft.CodeAnalysis.OptimizationLevel.Debug
            )
        );

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            var errors = result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString());
                
            return new CompilationResult(
                Success: false,
                Errors: errors
            );
        }

        var assembly = ms.ToArray();
        
        // Optionally write to file
        if (!string.IsNullOrEmpty(options.OutputPath))
        {
            File.WriteAllBytes(options.OutputPath, assembly);
        }

        return new CompilationResult(
            Success: true,
            Assembly: assembly,
            AssemblyPath: options.OutputPath
        );
    }

    private static IEnumerable<MetadataReference> GetReferences(CompilerOptions options)
    {
        var references = new List<MetadataReference>
        {
            // Core .NET references
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            
            // FLua runtime reference
            MetadataReference.CreateFromFile(typeof(FLua.Runtime.LuaValue).Assembly.Location),
        };

        // Add custom references
        if (options.References != null)
        {
            foreach (var reference in options.References)
            {
                references.Add(MetadataReference.CreateFromFile(reference));
            }
        }

        return references;
    }
}