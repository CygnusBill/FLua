using FLua.Ast;
using System.Collections.Generic;

namespace FLua.Compiler;

using System.Linq.Expressions;

/// <summary>
/// Represents the output of a Lua compilation
/// </summary>
public record CompilationResult(
    bool Success,
    byte[]? Assembly = null,
    string? AssemblyPath = null,
    IEnumerable<string>? Errors = null,
    IEnumerable<string>? Warnings = null,
    // Hosting-specific results
    Delegate? CompiledDelegate = null,
    LambdaExpression? ExpressionTree = null,
    Type? GeneratedType = null
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
    IEnumerable<string>? References = null,
    // Hosting-specific options
    bool GenerateExpressionTree = false,
    bool GenerateInMemory = false,
    string? ModuleResolverTypeName = null,
    Dictionary<string, string>? HostProvidedTypes = null
);

/// <summary>
/// Compilation target types
/// </summary>
public enum CompilationTarget
{
    Library,        // .dll
    ConsoleApp,     // .exe
    NativeAot,      // native executable (future)
    Lambda,         // in-memory lambda/delegate
    Expression      // expression tree
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