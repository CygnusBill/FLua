using FLua.Runtime;
using FLua.Hosting.Environment;

namespace FLua.Hosting.Security;

/// <summary>
/// Standard security policy implementation for FLua hosting.
/// Provides default security rules based on trust levels.
/// </summary>
public class StandardSecurityPolicy : ILuaSecurityPolicy
{
    private readonly IEnvironmentProvider _environmentProvider;
    
    // Function restrictions by trust level
    private readonly Dictionary<TrustLevel, HashSet<string>> _blockedFunctions = new()
    {
        [TrustLevel.Untrusted] = new()
        {
            "load", "loadfile", "dofile", "require", "collectgarbage",
            "rawget", "rawset", "rawequal", "rawlen", "getmetatable", "setmetatable",
            "pcall", "xpcall", "error", "warn"
        },
        [TrustLevel.Sandbox] = new()
        {
            "load", "loadfile", "dofile", "require", "collectgarbage"
        },
        [TrustLevel.Restricted] = new()
        {
            "loadfile", "dofile"
        },
        [TrustLevel.Trusted] = new(),
        [TrustLevel.FullTrust] = new()
    };
    
    // Library availability by trust level
    private readonly Dictionary<TrustLevel, HashSet<string>> _allowedLibraries = new()
    {
        [TrustLevel.Untrusted] = new() { "math", "string" },
        [TrustLevel.Sandbox] = new() { "math", "string", "table", "coroutine", "utf8" },
        [TrustLevel.Restricted] = new() { "math", "string", "table", "coroutine", "utf8", "os" },
        [TrustLevel.Trusted] = new() { "math", "string", "table", "coroutine", "utf8", "os", "io", "package" },
        [TrustLevel.FullTrust] = new() { "math", "string", "table", "coroutine", "utf8", "os", "io", "package", "debug" }
    };
    
    public StandardSecurityPolicy(IEnvironmentProvider? environmentProvider = null)
    {
        _environmentProvider = environmentProvider ?? new FilteredEnvironmentProvider(this);
    }
    
    public LuaEnvironment CreateSecureEnvironment(TrustLevel trustLevel, LuaHostOptions? options = null)
    {
        return _environmentProvider.CreateEnvironment(trustLevel, options);
    }
    
    public bool IsAllowedFunction(string functionName, TrustLevel trustLevel)
    {
        if (_blockedFunctions.TryGetValue(trustLevel, out var blocked))
        {
            return !blocked.Contains(functionName);
        }
        
        return true;
    }
    
    public bool IsAllowedLibrary(string libraryName, TrustLevel trustLevel)
    {
        if (_allowedLibraries.TryGetValue(trustLevel, out var allowed))
        {
            return allowed.Contains(libraryName);
        }
        
        return false;
    }
    
    public IEnumerable<string> GetBlockedFunctions(TrustLevel trustLevel)
    {
        return _blockedFunctions.TryGetValue(trustLevel, out var blocked) 
            ? blocked 
            : Enumerable.Empty<string>();
    }
    
    public IEnumerable<string> GetAllowedLibraries(TrustLevel trustLevel)
    {
        return _allowedLibraries.TryGetValue(trustLevel, out var allowed) 
            ? allowed 
            : Enumerable.Empty<string>();
    }
    
    public SecurityRestrictions GetRestrictionsForTrustLevel(TrustLevel trustLevel)
    {
        return new SecurityRestrictions
        {
            ForbiddenGlobals = GetBlockedFunctions(trustLevel),
            AllowedLibraries = GetAllowedLibraries(trustLevel),
            BlockedFunctions = GetBlockedFunctions(trustLevel),
            ForbiddenModules = GetForbiddenModules(trustLevel)
        };
    }
    
    private IEnumerable<string> GetForbiddenModules(TrustLevel trustLevel)
    {
        return trustLevel switch
        {
            TrustLevel.Untrusted => new[] { "*" }, // All modules forbidden
            TrustLevel.Sandbox => new[] { "io", "os", "package", "debug", "ffi" },
            TrustLevel.Restricted => new[] { "debug", "ffi" },
            TrustLevel.Trusted => new[] { "debug", "ffi" },
            TrustLevel.FullTrust => Array.Empty<string>(),
            _ => new[] { "*" }
        };
    }
}