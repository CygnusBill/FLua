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
            tableTable.Set(LuaValue.String("insert"), LuaValue.Function(new BuiltinFunction(Insert)));
            tableTable.Set(LuaValue.String("remove"), LuaValue.Function(new BuiltinFunction(Remove)));
            tableTable.Set(LuaValue.String("move"), LuaValue.Function(new BuiltinFunction(Move)));
            
            // Utility functions
            tableTable.Set(LuaValue.String("concat"), LuaValue.Function(new BuiltinFunction(Concat)));
            tableTable.Set(LuaValue.String("sort"), LuaValue.Function(new BuiltinFunction(Sort)));
            
            // Packing functions
            tableTable.Set(LuaValue.String("pack"), LuaValue.Function(new BuiltinFunction(Pack)));
            tableTable.Set(LuaValue.String("unpack"), LuaValue.Function(new BuiltinFunction(Unpack)));
            
            env.SetVariable("table", LuaValue.Table(tableTable));
        }
        
        #region Table Manipulation Functions
        
        private static LuaValue[] Insert(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'insert' (table expected)");
            
            if (!args[0].IsTable)
                throw new LuaRuntimeException("bad argument #1 to 'insert' (table expected)");
                
            var luaTable = args[0].AsTable<LuaTable>();
            
            if (args.Length == 2)
            {
                // table.insert(t, value) - insert at end
                var value = args[1];
                var newIndex = luaTable.Array.Count + 1;
                luaTable.Set(LuaValue.Integer(newIndex), value);
            }
            else if (args.Length >= 3)
            {
                // table.insert(t, pos, value) - insert at position
                var pos = args[1];
                var value = args[2];
                
                if (!pos.IsInteger)
                    throw new LuaRuntimeException("bad argument #2 to 'insert' (number expected)");
                
                var position = (int)pos.AsInteger();
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
                    luaTable.Set(LuaValue.Integer(1), LuaValue.Nil); // Clear array
                    for (int i = 0; i < arrayList.Count; i++)
                    {
                        luaTable.Set(LuaValue.Integer(i + 1), arrayList[i]);
                    }
                }
                else
                {
                    // Insert beyond current array bounds
                    luaTable.Set(LuaValue.Integer(position + 1), value);
                }
            }
            else
            {
                throw new LuaRuntimeException("wrong number of arguments to 'insert'");
            }
            
            return [];
        }
        
        private static LuaValue[] Remove(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'remove' (table expected)");
            
            var table = args[0];
            if (!table.IsTable)
                throw new LuaRuntimeException("bad argument #1 to 'remove' (table expected)");
                
            var luaTable = table.AsTable<LuaTable>();
            
            // Find the length of the array part (similar to # operator)
            int arrayLength = luaTable.Length();
            
            int position;
            if (args.Length == 1)
            {
                // table.remove(t) - remove last element
                position = arrayLength;
            }
            else
            {
                // table.remove(t, pos) - remove at position
                var pos = args[1];
                if (!pos.IsInteger)
                    throw new LuaRuntimeException("bad argument #2 to 'remove' (number expected)");
                
                position = (int)pos.AsInteger();
            }
            
            if (position < 1 || position > arrayLength)
                return [LuaValue.Nil];
            
            var removedValue = luaTable.Get(LuaValue.Integer(position));
            
            // Shift elements to the left
            for (int i = position; i < arrayLength; i++)
            {
                var nextValue = luaTable.Get(LuaValue.Integer(i + 1));
                luaTable.Set(LuaValue.Integer(i), nextValue);
            }
            
            // Remove the last element by setting it to nil
            luaTable.Set(LuaValue.Integer(arrayLength), LuaValue.Nil);
            
            return [removedValue];
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
            
            if (!sourceTable.IsTable)
                throw new LuaRuntimeException("bad argument #1 to 'move' (table expected)");
            if (!destTable.IsTable)
                throw new LuaRuntimeException("bad argument #5 to 'move' (table expected)");
                
            var srcTable = sourceTable.AsTable<LuaTable>();
            var dstTable = destTable.AsTable<LuaTable>();
            
            if (!start.IsInteger)
                throw new LuaRuntimeException("bad argument #2 to 'move' (number expected)");
            if (!end.IsInteger)
                throw new LuaRuntimeException("bad argument #3 to 'move' (number expected)");
            if (!destIndex.IsInteger)
                throw new LuaRuntimeException("bad argument #4 to 'move' (number expected)");
            
            var startIdx = start.AsInteger();
            var endIdx = end.AsInteger();
            var destIdx = destIndex.AsInteger();
            
            if (startIdx > endIdx)
                return [destTable];
            
            // Copy elements
            for (long i = startIdx; i <= endIdx; i++)
            {
                var value = srcTable.Get(LuaValue.Integer(i));
                var targetIdx = destIdx + (i - startIdx);
                dstTable.Set(LuaValue.Integer(targetIdx), value);
            }
            
            return [destTable];
        }
        
        #endregion
        
        #region Utility Functions
        
        private static LuaValue[] Concat(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'concat' (table expected)");
            
            var table = args[0];
            if (!table.IsTable)
                throw new LuaRuntimeException("bad argument #1 to 'concat' (table expected)");
                
            var luaTable = table.AsTable<LuaTable>();
            
            var separator = args.Length > 1 && args[1].IsString ? args[1].AsString() : "";
            var start = args.Length > 2 && args[2].IsInteger ? (int)args[2].AsInteger() : 1;
            var end = args.Length > 3 && args[3].IsInteger ? (int)args[3].AsInteger() : luaTable.Array.Count;
            
            if (start > end)
                return [LuaValue.String("")];
            
            var sb = new StringBuilder();
            bool first = true;
            
            for (int i = start; i <= end && i <= luaTable.Array.Count; i++)
            {
                var value = luaTable.Get(LuaValue.Integer(i));
                if (value.IsNil)
                    break;
                
                if (!first && !string.IsNullOrEmpty(separator))
                    sb.Append(separator);
                
                sb.Append(value.ToString());
                first = false;
            }
            
            return [LuaValue.String(sb.ToString())];
        }
        
        private static LuaValue[] Sort(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'sort' (table expected)");
            
            if (!args[0].IsTable)
                throw new LuaRuntimeException("bad argument #1 to 'sort' (table expected)");
            
            var luaTable = args[0].AsTable<LuaTable>();
            
            LuaFunction? compareFunc = null;
            if (args.Length > 1 && args[1].IsFunction)
            {
                compareFunc = args[1].AsFunction<LuaFunction>();
            }
            
            // Get array elements
            var elements = new List<LuaValue>();
            for (int i = 1; i <= luaTable.Array.Count; i++)
            {
                var value = luaTable.Get(LuaValue.Integer(i));
                if (value.IsNil)
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
                        if (result.Length > 0 && result[0].IsTruthy())
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
                    if (a.IsNumber && b.IsNumber)
                        return a.AsDouble().CompareTo(b.AsDouble());
                    
                    // Compare strings lexicographically
                    return string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal);
                });
            }
            
            // Put sorted elements back into table
            for (int i = 0; i < elements.Count; i++)
            {
                luaTable.Set(LuaValue.Integer(i + 1), elements[i]);
            }
            
            // Clear any remaining elements
            for (int i = elements.Count + 1; i <= luaTable.Array.Count; i++)
            {
                var value = luaTable.Get(LuaValue.Integer(i));
                if (!value.IsNil)
                    luaTable.Set(LuaValue.Integer(i), LuaValue.Nil);
                else
                    break;
            }
            
            return [];
        }
        
        #endregion
        
        #region Packing Functions
        
        private static LuaValue[] Pack(LuaValue[] args)
        {
            var result = new LuaTable();
            
            // Pack all arguments into array part
            for (int i = 0; i < args.Length; i++)
            {
                result.Set(LuaValue.Integer(i + 1), args[i]);
            }
            
            // Set 'n' field with count
            result.Set(LuaValue.String("n"), LuaValue.Integer(args.Length));
            
            return [LuaValue.Table(result)];
        }
        
        private static LuaValue[] Unpack(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'unpack' (table expected)");
            
            if (!args[0].IsTable)
                throw new LuaRuntimeException("bad argument #1 to 'unpack' (table expected)");
            
            var luaTable = args[0].AsTable<LuaTable>();
            
            var start = args.Length > 1 && args[1].IsInteger ? (int)args[1].AsInteger() : 1;
            
            int end;
            if (args.Length > 2)
            {
                if (!args[2].IsInteger)
                    throw new LuaRuntimeException("bad argument #3 to 'unpack' (number expected)");
                end = (int)args[2].AsInteger();
            }
            else
            {
                // Try to get 'n' field first
                var nValue = luaTable.Get(LuaValue.String("n"));
                if (nValue.IsInteger)
                {
                    end = (int)nValue.AsInteger();
                }
                else
                {
                    // Find the length of the array part
                    end = luaTable.Array.Count;
                }
            }
            
            if (start > end)
                return [];
            
            var results = new List<LuaValue>();
            for (int i = start; i <= end; i++)
            {
                var value = luaTable.Get(LuaValue.Integer(i));
                results.Add(value);
            }
            
            return results.ToArray();
        }
        
        #endregion
    }
} 