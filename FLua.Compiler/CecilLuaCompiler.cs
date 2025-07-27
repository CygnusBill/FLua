using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FLua.Ast;
using FLua.Common.Diagnostics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace FLua.Compiler;

/// <summary>
/// Mono.Cecil-based Lua compiler for size-optimized compilation
/// Generates IL directly without Roslyn dependency
/// </summary>
public class CecilLuaCompiler : ILuaCompiler
{
    private readonly IDiagnosticCollector _diagnostics = new DiagnosticCollector();
    
    public IEnumerable<CompilationTarget> SupportedTargets =>
        [CompilationTarget.Library, CompilationTarget.ConsoleApp];
    
    public string BackendName => "Mono.Cecil";
    
    public CompilationResult Compile(IList<Statement> ast, CompilerOptions options)
    {
        try
        {
            var generator = new CecilCodeGenerator(_diagnostics);
            var assembly = generator.GenerateAssembly(ast, options);
            
            // Save the assembly to disk
            if (!string.IsNullOrEmpty(options.OutputPath))
            {
                var outputPath = Path.GetFullPath(options.OutputPath);
                var outputDir = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }
                
                assembly.Write(outputPath);
                
                return new CompilationResult(
                    Success: true,
                    Assembly: File.ReadAllBytes(outputPath),
                    AssemblyPath: outputPath,
                    Errors: null,
                    Warnings: GetWarnings()
                );
            }
            
            // For in-memory usage, write to memory stream
            using var ms = new MemoryStream();
            assembly.Write(ms);
            
            return new CompilationResult(
                Success: true,
                Assembly: ms.ToArray(),
                AssemblyPath: null,
                Errors: null,
                Warnings: GetWarnings()
            );
        }
        catch (Exception ex)
        {
            var errorMessage = ex.ToString(); // Get full stack trace for debugging
            return new CompilationResult(
                Success: false,
                Assembly: null,
                AssemblyPath: null,
                Errors: [errorMessage],
                Warnings: GetWarnings()
            );
        }
    }
    
    private IEnumerable<string>? GetWarnings()
    {
        var diagnostics = _diagnostics.GetDiagnostics();
        var warnings = diagnostics
            .Where(d => d.Severity == ErrorSeverity.Warning)
            .Select(d => d.Message)
            .ToList();
        
        return warnings.Any() ? warnings : null;
    }
}