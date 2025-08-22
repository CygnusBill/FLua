using System;
using System.Collections.Generic;
using System.Linq;
using FLua.Common;

namespace FLua.Runtime
{
    /// <summary>
    /// Result-based version of Lua Table Library implementation
    /// Converts all exception-based error handling to Result pattern
    /// </summary>
    public static class ResultLuaTableLib
    {
        #region Table Library Functions
        
        public static Result<LuaValue[]> InsertResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'insert' (table expected)");

            if (!args[0].IsTable)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'insert' (table expected)");

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
                    return Result<LuaValue[]>.Failure("bad argument #2 to 'insert' (number expected)");

                var position = (int)pos.AsInteger();
                if (position < 1)
                    return Result<LuaValue[]>.Failure("bad argument #2 to 'insert' (position out of bounds)");

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
                return Result<LuaValue[]>.Failure("wrong number of arguments to 'insert'");
            }

            return Result<LuaValue[]>.Success([]);
        }

        public static Result<LuaValue[]> RemoveResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'remove' (table expected)");

            if (!args[0].IsTable)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'remove' (table expected)");

            var luaTable = args[0].AsTable<LuaTable>();

            if (args.Length == 1)
            {
                // table.remove(t) - remove from end
                var arrayList = luaTable.Array.ToList();
                if (arrayList.Count == 0)
                {
                    return Result<LuaValue[]>.Success([LuaValue.Nil]);
                }

                var lastElement = arrayList[arrayList.Count - 1];
                arrayList.RemoveAt(arrayList.Count - 1);

                // Rebuild the array part
                luaTable.Set(LuaValue.Integer(1), LuaValue.Nil); // Clear array
                for (int i = 0; i < arrayList.Count; i++)
                {
                    luaTable.Set(LuaValue.Integer(i + 1), arrayList[i]);
                }

                return Result<LuaValue[]>.Success([lastElement]);
            }
            else if (args.Length >= 2)
            {
                // table.remove(t, pos) - remove from position
                var pos = args[1];

                if (!pos.IsInteger)
                    return Result<LuaValue[]>.Failure("bad argument #2 to 'remove' (number expected)");

                var position = (int)pos.AsInteger();
                var arrayList = luaTable.Array.ToList();

                // Convert to 0-based indexing
                position--;

                if (position < 0 || position >= arrayList.Count)
                {
                    return Result<LuaValue[]>.Success([LuaValue.Nil]);
                }

                var removedElement = arrayList[position];
                arrayList.RemoveAt(position);

                // Rebuild the array part
                luaTable.Set(LuaValue.Integer(1), LuaValue.Nil); // Clear array
                for (int i = 0; i < arrayList.Count; i++)
                {
                    luaTable.Set(LuaValue.Integer(i + 1), arrayList[i]);
                }

                return Result<LuaValue[]>.Success([removedElement]);
            }

            return Result<LuaValue[]>.Success([LuaValue.Nil]);
        }

        public static Result<LuaValue[]> MoveResult(LuaValue[] args)
        {
            if (args.Length < 4)
                return Result<LuaValue[]>.Failure("bad argument #4 to 'move' (table expected)");

            var a1 = args[0];
            var f = args[1];
            var e = args[2];
            var t = args[3];
            var a2 = args.Length > 4 ? args[4] : a1;

            if (!a1.IsTable)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'move' (table expected)");

            if (!a2.IsTable)
                return Result<LuaValue[]>.Failure("bad argument #5 to 'move' (table expected)");

            if (!f.IsInteger)
                return Result<LuaValue[]>.Failure("bad argument #2 to 'move' (number expected)");
            if (!e.IsInteger)
                return Result<LuaValue[]>.Failure("bad argument #3 to 'move' (number expected)");
            if (!t.IsInteger)
                return Result<LuaValue[]>.Failure("bad argument #4 to 'move' (number expected)");

            var sourceTable = a1.AsTable<LuaTable>();
            var destTable = a2.AsTable<LuaTable>();
            var from = (int)f.AsInteger();
            var to = (int)e.AsInteger();
            var dest = (int)t.AsInteger();

            // Perform the move operation
            for (int i = from; i <= to; i++)
            {
                var sourceKey = LuaValue.Integer(i);
                var destKey = LuaValue.Integer(dest + (i - from));
                var value = sourceTable.Get(sourceKey);
                destTable.Set(destKey, value);
            }

            return Result<LuaValue[]>.Success([a2]);
        }

        public static Result<LuaValue[]> ConcatResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'concat' (table expected)");

            if (!args[0].IsTable)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'concat' (table expected)");

            var luaTable = args[0].AsTable<LuaTable>();

            var separator = args.Length > 1 ? args[1].AsString() : "";
            var i = args.Length > 2 && args[2].IsInteger ? (int)args[2].AsInteger() : 1;
            var j = args.Length > 3 && args[3].IsInteger ? (int)args[3].AsInteger() : luaTable.Length();

            var result = new List<string>();
            for (int index = i; index <= j; index++)
            {
                var key = LuaValue.Integer(index);
                var value = luaTable.Get(key);
                if (!value.IsNil)
                {
                    result.Add(value.AsString());
                }
            }

            return Result<LuaValue[]>.Success([LuaValue.String(string.Join(separator, result))]);
        }

        public static Result<LuaValue[]> SortResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'sort' (table expected)");

            if (!args[0].IsTable)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'sort' (table expected)");

            var luaTable = args[0].AsTable<LuaTable>();

            var comp = args.Length > 1 ? args[1] : LuaValue.Nil;

            // Convert array part to list for sorting
            var arrayList = luaTable.Array.ToList();

            // Sort elements
            if (comp.IsNil)
            {
                // Default comparison
                arrayList.Sort((a, b) =>
                {
                    if (a.IsNumber && b.IsNumber)
                        return a.AsDouble().CompareTo(b.AsDouble());
                    if (a.IsString && b.IsString)
                        return string.Compare(a.AsString(), b.AsString());
                    return 0;
                });
            }
            else if (comp.IsFunction)
            {
                // Custom comparison function
                var compFunc = comp.AsFunction();
                arrayList.Sort((a, b) =>
                {
                    try
                    {
                        var result = compFunc.Call([a, b]);
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

            // Put sorted elements back
            luaTable.Set(LuaValue.Integer(1), LuaValue.Nil); // Clear array
            for (int i = 0; i < arrayList.Count; i++)
            {
                luaTable.Set(LuaValue.Integer(i + 1), arrayList[i]);
            }

            return Result<LuaValue[]>.Success([]);
        }

        public static Result<LuaValue[]> PackResult(LuaValue[] args)
        {
            var result = new LuaTable();

            // Pack all arguments
            for (int i = 0; i < args.Length; i++)
            {
                result.Set(LuaValue.Integer(i + 1), args[i]);
            }

            // Add count
            result.Set(LuaValue.String("n"), LuaValue.Integer(args.Length));

            return Result<LuaValue[]>.Success([LuaValue.Table(result)]);
        }

        public static Result<LuaValue[]> UnpackResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'unpack' (table expected)");

            if (!args[0].IsTable)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'unpack' (table expected)");

            var luaTable = args[0].AsTable<LuaTable>();

            var i = args.Length > 1 && args[1].IsInteger ? (int)args[1].AsInteger() : 1;
            var n = luaTable.Length();

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
                var value = luaTable.Get(key);
                result.Add(value);
            }

            return Result<LuaValue[]>.Success(result.ToArray());
        }

        #endregion
    }
}