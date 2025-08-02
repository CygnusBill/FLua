using System.Linq.Expressions;
using FLua.Compiler;
using FLua.Hosting.Security;
using FLua.Hosting.Environment;
using FLua.Runtime;

namespace FLua.Hosting;

/// <summary>
/// Configuration options for hosting Lua code execution.
/// Controls security, compilation, and runtime behavior.
/// </summary>
public record LuaHostOptions
{
    /// <summary>
    /// Trust level for the hosted code - determines available functionality.
    /// </summary>
    public TrustLevel TrustLevel { get; init; } = TrustLevel.Sandbox;
    
    /// <summary>
    /// Compiler options for code generation.
    /// </summary>
    public CompilerOptions? CompilerOptions { get; init; } = null;
    
    /// <summary>
    /// Custom environment provider for specialized setups.
    /// </summary>
    public IEnvironmentProvider? EnvironmentProvider { get; init; } = null;
    
    /// <summary>
    /// Security policy to apply - if null, uses default policy.
    /// </summary>
    public ILuaSecurityPolicy? SecurityPolicy { get; init; } = null;
    
    /// <summary>
    /// Host-provided context variables available to Lua code.
    /// These are injected as global variables in the Lua environment.
    /// </summary>
    public Dictionary<string, object> HostContext { get; init; } = new();
    
    /// <summary>
    /// Host-provided functions that replace standard library functions.
    /// Key is the function name, value is the host implementation.
    /// </summary>
    public Dictionary<string, Func<LuaValue[], LuaValue>> HostFunctions { get; init; } = new();
    
    /// <summary>
    /// Module resolver for handling require() calls.
    /// The host controls all module loading through this resolver.
    /// </summary>
    public IModuleResolver? ModuleResolver { get; init; } = null;
    
    /// <summary>
    /// Enable debugging support for hosted code.
    /// </summary>
    public bool EnableDebugging { get; init; } = false;
    
    /// <summary>
    /// Maximum execution time before timeout.
    /// </summary>
    public TimeSpan? ExecutionTimeout { get; init; } = null;
    
    /// <summary>
    /// Maximum memory usage allowed for the hosted code.
    /// </summary>
    public long? MaxMemoryUsage { get; init; } = null;
    
    /// <summary>
    /// Search paths for module resolution.
    /// Used by the ModuleResolver to locate Lua modules.
    /// </summary>
    public List<string> ModuleSearchPaths { get; init; } = new() { ".", "lua_modules" };
}