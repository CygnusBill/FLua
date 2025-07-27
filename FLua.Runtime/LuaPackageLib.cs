using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
            packageTable.Set(LuaValue.String("loaded"), loadedTable);
            
            // Set up package.path with default Lua search paths
            var defaultPaths = new[]
            {
                "./?.lua",
                "./?/init.lua", 
                "./lib/?.lua",
                "./lib/?/init.lua"
            };
            packageTable.Set(LuaValue.String("path"), LuaValue.String(string.Join(";", defaultPaths)));
            
            // Set up package.cpath (C library paths - mostly unused in FLua)
            packageTable.Set(LuaValue.String("cpath"), LuaValue.String(""));
            
            // Set up package.searchers (Lua 5.4) / package.loaders (Lua 5.1)
            var searchersTable = new LuaTable();
            searchersTable.Set(LuaValue.Integer(1), new BuiltinFunction(args => LuaSearcher(args, env)));
            searchersTable.Set(LuaValue.Integer(2), new BuiltinFunction(args => FileSearcher(args, packageTable)));
            packageTable.Set(LuaValue.String("searchers"), searchersTable);
            packageTable.Set(LuaValue.String("loaders"), searchersTable); // Lua 5.1 compatibility
            
            // Set up package.config (path configuration)
            packageTable.Set(LuaValue.String("config"), LuaValue.String("\\n;\n?\n!\n-\n"));
            
            // Package library functions
            packageTable.Set(LuaValue.String("searchpath"), new BuiltinFunction(args => SearchPath(args)));
            
            // Add the package table to globals
            env.Globals.Set(LuaValue.String("package"), packageTable);
            
            // Add the require function to globals
            env.SetVariable("require", new BuiltinFunction(args => Require(args, env, packageTable)));
        }
        
        /// <summary>
        /// Implements the require function
        /// </summary>
        private static LuaValue[] Require(LuaValue[] args, LuaEnvironment env, LuaTable packageTable)
        {
            if (args.Length == 0 || !args[0].IsString)
                throw new LuaRuntimeException("bad argument #1 to 'require' (string expected)");
            
            var moduleName = args[0];
            var name = moduleName.AsString();
            
            var loadedValue = packageTable.Get(LuaValue.String("loaded"));
            if (!loadedValue.IsTable)
                throw new LuaRuntimeException("package.loaded is not a table");
                
            var loadedTable = loadedValue.AsTable<LuaTable>();
            
            // Check if module is already loaded
            var existingModule = loadedTable.Get(moduleName);
            if (!existingModule.IsNil)
            {
                return [existingModule];
            }
            
            // Search for the module using package.searchers
            var searchersValue = packageTable.Get(LuaValue.String("searchers"));
            if (searchersValue.IsNil)
                searchersValue = packageTable.Get(LuaValue.String("loaders"));
                
            if (!searchersValue.IsTable)
                throw new LuaRuntimeException("package.searchers is not a table");
                
            var searchersTable = searchersValue.AsTable<LuaTable>();
            
            LuaFunction? loader = null;
            LuaValue extra = LuaValue.Nil;
            var errors = new List<string>();
            
            // Try each searcher
            for (int i = 1; ; i++)
            {
                var searcher = searchersTable.Get(LuaValue.Integer(i));
                if (searcher.IsNil)
                    break;
                
                if (searcher.IsFunction)
                {
                    try
                    {
                        var searcherFunc = searcher.AsFunction<LuaFunction>();
                        var result = searcherFunc.Call(new LuaValue[] { moduleName });
                        if (result.Length > 0 && result[0].IsFunction)
                        {
                            loader = result[0].AsFunction<LuaFunction>();
                            extra = result.Length > 1 ? result[1] : LuaValue.Nil;
                            break;
                        }
                        else if (result.Length > 0 && result[0].IsString)
                        {
                            errors.Add(result[0].AsString());
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
            LuaValue[] loaderArgs = extra == LuaValue.Nil 
                ? [moduleName]
                : [moduleName, extra];
            
            var moduleResult = loader.Call(loaderArgs);
            var moduleValue = moduleResult.Length > 0 ? moduleResult[0] : LuaValue.Boolean(true);
            
            // Cache the module
            loadedTable.Set(moduleName, moduleValue);
            
            return [moduleValue];
        }
        
        /// <summary>
        /// Searcher for built-in Lua modules (like string, math, etc.)
        /// </summary>
        private static LuaValue[] LuaSearcher(LuaValue[] args, LuaEnvironment env)
        {
            if (args.Length == 0 || !args[0].IsString)
                return [LuaValue.String("module name not string")];
            
            var name = args[0].AsString();
            
            // Check for built-in libraries
            switch (name)
            {
                case "coroutine":
                    return [new BuiltinFunction(CreateCoroutineLoader(env))];
                case "string":
                    return [new BuiltinFunction(CreateStringLoader(env))];
                case "table":
                    return [new BuiltinFunction(CreateTableLoader(env))];
                case "math":
                    return [new BuiltinFunction(CreateMathLoader(env))];
                case "io":
                    return [new BuiltinFunction(CreateIOLoader(env))];
                case "os":
                    return [new BuiltinFunction(CreateOSLoader(env))];
                case "utf8":
                    return [new BuiltinFunction(CreateUTF8Loader(env))];
                case "debug":
                    return [new BuiltinFunction(CreateDebugLoader(env))];
                case "package":
                    return [new BuiltinFunction(CreatePackageLoader(env))];
                default:
                    return [LuaValue.String($"no field package.preload['{name}']")];
            }
        }
        
        /// <summary>
        /// Searcher for file-based modules
        /// </summary>
        private static LuaValue[] FileSearcher(LuaValue[] args, LuaTable packageTable)
        {
            if (args.Length == 0 || !args[0].IsString)
                return [LuaValue.String("module name not string")];
            
            var moduleName = args[0];
            var name = moduleName.AsString();
            var packagePathValue = packageTable.Get(LuaValue.String("path"));
            
            if (!packagePathValue.IsString)
                return [LuaValue.String("package.path is not a string")];
                
            var packagePath = packagePathValue.AsString();
            
            var searchResult = SearchPath([moduleName, LuaValue.String(packagePath)]);
            if (searchResult.Length > 0 && searchResult[0].IsString)
            {
                // Create a loader function for this file
                var filePath = searchResult[0].AsString();
                var loader = new BuiltinFunction(loaderArgs => LoadLuaFile(filePath, name));
                return [LuaValue.Function(loader), searchResult[0]];
            }
            
            return [LuaValue.String($"no file '{name}' in package.path")];
        }
        
        /// <summary>
        /// Implements package.searchpath
        /// </summary>
        private static LuaValue[] SearchPath(LuaValue[] args)
        {
            if (args.Length < 2 || !args[0].IsString || !args[1].IsString)
                throw new LuaRuntimeException("bad arguments to 'searchpath'");
            
            var moduleName = args[0].AsString();
            var searchPath = args[1].AsString();
            var sep = args.Length > 2 && args[2].IsString ? args[2].AsString() : ".";
            var rep = args.Length > 3 && args[3].IsString ? args[3].AsString() : "/";
            
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
                        return [LuaValue.String(Path.GetFullPath(fullPath))];
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
            return [LuaValue.Nil, LuaValue.String(string.Join("\n\t", errors))];
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
                // TODO: LuaCoroutineLib.AddCoroutineLibrary(tempEnv);
                return [tempEnv.Globals.Get(LuaValue.String("coroutine"))];
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
                return [tempEnv.Globals.Get(LuaValue.String("string"))];
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
                return [tempEnv.Globals.Get(LuaValue.String("table"))];
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
                return [tempEnv.Globals.Get(LuaValue.String("math"))];
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
                return [tempEnv.Globals.Get(LuaValue.String("io"))];
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
                return [tempEnv.Globals.Get(LuaValue.String("os"))];
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
                return [tempEnv.Globals.Get(LuaValue.String("utf8"))];
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
                return [tempEnv.Globals.Get(LuaValue.String("debug"))];
            };
        }
        
        /// <summary>
        /// Creates a loader function for the package library
        /// </summary>
        private static Func<LuaValue[], LuaValue[]> CreatePackageLoader(LuaEnvironment env)
        {
            return args =>
            {
                return [env.Globals.Get(LuaValue.String("package"))];
            };
        }
    }
}
