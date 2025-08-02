using FLua.Runtime;

namespace FLua.Hosting.Security;

/// <summary>
/// Defines security policies for hosted Lua code execution.
/// Implementations control which functions and libraries are available
/// based on trust level and other security considerations.
/// </summary>
public interface ILuaSecurityPolicy
{
    /// <summary>
    /// Creates a secure Lua environment based on the specified trust level and options.
    /// </summary>
    /// <param name="trustLevel">The trust level determining available functionality</param>
    /// <param name="options">Additional hosting options that may affect security</param>
    /// <returns>A configured LuaEnvironment with appropriate restrictions</returns>
    LuaEnvironment CreateSecureEnvironment(TrustLevel trustLevel, LuaHostOptions? options = null);
    
    /// <summary>
    /// Determines if a specific function is allowed for the given trust level.
    /// </summary>
    /// <param name="functionName">The name of the function to check</param>
    /// <param name="trustLevel">The trust level to evaluate against</param>
    /// <returns>True if the function is allowed, false otherwise</returns>
    bool IsAllowedFunction(string functionName, TrustLevel trustLevel);
    
    /// <summary>
    /// Determines if a specific library is allowed for the given trust level.
    /// </summary>
    /// <param name="libraryName">The name of the library to check (e.g., "io", "os", "debug")</param>
    /// <param name="trustLevel">The trust level to evaluate against</param>
    /// <returns>True if the library is allowed, false otherwise</returns>
    bool IsAllowedLibrary(string libraryName, TrustLevel trustLevel);
    
    /// <summary>
    /// Gets a list of blocked functions for the specified trust level.
    /// </summary>
    /// <param name="trustLevel">The trust level to get blocked functions for</param>
    /// <returns>A collection of function names that should be blocked</returns>
    IEnumerable<string> GetBlockedFunctions(TrustLevel trustLevel);
    
    /// <summary>
    /// Gets a list of allowed libraries for the specified trust level.
    /// </summary>
    /// <param name="trustLevel">The trust level to get allowed libraries for</param>
    /// <returns>A collection of library names that are allowed</returns>
    IEnumerable<string> GetAllowedLibraries(TrustLevel trustLevel);
}