using FLua.Hosting.Security;

namespace FLua.Hosting;

/// <summary>
/// Default file system-based module resolver.
/// Searches configured paths for Lua modules.
/// </summary>
public class FileSystemModuleResolver : IModuleResolver
{
    private readonly List<string> _searchPaths;
    private readonly Dictionary<string, string> _moduleCache = new();
    private readonly bool _enableCaching;
    
    public IReadOnlyList<string> SearchPaths => _searchPaths.AsReadOnly();
    
    public FileSystemModuleResolver(IEnumerable<string>? searchPaths = null, bool enableCaching = true)
    {
        _searchPaths = new List<string>(searchPaths ?? GetDefaultSearchPaths());
        _enableCaching = enableCaching;
    }
    
    public async Task<ModuleResolutionResult> ResolveModuleAsync(string moduleName, ModuleContext context)
    {
        // Check cache first
        if (_enableCaching && _moduleCache.TryGetValue(moduleName, out var cachedPath))
        {
            try
            {
                var cachedSource = await File.ReadAllTextAsync(cachedPath);
                return ModuleResolutionResult.CreateSuccess(cachedSource, cachedPath);
            }
            catch
            {
                // Cache might be stale, remove and continue
                _moduleCache.Remove(moduleName);
            }
        }
        
        // Convert module name to file path (e.g., "foo.bar" -> "foo/bar.lua")
        var moduleFileName = moduleName.Replace('.', Path.DirectorySeparatorChar) + ".lua";
        
        // Search in all configured paths
        foreach (var searchPath in _searchPaths)
        {
            var fullPath = Path.Combine(searchPath, moduleFileName);
            
            if (File.Exists(fullPath))
            {
                try
                {
                    // Check if path is allowed based on trust level
                    if (!IsPathAllowed(fullPath, context.TrustLevel))
                    {
                        return ModuleResolutionResult.CreateFailure(
                            $"Module '{moduleName}' access denied for trust level {context.TrustLevel}");
                    }
                    
                    var sourceCode = await File.ReadAllTextAsync(fullPath);
                    
                    // Cache the resolved path
                    if (_enableCaching)
                    {
                        _moduleCache[moduleName] = fullPath;
                    }
                    
                    return ModuleResolutionResult.CreateSuccess(sourceCode, fullPath);
                }
                catch (Exception ex)
                {
                    return ModuleResolutionResult.CreateFailure(
                        $"Error reading module '{moduleName}': {ex.Message}");
                }
            }
        }
        
        // Also check for init.lua in directory
        var moduleDirName = moduleName.Replace('.', Path.DirectorySeparatorChar);
        foreach (var searchPath in _searchPaths)
        {
            var initPath = Path.Combine(searchPath, moduleDirName, "init.lua");
            
            if (File.Exists(initPath))
            {
                try
                {
                    if (!IsPathAllowed(initPath, context.TrustLevel))
                    {
                        return ModuleResolutionResult.CreateFailure(
                            $"Module '{moduleName}' access denied for trust level {context.TrustLevel}");
                    }
                    
                    var sourceCode = await File.ReadAllTextAsync(initPath);
                    
                    if (_enableCaching)
                    {
                        _moduleCache[moduleName] = initPath;
                    }
                    
                    return ModuleResolutionResult.CreateSuccess(sourceCode, initPath);
                }
                catch (Exception ex)
                {
                    return ModuleResolutionResult.CreateFailure(
                        $"Error reading module '{moduleName}': {ex.Message}");
                }
            }
        }
        
        return ModuleResolutionResult.CreateFailure(
            $"Module '{moduleName}' not found in search paths: {string.Join(", ", _searchPaths)}");
    }
    
    public bool IsModuleAllowed(string moduleName, TrustLevel trustLevel)
    {
        // Define module restrictions based on trust level
        return trustLevel switch
        {
            TrustLevel.Untrusted => false, // No module loading
            TrustLevel.Sandbox => IsStandardLibraryModule(moduleName), // Only standard safe modules
            TrustLevel.Restricted => !IsDangerousModule(moduleName), // Most modules except dangerous ones
            TrustLevel.Trusted or TrustLevel.FullTrust => true, // All modules allowed
            _ => false
        };
    }
    
    private bool IsPathAllowed(string path, TrustLevel trustLevel)
    {
        // Additional path-based security checks
        var fullPath = Path.GetFullPath(path);
        
        return trustLevel switch
        {
            TrustLevel.Untrusted => false,
            TrustLevel.Sandbox => IsInSandboxPath(fullPath),
            TrustLevel.Restricted => !IsSystemPath(fullPath),
            _ => true
        };
    }
    
    private bool IsStandardLibraryModule(string moduleName)
    {
        // Define safe standard library modules
        var safeModules = new HashSet<string>
        {
            "math", "string", "table", "utf8", "coroutine"
        };
        
        return safeModules.Contains(moduleName.Split('.')[0]);
    }
    
    private bool IsDangerousModule(string moduleName)
    {
        // Define potentially dangerous modules
        var dangerousModules = new HashSet<string>
        {
            "ffi", "debug", "io", "os", "package"
        };
        
        return dangerousModules.Contains(moduleName.Split('.')[0]);
    }
    
    private bool IsInSandboxPath(string fullPath)
    {
        // Check if path is within allowed sandbox directories
        // This is a simplified check - real implementation would be more robust
        return fullPath.Contains("sandbox") || fullPath.Contains("modules");
    }
    
    private bool IsSystemPath(string fullPath)
    {
        // Check if path is in system directories
        return fullPath.StartsWith("/usr/") || 
               fullPath.StartsWith("/etc/") || 
               fullPath.StartsWith("C:\\Windows", StringComparison.OrdinalIgnoreCase);
    }
    
    private static IEnumerable<string> GetDefaultSearchPaths()
    {
        // Default search paths
        yield return "."; // Current directory
        yield return "lua_modules"; // Local modules directory
        yield return Path.Combine(AppContext.BaseDirectory, "modules"); // Application modules
        
        // Add LUA_PATH environment variable paths if set
        var luaPath = System.Environment.GetEnvironmentVariable("LUA_PATH");
        if (!string.IsNullOrEmpty(luaPath))
        {
            foreach (var path in luaPath.Split(';'))
            {
                var cleanPath = path.Replace("?.lua", "").Trim();
                if (!string.IsNullOrEmpty(cleanPath) && cleanPath != "?")
                {
                    yield return cleanPath;
                }
            }
        }
    }
    
    /// <summary>
    /// Adds a search path to the resolver.
    /// </summary>
    public void AddSearchPath(string path)
    {
        if (!_searchPaths.Contains(path))
        {
            _searchPaths.Add(path);
        }
    }
    
    /// <summary>
    /// Clears the module cache.
    /// </summary>
    public void ClearCache()
    {
        _moduleCache.Clear();
    }
}