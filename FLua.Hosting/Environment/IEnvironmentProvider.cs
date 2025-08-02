using FLua.Runtime;
using FLua.Hosting.Security;

namespace FLua.Hosting.Environment;

/// <summary>
/// Provides custom Lua environments for hosted code execution.
/// Implementations can create specialized environments with custom functions,
/// security restrictions, and host-provided capabilities.
/// </summary>
public interface IEnvironmentProvider
{
    /// <summary>
    /// Creates a Lua environment configured for the specified trust level.
    /// </summary>
    /// <param name="trustLevel">The trust level for security restrictions</param>
    /// <param name="options">Host options for additional configuration</param>
    /// <returns>A configured LuaEnvironment</returns>
    LuaEnvironment CreateEnvironment(TrustLevel trustLevel, LuaHostOptions? options = null);
    
    /// <summary>
    /// Configures the module loading system for the environment.
    /// </summary>
    /// <param name="environment">The environment to configure</param>
    /// <param name="moduleResolver">The module resolver to use</param>
    /// <param name="trustLevel">Current trust level</param>
    void ConfigureModuleSystem(LuaEnvironment environment, IModuleResolver? moduleResolver, TrustLevel trustLevel);
    
    /// <summary>
    /// Adds host-provided functions to the environment.
    /// </summary>
    /// <param name="environment">The environment to configure</param>
    /// <param name="hostFunctions">Dictionary of function name to implementation</param>
    void AddHostFunctions(LuaEnvironment environment, Dictionary<string, Func<LuaValue[], LuaValue>> hostFunctions);
    
    /// <summary>
    /// Injects host context variables into the environment.
    /// </summary>
    /// <param name="environment">The environment to configure</param>
    /// <param name="hostContext">Context variables to inject</param>
    void InjectHostContext(LuaEnvironment environment, Dictionary<string, object> hostContext);
}