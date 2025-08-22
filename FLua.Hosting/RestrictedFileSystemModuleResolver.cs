using FLua.Hosting.Security;

namespace FLua.Hosting;

/// <summary>
/// Restricted file system-based module resolver for trusted environments.
/// Same as FileSystemModuleResolver but with additional path restrictions.
/// </summary>
public class RestrictedFileSystemModuleResolver : FileSystemModuleResolver
{
    public RestrictedFileSystemModuleResolver(IEnumerable<string>? searchPaths = null, bool enableCaching = true)
        : base(searchPaths, enableCaching)
    {
    }
    
    public override async Task<ModuleResolutionResult> ResolveModuleAsync(string moduleName, ModuleContext context)
    {
        // Enforce restricted trust level
        var restrictedContext = context with { TrustLevel = TrustLevel.Restricted };
        return await base.ResolveModuleAsync(moduleName, restrictedContext);
    }
}