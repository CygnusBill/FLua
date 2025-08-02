using System.Linq.Expressions;
using System.Reflection;
using FLua.Runtime;
using FLua.Hosting.Security;

namespace FLua.Hosting;

/// <summary>
/// Main interface for hosting Lua code in .NET applications.
/// Provides string-to-lambda transformation, assembly generation,
/// and secure execution environments.
/// </summary>
public interface ILuaHost
{
    /// <summary>
    /// Compiles Lua code to a strongly-typed function.
    /// </summary>
    /// <typeparam name="T">The return type of the function</typeparam>
    /// <param name="luaCode">The Lua code to compile</param>
    /// <param name="options">Optional hosting options</param>
    /// <returns>A compiled function that can be invoked</returns>
    Func<T> CompileToFunction<T>(string luaCode, LuaHostOptions? options = null);
    
    /// <summary>
    /// Compiles Lua code to a function with parameters.
    /// </summary>
    /// <typeparam name="TResult">The return type of the function</typeparam>
    /// <param name="luaCode">The Lua code to compile</param>
    /// <param name="parameterNames">Names of parameters accessible in Lua code</param>
    /// <param name="options">Optional hosting options</param>
    /// <returns>A compiled function that accepts parameters</returns>
    Delegate CompileToDelegate(string luaCode, Type delegateType, string[]? parameterNames = null, LuaHostOptions? options = null);
    
    /// <summary>
    /// Compiles Lua code to an expression tree for inspection or further compilation.
    /// </summary>
    /// <typeparam name="T">The return type of the expression</typeparam>
    /// <param name="luaCode">The Lua code to compile</param>
    /// <param name="options">Optional hosting options</param>
    /// <returns>An expression tree representing the Lua code</returns>
    Expression<Func<T>> CompileToExpression<T>(string luaCode, LuaHostOptions? options = null);
    
    /// <summary>
    /// Compiles Lua code to an assembly that can be saved or loaded.
    /// </summary>
    /// <param name="luaCode">The Lua code to compile</param>
    /// <param name="options">Optional hosting options</param>
    /// <returns>A compiled assembly</returns>
    Assembly CompileToAssembly(string luaCode, LuaHostOptions? options = null);
    
    /// <summary>
    /// Compiles Lua code to assembly bytes for distribution or storage.
    /// </summary>
    /// <param name="luaCode">The Lua code to compile</param>
    /// <param name="options">Optional hosting options</param>
    /// <returns>The compiled assembly as a byte array</returns>
    byte[] CompileToBytes(string luaCode, LuaHostOptions? options = null);
    
    /// <summary>
    /// Executes Lua code in a secure environment and returns the result.
    /// </summary>
    /// <param name="luaCode">The Lua code to execute</param>
    /// <param name="options">Optional hosting options</param>
    /// <returns>The execution result</returns>
    LuaValue Execute(string luaCode, LuaHostOptions? options = null);
    
    /// <summary>
    /// Executes Lua code asynchronously with cancellation support.
    /// </summary>
    /// <param name="luaCode">The Lua code to execute</param>
    /// <param name="options">Optional hosting options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The execution result</returns>
    Task<LuaValue> ExecuteAsync(string luaCode, LuaHostOptions? options = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a filtered Lua environment based on trust level.
    /// </summary>
    /// <param name="trustLevel">The trust level for the environment</param>
    /// <param name="options">Optional hosting options</param>
    /// <returns>A configured Lua environment</returns>
    LuaEnvironment CreateFilteredEnvironment(TrustLevel trustLevel, LuaHostOptions? options = null);
    
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
    
    /// <summary>
    /// Validates Lua code without executing it.
    /// </summary>
    /// <param name="luaCode">The Lua code to validate</param>
    /// <returns>Validation result with any syntax errors</returns>
    ValidationResult ValidateCode(string luaCode);
}

/// <summary>
/// Result of code validation.
/// </summary>
public record ValidationResult
{
    /// <summary>
    /// Whether the code is valid.
    /// </summary>
    public bool IsValid { get; init; }
    
    /// <summary>
    /// Any syntax or semantic errors found.
    /// </summary>
    public List<string> Errors { get; init; } = new();
    
    /// <summary>
    /// Any warnings about the code.
    /// </summary>
    public List<string> Warnings { get; init; } = new();
    
    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    public static ValidationResult Success() => new() { IsValid = true };
    
    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static ValidationResult Failure(params string[] errors) 
        => new() { IsValid = false, Errors = errors.ToList() };
}