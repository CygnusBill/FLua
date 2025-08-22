using System;
using System.Collections.Generic;
using System.Linq;
using FLua.Common;

namespace FLua.Runtime
{
    /// <summary>
    /// Result-based version of Lua Environment core functions
    /// Converts all exception-based error handling to Result pattern
    /// </summary>
    public static class ResultLuaEnvironment
    {
        #region Core Functions
        
        public static Result<LuaValue[]> AssertResult(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                // assert() with no arguments - this is falsy, so error
                return Result<LuaValue[]>.Failure("assertion failed!");
            }
            
            var firstArg = args[0];
            
            // Check if the first argument is falsy (nil or false)
            if (!firstArg.IsTruthy())
            {
                // Get error message from second argument or use default
                string message = args.Length > 1 && !args[1].IsNil
                    ? (args[1].ToString() ?? "assertion failed!")
                    : "assertion failed!";
                
                return Result<LuaValue[]>.Failure(message);
            }
            
            // If assertion passes, return all arguments
            return Result<LuaValue[]>.Success(args);
        }

        public static Result<LuaValue[]> ErrorResult(LuaValue[] args)
        {
            string message = args.Length > 0 && !args[0].IsNil
                ? args[0].ToString() ?? "error"
                : "error";
            
            return Result<LuaValue[]>.Failure(message);
        }

        public static Result<LuaValue[]> PairsResult(LuaValue[] args)
        {
            if (args.Length == 0 || !args[0].IsTable)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'pairs' (table expected)");
            
            var table = args[0].AsTable<LuaTable>();
            
            // Check for __pairs metamethod
            if (table.Metatable != null)
            {
                var pairsMethod = table.Metatable.RawGet("__pairs");
                if (pairsMethod.IsFunction)
                {
                    var func = pairsMethod.AsFunction<LuaFunction>();
                    try
                    {
                        var result = func.Call(new[] { args[0] });
                        return Result<LuaValue[]>.Success(result);
                    }
                    catch (Exception ex)
                    {
                        return Result<LuaValue[]>.Failure($"error in __pairs metamethod: {ex.Message}");
                    }
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
            
            return Result<LuaValue[]>.Success([LuaValue.Function(nextFunc), args[0], LuaValue.Nil]);
        }

        public static Result<LuaValue[]> NextResult(LuaValue[] args)
        {
            if (args.Length < 1 || !args[0].IsTable)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'next' (table expected)");
            
            var table = args[0].AsTable<LuaTable>();
            var key = args.Length > 1 ? args[1] : LuaValue.Nil;
            
            // First check the dictionary
            var dict = table.Dictionary;
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
                return Result<LuaValue[]>.Success([nextKey.Value, table.Get(nextKey.Value)]);
            }
            
            return Result<LuaValue[]>.Success([LuaValue.Nil]);
        }

        public static Result<LuaValue[]> IPairsResult(LuaValue[] args)
        {
            if (args.Length == 0 || !args[0].IsTable)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'ipairs' (table expected)");
            
            var table = args[0].AsTable<LuaTable>();
            
            // Create the iterator function
            var iteratorFunc = new BuiltinFunction(iterArgs =>
            {
                if (iterArgs.Length < 2)
                    throw new LuaRuntimeException("bad argument #1 to 'ipairs iterator' (table expected)");
                
                if (!iterArgs[0].IsTable)
                    throw new LuaRuntimeException("bad argument #1 to 'ipairs iterator' (table expected)");
                    
                if (!iterArgs[1].IsInteger)
                    throw new LuaRuntimeException("bad argument #2 to 'ipairs iterator' (number expected)");
                
                var t = iterArgs[0].AsTable<LuaTable>();
                var index = (int)iterArgs[1].AsInteger() + 1;
                var key = LuaValue.Integer(index);
                var value = t.Get(key);
                
                if (!value.IsNil)
                {
                    return [key, value];
                }
                
                return [LuaValue.Nil];
            });
            
            return Result<LuaValue[]>.Success([LuaValue.Function(iteratorFunc), args[0], LuaValue.Integer(0)]);
        }

        public static Result<LuaValue[]> SetMetatableResult(LuaValue[] args)
        {
            if (args.Length < 2)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'setmetatable' (table expected)");
            
            if (!args[0].IsTable)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'setmetatable' (table expected)");
            
            var table = args[0].AsTable<LuaTable>();
            var metatable = args[1];
            
            if (!metatable.IsNil && !metatable.IsTable)
                return Result<LuaValue[]>.Failure("bad argument #2 to 'setmetatable' (nil or table expected)");
            
            // Check if the current metatable is protected
            if (table.Metatable != null)
            {
                var metaMetatable = table.Metatable.RawGet("__metatable");
                if (!metaMetatable.IsNil)
                    return Result<LuaValue[]>.Failure("cannot change a protected metatable");
            }
            
            // Set the new metatable
            if (metatable.IsNil)
            {
                table.Metatable = null;
            }
            else
            {
                table.Metatable = metatable.AsTable<LuaTable>();
            }
            
            return Result<LuaValue[]>.Success([args[0]]);
        }

        public static Result<LuaValue[]> RawGetResult(LuaValue[] args)
        {
            if (args.Length < 2)
                return Result<LuaValue[]>.Failure("bad arguments to 'rawget'");
            
            if (!args[0].IsTable)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'rawget' (table expected)");
            
            var table = args[0].AsTable<LuaTable>();
            var key = args[1];
            var value = table.RawGet(key);
            
            return Result<LuaValue[]>.Success([value]);
        }

        public static Result<LuaValue[]> RawSetResult(LuaValue[] args)
        {
            if (args.Length < 3)
                return Result<LuaValue[]>.Failure("bad arguments to 'rawset'");
            
            if (!args[0].IsTable)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'rawset' (table expected)");
            
            var table = args[0].AsTable<LuaTable>();
            var key = args[1];
            var value = args[2];
            
            table.RawSet(key, value);
            
            return Result<LuaValue[]>.Success([args[0]]);
        }

        public static Result<LuaValue[]> RawLenResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'rawlen'");
            
            var value = args[0];
            
            if (value.IsString)
            {
                return Result<LuaValue[]>.Success([LuaValue.Integer(value.AsString().Length)]);
            }
            else if (value.IsTable)
            {
                var table = value.AsTable<LuaTable>();
                return Result<LuaValue[]>.Success([LuaValue.Integer(table.Length())]);
            }
            else
            {
                return Result<LuaValue[]>.Failure($"attempt to get length of a {GetTypeName(value)} value");
            }
        }

        public static Result<LuaValue[]> SelectResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'select'");
            
            var selector = args[0];
            
            if (selector.IsString && selector.AsString() == "#")
            {
                // Return count of remaining arguments
                return Result<LuaValue[]>.Success([LuaValue.Integer(args.Length - 1)]);
            }
            else if (selector.IsInteger)
            {
                var index = (int)selector.AsInteger();
                
                if (index < 1 || index > args.Length - 1)
                    return Result<LuaValue[]>.Success([]);
                
                // Return arguments from index to end
                var result = args.Skip(index).ToArray();
                return Result<LuaValue[]>.Success(result);
            }
            else
            {
                return Result<LuaValue[]>.Failure("bad argument #1 to 'select' (number expected)");
            }
        }

        public static Result<LuaValue[]> UnpackResult(LuaValue[] args)
        {
            if (args.Length == 0 || !args[0].IsTable)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'unpack' (table expected)");
            
            var table = args[0].AsTable<LuaTable>();
            var i = args.Length > 1 && args[1].IsInteger ? (int)args[1].AsInteger() : 1;
            var n = table.Length();
            
            if (args.Length > 2)
            {
                if (!args[2].IsInteger)
                    return Result<LuaValue[]>.Failure("bad argument #3 to 'unpack' (number expected)");
                n = (int)args[2].AsInteger();
            }

            var result = new List<LuaValue>();
            for (int index = i; index <= n; index++)
            {
                var key = LuaValue.Integer(index);
                var value = table.Get(key);
                result.Add(value);
            }

            return Result<LuaValue[]>.Success(result.ToArray());
        }

        #endregion

        #region Helper Methods

        private static string GetTypeName(LuaValue value)
        {
            if (value.IsNil) return "nil";
            if (value.IsBoolean) return "boolean";
            if (value.IsNumber) return "number";
            if (value.IsString) return "string";
            if (value.IsFunction) return "function";
            if (value.IsTable) return "table";
            if (value.IsUserData) return "userdata";
            if (value.IsThread) return "thread";
            return "unknown";
        }

        #endregion
    }
}