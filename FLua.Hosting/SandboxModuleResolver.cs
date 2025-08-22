using FLua.Hosting.Security;

namespace FLua.Hosting;

/// <summary>
/// Sandbox module resolver for sandbox environments.
/// Only allows safe modules within configured sandbox paths.
/// </summary>
public class SandboxModuleResolver : FileSystemModuleResolver
{
    public SandboxModuleResolver(IEnumerable<string>? searchPaths = null, bool enableCaching = true)
        : base(searchPaths, enableCaching)
    {
    }
    
    public override async Task<ModuleResolutionResult> ResolveModuleAsync(string moduleName, ModuleContext context)
    {
        // Enforce sandbox trust level
        var sandboxContext = context with { TrustLevel = TrustLevel.Sandbox };
        return await base.ResolveModuleAsync(moduleName, sandboxContext);
    }
}