using System;
using System.Collections.Generic;
using System.Linq;

namespace FLua.Runtime
{
    /// <summary>
    /// Represents a Lua execution environment with variable scopes
    /// </summary>
    public class LuaEnvironment : IDisposable
    {
        /// <summary>
        /// Optional load function implementation that can be set by the host
        /// </summary>
        public static Func<LuaValue[], LuaValue[]>? LoadImplementation { get; set; }
        private readonly LuaEnvironment? _parent;
        private readonly Dictionary<string, LuaVariable> _variables = new Dictionary<string, LuaVariable>();
        private readonly List<LuaVariable> _toBeClosedVariables = [];

        public LuaTable Globals { get; }
        private bool _disposed = false;

        /// <summary>
        /// Creates a new environment with the specified parent environment
        /// </summary>
        public LuaEnvironment(LuaEnvironment? parent = null)
        {
            _parent = parent;
            Globals = parent?.Globals ?? new LuaTable();
        }

        /// <summary>
        /// Creates a new child environment
        /// </summary>
        public LuaEnvironment CreateChild()
        {
            return new LuaEnvironment(this);
        }

        /// <summary>
        /// Gets the value of a variable from the environment
        /// </summary>
        public LuaValue GetVariable(string name)
        {
            // Check local variables first
            if (_variables.TryGetValue(name, out var variable))
            {
                return variable.GetValue();
            }

            // Check parent environment if available
            if (_parent != null)
            {
                return _parent.GetVariable(name);
            }

            // Finally, check global table
            return Globals.Get(name);
        }

        /// <summary>
        /// Sets a variable in the appropriate scope
        /// </summary>
        public void SetVariable(string name, LuaValue value)
        {
            // If variable exists in local scope, update it
            if (_variables.TryGetValue(name, out var variable))
            {
                // This will throw an exception if the variable is const
                variable.SetValue(value);
                return;
            }

            // If variable exists in parent scope, update it there
            if (_parent != null && _parent.HasLocalVariable(name))
            {
                _parent.SetVariable(name, value);
                return;
            }



            // Set in global table
            Globals.Set(name, value);
        }

        /// <summary>
        /// Checks if a variable exists in local scope only
        /// </summary>
        private bool HasLocalVariable(string name)
        {
            return _variables.ContainsKey(name);
        }

        /// <summary>
        /// Sets a local variable in the current scope
        /// </summary>
        public void SetLocalVariable(string name, LuaValue value)
        {
            SetLocalVariable(name, value, LuaAttribute.NoAttribute);
        }

        /// <summary>
        /// Sets a local variable in the current scope with attributes
        /// </summary>
        public void SetLocalVariable(string name, LuaValue value, LuaAttribute attribute)
        {
            var variable = new LuaVariable(value, attribute, name);
            _variables[name] = variable;
            
            // Track to-be-closed variables for proper cleanup
            if (attribute == LuaAttribute.Close)
            {
                _toBeClosedVariables.Add(variable);
            }
        }

        /// <summary>
        /// Checks if a variable exists in this environment or its parents
        /// </summary>
        public bool HasVariable(string name)
        {
            if (_variables.ContainsKey(name))
            {
                return true;
            }

            if (_parent != null && _parent.HasVariable(name))
            {
                return true;
            }

            // Check global table
            return Globals.Get(name) != LuaValue.Nil;
        }

        /// <summary>
        /// Closes all to-be-closed variables in reverse order (LIFO)
        /// </summary>
        public void CloseToBeClosedVariables()
        {
            // Close variables in reverse order (LIFO - last declared, first closed)
            for (int i = _toBeClosedVariables.Count - 1; i >= 0; i--)
            {
                _toBeClosedVariables[i].Close();
            }
            _toBeClosedVariables.Clear();
        }

        /// <summary>
        /// Implements IDisposable to ensure to-be-closed variables are cleaned up
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                CloseToBeClosedVariables();
                _disposed = true;
            }
        }



        /// <summary>
        /// Creates a standard Lua environment with built-in functions
        /// </summary>
        public static LuaEnvironment CreateStandardEnvironment()
        {
            var env = new LuaEnvironment();
            
            // Basic functions
            env.SetVariable("print", new BuiltinFunction(Print));
            env.SetVariable("type", new BuiltinFunction(Type));
            env.SetVariable("tostring", new BuiltinFunction(ToString));
            env.SetVariable("tonumber", new BuiltinFunction(ToNumber));
            env.SetVariable("assert", new BuiltinFunction(Assert));
            
            // Error handling
            env.SetVariable("pcall", new BuiltinFunction(ProtectedCall));
            env.SetVariable("xpcall", new BuiltinFunction(ExtendedProtectedCall));
            env.SetVariable("error", new BuiltinFunction(Error));
            
            // Table functions
            env.SetVariable("pairs", new BuiltinFunction(Pairs));
            env.SetVariable("ipairs", new BuiltinFunction(IPairs));
            env.SetVariable("next", new BuiltinFunction(Next));
            
            // Raw operations
            env.SetVariable("rawget", new BuiltinFunction(RawGet));
            env.SetVariable("rawset", new BuiltinFunction(RawSet));
            env.SetVariable("rawequal", new BuiltinFunction(RawEqual));
            env.SetVariable("rawlen", new BuiltinFunction(RawLen));
            
            // Metatable functions
            env.SetVariable("setmetatable", new BuiltinFunction(SetMetatable));
            env.SetVariable("getmetatable", new BuiltinFunction(GetMetatable));
        
        // Lua 5.4 functions
        env.SetVariable("warn", new BuiltinFunction(Warn));
            
            // Varargs and unpacking
            env.SetVariable("select", new BuiltinFunction(Select));
            env.SetVariable("unpack", new BuiltinFunction(Unpack));
            
            // Garbage collection (stub implementation)
            env.SetVariable("collectgarbage", new BuiltinFunction(CollectGarbage));
            
            // Dynamic loading (simplified - no actual compilation)
            env.SetVariable("load", new BuiltinFunction(Load));
            
            // Add standard libraries
            LuaCoroutineLib.AddCoroutineLibrary(env);
            LuaMathLib.AddMathLibrary(env);
            LuaStringLib.AddStringLibrary(env);
            LuaTableLib.AddTableLibrary(env);
            LuaIOLib.AddIOLibrary(env);
            LuaOSLib.AddOSLibrary(env);
            LuaUTF8Lib.AddUTF8Library(env);
            LuaDebugLib.AddDebugLibrary(env);
            
            // Add package library and require function
            LuaPackageLib.AddPackageLibrary(env);
            
            return env;
        }
        
        /// <summary>
        /// Implements the print function
        /// </summary>
        private static LuaValue[] Print(LuaValue[] args)
        {
            Console.WriteLine(string.Join("\t", args.Select(a => a.ToString())));
            return [];
        }
        
        /// <summary>
        /// Implements the type function
        /// </summary>
        private static LuaValue[] Type(LuaValue[] args)
        {
            if (args.Length == 0)
                return ["no value"];
            
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
            
            return [(LuaValue)type];
        }
        
        /// <summary>
        /// Implements the tostring function
        /// </summary>
        private static LuaValue[] ToString(LuaValue[] args)
        {
            if (args.Length == 0)
                return [LuaValue.Nil];
            
            // Check for __tostring metamethod
            if (args[0].IsTable)
            {
                var table = args[0].AsTable<LuaTable>();
                if (table.Metatable != null)
                {
                    var toStringMethod = table.Metatable.RawGet("__tostring");
                    if (toStringMethod.IsFunction)
                {
                        var func = toStringMethod.AsFunction<LuaFunction>();
                        return func.Call(new[] { args[0] });
                    }
                }
            }
            
            return [args[0]];
        }
        
        /// <summary>
        /// Implements the tonumber function
        /// </summary>
        private static LuaValue[] ToNumber(LuaValue[] args)
        {
            if (args.Length == 0)
                return [LuaValue.Nil];
            
            var value = args[0];
            
            if (value.IsNumber)
                return [(LuaValue)value.AsDouble()];
            
            if (value.IsString)
            {
                var str = value.AsString();
                if (double.TryParse(str, out var result))
                    return [(LuaValue)result];
            }
            
            return [LuaValue.Nil];
        }
        
        /// <summary>
        /// Implements the pcall function
        /// </summary>
        private static LuaValue[] ProtectedCall(LuaValue[] args)
        {
            if (args.Length == 0)
                return [false, "no function to call"];
            
            if (args[0].IsFunction)
            {
                try
                {
                    // Call the function with the rest of the arguments
                    var funcArgs = new LuaValue[args.Length - 1];
                    Array.Copy(args, 1, funcArgs, 0, args.Length - 1);
                    
                    var func = args[0].AsFunction<LuaFunction>();
                    var results = func.Call(funcArgs);
                    
                    // Prepend success value
                    var pcallResults = new LuaValue[results.Length + 1];
                    pcallResults[0] = true;
                    Array.Copy(results, 0, pcallResults, 1, results.Length);
                    
                    return pcallResults;
                }
                catch (LuaRuntimeException ex)
                {
                    return [false, ex.Message];
                }
                catch (Exception ex)
                {
                    return [false, $"Internal error: {ex.Message}"];
                }
            }
            
            return [false, "attempt to call a non-function"];
        }
        
        /// <summary>
        /// Implements the assert function
        /// </summary>
        private static LuaValue[] Assert(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                // assert() with no arguments - this is falsy, so error
                throw new LuaRuntimeException("assertion failed!");
            }
            
            var firstArg = args[0];
            
            // Check if the first argument is falsy (nil or false)
            if (!firstArg.IsTruthy())
            {
                // Get error message from second argument or use default
                string message = args.Length > 1 && !args[1].IsNil
                    ? (args[1].ToString() ?? "assertion failed!")
                    : "assertion failed!";
                
                throw new LuaRuntimeException(message);
            }
            
            // If assertion passes, return all arguments
            return args;
        }
        
        /// <summary>
        /// Implements the error function
        /// </summary>
        private static LuaValue[] Error(LuaValue[] args)
        {
            string message = args.Length > 0 ? (args[0].ToString() ?? "error") : "error";
            throw new LuaRuntimeException(message);
        }
        
        /// <summary>
        /// Implements the pairs function
        /// </summary>
        private static LuaValue[] Pairs(LuaValue[] args)
        {
            if (args.Length == 0 || !args[0].IsTable)
                throw new LuaRuntimeException("bad argument #1 to 'pairs' (table expected)");
            
            var table = args[0].AsTable<LuaTable>();
            
            // Check for __pairs metamethod
            if (table.Metatable != null)
            {
                var pairsMethod = table.Metatable.RawGet("__pairs");
                if (pairsMethod.IsFunction)
                {
                    var func = pairsMethod.AsFunction<LuaFunction>();
                    return func.Call(new[] { args[0] });
                }
            }
            
            // Create the iterator function
            var nextFunc = new BuiltinFunction(nextArgs =>
            {
                if (nextArgs.Length < 2)
                    throw new LuaRuntimeException("bad argument #1 to 'next' (table expected)");
                
                if (!nextArgs[0].IsTable)
                    throw new LuaRuntimeException("bad argument #1 to 'next' (table expected)");
                    
                var t = nextArgs[0].AsTable<LuaTable>();
                var key = nextArgs.Length > 1 ? nextArgs[1] : LuaValue.Nil;
                
                // First check the dictionary
                var dict = t.Dictionary;
                var found = false;
                LuaValue? nextKey = null;
                
                if (key.IsNil)
                {
                    // Start with the first key
                    var enumerator = dict.GetEnumerator();
                    if (enumerator.MoveNext())
                    {
                        nextKey = enumerator.Current.Key;
                        found = true;
                    }
                }
                else
                {
                    // Find the next key
                    bool keyFound = false;
                    foreach (var k in dict.Keys)
                    {
                        if (keyFound)
                        {
                            nextKey = k;
                            found = true;
                            break;
                        }
                        
                        if (k.ToString() == key.ToString())
                        {
                            keyFound = true;
                        }
                    }
                }
                
                if (found && nextKey.HasValue)
                {
                    return [nextKey.Value, t.Get(nextKey.Value)];
                }
                
                return [LuaValue.Nil];
            });
            
            return [LuaValue.Function(nextFunc), args[0], LuaValue.Nil];
        }
        
        /// <summary>
        /// Implements the ipairs function
        /// </summary>
        private static LuaValue[] IPairs(LuaValue[] args)
        {
            if (args.Length == 0 || !args[0].IsTable)
                throw new LuaRuntimeException("bad argument #1 to 'ipairs' (table expected)");
            
            var table = args[0].AsTable<LuaTable>();
            
            // Check for __ipairs metamethod
            if (table.Metatable != null)
            {
                var ipairsMethod = table.Metatable.RawGet(LuaValue.String("__ipairs"));
                if (ipairsMethod.IsFunction)
                {
                    var func = ipairsMethod.AsFunction<LuaFunction>();
                    return func.Call(new[] { args[0] });
                }
            }
            
            // Create the iterator function
            var iterFunc = new BuiltinFunction(iterArgs =>
            {
                if (iterArgs.Length < 2)
                    throw new LuaRuntimeException("bad argument #1 to 'ipairs iterator' (table expected)");
                
                if (!iterArgs[0].IsTable)
                    throw new LuaRuntimeException("bad argument #1 to 'ipairs iterator' (table expected)");
                
                if (!iterArgs[1].IsInteger)
                    throw new LuaRuntimeException("bad argument #2 to 'ipairs iterator' (number expected)");
                
                var t = iterArgs[0].AsTable<LuaTable>();
                var index = iterArgs[1].AsInteger();
                
                var nextIndex = index + 1;
                var nextValue = t.Get((LuaValue)nextIndex);
                
                if (nextValue.IsNil)
                    return [LuaValue.Nil];
                
                return [(LuaValue)nextIndex, nextValue];
            });
            
            return [iterFunc, args[0], 0];
        }
        
        /// <summary>
        /// Implements the setmetatable function
        /// </summary>
        private static LuaValue[] SetMetatable(LuaValue[] args)
        {
            if (args.Length < 2)
                throw new LuaRuntimeException("bad argument #1 to 'setmetatable' (table expected)");
            
            if (!args[0].IsTable)
                throw new LuaRuntimeException("bad argument #1 to 'setmetatable' (table expected)");
            
            var table = args[0].AsTable<LuaTable>();
            
            if (!args[1].IsNil && !args[1].IsTable)
                throw new LuaRuntimeException("bad argument #2 to 'setmetatable' (nil or table expected)");
            
            // Check for __metatable field in the current metatable
            if (table.Metatable != null)
            {
                var protectedMeta = table.Metatable.RawGet(LuaValue.String("__metatable"));
                if (!protectedMeta.IsNil)
                    throw new LuaRuntimeException("cannot change a protected metatable");
            }
            
            // Set the metatable
            table.Metatable = args[1].IsTable ? args[1].AsTable<LuaTable>() : null;
            
            return [args[0]];
        }
        
        /// <summary>
        /// Implements the getmetatable function
        /// </summary>
        private static LuaValue[] GetMetatable(LuaValue[] args)
        {
            if (args.Length == 0)
                return [LuaValue.Nil];
            
            if (!args[0].IsTable)
                return [LuaValue.Nil];
            
            var table = args[0].AsTable<LuaTable>();
            
            if (table.Metatable == null)
                return [LuaValue.Nil];
            
            // Check for __metatable field
            var protectedMeta = table.Metatable.RawGet(LuaValue.String("__metatable"));
            if (!protectedMeta.IsNil)
                return [protectedMeta];
            
            return [LuaValue.Table(table.Metatable)];
        }
        
        /// <summary>
        /// Implements the next function
        /// </summary>
        private static LuaValue[] Next(LuaValue[] args)
        {
            if (args.Length == 0 || !args[0].IsTable)
                throw new LuaRuntimeException("bad argument #1 to 'next' (table expected)");
            
            var table = args[0].AsTable<LuaTable>();
            var key = args.Length > 1 ? args[1] : LuaValue.Nil;
            
            // Get all keys from the table
            var dict = table.Dictionary;
            var keys = dict.Keys.ToList();
            
            if (key.IsNil)
            {
                // Return first key-value pair
                if (keys.Count > 0)
                {
                    var firstKey = keys[0];
                    return [firstKey, table.Get(firstKey)];
                }
                return [LuaValue.Nil];
            }
            
            // Find the next key after the given key
            bool keyFound = false;
            foreach (var k in keys)
            {
                if (keyFound)
                {
                    return [k, table.Get(k)];
                }
                
                if (k.ToString() == key.ToString())
                {
                    keyFound = true;
                }
            }
            
            // No next key found
            return [LuaValue.Nil];
        }
        
        /// <summary>
        /// Implements the xpcall function
        /// </summary>
        private static LuaValue[] ExtendedProtectedCall(LuaValue[] args)
        {
            if (args.Length < 2)
                return [LuaValue.Boolean(false), LuaValue.String("bad arguments to 'xpcall'")];
            
            if (!args[0].IsFunction)
                return [false, "attempt to call a non-function"];
            
            if (!args[1].IsFunction)
                return [LuaValue.Boolean(false), LuaValue.String("bad argument #2 to 'xpcall' (function expected)")];
            
            try
            {
                // Call the function with the rest of the arguments
                var funcArgs = new LuaValue[args.Length - 2];
                Array.Copy(args, 2, funcArgs, 0, args.Length - 2);
                
                var func = args[0].AsFunction<LuaFunction>();
                var results = func.Call(funcArgs);
                
                // Prepend success value
                var xpcallResults = new LuaValue[results.Length + 1];
                xpcallResults[0] = LuaValue.Boolean(true);
                Array.Copy(results, 0, xpcallResults, 1, results.Length);
                
                return xpcallResults;
            }
            catch (LuaRuntimeException ex)
            {
                try
                {
                    // Call error handler
                    var errorHandler = args[1].AsFunction<LuaFunction>();
                    var errorResults = errorHandler.Call(new[] { LuaValue.String(ex.Message) });
                    var errorMessage = errorResults.Length > 0 ? errorResults[0] : LuaValue.String(ex.Message);
                    return [LuaValue.Boolean(false), errorMessage];
                }
                catch
                {
                    // Error in error handler
                    return [LuaValue.Boolean(false), LuaValue.String("error in error handling")];
                }
            }
            catch (Exception ex)
            {
                try
                {
                    // Call error handler
                    var errorHandler = args[1].AsFunction<LuaFunction>();
                    var errorResults = errorHandler.Call(new[] { LuaValue.String($"Internal error: {ex.Message}") });
                    var errorMessage = errorResults.Length > 0 ? errorResults[0] : LuaValue.String(ex.Message);
                    return [LuaValue.Boolean(false), errorMessage];
                }
                catch
                {
                    // Error in error handler
                    return [LuaValue.Boolean(false), LuaValue.String("error in error handling")];
                }
            }
        }
        
        /// <summary>
        /// Implements the rawget function
        /// </summary>
        private static LuaValue[] RawGet(LuaValue[] args)
        {
            if (args.Length < 2)
                throw new LuaRuntimeException("bad arguments to 'rawget'");
            
            if (!args[0].IsTable)
                throw new LuaRuntimeException("bad argument #1 to 'rawget' (table expected)");
            
            var table = args[0].AsTable<LuaTable>();
            return [table.RawGet(args[1])];
        }
        
        /// <summary>
        /// Implements the rawset function
        /// </summary>
        private static LuaValue[] RawSet(LuaValue[] args)
        {
            if (args.Length < 3)
                throw new LuaRuntimeException("bad arguments to 'rawset'");
            
            if (!args[0].IsTable)
                throw new LuaRuntimeException("bad argument #1 to 'rawset' (table expected)");
            
            var table = args[0].AsTable<LuaTable>();
            table.Set(args[1], args[2]);
            return [args[0]];
        }
        
        /// <summary>
        /// Implements the rawequal function
        /// </summary>
        private static LuaValue[] RawEqual(LuaValue[] args)
        {
            if (args.Length < 2)
                return [LuaValue.Boolean(false)];
            
            // Raw equality means no metamethods
            bool equal = args[0].GetType() == args[1].GetType() && 
                        args[0].ToString() == args[1].ToString();
            
            return [LuaValue.Boolean(equal)];
        }
        
        /// <summary>
        /// Implements the rawlen function
        /// </summary>
        private static LuaValue[] RawLen(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'rawlen'");
            
            var value = args[0];
            
            if (value.IsString)
            {
                var str = value.AsString();
                return [LuaValue.Integer(str.Length)];
            }
            
            if (value.IsTable)
            {
                var table = value.AsTable<LuaTable>();
                // Count consecutive integer keys starting from 1
                int length = 0;
                while (!table.RawGet(LuaValue.Integer(length + 1)).IsNil)
                {
                    length++;
                }
                return [LuaValue.Integer(length)];
            }
            
            throw new LuaRuntimeException($"attempt to get length of a {Type([value])[0]} value");
        }
        
        /// <summary>
        /// Implements the select function
        /// </summary>
        private static LuaValue[] Select(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'select'");
            
            var selector = args[0];
            
            // Handle '#' selector - return count of remaining arguments
            if (selector.IsString && selector.AsString() == "#")
            {
                return [LuaValue.Integer(args.Length - 1)];
            }
            
            // Handle numeric selector
            if (selector.IsNumber)
            {
                int index = (int)selector.AsDouble();
                
                // Handle negative indices (from end)
                if (index < 0)
                {
                    index = args.Length + index;
                }
                else
                {
                    index--; // Convert to 0-based indexing
                }
                
                // Validate index
                if (index < 0 || index >= args.Length - 1)
                {
                    return [];
                }
                
                // Return arguments starting from the specified index
                var result = new LuaValue[args.Length - 1 - index];
                Array.Copy(args, index + 1, result, 0, result.Length);
                return result;
            }
            
            throw new LuaRuntimeException("bad argument #1 to 'select' (number expected)");
        }
        
        /// <summary>
        /// Implements the unpack function
        /// </summary>
        private static LuaValue[] Unpack(LuaValue[] args)
        {
            if (args.Length == 0 || !args[0].IsTable)
                throw new LuaRuntimeException("bad argument #1 to 'unpack' (table expected)");
            
            var table = args[0].AsTable<LuaTable>();
            
            // Get start index (default 1)
            int start = 1;
            if (args.Length > 1 && args[1].IsNumber)
            {
                start = (int)args[1].AsDouble();
            }
            
            // Get end index (default table length)
            int end = start;
            if (args.Length > 2 && args[2].IsNumber)
            {
                end = (int)args[2].AsDouble();
            }
            else
            {
                // Calculate table length
                while (!table.RawGet(LuaValue.Integer(end)).IsNil)
                {
                    end++;
                }
                end--; // Last valid index
            }
            
            // Extract values
            var results = new List<LuaValue>();
            for (int i = start; i <= end; i++)
            {
                results.Add(table.RawGet(LuaValue.Integer(i)));
            }
            
            return results.ToArray();
        }
        
        /// <summary>
        /// Implements the warn function (Lua 5.4)
        /// </summary>
        private static LuaValue[] Warn(LuaValue[] args)
        {
            if (args.Length == 0)
                return [];
            
            var message = args[0].IsString ? args[0].AsString() : args[0].ToString();
            var tocont = args.Length > 1 && args[1].IsString ? args[1].AsString() : "@off";
            
            // Simple warning implementation - write to stderr
            // In a full implementation, this would respect warning control modes
            if (tocont != "@off")
            {
                Console.Error.WriteLine($"Lua warning: {message}"); 
            }
            
            return [];
        }
        
        /// <summary>
        /// Implements the collectgarbage function (stub implementation)
        /// </summary>
        private static LuaValue[] CollectGarbage(LuaValue[] args)
        {
            // In a real Lua implementation, this would control the garbage collector
            // For now, we'll just run .NET's garbage collector for some options
            
            string option = "collect";
            if (args.Length > 0 && args[0].IsString)
            {
                option = args[0].AsString()!;
            }
            
            switch (option)
            {
                case "collect":
                    // Force a garbage collection
                    GC.Collect();
                    return [LuaValue.Integer(0)]; // Return 0 for compatibility
                    
                case "stop":
                case "restart":
                case "step":
                    // These don't map well to .NET GC, just return success
                    return [LuaValue.Integer(0)];
                    
                case "count":
                    // Return memory usage in KB
                    long memoryUsed = GC.GetTotalMemory(false) / 1024;
                    return [LuaValue.Number(memoryUsed)];
                    
                case "isrunning":
                    // .NET GC is always running
                    return [LuaValue.Boolean(true)];
                    
                default:
                    // Unknown option, return nil
                    return [LuaValue.Nil];
            }
        }
        
        /// <summary>
        /// Implements the load function (simplified - no actual compilation)
        /// </summary>
        private static LuaValue[] Load(LuaValue[] args)
        {
            if (args.Length == 0)
                return [LuaValue.Nil, LuaValue.String("no chunk to load")];
            
            // Use the host-provided implementation if available
            if (LoadImplementation != null)
            {
                return LoadImplementation(args);
            }
            
            // Otherwise, return an error since we don't support dynamic compilation in the runtime
            if (args[0].IsString)
            {
                // Return nil and an error message
                return [LuaValue.Nil, LuaValue.String("dynamic loading not supported")];
            }
            
            return [LuaValue.Nil, LuaValue.String("bad argument #1 to 'load' (string expected)")];
        }
    }

    // Moved to LuaTypes.cs
    /*
    /// <summary>
    /// Represents a built-in function implemented in C#
    /// </summary>
    public class BuiltinFunction : LuaFunction
    {
        private readonly Func<LuaValue[], LuaValue[]> _function;

        public BuiltinFunction(Func<LuaValue[], LuaValue[]> function)
        {
            _function = function ?? throw new ArgumentNullException(nameof(function));
        }

        public override LuaValue[] Call(LuaValue[] arguments)
        {
            return _function(arguments);
        }
    }
    */
} 