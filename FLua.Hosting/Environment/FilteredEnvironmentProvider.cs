using FLua.Runtime;
using FLua.Hosting.Security;

namespace FLua.Hosting.Environment;

/// <summary>
/// Provides filtered Lua environments based on trust levels.
/// Creates secure environments with appropriate restrictions.
/// </summary>
public class FilteredEnvironmentProvider : IEnvironmentProvider
{
    private readonly ILuaSecurityPolicy _securityPolicy;
    
    public FilteredEnvironmentProvider(ILuaSecurityPolicy? securityPolicy = null)
    {
        _securityPolicy = securityPolicy ?? new StandardSecurityPolicy();
    }
    
    public LuaEnvironment CreateEnvironment(TrustLevel trustLevel, LuaHostOptions? options = null)
    {
        var env = new LuaEnvironment();
        
        // Add functions based on trust level
        AddBasicFunctions(env, trustLevel);
        AddLibraries(env, trustLevel);
        
        // Configure module system if resolver provided
        if (options?.ModuleResolver != null)
        {
            ConfigureModuleSystem(env, options.ModuleResolver, trustLevel);
        }
        
        // Add host functions if provided
        if (options?.HostFunctions?.Count > 0)
        {
            AddHostFunctions(env, options.HostFunctions);
        }
        
        // Inject host context if provided
        if (options?.HostContext?.Count > 0)
        {
            InjectHostContext(env, options.HostContext);
        }
        
        return env;
    }
    
    public void ConfigureModuleSystem(LuaEnvironment environment, IModuleResolver? moduleResolver, TrustLevel trustLevel)
    {
        if (moduleResolver == null) return;
        
        // Replace the standard require function with host-controlled version
        environment.SetVariable("require", new BuiltinFunction((args) =>
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("require: module name expected");
            
            var moduleName = args[0].AsString();
            var context = new ModuleContext 
            { 
                TrustLevel = trustLevel,
                RequestingModulePath = null // TODO: Track current module path
            };
            
            // Synchronously wait for async resolution (not ideal, but matches Lua's sync nature)
            var result = moduleResolver.ResolveModuleAsync(moduleName, context).GetAwaiter().GetResult();
            
            if (!result.Success)
            {
                throw new LuaRuntimeException($"module '{moduleName}' not found: {result.ErrorMessage}");
            }
            
            // TODO: Compile and execute the module source code
            // For now, return a placeholder
            return LuaValue.Nil;
        }));
        
        // Configure package.path based on module resolver's search paths
        var packageTable = new LuaTable();
        packageTable.Set("path", string.Join(";", moduleResolver.SearchPaths.Select(p => Path.Combine(p, "?.lua"))));
        environment.SetVariable("package", packageTable);
    }
    
    public void AddHostFunctions(LuaEnvironment environment, Dictionary<string, Func<LuaValue[], LuaValue>> hostFunctions)
    {
        foreach (var (name, func) in hostFunctions)
        {
            environment.SetVariable(name, new BuiltinFunction(func));
        }
    }
    
    public void InjectHostContext(LuaEnvironment environment, Dictionary<string, object> hostContext)
    {
        foreach (var (name, value) in hostContext)
        {
            // Convert .NET objects to LuaValue
            var luaValue = ConvertToLuaValue(value);
            environment.SetVariable(name, luaValue);
        }
    }
    
    private void AddBasicFunctions(LuaEnvironment env, TrustLevel trustLevel)
    {
        // Always available functions
        env.SetVariable("print", new BuiltinFunction(LuaEnvironment.Print));
        env.SetVariable("type", new BuiltinFunction(LuaEnvironment.Type));
        env.SetVariable("tostring", new BuiltinFunction(LuaEnvironment.ToString));
        env.SetVariable("tonumber", new BuiltinFunction(LuaEnvironment.ToNumber));
        env.SetVariable("assert", new BuiltinFunction(LuaEnvironment.Assert));
        
        // Conditionally available functions
        if (trustLevel >= TrustLevel.Sandbox)
        {
            env.SetVariable("pcall", new BuiltinFunction(LuaEnvironment.ProtectedCall));
            env.SetVariable("xpcall", new BuiltinFunction(LuaEnvironment.ExtendedProtectedCall));
            env.SetVariable("error", new BuiltinFunction(LuaEnvironment.Error));
            
            // Table functions
            env.SetVariable("pairs", new BuiltinFunction(LuaEnvironment.Pairs));
            env.SetVariable("ipairs", new BuiltinFunction(LuaEnvironment.IPairs));
            env.SetVariable("next", new BuiltinFunction(LuaEnvironment.Next));
            
            // Raw operations
            env.SetVariable("rawget", new BuiltinFunction(LuaEnvironment.RawGet));
            env.SetVariable("rawset", new BuiltinFunction(LuaEnvironment.RawSet));
            env.SetVariable("rawequal", new BuiltinFunction(LuaEnvironment.RawEqual));
            env.SetVariable("rawlen", new BuiltinFunction(LuaEnvironment.RawLen));
            
            // Metatable functions
            env.SetVariable("setmetatable", new BuiltinFunction(LuaEnvironment.SetMetatable));
            env.SetVariable("getmetatable", new BuiltinFunction(LuaEnvironment.GetMetatable));
            
            // Varargs and unpacking
            env.SetVariable("select", new BuiltinFunction(LuaEnvironment.Select));
            env.SetVariable("unpack", new BuiltinFunction(LuaEnvironment.Unpack));
        }
        
        if (trustLevel >= TrustLevel.Trusted)
        {
            env.SetVariable("collectgarbage", new BuiltinFunction(LuaEnvironment.CollectGarbage));
            env.SetVariable("load", new BuiltinFunction(LuaEnvironment.Load));
            env.SetVariable("warn", new BuiltinFunction(LuaEnvironment.Warn));
        }
    }
    
    private void AddLibraries(LuaEnvironment env, TrustLevel trustLevel)
    {
        // Always safe libraries
        LuaMathLib.AddMathLibrary(env);
        LuaStringLib.AddStringLibrary(env);
        
        if (trustLevel >= TrustLevel.Sandbox)
        {
            LuaTableLib.AddTableLibrary(env);
            LuaCoroutineLib.AddCoroutineLibrary(env);
            LuaUTF8Lib.AddUTF8Library(env);
        }
        
        if (trustLevel >= TrustLevel.Restricted)
        {
            // IO with restrictions would be added here
            // But since host controls I/O, we might provide a limited facade
        }
        
        if (trustLevel >= TrustLevel.Trusted)
        {
            LuaIOLib.AddIOLibrary(env);
            LuaOSLib.AddOSLibrary(env);
            LuaPackageLib.AddPackageLibrary(env);
        }
        
        if (trustLevel >= TrustLevel.FullTrust)
        {
            LuaDebugLib.AddDebugLibrary(env);
        }
    }
    
    private LuaValue ConvertToLuaValue(object value)
    {
        return value switch
        {
            null => LuaValue.Nil,
            bool b => b,
            int i => i,
            long l => l,
            float f => f,
            double d => d,
            string s => s,
            LuaValue lv => lv,
            _ => throw new ArgumentException($"Cannot convert type {value.GetType()} to LuaValue")
        };
    }
}