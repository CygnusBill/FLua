using System;
using System.Collections.Generic;

namespace FLua.Runtime
{
    /// <summary>
    /// Provides centralized metamethod handling for Lua values.
    /// This ensures consistent metamethod behavior between interpreter and future compiler.
    /// </summary>
    public static class LuaMetamethods
    {

        /// <summary>
        /// Attempts to invoke a binary metamethod on two values
        /// </summary>
        public static LuaValue? InvokeBinaryMetamethod(LuaValue left, LuaValue right, string metamethod)
        {
            // Special handling for __eq metamethod
            if (metamethod == "__eq")
            {
                // For equality, both values must have the same metatable with __eq defined
                if (left.Type == LuaType.Table && right.Type == LuaType.Table)
                {
                    var leftTable = left.AsTable<LuaTable>();
                    var rightTable = right.AsTable<LuaTable>();
                    if (leftTable.Metatable == rightTable.Metatable && leftTable.Metatable != null)
                    {
                        var eqFunc = leftTable.Metatable.Get(LuaValue.String(metamethod));
                        if (eqFunc.Type == LuaType.Function)
                        {
                            var eqFunction = eqFunc.AsFunction<LuaFunction>();
                            var result = eqFunction.Call(new[] { left, right });
                            return result.Length > 0 ? result[0] : LuaValue.Nil;
                        }
                    }
                }
                return LuaValue.Nil;
            }

            // Try left operand first
            var leftMeta = GetMetamethod(left, metamethod);
            if (leftMeta != null)
            {
                var result = leftMeta.Call(new[] { left, right });
                return result.Length > 0 ? result[0] : LuaValue.Nil;
            }

            // Try right operand if left didn't have the metamethod
            var rightMeta = GetMetamethod(right, metamethod);
            if (rightMeta != null)
            {
                var result = rightMeta.Call(new[] { left, right });
                return result.Length > 0 ? result[0] : LuaValue.Nil;
            }

            return null;
        }

        /// <summary>
        /// Attempts to invoke a unary metamethod on a value
        /// </summary>
        public static LuaValue? InvokeUnaryMetamethod(LuaValue value, string metamethod)
        {
            var meta = GetMetamethod(value, metamethod);
            if (meta != null)
            {
                var result = meta.Call(new[] { value });
                return result.Length > 0 ? result[0] : LuaValue.Nil;
            }

            return null;
        }

        /// <summary>
        /// Gets a metamethod from a value's metatable
        /// </summary>
        public static LuaFunction? GetMetamethod(LuaValue value, string metamethod)
        {
            if (value.IsTable)
            {
                var table = value.AsTable<LuaTable>();
                if (table.Metatable != null)
                {
                    var meta = table.Metatable.RawGet(LuaValue.String(metamethod));
                    if (meta.IsFunction)
                    {
                        return meta.AsFunction<LuaFunction>();
                    }
                }
            }

            // In the future, we might support metatables for other types
            return null;
        }

        /// <summary>
        /// Gets the metatable of a value
        /// </summary>
        public static LuaTable? GetMetatable(LuaValue value)
        {
            if (value.IsTable)
            {
                var table = value.AsTable<LuaTable>();
                return table.Metatable;
            }

            // In the future, we might support metatables for other types
            return null;
        }

        /// <summary>
        /// Sets the metatable of a value
        /// </summary>
        public static bool SetMetatable(LuaValue value, LuaTable? metatable)
        {
            if (value.IsTable)
            {
                var table = value.AsTable<LuaTable>();
                // Check for __metatable field which protects the metatable
                if (table.Metatable != null)
                {
                    var protection = table.Metatable.RawGet(LuaValue.String("__metatable"));
                    if (!protection.IsNil)
                    {
                        throw new LuaRuntimeException("cannot change a protected metatable");
                    }
                }

                table.Metatable = metatable;
                return true;
            }

            // In the future, we might support metatables for other types
            return false;
        }

        /// <summary>
        /// Invokes the __index metamethod for table access
        /// </summary>
        public static LuaValue? InvokeIndex(LuaTable table, LuaValue key)
        {
            if (table.Metatable != null)
            {
                var indexMeta = table.Metatable.RawGet(LuaValue.String("__index"));
                
                // __index can be a function or a table
                if (indexMeta.IsFunction)
                {
                    var indexFunc = indexMeta.AsFunction<LuaFunction>();
                    var result = indexFunc.Call(new[] { LuaValue.Table(table), key });
                    return result.Length > 0 ? result[0] : LuaValue.Nil;
                }
                else if (indexMeta.IsTable)
                {
                    var indexTable = indexMeta.AsTable<LuaTable>();
                    return indexTable.Get(key);
                }
            }

            return null;
        }

        /// <summary>
        /// Invokes the __newindex metamethod for table assignment
        /// </summary>
        public static bool InvokeNewIndex(LuaTable table, LuaValue key, LuaValue value)
        {
            if (table.Metatable != null)
            {
                var newIndexMeta = table.Metatable.RawGet(LuaValue.String("__newindex"));
                
                // __newindex can be a function or a table
                if (newIndexMeta.IsFunction)
                {
                    var newIndexFunc = newIndexMeta.AsFunction<LuaFunction>();
                    newIndexFunc.Call(new[] { LuaValue.Table(table), key, value });
                    return true;
                }
                else if (newIndexMeta.IsTable)
                {
                    var newIndexTable = newIndexMeta.AsTable<LuaTable>();
                    newIndexTable.Set(key, value);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Invokes the __call metamethod for calling a non-function value
        /// </summary>
        public static LuaValue[]? InvokeCall(LuaValue value, LuaValue[] args)
        {
            var callMeta = GetMetamethod(value, "__call");
            if (callMeta != null)
            {
                // Prepend the value itself as the first argument
                var fullArgs = new LuaValue[args.Length + 1];
                fullArgs[0] = value;
                Array.Copy(args, 0, fullArgs, 1, args.Length);
                
                return callMeta.Call(fullArgs);
            }

            return null;
        }

        /// <summary>
        /// Invokes the __tostring metamethod for string conversion
        /// </summary>
        public static string? InvokeToString(LuaValue value)
        {
            var toStringMeta = GetMetamethod(value, "__tostring");
            if (toStringMeta != null)
            {
                var result = toStringMeta.Call(new[] { value });
                if (result.Length > 0)
                {
                    return result[0].ToString();
                }
            }

            return null;
        }

        /// <summary>
        /// Invokes the __pairs metamethod for iteration
        /// </summary>
        public static LuaValue[]? InvokePairs(LuaValue value)
        {
            var pairsMeta = GetMetamethod(value, "__pairs");
            if (pairsMeta != null)
            {
                return pairsMeta.Call(new[] { value });
            }

            return null;
        }

        /// <summary>
        /// Invokes the __ipairs metamethod for array iteration
        /// </summary>
        public static LuaValue[]? InvokeIPairs(LuaValue value)
        {
            var ipairsMeta = GetMetamethod(value, "__ipairs");
            if (ipairsMeta != null)
            {
                return ipairsMeta.Call(new[] { value });
            }

            return null;
        }

        /// <summary>
        /// Checks if a metamethod exists for a value
        /// </summary>
        public static bool HasMetamethod(LuaValue value, string metamethod)
        {
            return GetMetamethod(value, metamethod) != null;
        }

        /// <summary>
        /// Gets the __mode metamethod value for weak tables
        /// </summary>
        public static string? GetWeakMode(LuaTable table)
        {
            if (table.Metatable != null)
            {
                var mode = table.Metatable.RawGet(LuaValue.String("__mode"));
                if (mode.IsString)
                {
                    return mode.AsString();
                }
            }

            return null;
        }

        /// <summary>
        /// Invokes the __gc metamethod for garbage collection
        /// </summary>
        public static void InvokeGC(LuaValue value)
        {
            var gcMeta = GetMetamethod(value, "__gc");
            gcMeta?.Call(new[] { value });
        }

        /// <summary>
        /// Invokes the __close metamethod for to-be-closed variables
        /// </summary>
        public static void InvokeClose(LuaValue value, LuaValue? error = null)
        {
            var closeMeta = GetMetamethod(value, "__close");
            if (closeMeta != null)
            {
                if (error.HasValue)
                    closeMeta.Call(new[] { value, error.Value });
                else
                    closeMeta.Call(new[] { value });
            }
        }
    }
}