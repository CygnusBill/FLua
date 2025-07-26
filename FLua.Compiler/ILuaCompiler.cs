using FLua.Ast;
using System.Collections.Generic;

namespace FLua.Compiler;

/// <summary>
/// Represents the output of a Lua compilation
/// </summary>
public record CompilationResult(
    bool Success,
    byte[]? Assembly = null,
    string? AssemblyPath = null,
    IEnumerable<string>? Errors = null,
    IEnumerable<string>? Warnings = null
);

/// <summary>
/// Configuration options for Lua compilation
/// </summary>
public record CompilerOptions(
    string OutputPath,
    CompilationTarget Target = CompilationTarget.Library,
    OptimizationLevel Optimization = OptimizationLevel.Release,
    bool IncludeDebugInfo = false,
    string? AssemblyName = null,
    IEnumerable<string>? References = null
);

/// <summary>
/// Compilation target types
/// </summary>
public enum CompilationTarget
{
    Library,        // .dll
    ConsoleApp,     // .exe
    NativeAot       // native executable (future)
}

/// <summary>
/// Optimization levels
/// </summary>
public enum OptimizationLevel
{
    Debug,
    Release
}

/// <summary>
/// Interface for Lua compilation backends
/// </summary>
public interface ILuaCompiler
{
    /// <summary>
    /// Compile a Lua AST to executable code
    /// </summary>
    CompilationResult Compile(IList<Statement> ast, CompilerOptions options);
    
    /// <summary>
    /// Get the supported compilation targets for this backend
    /// </summary>
    IEnumerable<CompilationTarget> SupportedTargets { get; }
    
    /// <summary>
    /// Get the backend name for logging/diagnostics
    /// </summary>
    string BackendName { get; }
}