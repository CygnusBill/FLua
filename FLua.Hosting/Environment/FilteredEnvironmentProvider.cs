using FLua.Runtime;
using FLua.Hosting.Security;
using System;
using System.Linq;
using System.Collections.Generic;

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
            return new[] { LuaValue.Nil };
        }));
        
        // Configure package.path based on module resolver's search paths
        var packageTable = new LuaTable();
        packageTable.Set("path", string.Join(";", moduleResolver.SearchPaths.Select(p => System.IO.Path.Combine(p, "?.lua"))));
        environment.SetVariable("package", packageTable);
    }
    
    public void AddHostFunctions(LuaEnvironment environment, Dictionary<string, Func<LuaValue[], LuaValue>> hostFunctions)
    {
        foreach (var (name, func) in hostFunctions)
        {
            environment.SetVariable(name, new BuiltinFunction(args => new[] { func(args) }));
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
        // Copy implementations from LuaEnvironment.CreateStandardEnvironment()
        // but only add functions allowed by trust level
        
        // Always available functions
        env.SetVariable("print", new BuiltinFunction(args => 
        {
            Console.WriteLine(string.Join("\t", args.Select(a => a.ToString())));
            return Array.Empty<LuaValue>();
        }));
        
        env.SetVariable("type", new BuiltinFunction(args =>
        {
            if (args.Length == 0)
                return new[] { (LuaValue)"no value" };
            
            var value = args[0];
            string type = value.Type switch
            {
                LuaType.Nil => "nil",
                LuaType.Boolean => "boolean",
                LuaType.Float => "number",
                LuaType.Integer => "number",
                LuaType.String => "string",
                LuaType.Table => "table",
                LuaType.Function => "function",
                LuaType.Thread => "thread",
                _ => "userdata"
            };
            
            return new[] { (LuaValue)type };
        }));
        
        // Based on the LuaEnvironment source, we'll create our own implementations
        // instead of trying to call private methods
        if (trustLevel >= TrustLevel.Untrusted)
        {
            // Add more basic functions as needed
        }
        
        if (trustLevel >= TrustLevel.Sandbox)
        {
            // Error handling functions
            env.SetVariable("error", new BuiltinFunction(args =>
            {
                if (args.Length == 0)
                    throw new LuaRuntimeException("error");
                throw new LuaRuntimeException(args[0].ToString());
            }));
            
            // Add more sandbox-level functions
        }
        
        if (trustLevel >= TrustLevel.Trusted)
        {
            // Add trusted-level functions
        }
    }
    
    private void AddLibraries(LuaEnvironment env, TrustLevel trustLevel)
    {
        // Always safe libraries
        if (_securityPolicy.IsAllowedLibrary("math", trustLevel))
            LuaMathLib.AddMathLibrary(env);
        if (_securityPolicy.IsAllowedLibrary("string", trustLevel))
            LuaStringLib.AddStringLibrary(env);
        
        if (trustLevel >= TrustLevel.Sandbox)
        {
            if (_securityPolicy.IsAllowedLibrary("table", trustLevel))
                LuaTableLib.AddTableLibrary(env);
            if (_securityPolicy.IsAllowedLibrary("coroutine", trustLevel))
                LuaCoroutineLib.AddCoroutineLibrary(env);
            if (_securityPolicy.IsAllowedLibrary("utf8", trustLevel))
                LuaUTF8Lib.AddUTF8Library(env);
        }
        
        if (trustLevel >= TrustLevel.Restricted)
        {
            // IO with restrictions would be added here
            // But since host controls I/O, we might provide a limited facade
        }
        
        if (trustLevel >= TrustLevel.Trusted)
        {
            if (_securityPolicy.IsAllowedLibrary("io", trustLevel))
                LuaIOLib.AddIOLibrary(env);
            if (_securityPolicy.IsAllowedLibrary("os", trustLevel))
                LuaOSLib.AddOSLibrary(env);
            if (_securityPolicy.IsAllowedLibrary("package", trustLevel))
                LuaPackageLib.AddPackageLibrary(env);
        }
        
        if (trustLevel >= TrustLevel.FullTrust)
        {
            if (_securityPolicy.IsAllowedLibrary("debug", trustLevel))
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