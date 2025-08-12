using FLua.Runtime;
using FLua.Hosting.Security;
using System;
using System.Linq;
using System.Collections.Generic;
using FLua.Compiler;
using FLua.Parser;
using FLua.Interpreter;
using FLua.Ast;
using Microsoft.FSharp.Collections;
using System.IO;

namespace FLua.Hosting.Environment;

/// <summary>
/// Provides filtered Lua environments based on trust levels.
/// Creates secure environments with appropriate restrictions.
/// </summary>
public class FilteredEnvironmentProvider : IEnvironmentProvider
{
    private readonly ILuaSecurityPolicy _securityPolicy;
    private readonly ILuaCompiler? _compiler;
    private readonly Dictionary<string, CompiledModule> _compiledModuleCache = new();
    
    public FilteredEnvironmentProvider(ILuaSecurityPolicy? securityPolicy = null, ILuaCompiler? compiler = null)
    {
        _securityPolicy = securityPolicy ?? new StandardSecurityPolicy();
        _compiler = compiler;
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
                    "load", "loadfile", "dofile", // Don't remove require - it will be replaced with controlled version
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
        
        // Create a loaded modules table to prevent circular dependencies
        var loadedModules = new LuaTable();
        
        // Replace the standard require function with host-controlled version
        var requireFunc = new BuiltinFunction((args) =>
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("require: module name expected");
            
            var moduleName = args[0].AsString();
            
            // Check if module is already loaded
            var cachedModule = loadedModules.Get(moduleName);
            if (!cachedModule.IsNil)
            {
                return new[] { cachedModule };
            }
            
            // Mark module as being loaded (to handle circular dependencies)
            loadedModules.Set(moduleName, new LuaTable()); // Placeholder while loading
            
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
            
            // Compile and execute the module
            var moduleResults = ExecuteModule(result, moduleName, environment, trustLevel);
            
            // Cache the loaded module
            if (moduleResults.Length > 0)
            {
                loadedModules.Set(moduleName, moduleResults[0]);
            }
            
            return moduleResults;
        });
        
        environment.SetVariable("require", requireFunc);
        
        // Configure package table based on module resolver's search paths
        var packageTable = environment.GetVariable("package").AsTable<LuaTable>() ?? new LuaTable();
        packageTable.Set("path", string.Join(";", moduleResolver.SearchPaths.Select(p => System.IO.Path.Combine(p, "?.lua"))));
        packageTable.Set("loaded", loadedModules); // Standard Lua package.loaded table
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
    
    private LuaValue[] ExecuteModule(ModuleResolutionResult result, string moduleName, LuaEnvironment environment, TrustLevel trustLevel)
    {
        // Check if we have a cached compiled module
        var cacheKey = $"{result.ResolvedPath}:{trustLevel}";
        if (_compiledModuleCache.TryGetValue(cacheKey, out var cachedModule) && result.Cacheable)
        {
            return cachedModule.Execute(environment);
        }
        
        try
        {
            // Parse the module source code
            FSharpList<FLua.Ast.Statement> statements;
            try
            {
                statements = ParserHelper.ParseString(result.SourceCode!);
            }
            catch (Exception ex)
            {
                throw new LuaRuntimeException($"Module '{moduleName}' parse error: {ex.Message}");
            }
            
            // Check if we should compile or interpret based on trust level and compiler availability
            if (_compiler != null && trustLevel >= TrustLevel.Trusted)
            {
                // Try to compile the module for better performance
                var compilerOptions = new CompilerOptions(
                    OutputPath: null!,
                    Target: CompilationTarget.Lambda,
                    AssemblyName: $"LuaModule_{Path.GetFileNameWithoutExtension(moduleName)}_{Guid.NewGuid():N}",
                    GenerateInMemory: true
                );
                
                var compilationResult = _compiler.Compile(ListModule.ToArray(statements).ToList(), compilerOptions);
                
                if (compilationResult.Success && compilationResult.CompiledDelegate != null)
                {
                    // Cache the compiled module
                    var compiledModule = new CompiledModule((Func<LuaEnvironment, LuaValue[]>)compilationResult.CompiledDelegate);
                    if (result.Cacheable)
                    {
                        _compiledModuleCache[cacheKey] = compiledModule;
                    }
                    return compiledModule.Execute(environment);
                }
                
                // Fall back to interpretation if compilation failed
                // Log warning if needed
            }
            
            // Interpret the module
            // Create a new interpreter instance with the shared environment
            var moduleInterpreter = new LuaInterpreter();
            
            // HACK: Use reflection to set the environment since there's no public constructor
            // This should be improved by adding a constructor that accepts an environment
            var envField = moduleInterpreter.GetType()
                .GetField("_environment", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (envField != null)
            {
                // Create module environment as a child of the current environment
                // This allows the module to see global functions including require
                var moduleEnv = new LuaEnvironment(environment);
                
                // Set module-specific variables
                moduleEnv.SetVariable("...", moduleName); // Module name vararg
                
                // Set the environment
                envField.SetValue(moduleInterpreter, moduleEnv);
                
                // Execute the module
                var moduleResults = moduleInterpreter.ExecuteStatements(statements);
                
                // Module should return a table or value
                if (moduleResults.Length > 0)
                {
                    return moduleResults;
                }
                
                // If no explicit return, create an empty table
                // (Lua modules typically must explicitly return their exports)
                var emptyTable = new LuaTable();
                return new LuaValue[] { emptyTable };
            }
            else
            {
                throw new InvalidOperationException("Cannot access interpreter environment field");
            }
        }
        catch (Exception ex) when (!(ex is LuaRuntimeException))
        {
            throw new LuaRuntimeException($"Module '{moduleName}' execution error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Represents a compiled Lua module that can be executed multiple times.
    /// </summary>
    private class CompiledModule
    {
        private readonly Func<LuaEnvironment, LuaValue[]> _executeDelegate;
        
        public CompiledModule(Func<LuaEnvironment, LuaValue[]> executeDelegate)
        {
            _executeDelegate = executeDelegate;
        }
        
        public LuaValue[] Execute(LuaEnvironment environment)
        {
            // Create a new environment for the module that inherits from the provided environment
            var moduleEnv = new LuaEnvironment(environment);
            return _executeDelegate(moduleEnv);
        }
    }
}