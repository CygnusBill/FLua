using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FLua.Runtime
{
    /// <summary>
    /// Lua Table Library implementation
    /// </summary>
    public static class LuaTableLib
    {
        /// <summary>
        /// Adds the table library to the Lua environment
        /// </summary>
        public static void AddTableLibrary(LuaEnvironment env)
        {
            var tableTable = new LuaTable();
            
            // Table manipulation functions
            tableTable.Set(new LuaString("insert"), new BuiltinFunction(Insert));
            tableTable.Set(new LuaString("remove"), new BuiltinFunction(Remove));
            tableTable.Set(new LuaString("move"), new BuiltinFunction(Move));
            
            // Utility functions
            tableTable.Set(new LuaString("concat"), new BuiltinFunction(Concat));
            tableTable.Set(new LuaString("sort"), new BuiltinFunction(Sort));
            
            // Packing functions
            tableTable.Set(new LuaString("pack"), new BuiltinFunction(Pack));
            tableTable.Set(new LuaString("unpack"), new BuiltinFunction(Unpack));
            
            env.SetVariable("table", tableTable);
        }
        
        #region Table Manipulation Functions
        
        private static LuaValue[] Insert(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'insert' (table expected)");
            
            var table = args[0];
            if (!(table is LuaTable luaTable))
                throw new LuaRuntimeException("bad argument #1 to 'insert' (table expected)");
            
            if (args.Length == 2)
            {
                // table.insert(t, value) - insert at end
                var value = args[1];
                var newIndex = luaTable.Array.Count + 1;
                luaTable.Set(new LuaInteger(newIndex), value);
            }
            else if (args.Length >= 3)
            {
                // table.insert(t, pos, value) - insert at position
                var pos = args[1];
                var value = args[2];
                
                if (!pos.AsInteger.HasValue)
                    throw new LuaRuntimeException("bad argument #2 to 'insert' (number expected)");
                
                var position = (int)pos.AsInteger.Value;
                if (position < 1)
                    throw new LuaRuntimeException("bad argument #2 to 'insert' (position out of bounds)");
                
                // Convert to 0-based indexing
                position--;
                
                // Shift elements to the right
                var arrayList = luaTable.Array.ToList();
                if (position <= arrayList.Count)
                {
                    arrayList.Insert(position, value);
                    
                    // Rebuild the array part of the table
                    luaTable.Set(new LuaInteger(1), LuaNil.Instance); // Clear array
                    for (int i = 0; i < arrayList.Count; i++)
                    {
                        luaTable.Set(new LuaInteger(i + 1), arrayList[i]);
                    }
                }
                else
                {
                    // Insert beyond current array bounds
                    luaTable.Set(new LuaInteger(position + 1), value);
                }
            }
            else
            {
                throw new LuaRuntimeException("wrong number of arguments to 'insert'");
            }
            
            return Array.Empty<LuaValue>();
        }
        
        private static LuaValue[] Remove(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'remove' (table expected)");
            
            var table = args[0];
            if (!(table is LuaTable luaTable))
                throw new LuaRuntimeException("bad argument #1 to 'remove' (table expected)");
            
            int position;
            if (args.Length == 1)
            {
                // table.remove(t) - remove last element
                position = luaTable.Array.Count;
            }
            else
            {
                // table.remove(t, pos) - remove at position
                var pos = args[1];
                if (!pos.AsInteger.HasValue)
                    throw new LuaRuntimeException("bad argument #2 to 'remove' (number expected)");
                
                position = (int)pos.AsInteger.Value;
            }
            
            if (position < 1 || position > luaTable.Array.Count)
                return new[] { LuaNil.Instance };
            
            var removedValue = luaTable.Get(new LuaInteger(position));
            
            // Shift elements to the left
            var arrayList = luaTable.Array.ToList();
            if (position - 1 < arrayList.Count)
            {
                arrayList.RemoveAt(position - 1);
                
                // Rebuild the array part of the table
                var lastIndex = luaTable.Array.Count;
                luaTable.Set(new LuaInteger(lastIndex), LuaNil.Instance); // Remove last element
                
                for (int i = 0; i < arrayList.Count; i++)
                {
                    luaTable.Set(new LuaInteger(i + 1), arrayList[i]);
                }
            }
            
            return new[] { removedValue };
        }
        
        private static LuaValue[] Move(LuaValue[] args)
        {
            if (args.Length < 4)
                throw new LuaRuntimeException("bad argument #4 to 'move' (table expected)");
            
            var sourceTable = args[0];
            var start = args[1];
            var end = args[2];
            var destIndex = args[3];
            var destTable = args.Length > 4 ? args[4] : sourceTable;
            
            if (!(sourceTable is LuaTable srcTable))
                throw new LuaRuntimeException("bad argument #1 to 'move' (table expected)");
            if (!(destTable is LuaTable dstTable))
                throw new LuaRuntimeException("bad argument #5 to 'move' (table expected)");
            
            if (!start.AsInteger.HasValue)
                throw new LuaRuntimeException("bad argument #2 to 'move' (number expected)");
            if (!end.AsInteger.HasValue)
                throw new LuaRuntimeException("bad argument #3 to 'move' (number expected)");
            if (!destIndex.AsInteger.HasValue)
                throw new LuaRuntimeException("bad argument #4 to 'move' (number expected)");
            
            var startIdx = start.AsInteger.Value;
            var endIdx = end.AsInteger.Value;
            var destIdx = destIndex.AsInteger.Value;
            
            if (startIdx > endIdx)
                return new[] { destTable };
            
            // Copy elements
            for (long i = startIdx; i <= endIdx; i++)
            {
                var value = srcTable.Get(new LuaInteger(i));
                var targetIdx = destIdx + (i - startIdx);
                dstTable.Set(new LuaInteger(targetIdx), value);
            }
            
            return new[] { destTable };
        }
        
        #endregion
        
        #region Utility Functions
        
        private static LuaValue[] Concat(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'concat' (table expected)");
            
            var table = args[0];
            if (!(table is LuaTable luaTable))
                throw new LuaRuntimeException("bad argument #1 to 'concat' (table expected)");
            
            var separator = args.Length > 1 ? args[1].AsString : "";
            var start = args.Length > 2 ? (int)(args[2].AsInteger ?? 1) : 1;
            var end = args.Length > 3 ? (int)(args[3].AsInteger ?? luaTable.Array.Count) : luaTable.Array.Count;
            
            if (start > end)
                return new[] { new LuaString("") };
            
            var sb = new StringBuilder();
            bool first = true;
            
            for (int i = start; i <= end && i <= luaTable.Array.Count; i++)
            {
                var value = luaTable.Get(new LuaInteger(i));
                if (value is LuaNil)
                    break;
                
                if (!first && !string.IsNullOrEmpty(separator))
                    sb.Append(separator);
                
                sb.Append(value.AsString);
                first = false;
            }
            
            return new[] { new LuaString(sb.ToString()) };
        }
        
        private static LuaValue[] Sort(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'sort' (table expected)");
            
            var table = args[0];
            if (!(table is LuaTable luaTable))
                throw new LuaRuntimeException("bad argument #1 to 'sort' (table expected)");
            
            var compareFunc = args.Length > 1 ? args[1] as LuaFunction : null;
            
            // Get array elements
            var elements = new List<LuaValue>();
            for (int i = 1; i <= luaTable.Array.Count; i++)
            {
                var value = luaTable.Get(new LuaInteger(i));
                if (value is LuaNil)
                    break;
                elements.Add(value);
            }
            
            // Sort elements
            if (compareFunc != null)
            {
                // Custom comparison function
                elements.Sort((a, b) =>
                {
                    try
                    {
                        var result = compareFunc.Call(new[] { a, b });
                        if (result.Length > 0 && LuaValue.IsValueTruthy(result[0]))
                            return -1;
                        return 1;
                    }
                    catch
                    {
                        return 0;
                    }
                });
            }
            else
            {
                // Default comparison (lexicographic)
                elements.Sort((a, b) =>
                {
                    // Compare numbers numerically
                    if (a.AsNumber.HasValue && b.AsNumber.HasValue)
                        return a.AsNumber.Value.CompareTo(b.AsNumber.Value);
                    
                    // Compare strings lexicographically
                    return string.Compare(a.AsString, b.AsString, StringComparison.Ordinal);
                });
            }
            
            // Put sorted elements back into table
            for (int i = 0; i < elements.Count; i++)
            {
                luaTable.Set(new LuaInteger(i + 1), elements[i]);
            }
            
            // Clear any remaining elements
            for (int i = elements.Count + 1; i <= luaTable.Array.Count; i++)
            {
                var value = luaTable.Get(new LuaInteger(i));
                if (value is not LuaNil)
                    luaTable.Set(new LuaInteger(i), LuaNil.Instance);
                else
                    break;
            }
            
            return Array.Empty<LuaValue>();
        }
        
        #endregion
        
        #region Packing Functions
        
        private static LuaValue[] Pack(LuaValue[] args)
        {
            var result = new LuaTable();
            
            // Pack all arguments into array part
            for (int i = 0; i < args.Length; i++)
            {
                result.Set(new LuaInteger(i + 1), args[i]);
            }
            
            // Set 'n' field with count
            result.Set(new LuaString("n"), new LuaInteger(args.Length));
            
            return new[] { result };
        }
        
        private static LuaValue[] Unpack(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'unpack' (table expected)");
            
            var table = args[0];
            if (!(table is LuaTable luaTable))
                throw new LuaRuntimeException("bad argument #1 to 'unpack' (table expected)");
            
            var start = args.Length > 1 ? (int)(args[1].AsInteger ?? 1) : 1;
            
            int end;
            if (args.Length > 2)
            {
                if (!args[2].AsInteger.HasValue)
                    throw new LuaRuntimeException("bad argument #3 to 'unpack' (number expected)");
                end = (int)args[2].AsInteger!.Value;
            }
            else
            {
                // Try to get 'n' field first
                var nValue = luaTable.Get(new LuaString("n"));
                if (nValue.AsInteger.HasValue)
                {
                    end = (int)nValue.AsInteger.Value;
                }
                else
                {
                    // Find the length of the array part
                    end = luaTable.Array.Count;
                }
            }
            
            if (start > end)
                return Array.Empty<LuaValue>();
            
            var results = new List<LuaValue>();
            for (int i = start; i <= end; i++)
            {
                var value = luaTable.Get(new LuaInteger(i));
                results.Add(value);
            }
            
            return results.ToArray();
        }
        
        #endregion
    }
} 