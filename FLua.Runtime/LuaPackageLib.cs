using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FLua.Ast;

namespace FLua.Runtime
{
    /// <summary>
    /// Implements Lua's package library for module loading
    /// </summary>
    public static class LuaPackageLib
    {
        /// <summary>
        /// Adds the package library to the Lua environment
        /// </summary>
        public static void AddPackageLibrary(LuaEnvironment env)
        {
            var packageTable = new LuaTable();
            
            // Set up package.loaded table
            var loadedTable = new LuaTable();
            packageTable.Set(new LuaString("loaded"), loadedTable);
            
            // Set up package.path with default Lua search paths
            var defaultPaths = new[]
            {
                "./?.lua",
                "./?/init.lua", 
                "./lib/?.lua",
                "./lib/?/init.lua"
            };
            packageTable.Set(new LuaString("path"), new LuaString(string.Join(";", defaultPaths)));
            
            // Set up package.cpath (C library paths - mostly unused in FLua)
            packageTable.Set(new LuaString("cpath"), new LuaString(""));
            
            // Set up package.searchers (Lua 5.4) / package.loaders (Lua 5.1)
            var searchersTable = new LuaTable();
            searchersTable.Set(new LuaInteger(1), new BuiltinFunction(args => LuaSearcher(args, env)));
            searchersTable.Set(new LuaInteger(2), new BuiltinFunction(args => FileSearcher(args, packageTable)));
            packageTable.Set(new LuaString("searchers"), searchersTable);
            packageTable.Set(new LuaString("loaders"), searchersTable); // Lua 5.1 compatibility
            
            // Set up package.config (path configuration)
            packageTable.Set(new LuaString("config"), new LuaString("\\n;\n?\n!\n-\n"));
            
            // Package library functions
            packageTable.Set(new LuaString("searchpath"), new BuiltinFunction(args => SearchPath(args)));
            
            // Add the package table to globals
            env.Globals.Set(new LuaString("package"), packageTable);
            
            // Add the require function to globals
            env.SetVariable("require", new BuiltinFunction(args => Require(args, env, packageTable)));
        }
        
        /// <summary>
        /// Implements the require function
        /// </summary>
        private static LuaValue[] Require(LuaValue[] args, LuaEnvironment env, LuaTable packageTable)
        {
            if (args.Length == 0 || !(args[0] is LuaString moduleName))
                throw new LuaRuntimeException("bad argument #1 to 'require' (string expected)");
            
            var name = moduleName.Value;
            var loadedTable = packageTable.Get(new LuaString("loaded")) as LuaTable;
            
            if (loadedTable == null)
                throw new LuaRuntimeException("package.loaded is not a table");
            
            // Check if module is already loaded
            var existingModule = loadedTable.Get(moduleName);
            if (existingModule != LuaNil.Instance)
            {
                return new LuaValue[] { existingModule };
            }
            
            // Search for the module using package.searchers
            var searchersTable = packageTable.Get(new LuaString("searchers")) as LuaTable ??
                               packageTable.Get(new LuaString("loaders")) as LuaTable;
            
            if (searchersTable == null)
                throw new LuaRuntimeException("package.searchers is not a table");
            
            LuaFunction? loader = null;
            LuaValue extra = LuaNil.Instance;
            var errors = new List<string>();
            
            // Try each searcher
            for (int i = 1; ; i++)
            {
                var searcher = searchersTable.Get(new LuaInteger(i));
                if (searcher == LuaNil.Instance)
                    break;
                
                if (searcher is LuaFunction searcherFunc)
                {
                    try
                    {
                        var result = searcherFunc.Call(new LuaValue[] { moduleName });
                        if (result.Length > 0 && result[0] is LuaFunction foundLoader)
                        {
                            loader = foundLoader;
                            extra = result.Length > 1 ? result[1] : LuaNil.Instance;
                            break;
                        }
                        else if (result.Length > 0 && result[0] is LuaString errorMsg)
                        {
                            errors.Add(errorMsg.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"searcher error: {ex.Message}");
                    }
                }
            }
            
            if (loader == null)
            {
                var errorMessage = $"module '{name}' not found";
                if (errors.Count > 0)
                {
                    errorMessage += ":\n" + string.Join("\n", errors);
                }
                throw new LuaRuntimeException(errorMessage);
            }
            
            // Call the loader
            LuaValue[] loaderArgs = extra == LuaNil.Instance 
                ? new LuaValue[] { moduleName } 
                : new LuaValue[] { moduleName, extra };
            
            var moduleResult = loader.Call(loaderArgs);
            var moduleValue = moduleResult.Length > 0 ? moduleResult[0] : new LuaBoolean(true);
            
            // Cache the module
            loadedTable.Set(moduleName, moduleValue);
            
            return new LuaValue[] { moduleValue };
        }
        
        /// <summary>
        /// Searcher for built-in Lua modules (like string, math, etc.)
        /// </summary>
        private static LuaValue[] LuaSearcher(LuaValue[] args, LuaEnvironment env)
        {
            if (args.Length == 0 || !(args[0] is LuaString moduleName))
                return new LuaValue[] { new LuaString("module name not string") };
            
            var name = moduleName.Value;
            
            // Check for built-in libraries
            switch (name)
            {
                case "coroutine":
                    return new LuaValue[] { new BuiltinFunction(CreateCoroutineLoader(env)) };
                case "string":
                    return new LuaValue[] { new BuiltinFunction(CreateStringLoader(env)) };
                case "table":
                    return new LuaValue[] { new BuiltinFunction(CreateTableLoader(env)) };
                case "math":
                    return new LuaValue[] { new BuiltinFunction(CreateMathLoader(env)) };
                case "io":
                    return new LuaValue[] { new BuiltinFunction(CreateIOLoader(env)) };
                case "os":
                    return new LuaValue[] { new BuiltinFunction(CreateOSLoader(env)) };
                case "utf8":
                    return new LuaValue[] { new BuiltinFunction(CreateUTF8Loader(env)) };
                case "debug":
                    return new LuaValue[] { new BuiltinFunction(CreateDebugLoader(env)) };
                case "package":
                    return new LuaValue[] { new BuiltinFunction(CreatePackageLoader(env)) };
                default:
                    return new LuaValue[] { new LuaString($"no field package.preload['{name}']") };
            }
        }
        
        /// <summary>
        /// Searcher for file-based modules
        /// </summary>
        private static LuaValue[] FileSearcher(LuaValue[] args, LuaTable packageTable)
        {
            if (args.Length == 0 || !(args[0] is LuaString moduleName))
                return new LuaValue[] { new LuaString("module name not string") };
            
            var name = moduleName.Value;
            var packagePath = packageTable.Get(new LuaString("path")) as LuaString;
            
            if (packagePath == null)
                return new LuaValue[] { new LuaString("package.path is not a string") };
            
            var searchResult = SearchPath(new LuaValue[] { moduleName, packagePath });
            if (searchResult.Length > 0 && searchResult[0] is LuaString filePath)
            {
                // Create a loader function for this file
                var loader = new BuiltinFunction(loaderArgs => LoadLuaFile(filePath.Value, name));
                return new LuaValue[] { loader, filePath };
            }
            
            return new LuaValue[] { new LuaString($"no file '{name}' in package.path") };
        }
        
        /// <summary>
        /// Implements package.searchpath
        /// </summary>
        private static LuaValue[] SearchPath(LuaValue[] args)
        {
            if (args.Length < 2 || !(args[0] is LuaString name) || !(args[1] is LuaString path))
                throw new LuaRuntimeException("bad arguments to 'searchpath'");
            
            var moduleName = name.Value;
            var searchPath = path.Value;
            var sep = args.Length > 2 && args[2] is LuaString sepStr ? sepStr.Value : ".";
            var rep = args.Length > 3 && args[3] is LuaString repStr ? repStr.Value : "/";
            
            // Replace module separators with directory separators
            var fileName = moduleName.Replace(sep, rep);
            
            // Split path and try each template
            var paths = searchPath.Split(';');
            var errors = new List<string>();
            
            foreach (var template in paths)
            {
                if (string.IsNullOrWhiteSpace(template))
                    continue;
                
                var fullPath = template.Replace("?", fileName);
                
                try
                {
                    if (File.Exists(fullPath))
                    {
                        return new LuaValue[] { new LuaString(Path.GetFullPath(fullPath)) };
                    }
                    else
                    {
                        errors.Add($"no file '{fullPath}'");
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"cannot access '{fullPath}': {ex.Message}");
                }
            }
            
            // Return nil and error message
            return new LuaValue[] { LuaNil.Instance, new LuaString(string.Join("\n\t", errors)) };
        }
        
        /// <summary>
        /// Function to parse and execute Lua code - should be set by the host application
        /// </summary>
        public static Func<string, string, LuaValue[]>? LuaFileLoader { get; set; }
        
        /// <summary>
        /// Loads a Lua file and returns its result
        /// </summary>
        private static LuaValue[] LoadLuaFile(string filePath, string moduleName)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new LuaRuntimeException($"module '{moduleName}' not found: no file '{filePath}'");
                
                var content = File.ReadAllText(filePath);
                
                if (LuaFileLoader == null)
                    throw new LuaRuntimeException("Lua file loader not configured - cannot load modules from files");
                
                // Use the configured loader to parse and execute the file
                return LuaFileLoader(content, moduleName);
            }
            catch (Exception ex) when (!(ex is LuaRuntimeException))
            {
                throw new LuaRuntimeException($"error loading module '{moduleName}': {ex.Message}");
            }
        }
        
        /// <summary>
        /// Creates a loader function for the coroutine library
        /// </summary>
        private static Func<LuaValue[], LuaValue[]> CreateCoroutineLoader(LuaEnvironment env)
        {
            return args =>
            {
                var tempEnv = new LuaEnvironment();
                LuaCoroutineLib.AddCoroutineLibrary(tempEnv);
                return new LuaValue[] { tempEnv.Globals.Get(new LuaString("coroutine")) };
            };
        }
        
        /// <summary>
        /// Creates a loader function for the string library
        /// </summary>
        private static Func<LuaValue[], LuaValue[]> CreateStringLoader(LuaEnvironment env)
        {
            return args =>
            {
                var tempEnv = new LuaEnvironment();
                LuaStringLib.AddStringLibrary(tempEnv);
                return new LuaValue[] { tempEnv.Globals.Get(new LuaString("string")) };
            };
        }
        
        /// <summary>
        /// Creates a loader function for the table library
        /// </summary>
        private static Func<LuaValue[], LuaValue[]> CreateTableLoader(LuaEnvironment env)
        {
            return args =>
            {
                var tempEnv = new LuaEnvironment();
                LuaTableLib.AddTableLibrary(tempEnv);
                return new LuaValue[] { tempEnv.Globals.Get(new LuaString("table")) };
            };
        }
        
        /// <summary>
        /// Creates a loader function for the math library
        /// </summary>
        private static Func<LuaValue[], LuaValue[]> CreateMathLoader(LuaEnvironment env)
        {
            return args =>
            {
                var tempEnv = new LuaEnvironment();
                LuaMathLib.AddMathLibrary(tempEnv);
                return new LuaValue[] { tempEnv.Globals.Get(new LuaString("math")) };
            };
        }
        
        /// <summary>
        /// Creates a loader function for the io library
        /// </summary>
        private static Func<LuaValue[], LuaValue[]> CreateIOLoader(LuaEnvironment env)
        {
            return args =>
            {
                var tempEnv = new LuaEnvironment();
                LuaIOLib.AddIOLibrary(tempEnv);
                return new LuaValue[] { tempEnv.Globals.Get(new LuaString("io")) };
            };
        }
        
        /// <summary>
        /// Creates a loader function for the os library
        /// </summary>
        private static Func<LuaValue[], LuaValue[]> CreateOSLoader(LuaEnvironment env)
        {
            return args =>
            {
                var tempEnv = new LuaEnvironment();
                LuaOSLib.AddOSLibrary(tempEnv);
                return new LuaValue[] { tempEnv.Globals.Get(new LuaString("os")) };
            };
        }
        
        /// <summary>
        /// Creates a loader function for the utf8 library
        /// </summary>
        private static Func<LuaValue[], LuaValue[]> CreateUTF8Loader(LuaEnvironment env)
        {
            return args =>
            {
                var tempEnv = new LuaEnvironment();
                LuaUTF8Lib.AddUTF8Library(tempEnv);
                return new LuaValue[] { tempEnv.Globals.Get(new LuaString("utf8")) };
            };
        }
        
        /// <summary>
        /// Creates a loader function for the debug library
        /// </summary>
        private static Func<LuaValue[], LuaValue[]> CreateDebugLoader(LuaEnvironment env)
        {
            return args =>
            {
                var tempEnv = new LuaEnvironment();
                LuaDebugLib.AddDebugLibrary(tempEnv);
                return new LuaValue[] { tempEnv.Globals.Get(new LuaString("debug")) };
            };
        }
        
        /// <summary>
        /// Creates a loader function for the package library
        /// </summary>
        private static Func<LuaValue[], LuaValue[]> CreatePackageLoader(LuaEnvironment env)
        {
            return args =>
            {
                return new LuaValue[] { env.Globals.Get(new LuaString("package")) };
            };
        }
    }
}
