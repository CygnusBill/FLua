using FLua.Hosting.Security;

namespace FLua.Hosting;

/// <summary>
/// Default file system-based module resolver.
/// Searches configured paths for Lua modules.
/// </summary>
public class FileSystemModuleResolver : IModuleResolver
{
    private readonly List<string> _searchPaths;
    private readonly Dictionary<string, (string sourceCode, string path)> _moduleCache = new();
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
        if (_enableCaching && _moduleCache.TryGetValue(moduleName, out var cached))
        {
            return ModuleResolutionResult.CreateSuccess(cached.sourceCode, cached.path, cacheable: true);
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
                    
                    // Cache the resolved module
                    if (_enableCaching)
                    {
                        _moduleCache[moduleName] = (sourceCode, fullPath);
                    }
                    
                    return ModuleResolutionResult.CreateSuccess(sourceCode, fullPath, cacheable: true);
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
                        _moduleCache[moduleName] = (sourceCode, initPath);
                    }
                    
                    return ModuleResolutionResult.CreateSuccess(sourceCode, initPath, cacheable: true);
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
            TrustLevel.Restricted => !IsDangerousModule(moduleName) && moduleName != "io", // Most modules except dangerous ones and io
            TrustLevel.Trusted => moduleName != "debug" && !IsDangerousModule(moduleName), // All except debug
            TrustLevel.FullTrust => true, // All modules allowed
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
        // Define potentially dangerous modules (for Restricted level)
        // Only debug and ffi are considered truly dangerous
        var dangerousModules = new HashSet<string>
        {
            "ffi", "debug"
        };
        
        return dangerousModules.Contains(moduleName.Split('.')[0]);
    }
    
    private bool IsInSandboxPath(string fullPath)
    {
        // Check if path is within the configured search paths
        // Sandbox level can only load modules from explicitly configured paths
        var normalizedPath = Path.GetFullPath(fullPath).Replace('\\', '/');
        return _searchPaths.Any(searchPath => 
        {
            var normalizedSearchPath = Path.GetFullPath(searchPath).Replace('\\', '/');
            return normalizedPath.StartsWith(normalizedSearchPath, StringComparison.OrdinalIgnoreCase);
        });
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