using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FLua.Common;
using FLua.Runtime;
using FLua.Hosting.Security;

namespace FLua.Hosting
{
    /// <summary>
    /// Result-based interface for hosting Lua code in .NET applications.
    /// Provides explicit error handling through Result patterns instead of exceptions.
    /// </summary>
    public interface IResultLuaHost
    {
        /// <summary>
        /// Compiles Lua code to a strongly-typed function with Result-based error handling.
        /// </summary>
        /// <typeparam name="T">The return type of the function</typeparam>
        /// <param name="luaCode">The Lua code to compile</param>
        /// <param name="options">Optional hosting options</param>
        /// <returns>A hosting result containing the compiled function or error diagnostics</returns>
        HostingResult<Func<T>> CompileToFunctionResult<T>(string luaCode, LuaHostOptions? options = null);
        
        /// <summary>
        /// Compiles Lua code to a function with parameters using Result pattern.
        /// </summary>
        /// <typeparam name="TResult">The return type of the function</typeparam>
        /// <param name="luaCode">The Lua code to compile</param>
        /// <param name="delegateType">The delegate type to compile to</param>
        /// <param name="parameterNames">Names of parameters accessible in Lua code</param>
        /// <param name="options">Optional hosting options</param>
        /// <returns>A hosting result containing the compiled delegate or error diagnostics</returns>
        HostingResult<Delegate> CompileToDelegateResult(string luaCode, Type delegateType, string[]? parameterNames = null, LuaHostOptions? options = null);
        
        /// <summary>
        /// Compiles Lua code to an expression tree with Result-based error handling.
        /// </summary>
        /// <typeparam name="T">The return type of the expression</typeparam>
        /// <param name="luaCode">The Lua code to compile</param>
        /// <param name="options">Optional hosting options</param>
        /// <returns>A hosting result containing the expression tree or error diagnostics</returns>
        HostingResult<Expression<Func<T>>> CompileToExpressionResult<T>(string luaCode, LuaHostOptions? options = null);
        
        /// <summary>
        /// Compiles Lua code to an assembly with Result-based error handling.
        /// </summary>
        /// <param name="luaCode">The Lua code to compile</param>
        /// <param name="options">Optional hosting options</param>
        /// <returns>A hosting result containing the compiled assembly or error diagnostics</returns>
        HostingResult<Assembly> CompileToAssemblyResult(string luaCode, LuaHostOptions? options = null);
        
        /// <summary>
        /// Compiles Lua code to assembly bytes with Result-based error handling.
        /// </summary>
        /// <param name="luaCode">The Lua code to compile</param>
        /// <param name="options">Optional hosting options</param>
        /// <returns>A hosting result containing the assembly bytes or error diagnostics</returns>
        HostingResult<byte[]> CompileToBytesResult(string luaCode, LuaHostOptions? options = null);
        
        /// <summary>
        /// Executes Lua code in a secure environment with Result-based error handling.
        /// </summary>
        /// <param name="luaCode">The Lua code to execute</param>
        /// <param name="options">Optional hosting options</param>
        /// <returns>A hosting result containing the execution result or error diagnostics</returns>
        HostingResult<LuaValue> ExecuteResult(string luaCode, LuaHostOptions? options = null);
        
        /// <summary>
        /// Executes Lua code asynchronously with Result-based error handling.
        /// </summary>
        /// <param name="luaCode">The Lua code to execute</param>
        /// <param name="options">Optional hosting options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A hosting result containing the execution result or error diagnostics</returns>
        Task<HostingResult<LuaValue>> ExecuteResultAsync(string luaCode, LuaHostOptions? options = null, CancellationToken cancellationToken = default);
        
        /// <summary>
        /// Creates a filtered Lua environment with Result-based error handling.
        /// </summary>
        /// <param name="trustLevel">The trust level for the environment</param>
        /// <param name="options">Optional hosting options</param>
        /// <returns>A hosting result containing the configured environment or error diagnostics</returns>
        HostingResult<LuaEnvironment> CreateFilteredEnvironmentResult(TrustLevel trustLevel, LuaHostOptions? options = null);
        
        /// <summary>
        /// Validates Lua code with detailed Result-based diagnostics.
        /// </summary>
        /// <param name="luaCode">The Lua code to validate</param>
        /// <returns>A hosting result containing validation information or error diagnostics</returns>
        HostingResult<ValidationInfo> ValidateCodeResult(string luaCode);
        
        /// <summary>
        /// Default options used when none are specified.
        /// </summary>
        LuaHostOptions DefaultOptions { get; set; }
        
        /// <summary>
        /// Security policy used for creating secure environments.
        /// </summary>
        ILuaSecurityPolicy SecurityPolicy { get; set; }
        
        /// <summary>
        /// Module resolver used for handling require() calls.
        /// </summary>
        IModuleResolver ModuleResolver { get; set; }
    }

    /// <summary>
    /// Enhanced validation information with detailed diagnostics
    /// </summary>
    public record ValidationInfo
    {
        /// <summary>
        /// Whether the code is syntactically valid
        /// </summary>
        public bool IsValid { get; init; }
        
        /// <summary>
        /// Detailed syntax errors found during parsing
        /// </summary>
        public List<SyntaxError> SyntaxErrors { get; init; } = new();
        
        /// <summary>
        /// Semantic warnings about the code
        /// </summary>
        public List<SemanticWarning> SemanticWarnings { get; init; } = new();
        
        /// <summary>
        /// Security policy violations found in the code
        /// </summary>
        public List<SecurityViolation> SecurityViolations { get; init; } = new();
        
        /// <summary>
        /// Performance recommendations for the code
        /// </summary>
        public List<PerformanceHint> PerformanceHints { get; init; } = new();
        
        /// <summary>
        /// Information about the parsed AST structure
        /// </summary>
        public AstInfo? AstInfo { get; init; }
        
        /// <summary>
        /// Estimated complexity metrics
        /// </summary>
        public ComplexityMetrics? Complexity { get; init; }
        
        /// <summary>
        /// Creates a successful validation result
        /// </summary>
        public static ValidationInfo Valid(AstInfo? astInfo = null, ComplexityMetrics? complexity = null, 
            List<SemanticWarning>? warnings = null, List<PerformanceHint>? hints = null) 
            => new() 
            { 
                IsValid = true, 
                AstInfo = astInfo,
                Complexity = complexity,
                SemanticWarnings = warnings ?? new(),
                PerformanceHints = hints ?? new()
            };
        
        /// <summary>
        /// Creates a failed validation result
        /// </summary>
        public static ValidationInfo Invalid(List<SyntaxError> syntaxErrors, List<SecurityViolation>? violations = null) 
            => new() 
            { 
                IsValid = false, 
                SyntaxErrors = syntaxErrors,
                SecurityViolations = violations ?? new()
            };
    }

    /// <summary>
    /// Represents a syntax error found during parsing
    /// </summary>
    public record SyntaxError(string Message, int Line, int Column, string? Context = null);

    /// <summary>
    /// Represents a semantic warning about potentially problematic code
    /// </summary>
    public record SemanticWarning(string Message, string Category, int? Line = null, int? Column = null);

    /// <summary>
    /// Represents a security policy violation
    /// </summary>
    public record SecurityViolation(string Message, string RuleName, TrustLevel RequiredLevel, int? Line = null, int? Column = null);

    /// <summary>
    /// Represents a performance optimization hint
    /// </summary>
    public record PerformanceHint(string Message, string Category, string Suggestion, int? Line = null, int? Column = null);

    /// <summary>
    /// Information about the parsed AST structure
    /// </summary>
    public record AstInfo(
        int StatementCount,
        int FunctionCount, 
        int LoopCount,
        int MaxNestingDepth,
        List<string> GlobalVariables,
        List<string> LocalVariables,
        bool HasTailCalls,
        bool UsesCoroutines);

    /// <summary>
    /// Code complexity metrics
    /// </summary>
    public record ComplexityMetrics(
        int CyclomaticComplexity,
        int LinesOfCode,
        int TokenCount,
        double HalsteadComplexity,
        TimeSpan EstimatedExecutionTime);
}