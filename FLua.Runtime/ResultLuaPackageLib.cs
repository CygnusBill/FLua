using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FLua.Common;

namespace FLua.Runtime
{
    /// <summary>
    /// Result-based version of Lua Package Library implementation
    /// Converts all exception-based error handling to Result pattern
    /// </summary>
    public static class ResultLuaPackageLib
    {
        #region Core Package Functions
        
        /// <summary>
        /// Result-based implementation of the require function
        /// </summary>
        public static Result<LuaValue[]> RequireResult(LuaValue[] args, LuaEnvironment env, LuaTable packageTable)
        {
            if (args.Length == 0 || !args[0].IsString)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'require' (string expected)");
            
            var moduleName = args[0];
            var name = moduleName.AsString();
            
            var loadedValue = packageTable.Get(LuaValue.String("loaded"));
            if (!loadedValue.IsTable)
                return Result<LuaValue[]>.Failure("package.loaded is not a table");
                
            var loadedTable = loadedValue.AsTable<LuaTable>();
            
            // Check if module is already loaded
            var existingModule = loadedTable.Get(moduleName);
            if (!existingModule.IsNil)
            {
                return Result<LuaValue[]>.Success([existingModule]);
            }
            
            // Search for the module using package.searchers
            var searchersValue = packageTable.Get(LuaValue.String("searchers"));
            if (searchersValue.IsNil)
                searchersValue = packageTable.Get(LuaValue.String("loaders"));
                
            if (!searchersValue.IsTable)
                return Result<LuaValue[]>.Failure("package.searchers is not a table");
                
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
                return Result<LuaValue[]>.Failure(errorMessage);
            }
            
            try
            {
                // Call the loader
                LuaValue[] loaderArgs = extra == LuaValue.Nil 
                    ? [moduleName]
                    : [moduleName, extra];
                
                var moduleResult = loader.Call(loaderArgs);
                var moduleValue = moduleResult.Length > 0 ? moduleResult[0] : LuaValue.Boolean(true);
                
                // Cache the module
                loadedTable.Set(moduleName, moduleValue);
                
                return Result<LuaValue[]>.Success([moduleValue]);
            }
            catch (Exception ex)
            {
                return Result<LuaValue[]>.Failure($"error loading module '{name}': {ex.Message}");
            }
        }
        
        /// <summary>
        /// Result-based searcher for built-in Lua modules
        /// </summary>
        public static Result<LuaValue[]> LuaSearcherResult(LuaValue[] args, LuaEnvironment env)
        {
            if (args.Length == 0 || !args[0].IsString)
                return Result<LuaValue[]>.Success([LuaValue.String("module name not string")]);
            
            var name = args[0].AsString();
            
            try
            {
                // Check for built-in libraries
                switch (name)
                {
                    case "coroutine":
                        return Result<LuaValue[]>.Success([new BuiltinFunction(CreateCoroutineLoader(env))]);
                    case "string":
                        return Result<LuaValue[]>.Success([new BuiltinFunction(CreateStringLoader(env))]);
                    case "table":
                        return Result<LuaValue[]>.Success([new BuiltinFunction(CreateTableLoader(env))]);
                    case "math":
                        return Result<LuaValue[]>.Success([new BuiltinFunction(CreateMathLoader(env))]);
                    case "io":
                        return Result<LuaValue[]>.Success([new BuiltinFunction(CreateIOLoader(env))]);
                    case "os":
                        return Result<LuaValue[]>.Success([new BuiltinFunction(CreateOSLoader(env))]);
                    case "utf8":
                        return Result<LuaValue[]>.Success([new BuiltinFunction(CreateUTF8Loader(env))]);
                    case "debug":
                        return Result<LuaValue[]>.Success([new BuiltinFunction(CreateDebugLoader(env))]);
                    case "package":
                        return Result<LuaValue[]>.Success([new BuiltinFunction(CreatePackageLoader(env))]);
                    default:
                        return Result<LuaValue[]>.Success([LuaValue.String($"no field package.preload['{name}']")]);
                }
            }
            catch (Exception ex)
            {
                return Result<LuaValue[]>.Failure($"error in Lua searcher: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Result-based searcher for file-based modules
        /// </summary>
        public static Result<LuaValue[]> FileSearcherResult(LuaValue[] args, LuaTable packageTable)
        {
            if (args.Length == 0 || !args[0].IsString)
                return Result<LuaValue[]>.Success([LuaValue.String("module name not string")]);
            
            var moduleName = args[0];
            var name = moduleName.AsString();
            var packagePathValue = packageTable.Get(LuaValue.String("path"));
            
            if (!packagePathValue.IsString)
                return Result<LuaValue[]>.Success([LuaValue.String("package.path is not a string")]);
                
            var packagePath = packagePathValue.AsString();
            
            try
            {
                var searchResult = SearchPathResult([moduleName, LuaValue.String(packagePath)]);
                if (searchResult.IsSuccess && searchResult.Value.Length > 0 && searchResult.Value[0].IsString)
                {
                    // Create a loader function for this file
                    var filePath = searchResult.Value[0].AsString();
                    var loader = new BuiltinFunction(loaderArgs => LoadLuaFileResult(filePath, name).IsSuccess 
                        ? LoadLuaFileResult(filePath, name).Value 
                        : [LuaValue.Nil, LuaValue.String(LoadLuaFileResult(filePath, name).Error)]);
                    return Result<LuaValue[]>.Success([LuaValue.Function(loader), searchResult.Value[0]]);
                }
                
                return Result<LuaValue[]>.Success([LuaValue.String($"no file '{name}' in package.path")]);
            }
            catch (Exception ex)
            {
                return Result<LuaValue[]>.Success([LuaValue.String($"error searching for file: {ex.Message}")]);
            }
        }
        
        /// <summary>
        /// Result-based implementation of package.searchpath
        /// </summary>
        public static Result<LuaValue[]> SearchPathResult(LuaValue[] args)
        {
            if (args.Length < 2 || !args[0].IsString || !args[1].IsString)
                return Result<LuaValue[]>.Failure("bad arguments to 'searchpath'");
            
            var moduleName = args[0].AsString();
            var searchPath = args[1].AsString();
            var sep = args.Length > 2 && args[2].IsString ? args[2].AsString() : ".";
            var rep = args.Length > 3 && args[3].IsString ? args[3].AsString() : "/";
            
            try
            {
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
                            return Result<LuaValue[]>.Success([LuaValue.String(Path.GetFullPath(fullPath))]);
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
                return Result<LuaValue[]>.Success([LuaValue.Nil, LuaValue.String(string.Join("\n\t", errors))]);
            }
            catch (Exception ex)
            {
                return Result<LuaValue[]>.Failure($"error in searchpath: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Function to parse and execute Lua code - should be set by the host application
        /// </summary>
        public static Func<string, string, Result<LuaValue[]>>? LuaFileLoaderResult { get; set; }
        
        /// <summary>
        /// Result-based Lua file loader
        /// </summary>
        public static Result<LuaValue[]> LoadLuaFileResult(string filePath, string moduleName)
        {
            try
            {
                if (!File.Exists(filePath))
                    return Result<LuaValue[]>.Failure($"module '{moduleName}' not found: no file '{filePath}'");
                
                var content = File.ReadAllText(filePath);
                
                if (LuaFileLoaderResult == null)
                    return Result<LuaValue[]>.Failure("Lua file loader not configured - cannot load modules from files");
                
                // Use the configured loader to parse and execute the file
                return LuaFileLoaderResult(content, moduleName);
            }
            catch (Exception ex)
            {
                return Result<LuaValue[]>.Failure($"error loading module '{moduleName}': {ex.Message}");
            }
        }
        
        #endregion
        
        #region Library Loader Functions
        
        /// <summary>
        /// Creates a loader function for the coroutine library
        /// </summary>
        private static Func<LuaValue[], LuaValue[]> CreateCoroutineLoader(LuaEnvironment env)
        {
            return args =>
            {
                try
                {
                    var tempEnv = new LuaEnvironment();
                    // TODO: LuaCoroutineLib.AddCoroutineLibrary(tempEnv);
                    return [tempEnv.Globals.Get(LuaValue.String("coroutine"))];
                }
                catch (Exception ex)
                {
                    return [LuaValue.Nil, LuaValue.String($"error loading coroutine library: {ex.Message}")];
                }
            };
        }
        
        /// <summary>
        /// Creates a loader function for the string library
        /// </summary>
        private static Func<LuaValue[], LuaValue[]> CreateStringLoader(LuaEnvironment env)
        {
            return args =>
            {
                try
                {
                    var tempEnv = new LuaEnvironment();
                    LuaStringLib.AddStringLibrary(tempEnv);
                    return [tempEnv.Globals.Get(LuaValue.String("string"))];
                }
                catch (Exception ex)
                {
                    return [LuaValue.Nil, LuaValue.String($"error loading string library: {ex.Message}")];
                }
            };
        }
        
        /// <summary>
        /// Creates a loader function for the table library
        /// </summary>
        private static Func<LuaValue[], LuaValue[]> CreateTableLoader(LuaEnvironment env)
        {
            return args =>
            {
                try
                {
                    var tempEnv = new LuaEnvironment();
                    LuaTableLib.AddTableLibrary(tempEnv);
                    return [tempEnv.Globals.Get(LuaValue.String("table"))];
                }
                catch (Exception ex)
                {
                    return [LuaValue.Nil, LuaValue.String($"error loading table library: {ex.Message}")];
                }
            };
        }
        
        /// <summary>
        /// Creates a loader function for the math library
        /// </summary>
        private static Func<LuaValue[], LuaValue[]> CreateMathLoader(LuaEnvironment env)
        {
            return args =>
            {
                try
                {
                    var tempEnv = new LuaEnvironment();
                    LuaMathLib.AddMathLibrary(tempEnv);
                    return [tempEnv.Globals.Get(LuaValue.String("math"))];
                }
                catch (Exception ex)
                {
                    return [LuaValue.Nil, LuaValue.String($"error loading math library: {ex.Message}")];
                }
            };
        }
        
        /// <summary>
        /// Creates a loader function for the io library
        /// </summary>
        private static Func<LuaValue[], LuaValue[]> CreateIOLoader(LuaEnvironment env)
        {
            return args =>
            {
                try
                {
                    var tempEnv = new LuaEnvironment();
                    LuaIOLib.AddIOLibrary(tempEnv);
                    return [tempEnv.Globals.Get(LuaValue.String("io"))];
                }
                catch (Exception ex)
                {
                    return [LuaValue.Nil, LuaValue.String($"error loading io library: {ex.Message}")];
                }
            };
        }
        
        /// <summary>
        /// Creates a loader function for the os library
        /// </summary>
        private static Func<LuaValue[], LuaValue[]> CreateOSLoader(LuaEnvironment env)
        {
            return args =>
            {
                try
                {
                    var tempEnv = new LuaEnvironment();
                    LuaOSLib.AddOSLibrary(tempEnv);
                    return [tempEnv.Globals.Get(LuaValue.String("os"))];
                }
                catch (Exception ex)
                {
                    return [LuaValue.Nil, LuaValue.String($"error loading os library: {ex.Message}")];
                }
            };
        }
        
        /// <summary>
        /// Creates a loader function for the utf8 library
        /// </summary>
        private static Func<LuaValue[], LuaValue[]> CreateUTF8Loader(LuaEnvironment env)
        {
            return args =>
            {
                try
                {
                    var tempEnv = new LuaEnvironment();
                    LuaUTF8Lib.AddUTF8Library(tempEnv);
                    return [tempEnv.Globals.Get(LuaValue.String("utf8"))];
                }
                catch (Exception ex)
                {
                    return [LuaValue.Nil, LuaValue.String($"error loading utf8 library: {ex.Message}")];
                }
            };
        }
        
        /// <summary>
        /// Creates a loader function for the debug library
        /// </summary>
        private static Func<LuaValue[], LuaValue[]> CreateDebugLoader(LuaEnvironment env)
        {
            return args =>
            {
                try
                {
                    var tempEnv = new LuaEnvironment();
                    LuaDebugLib.AddDebugLibrary(tempEnv);
                    return [tempEnv.Globals.Get(LuaValue.String("debug"))];
                }
                catch (Exception ex)
                {
                    return [LuaValue.Nil, LuaValue.String($"error loading debug library: {ex.Message}")];
                }
            };
        }
        
        /// <summary>
        /// Creates a loader function for the package library
        /// </summary>
        private static Func<LuaValue[], LuaValue[]> CreatePackageLoader(LuaEnvironment env)
        {
            return args =>
            {
                try
                {
                    return [env.Globals.Get(LuaValue.String("package"))];
                }
                catch (Exception ex)
                {
                    return [LuaValue.Nil, LuaValue.String($"error loading package library: {ex.Message}")];
                }
            };
        }
        
        #endregion
    }
}