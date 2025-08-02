namespace FLua.Hosting;

/// <summary>
/// Resolves module imports/requires for hosted Lua code.
/// The host controls module loading through this interface.
/// </summary>
public interface IModuleResolver
{
    /// <summary>
    /// Resolves a module name to its source code.
    /// </summary>
    /// <param name="moduleName">The module name as specified in require()</param>
    /// <param name="context">Context about the requesting module (e.g., current file path)</param>
    /// <returns>Module resolution result containing source code or error information</returns>
    Task<ModuleResolutionResult> ResolveModuleAsync(string moduleName, ModuleContext context);
    
    /// <summary>
    /// Gets the search paths configured for module resolution.
    /// </summary>
    IReadOnlyList<string> SearchPaths { get; }
    
    /// <summary>
    /// Determines if a module is allowed to be loaded based on security policy.
    /// </summary>
    /// <param name="moduleName">The module name to check</param>
    /// <param name="trustLevel">Current trust level</param>
    /// <returns>True if the module can be loaded</returns>
    bool IsModuleAllowed(string moduleName, Security.TrustLevel trustLevel);
}

/// <summary>
/// Result of module resolution attempt.
/// </summary>
public record ModuleResolutionResult
{
    /// <summary>
    /// Whether the module was successfully resolved.
    /// </summary>
    public bool Success { get; init; }
    
    /// <summary>
    /// The module source code (Lua script).
    /// </summary>
    public string? SourceCode { get; init; }
    
    /// <summary>
    /// The resolved file path (for debugging/error reporting).
    /// </summary>
    public string? ResolvedPath { get; init; }
    
    /// <summary>
    /// Error message if resolution failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Module metadata (e.g., version, dependencies).
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }
    
    /// <summary>
    /// Whether the module should be cached.
    /// </summary>
    public bool Cacheable { get; init; } = true;
    
    /// <summary>
    /// Creates a successful resolution result.
    /// </summary>
    public static ModuleResolutionResult CreateSuccess(string sourceCode, string resolvedPath, bool cacheable = true)
        => new() { Success = true, SourceCode = sourceCode, ResolvedPath = resolvedPath, Cacheable = cacheable };
    
    /// <summary>
    /// Creates a failed resolution result.
    /// </summary>
    public static ModuleResolutionResult CreateFailure(string errorMessage)
        => new() { Success = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Context information for module resolution.
/// </summary>
public record ModuleContext
{
    /// <summary>
    /// The file path of the module making the require() call.
    /// </summary>
    public string? RequestingModulePath { get; init; }
    
    /// <summary>
    /// Current trust level of the execution context.
    /// </summary>
    public Security.TrustLevel TrustLevel { get; init; }
    
    /// <summary>
    /// Additional context data from the host.
    /// </summary>
    public Dictionary<string, object> HostContext { get; init; } = new();
}