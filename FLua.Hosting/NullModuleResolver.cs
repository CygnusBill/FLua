using FLua.Hosting.Security;

namespace FLua.Hosting;

/// <summary>
/// Null module resolver that denies all module resolution requests.
/// Used for untrusted environments where no modules should be loaded.
/// </summary>
public class NullModuleResolver : IModuleResolver
{
    public IReadOnlyList<string> SearchPaths => Array.Empty<string>();
    
    public Task<ModuleResolutionResult> ResolveModuleAsync(string moduleName, ModuleContext context)
    {
        return Task.FromResult(ModuleResolutionResult.CreateFailure(
            $"Module loading is not allowed in {context.TrustLevel} trust level"));
    }
    
    public bool IsModuleAllowed(string moduleName, TrustLevel trustLevel)
    {
        return false; // No modules allowed
    }
}