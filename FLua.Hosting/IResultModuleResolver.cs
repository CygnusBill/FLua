using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FLua.Common;
using FLua.Hosting.Security;

namespace FLua.Hosting
{
    /// <summary>
    /// Result-based interface for resolving module imports/requires for hosted Lua code.
    /// Provides explicit error handling through Result patterns instead of exceptions.
    /// </summary>
    public interface IResultModuleResolver
    {
        /// <summary>
        /// Resolves a module name to its source code with Result-based error handling.
        /// </summary>
        /// <param name="moduleName">The module name as specified in require()</param>
        /// <param name="context">Context about the requesting module</param>
        /// <returns>A hosting result containing the module information or error diagnostics</returns>
        Task<HostingResult<ModuleInfo>> ResolveModuleResultAsync(string moduleName, ModuleContext context);
        
        /// <summary>
        /// Gets the search paths configured for module resolution.
        /// </summary>
        IReadOnlyList<string> SearchPaths { get; }
        
        /// <summary>
        /// Determines if a module is allowed to be loaded based on security policy with Result-based validation.
        /// </summary>
        /// <param name="moduleName">The module name to check</param>
        /// <param name="trustLevel">Current trust level</param>
        /// <returns>A result indicating whether the module is allowed with detailed reasoning</returns>
        HostingResult<ModulePermission> ValidateModulePermissionResult(string moduleName, TrustLevel trustLevel);
        
        /// <summary>
        /// Pre-validates a module name format and availability without loading it.
        /// </summary>
        /// <param name="moduleName">The module name to pre-validate</param>
        /// <param name="context">Module resolution context</param>
        /// <returns>A result indicating validation success with any warnings or errors</returns>
        HostingResult<ModuleValidation> PreValidateModuleResult(string moduleName, ModuleContext context);
    }

    /// <summary>
    /// Enhanced module information with detailed metadata
    /// </summary>
    public record ModuleInfo
    {
        /// <summary>
        /// The module source code (Lua script)
        /// </summary>
        public string SourceCode { get; init; } = string.Empty;
        
        /// <summary>
        /// The resolved file path (for debugging/error reporting)
        /// </summary>
        public string? ResolvedPath { get; init; }
        
        /// <summary>
        /// Module metadata (version, dependencies, etc.)
        /// </summary>
        public ModuleMetadata Metadata { get; init; } = new();
        
        /// <summary>
        /// Whether the module should be cached
        /// </summary>
        public bool Cacheable { get; init; } = true;
        
        /// <summary>
        /// Security classification of the module
        /// </summary>
        public ModuleSecurity Security { get; init; } = new();
        
        /// <summary>
        /// Dependencies required by this module
        /// </summary>
        public List<ModuleDependency> Dependencies { get; init; } = new();
    }

    /// <summary>
    /// Module permission validation result
    /// </summary>
    public record ModulePermission
    {
        /// <summary>
        /// Whether the module is allowed to be loaded
        /// </summary>
        public bool IsAllowed { get; init; }
        
        /// <summary>
        /// Reason for permission decision
        /// </summary>
        public string Reason { get; init; } = string.Empty;
        
        /// <summary>
        /// Required trust level to load this module
        /// </summary>
        public TrustLevel RequiredTrustLevel { get; init; }
        
        /// <summary>
        /// Security restrictions that apply to this module
        /// </summary>
        public List<string> SecurityRestrictions { get; init; } = new();
    }

    /// <summary>
    /// Module pre-validation result
    /// </summary>
    public record ModuleValidation
    {
        /// <summary>
        /// Whether the module name is valid
        /// </summary>
        public bool IsValidName { get; init; }
        
        /// <summary>
        /// Whether the module is available for loading
        /// </summary>
        public bool IsAvailable { get; init; }
        
        /// <summary>
        /// Potential file paths where the module might be found
        /// </summary>
        public List<string> CandidatePaths { get; init; } = new();
        
        /// <summary>
        /// Validation warnings (non-fatal issues)
        /// </summary>
        public List<string> Warnings { get; init; } = new();
    }

    /// <summary>
    /// Module metadata information
    /// </summary>
    public record ModuleMetadata
    {
        /// <summary>
        /// Module version
        /// </summary>
        public string? Version { get; init; }
        
        /// <summary>
        /// Module author(s)
        /// </summary>
        public List<string> Authors { get; init; } = new();
        
        /// <summary>
        /// Module description
        /// </summary>
        public string? Description { get; init; }
        
        /// <summary>
        /// Module license
        /// </summary>
        public string? License { get; init; }
        
        /// <summary>
        /// Last modification time
        /// </summary>
        public DateTimeOffset? LastModified { get; init; }
        
        /// <summary>
        /// File size in bytes
        /// </summary>
        public long? FileSize { get; init; }
        
        /// <summary>
        /// Additional custom metadata
        /// </summary>
        public Dictionary<string, object> CustomMetadata { get; init; } = new();
    }

    /// <summary>
    /// Security information about a module
    /// </summary>
    public record ModuleSecurity
    {
        /// <summary>
        /// Minimum trust level required to load this module
        /// </summary>
        public TrustLevel MinimumTrustLevel { get; init; } = TrustLevel.Untrusted;
        
        /// <summary>
        /// Whether the module performs file I/O operations
        /// </summary>
        public bool RequiresFileAccess { get; init; }
        
        /// <summary>
        /// Whether the module requires network access
        /// </summary>
        public bool RequiresNetworkAccess { get; init; }
        
        /// <summary>
        /// Whether the module can execute system commands
        /// </summary>
        public bool RequiresSystemAccess { get; init; }
        
        /// <summary>
        /// List of potentially dangerous operations the module performs
        /// </summary>
        public List<string> DangerousOperations { get; init; } = new();
        
        /// <summary>
        /// Digital signature or hash for integrity verification
        /// </summary>
        public string? IntegrityHash { get; init; }
    }

    /// <summary>
    /// Information about module dependencies
    /// </summary>
    public record ModuleDependency
    {
        /// <summary>
        /// Name of the required module
        /// </summary>
        public string Name { get; init; } = string.Empty;
        
        /// <summary>
        /// Required version (if specified)
        /// </summary>
        public string? Version { get; init; }
        
        /// <summary>
        /// Whether this is an optional dependency
        /// </summary>
        public bool IsOptional { get; init; }
        
        /// <summary>
        /// Reason why this dependency is needed
        /// </summary>
        public string? Purpose { get; init; }
    }

    /// <summary>
    /// Result-based implementation of a file system module resolver
    /// </summary>
    public class ResultFileSystemModuleResolver : IResultModuleResolver
    {
        private readonly List<string> _searchPaths;
        private readonly ILuaSecurityPolicy _securityPolicy;
        private readonly Dictionary<string, ModuleInfo> _moduleCache;
        
        public IReadOnlyList<string> SearchPaths => _searchPaths;
        
        public ResultFileSystemModuleResolver(ILuaSecurityPolicy securityPolicy, IEnumerable<string>? searchPaths = null)
        {
            _securityPolicy = securityPolicy ?? throw new ArgumentNullException(nameof(securityPolicy));
            _searchPaths = searchPaths?.ToList() ?? new List<string> { ".", "./modules", "./lib" };
            _moduleCache = new Dictionary<string, ModuleInfo>();
        }
        
        public async Task<HostingResult<ModuleInfo>> ResolveModuleResultAsync(string moduleName, ModuleContext context)
        {
            try
            {
                var diagnostics = new List<HostingDiagnostic>();
                
                // Pre-validate the module name
                var preValidationResult = PreValidateModuleResult(moduleName, context);
                if (!preValidationResult.IsSuccess)
                    return HostingResult<ModuleInfo>.Failure(preValidationResult.Diagnostics);
                
                var validation = preValidationResult.Value;
                if (!validation.IsValidName)
                {
                    return HostingResult<ModuleInfo>.Error(
                        $"Invalid module name: '{moduleName}'",
                        HostingOperation.ModuleResolution);
                }
                
                // Check permissions
                var permissionResult = ValidateModulePermissionResult(moduleName, context.TrustLevel);
                if (!permissionResult.IsSuccess)
                    return HostingResult<ModuleInfo>.Failure(permissionResult.Diagnostics);
                
                var permission = permissionResult.Value;
                if (!permission.IsAllowed)
                {
                    return HostingResult<ModuleInfo>.Error(
                        $"Module '{moduleName}' not allowed: {permission.Reason}",
                        HostingOperation.SecurityCheck);
                }
                
                // Check cache first
                if (_moduleCache.TryGetValue(moduleName, out var cachedModule) && cachedModule.Cacheable)
                {
                    diagnostics.Add(new HostingDiagnostic(
                        DiagnosticSeverity.Info,
                        $"Module '{moduleName}' loaded from cache",
                        HostingOperation.ModuleResolution));
                    return HostingResult<ModuleInfo>.Success(cachedModule, diagnostics);
                }
                
                // Try to resolve from search paths
                foreach (var searchPath in _searchPaths)
                {
                    var resolveResult = await TryResolveFromPath(moduleName, searchPath, context);
                    if (resolveResult.IsSuccess)
                    {
                        var moduleInfo = resolveResult.Value;
                        
                        // Cache successful resolution
                        if (moduleInfo.Cacheable)
                        {
                            _moduleCache[moduleName] = moduleInfo;
                        }
                        
                        diagnostics.AddRange(resolveResult.Diagnostics);
                        diagnostics.Add(new HostingDiagnostic(
                            DiagnosticSeverity.Info,
                            $"Module '{moduleName}' resolved from '{searchPath}'",
                            HostingOperation.ModuleResolution));
                        
                        return HostingResult<ModuleInfo>.Success(moduleInfo, diagnostics);
                    }
                    
                    // Collect warnings from failed attempts
                    diagnostics.AddRange(resolveResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning));
                }
                
                // Module not found in any search path
                return HostingResult<ModuleInfo>.Error(
                    $"Module '{moduleName}' not found in search paths: {string.Join(", ", _searchPaths)}",
                    HostingOperation.ModuleResolution);
            }
            catch (Exception ex)
            {
                return HostingResult<ModuleInfo>.FromException(ex, HostingOperation.ModuleResolution);
            }
        }
        
        public HostingResult<ModulePermission> ValidateModulePermissionResult(string moduleName, TrustLevel trustLevel)
        {
            try
            {
                // Check security policy restrictions
                var restrictions = _securityPolicy.GetRestrictionsForTrustLevel(trustLevel);
                
                // Check if module is explicitly forbidden
                if (restrictions.ForbiddenModules.Contains(moduleName))
                {
                    var permission = new ModulePermission
                    {
                        IsAllowed = false,
                        Reason = $"Module '{moduleName}' is explicitly forbidden for trust level {trustLevel}",
                        RequiredTrustLevel = TrustLevel.FullTrust
                    };
                    return HostingResult<ModulePermission>.Success(permission);
                }
                
                // Determine required trust level based on module name patterns
                var requiredTrustLevel = DetermineRequiredTrustLevel(moduleName);
                var isAllowed = trustLevel >= requiredTrustLevel;
                
                var result = new ModulePermission
                {
                    IsAllowed = isAllowed,
                    Reason = isAllowed 
                        ? $"Module '{moduleName}' is allowed for trust level {trustLevel}" 
                        : $"Module '{moduleName}' requires trust level {requiredTrustLevel} but current level is {trustLevel}",
                    RequiredTrustLevel = requiredTrustLevel,
                    SecurityRestrictions = GetSecurityRestrictions(moduleName, trustLevel).ToList()
                };
                
                return HostingResult<ModulePermission>.Success(result);
            }
            catch (Exception ex)
            {
                return HostingResult<ModulePermission>.FromException(ex, HostingOperation.SecurityCheck);
            }
        }
        
        public HostingResult<ModuleValidation> PreValidateModuleResult(string moduleName, ModuleContext context)
        {
            try
            {
                var validation = new ModuleValidation();
                var warnings = new List<string>();
                var candidatePaths = new List<string>();
                
                // Validate module name format
                var isValidName = ValidateModuleName(moduleName);
                if (!isValidName)
                {
                    validation = validation with { IsValidName = false };
                    return HostingResult<ModuleValidation>.Success(validation);
                }
                
                // Check availability in search paths
                bool isAvailable = false;
                foreach (var searchPath in _searchPaths)
                {
                    var candidatePath = BuildModulePath(moduleName, searchPath);
                    candidatePaths.Add(candidatePath);
                    
                    if (System.IO.File.Exists(candidatePath))
                    {
                        isAvailable = true;
                    }
                    else if (!System.IO.Directory.Exists(searchPath))
                    {
                        warnings.Add($"Search path '{searchPath}' does not exist");
                    }
                }
                
                // Check for potential naming conflicts
                if (moduleName.Contains("..") || moduleName.Contains("/") || moduleName.Contains("\\"))
                {
                    warnings.Add("Module name contains path separators which may indicate a security risk");
                }
                
                validation = validation with 
                { 
                    IsValidName = true,
                    IsAvailable = isAvailable,
                    CandidatePaths = candidatePaths,
                    Warnings = warnings
                };
                
                return HostingResult<ModuleValidation>.Success(validation);
            }
            catch (Exception ex)
            {
                return HostingResult<ModuleValidation>.FromException(ex, HostingOperation.Validation);
            }
        }
        
        #region Private Helper Methods
        
        private async Task<HostingResult<ModuleInfo>> TryResolveFromPath(string moduleName, string searchPath, ModuleContext context)
        {
            try
            {
                var modulePath = BuildModulePath(moduleName, searchPath);
                
                if (!System.IO.File.Exists(modulePath))
                {
                    return HostingResult<ModuleInfo>.Error($"Module file not found: {modulePath}", HostingOperation.ModuleResolution);
                }
                
                // Read module source
                var sourceCode = await System.IO.File.ReadAllTextAsync(modulePath);
                
                // Analyze module for security information
                var security = AnalyzeModuleSecurity(sourceCode, modulePath);
                
                // Extract metadata if available
                var metadata = ExtractModuleMetadata(modulePath, sourceCode);
                
                // Find dependencies
                var dependencies = ExtractModuleDependencies(sourceCode);
                
                var moduleInfo = new ModuleInfo
                {
                    SourceCode = sourceCode,
                    ResolvedPath = modulePath,
                    Metadata = metadata,
                    Security = security,
                    Dependencies = dependencies,
                    Cacheable = true
                };
                
                return HostingResult<ModuleInfo>.Success(moduleInfo);
            }
            catch (Exception ex)
            {
                return HostingResult<ModuleInfo>.FromException(ex, HostingOperation.ModuleResolution, $"path: {searchPath}");
            }
        }
        
        private bool ValidateModuleName(string moduleName)
        {
            return !string.IsNullOrWhiteSpace(moduleName) &&
                   !moduleName.Contains("..") &&
                   !moduleName.StartsWith("/") &&
                   !moduleName.StartsWith("\\") &&
                   moduleName.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '.' || c == '-');
        }
        
        private string BuildModulePath(string moduleName, string searchPath)
        {
            var fileName = moduleName.EndsWith(".lua") ? moduleName : $"{moduleName}.lua";
            return System.IO.Path.Combine(searchPath, fileName);
        }
        
        private TrustLevel DetermineRequiredTrustLevel(string moduleName)
        {
            // Determine trust level based on module naming conventions
            var lowerName = moduleName.ToLowerInvariant();
            
            if (lowerName.Contains("io") || lowerName.Contains("file") || lowerName.Contains("os"))
                return TrustLevel.Trusted;
            
            if (lowerName.Contains("net") || lowerName.Contains("http") || lowerName.Contains("socket"))
                return TrustLevel.Restricted;
            
            if (lowerName.Contains("system") || lowerName.Contains("admin"))
                return TrustLevel.FullTrust;
            
            return TrustLevel.Sandbox;
        }
        
        private IEnumerable<string> GetSecurityRestrictions(string moduleName, TrustLevel trustLevel)
        {
            var restrictions = new List<string>();
            
            if (trustLevel < TrustLevel.Trusted)
            {
                restrictions.Add("No file system access");
                restrictions.Add("No network access");
            }
            
            if (trustLevel < TrustLevel.FullTrust)
            {
                restrictions.Add("No system command execution");
            }
            
            return restrictions;
        }
        
        private ModuleSecurity AnalyzeModuleSecurity(string sourceCode, string filePath)
        {
            var requiresFileAccess = sourceCode.Contains("io.") || sourceCode.Contains("file");
            var requiresNetworkAccess = sourceCode.Contains("socket") || sourceCode.Contains("http");
            var requiresSystemAccess = sourceCode.Contains("os.execute") || sourceCode.Contains("os.system");
            
            var dangerousOperations = new List<string>();
            if (sourceCode.Contains("os.execute")) dangerousOperations.Add("System command execution");
            if (sourceCode.Contains("io.popen")) dangerousOperations.Add("Process spawning");
            if (sourceCode.Contains("loadfile")) dangerousOperations.Add("Dynamic file loading");
            
            return new ModuleSecurity
            {
                RequiresFileAccess = requiresFileAccess,
                RequiresNetworkAccess = requiresNetworkAccess,
                RequiresSystemAccess = requiresSystemAccess,
                DangerousOperations = dangerousOperations,
                MinimumTrustLevel = DetermineBMinimumTrustLevel(requiresFileAccess, requiresNetworkAccess, requiresSystemAccess)
            };
        }
        
        private TrustLevel DetermineBMinimumTrustLevel(bool fileAccess, bool networkAccess, bool systemAccess)
        {
            if (systemAccess) return TrustLevel.FullTrust;
            if (networkAccess) return TrustLevel.Restricted;
            if (fileAccess) return TrustLevel.Trusted;
            return TrustLevel.Sandbox;
        }
        
        private ModuleMetadata ExtractModuleMetadata(string filePath, string sourceCode)
        {
            var fileInfo = new System.IO.FileInfo(filePath);
            
            return new ModuleMetadata
            {
                LastModified = fileInfo.LastWriteTime,
                FileSize = fileInfo.Length,
                // Additional metadata extraction could be implemented here
                // e.g., parsing special comments for version, author, etc.
            };
        }
        
        private List<ModuleDependency> ExtractModuleDependencies(string sourceCode)
        {
            var dependencies = new List<ModuleDependency>();
            
            // Simple pattern matching for require() calls
            var requirePattern = new System.Text.RegularExpressions.Regex(@"require\s*\(\s*[""']([^""']+)[""']\s*\)");
            var matches = requirePattern.Matches(sourceCode);
            
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var moduleName = match.Groups[1].Value;
                dependencies.Add(new ModuleDependency
                {
                    Name = moduleName,
                    IsOptional = false // Could be enhanced to detect optional requires
                });
            }
            
            return dependencies;
        }
        
        #endregion
    }

    /// <summary>
    /// Adapter to convert legacy IModuleResolver to Result pattern
    /// </summary>
    public class ModuleResolverResultAdapter : IResultModuleResolver
    {
        private readonly IModuleResolver _innerResolver;
        
        public IReadOnlyList<string> SearchPaths => _innerResolver.SearchPaths;
        
        public ModuleResolverResultAdapter(IModuleResolver innerResolver)
        {
            _innerResolver = innerResolver ?? throw new ArgumentNullException(nameof(innerResolver));
        }
        
        public async Task<HostingResult<ModuleInfo>> ResolveModuleResultAsync(string moduleName, ModuleContext context)
        {
            try
            {
                var result = await _innerResolver.ResolveModuleAsync(moduleName, context);
                
                if (result.Success && result.SourceCode != null)
                {
                    var moduleInfo = new ModuleInfo
                    {
                        SourceCode = result.SourceCode,
                        ResolvedPath = result.ResolvedPath,
                        Cacheable = result.Cacheable,
                        Metadata = new ModuleMetadata
                        {
                            CustomMetadata = result.Metadata ?? new Dictionary<string, object>()
                        }
                    };
                    
                    return HostingResult<ModuleInfo>.Success(moduleInfo);
                }
                else
                {
                    return HostingResult<ModuleInfo>.Error(
                        result.ErrorMessage ?? "Module resolution failed",
                        HostingOperation.ModuleResolution);
                }
            }
            catch (Exception ex)
            {
                return HostingResult<ModuleInfo>.FromException(ex, HostingOperation.ModuleResolution);
            }
        }
        
        public HostingResult<ModulePermission> ValidateModulePermissionResult(string moduleName, TrustLevel trustLevel)
        {
            try
            {
                var isAllowed = _innerResolver.IsModuleAllowed(moduleName, trustLevel);
                var permission = new ModulePermission
                {
                    IsAllowed = isAllowed,
                    Reason = isAllowed ? "Module allowed by legacy resolver" : "Module forbidden by legacy resolver",
                    RequiredTrustLevel = isAllowed ? trustLevel : TrustLevel.FullTrust
                };
                
                return HostingResult<ModulePermission>.Success(permission);
            }
            catch (Exception ex)
            {
                return HostingResult<ModulePermission>.FromException(ex, HostingOperation.SecurityCheck);
            }
        }
        
        public HostingResult<ModuleValidation> PreValidateModuleResult(string moduleName, ModuleContext context)
        {
            // Basic validation since legacy interface doesn't expose pre-validation
            var validation = new ModuleValidation
            {
                IsValidName = !string.IsNullOrWhiteSpace(moduleName),
                IsAvailable = true, // Can't determine without actually resolving
                CandidatePaths = SearchPaths.Select(path => System.IO.Path.Combine(path, $"{moduleName}.lua")).ToList()
            };
            
            return HostingResult<ModuleValidation>.Success(validation);
        }
    }
}