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
        // Start with the standard environment and then filter it
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        // Filter functions based on trust level
        FilterEnvironmentByTrustLevel(env, trustLevel);
        
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
    
    private void FilterEnvironmentByTrustLevel(LuaEnvironment env, TrustLevel trustLevel)
    {
        // Remove dangerous functions based on trust level
        var functionsToRemove = new List<string>();
        
        switch (trustLevel)
        {
            case TrustLevel.Untrusted:
                // Remove everything except the most basic functions
                functionsToRemove.AddRange(new[] {
                    "load", "loadfile", "dofile", "require",
                    "io", "os", "debug", "package",
                    "collectgarbage", "rawget", "rawset", "rawequal", "rawlen",
                    "setmetatable", "getmetatable",
                    "pcall", "xpcall", "error"
                });
                break;
                
            case TrustLevel.Sandbox:
                // Remove dangerous functions but keep safe libraries
                functionsToRemove.AddRange(new[] {
                    "load", "loadfile", "dofile", "require",
                    "io", "os", "debug"
                });
                break;
                
            case TrustLevel.Restricted:
                // Remove file I/O and dangerous OS functions
                functionsToRemove.AddRange(new[] {
                    "loadfile", "dofile",
                    "io", "debug"
                });
                // Also remove dangerous OS functions (but keep time functions)
                if (env.GetVariable("os").AsTable<LuaTable>() is { } osTable)
                {
                    osTable.Set("execute", LuaValue.Nil);
                    osTable.Set("exit", LuaValue.Nil);
                    osTable.Set("setenv", LuaValue.Nil);
                    osTable.Set("remove", LuaValue.Nil);
                    osTable.Set("rename", LuaValue.Nil);
                }
                break;
                
            case TrustLevel.Trusted:
                // Remove only debug library
                functionsToRemove.Add("debug");
                break;
                
            case TrustLevel.FullTrust:
                // Keep everything
                break;
        }
        
        // Remove the functions
        foreach (var func in functionsToRemove)
        {
            env.SetVariable(func, LuaValue.Nil);
        }
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