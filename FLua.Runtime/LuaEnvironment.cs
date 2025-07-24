using System;
using System.Collections.Generic;
using System.Linq;

namespace FLua.Runtime
{
    /// <summary>
    /// Represents a Lua execution environment with variable scopes
    /// </summary>
    public class LuaEnvironment
    {
        private readonly LuaEnvironment? _parent;
        private readonly Dictionary<string, LuaValue> _variables = new Dictionary<string, LuaValue>();
        public LuaTable Globals { get; }

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
            if (_variables.TryGetValue(name, out var value))
            {
                return value;
            }

            // Check parent environment if available
            if (_parent != null)
            {
                return _parent.GetVariable(name);
            }

            // Finally, check global table
            return Globals.Get(new LuaString(name));
        }

        /// <summary>
        /// Sets a variable in the appropriate scope
        /// </summary>
        public void SetVariable(string name, LuaValue value)
        {
            // If variable exists in local scope, update it
            if (_variables.ContainsKey(name))
            {
                _variables[name] = value;
                return;
            }

            // If variable exists in parent scope, update it there
            if (_parent != null && _parent.HasVariable(name))
            {
                _parent.SetVariable(name, value);
                return;
            }

            // Otherwise, set in global table
            Globals.Set(new LuaString(name), value);
        }

        /// <summary>
        /// Sets a local variable in the current scope
        /// </summary>
        public void SetLocalVariable(string name, LuaValue value)
        {
            _variables[name] = value;
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
            return Globals.Get(new LuaString(name)) != LuaNil.Instance;
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
            
            // Error handling
            env.SetVariable("pcall", new BuiltinFunction(ProtectedCall));
            env.SetVariable("error", new BuiltinFunction(Error));
            
            // Table functions
            env.SetVariable("pairs", new BuiltinFunction(Pairs));
            env.SetVariable("ipairs", new BuiltinFunction(IPairs));
            
            // Metatable functions
            env.SetVariable("setmetatable", new BuiltinFunction(SetMetatable));
            env.SetVariable("getmetatable", new BuiltinFunction(GetMetatable));
            
            // Add standard libraries
            LuaCoroutineLib.AddCoroutineLibrary(env);
            
            return env;
        }
        
        /// <summary>
        /// Implements the print function
        /// </summary>
        private static LuaValue[] Print(LuaValue[] args)
        {
            Console.WriteLine(string.Join("\t", args.Select(a => a.ToString())));
            return Array.Empty<LuaValue>();
        }
        
        /// <summary>
        /// Implements the type function
        /// </summary>
        private static LuaValue[] Type(LuaValue[] args)
        {
            if (args.Length == 0)
                return new[] { new LuaString("no value") };
            
            var value = args[0];
            string type = value switch
            {
                LuaNil => "nil",
                LuaBoolean => "boolean",
                LuaNumber => "number",
                LuaInteger => "number",
                LuaString => "string",
                LuaTable => "table",
                LuaFunction => "function",
                _ => "userdata"
            };
            
            return new[] { new LuaString(type) };
        }
        
        /// <summary>
        /// Implements the tostring function
        /// </summary>
        private static LuaValue[] ToString(LuaValue[] args)
        {
            if (args.Length == 0)
                return new[] { new LuaString("nil") };
            
            // Check for __tostring metamethod
            if (args[0] is LuaTable table && table.Metatable != null)
            {
                var toStringMethod = table.Metatable.RawGet(new LuaString("__tostring"));
                if (toStringMethod is LuaFunction func)
                {
                    return func.Call(new[] { args[0] });
                }
            }
            
            return new[] { new LuaString(args[0].ToString() ?? string.Empty) };
        }
        
        /// <summary>
        /// Implements the tonumber function
        /// </summary>
        private static LuaValue[] ToNumber(LuaValue[] args)
        {
            if (args.Length == 0)
                return new[] { LuaNil.Instance };
            
            var value = args[0];
            
            if (value.AsNumber.HasValue)
                return new[] { new LuaNumber(value.AsNumber.Value) };
            
            if (value is LuaString str)
            {
                if (double.TryParse(str.Value, out var result))
                    return new[] { new LuaNumber(result) };
            }
            
            return new[] { LuaNil.Instance };
        }
        
        /// <summary>
        /// Implements the pcall function
        /// </summary>
        private static LuaValue[] ProtectedCall(LuaValue[] args)
        {
            if (args.Length == 0)
                return new LuaValue[] { new LuaBoolean(false), new LuaString("no function to call") };
            
            if (args[0] is LuaFunction func)
            {
                try
                {
                    // Call the function with the rest of the arguments
                    var funcArgs = new LuaValue[args.Length - 1];
                    Array.Copy(args, 1, funcArgs, 0, args.Length - 1);
                    
                    var results = func.Call(funcArgs);
                    
                    // Prepend success value
                    var pcallResults = new LuaValue[results.Length + 1];
                    pcallResults[0] = new LuaBoolean(true);
                    Array.Copy(results, 0, pcallResults, 1, results.Length);
                    
                    return pcallResults;
                }
                catch (LuaRuntimeException ex)
                {
                    return new LuaValue[] { new LuaBoolean(false), new LuaString(ex.Message) };
                }
                catch (Exception ex)
                {
                    return new LuaValue[] { new LuaBoolean(false), new LuaString($"Internal error: {ex.Message}") };
                }
            }
            
            return new LuaValue[] { new LuaBoolean(false), new LuaString("attempt to call a non-function") };
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
            if (args.Length == 0 || !(args[0] is LuaTable table))
                throw new LuaRuntimeException("bad argument #1 to 'pairs' (table expected)");
            
            // Check for __pairs metamethod
            if (table.Metatable != null)
            {
                var pairsMethod = table.Metatable.RawGet(new LuaString("__pairs"));
                if (pairsMethod is LuaFunction func)
                {
                    return func.Call(new[] { table });
                }
            }
            
            // Create the iterator function
            var nextFunc = new LuaUserFunction(nextArgs =>
            {
                if (nextArgs.Length < 2)
                    throw new LuaRuntimeException("bad argument #1 to 'next' (table expected)");
                
                var t = nextArgs[0] as LuaTable;
                var key = nextArgs.Length > 1 ? nextArgs[1] : LuaNil.Instance;
                
                if (t == null)
                    throw new LuaRuntimeException("bad argument #1 to 'next' (table expected)");
                
                // First check the dictionary
                var dict = t.Dictionary;
                var found = false;
                LuaValue? nextKey = null;
                
                if (key == LuaNil.Instance)
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
                
                if (found && nextKey != null)
                {
                    return new LuaValue[] { nextKey, t.Get(nextKey) };
                }
                
                return new LuaValue[] { LuaNil.Instance };
            });
            
            return new LuaValue[] { nextFunc, table, LuaNil.Instance };
        }
        
        /// <summary>
        /// Implements the ipairs function
        /// </summary>
        private static LuaValue[] IPairs(LuaValue[] args)
        {
            if (args.Length == 0 || !(args[0] is LuaTable table))
                throw new LuaRuntimeException("bad argument #1 to 'ipairs' (table expected)");
            
            // Check for __ipairs metamethod
            if (table.Metatable != null)
            {
                var ipairsMethod = table.Metatable.RawGet(new LuaString("__ipairs"));
                if (ipairsMethod is LuaFunction func)
                {
                    return func.Call(new[] { table });
                }
            }
            
            // Create the iterator function
            var iterFunc = new LuaUserFunction(iterArgs =>
            {
                if (iterArgs.Length < 2)
                    throw new LuaRuntimeException("bad argument #1 to 'ipairs iterator' (table expected)");
                
                var t = iterArgs[0] as LuaTable;
                var index = iterArgs[1] as LuaInteger;
                
                if (t == null)
                    throw new LuaRuntimeException("bad argument #1 to 'ipairs iterator' (table expected)");
                
                if (index == null)
                    throw new LuaRuntimeException("bad argument #2 to 'ipairs iterator' (number expected)");
                
                var nextIndex = index.Value + 1;
                var nextValue = t.Get(new LuaInteger(nextIndex));
                
                if (nextValue == LuaNil.Instance)
                    return new LuaValue[] { LuaNil.Instance };
                
                return new LuaValue[] { new LuaInteger(nextIndex), nextValue };
            });
            
            return new LuaValue[] { iterFunc, table, new LuaInteger(0) };
        }
        
        /// <summary>
        /// Implements the setmetatable function
        /// </summary>
        private static LuaValue[] SetMetatable(LuaValue[] args)
        {
            if (args.Length < 2)
                throw new LuaRuntimeException("bad argument #1 to 'setmetatable' (table expected)");
            
            if (!(args[0] is LuaTable table))
                throw new LuaRuntimeException("bad argument #1 to 'setmetatable' (table expected)");
            
            if (args[1] != LuaNil.Instance && !(args[1] is LuaTable metatable))
                throw new LuaRuntimeException("bad argument #2 to 'setmetatable' (nil or table expected)");
            
            // Check for __metatable field in the current metatable
            if (table.Metatable != null)
            {
                var protectedMeta = table.Metatable.RawGet(new LuaString("__metatable"));
                if (protectedMeta != LuaNil.Instance)
                    throw new LuaRuntimeException("cannot change a protected metatable");
            }
            
            // Set the metatable
            table.Metatable = args[1] as LuaTable;
            
            return new[] { table };
        }
        
        /// <summary>
        /// Implements the getmetatable function
        /// </summary>
        private static LuaValue[] GetMetatable(LuaValue[] args)
        {
            if (args.Length == 0)
                return new[] { LuaNil.Instance };
            
            if (!(args[0] is LuaTable table))
                return new[] { LuaNil.Instance };
            
            if (table.Metatable == null)
                return new[] { LuaNil.Instance };
            
            // Check for __metatable field
            var protectedMeta = table.Metatable.RawGet(new LuaString("__metatable"));
            if (protectedMeta != LuaNil.Instance)
                return new[] { protectedMeta };
            
            return new[] { table.Metatable };
        }
    }

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
} 