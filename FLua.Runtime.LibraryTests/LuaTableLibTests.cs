using FLua.Runtime;
using FLua.Common;

namespace FLua.Runtime.LibraryTests;

/// <summary>
/// Comprehensive unit tests for LuaTableLib using Lee Copeland's testing methodology.
/// Includes boundary value analysis, equivalence class partitioning, and error path testing.
/// </summary>
[TestClass]
public class LuaTableLibTests
{
    private LuaEnvironment _env = null!;

    [TestInitialize]
    public void Setup()
    {
        _env = new LuaEnvironment();
        LuaTableLib.AddTableLibrary(_env);
    }

    #region Insert Function Tests - Boundary Value Analysis

    [TestMethod]
    public void Insert_AtEnd_AddsElement()
    {
        var table = CreateTestTable(1, 2, 3);
        CallTableFunction("insert", LuaValue.Table(table), LuaValue.Integer(4));
        
        Assert.AreEqual(4L, table.Get(LuaValue.Integer(4)).AsInteger());
        Assert.AreEqual(4, table.Array.Count);
    }

    [TestMethod]
    public void Insert_AtPosition_ShiftsElements()
    {
        var table = CreateTestTable(1, 2, 3);
        CallTableFunction("insert", LuaValue.Table(table), LuaValue.Integer(2), LuaValue.Integer(99));
        
        Assert.AreEqual(1L, table.Get(LuaValue.Integer(1)).AsInteger());
        Assert.AreEqual(99L, table.Get(LuaValue.Integer(2)).AsInteger());
        Assert.AreEqual(2L, table.Get(LuaValue.Integer(3)).AsInteger());
        Assert.AreEqual(3L, table.Get(LuaValue.Integer(4)).AsInteger());
        Assert.AreEqual(4, table.Array.Count);
    }

    [TestMethod]
    public void Insert_AtBeginning_ShiftsAllElements()
    {
        var table = CreateTestTable(1, 2, 3);
        CallTableFunction("insert", LuaValue.Table(table), LuaValue.Integer(1), LuaValue.Integer(0));
        
        Assert.AreEqual(0L, table.Get(LuaValue.Integer(1)).AsInteger());
        Assert.AreEqual(1L, table.Get(LuaValue.Integer(2)).AsInteger());
        Assert.AreEqual(2L, table.Get(LuaValue.Integer(3)).AsInteger());
        Assert.AreEqual(3L, table.Get(LuaValue.Integer(4)).AsInteger());
    }

    [TestMethod]
    public void Insert_IntoEmptyTable_AddsFirstElement()
    {
        var table = new LuaTable();
        CallTableFunction("insert", LuaValue.Table(table), LuaValue.Integer(42));
        
        Assert.AreEqual(42L, table.Get(LuaValue.Integer(1)).AsInteger());
        Assert.AreEqual(1, table.Array.Count);
    }

    [TestMethod]
    public void Insert_BeyondArrayBounds_CreatesGap()
    {
        var table = CreateTestTable(1, 2);
        CallTableFunction("insert", LuaValue.Table(table), LuaValue.Integer(5), LuaValue.Integer(99));
        
        Assert.AreEqual(1L, table.Get(LuaValue.Integer(1)).AsInteger());
        Assert.AreEqual(2L, table.Get(LuaValue.Integer(2)).AsInteger());
        Assert.IsTrue(table.Get(LuaValue.Integer(3)).IsNil);
        Assert.IsTrue(table.Get(LuaValue.Integer(4)).IsNil);
        Assert.AreEqual(99L, table.Get(LuaValue.Integer(5)).AsInteger());
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Insert_NoTable_ThrowsException()
    {
        CallTableFunction("insert");
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Insert_NonTable_ThrowsException()
    {
        CallTableFunction("insert", LuaValue.String("not a table"), LuaValue.Integer(42));
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Insert_NonIntegerPosition_ThrowsException()
    {
        var table = CreateTestTable(1, 2, 3);
        CallTableFunction("insert", LuaValue.Table(table), LuaValue.String("not a number"), LuaValue.Integer(42));
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Insert_NegativePosition_ThrowsException()
    {
        var table = CreateTestTable(1, 2, 3);
        CallTableFunction("insert", LuaValue.Table(table), LuaValue.Integer(-1), LuaValue.Integer(42));
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Insert_ZeroPosition_ThrowsException()
    {
        var table = CreateTestTable(1, 2, 3);
        CallTableFunction("insert", LuaValue.Table(table), LuaValue.Integer(0), LuaValue.Integer(42));
    }

    #endregion

    #region Remove Function Tests - Boundary Value Analysis

    [TestMethod]
    public void Remove_LastElement_ReturnsAndRemoves()
    {
        var table = CreateTestTable(1, 2, 3);
        var result = CallTableFunction("remove", LuaValue.Table(table));
        
        Assert.AreEqual(3L, result.AsInteger());
        Assert.AreEqual(2, table.Array.Count);
        Assert.IsTrue(table.Get(LuaValue.Integer(3)).IsNil);
    }

    [TestMethod]
    public void Remove_AtPosition_ShiftsElements()
    {
        var table = CreateTestTable(1, 2, 3, 4);
        var result = CallTableFunction("remove", LuaValue.Table(table), LuaValue.Integer(2));
        
        Assert.AreEqual(2L, result.AsInteger());
        Assert.AreEqual(1L, table.Get(LuaValue.Integer(1)).AsInteger());
        Assert.AreEqual(3L, table.Get(LuaValue.Integer(2)).AsInteger());
        Assert.AreEqual(4L, table.Get(LuaValue.Integer(3)).AsInteger());
        Assert.AreEqual(3, table.Array.Count);
    }

    [TestMethod]
    public void Remove_FirstElement_ShiftsAllElements()
    {
        var table = CreateTestTable(1, 2, 3);
        var result = CallTableFunction("remove", LuaValue.Table(table), LuaValue.Integer(1));
        
        Assert.AreEqual(1L, result.AsInteger());
        Assert.AreEqual(2L, table.Get(LuaValue.Integer(1)).AsInteger());
        Assert.AreEqual(3L, table.Get(LuaValue.Integer(2)).AsInteger());
        Assert.AreEqual(2, table.Array.Count);
    }

    [TestMethod]
    public void Remove_FromEmptyTable_ReturnsNil()
    {
        var table = new LuaTable();
        var result = CallTableFunction("remove", LuaValue.Table(table));
        
        Assert.IsTrue(result.IsNil);
    }

    [TestMethod]
    public void Remove_FromSingleElementTable_ReturnsElement()
    {
        var table = CreateTestTable(42);
        var result = CallTableFunction("remove", LuaValue.Table(table));
        
        Assert.AreEqual(42L, result.AsInteger());
        Assert.AreEqual(0, table.Array.Count);
    }

    [TestMethod]
    public void Remove_OutOfBounds_ReturnsNil()
    {
        var table = CreateTestTable(1, 2, 3);
        var result = CallTableFunction("remove", LuaValue.Table(table), LuaValue.Integer(10));
        
        Assert.IsTrue(result.IsNil);
        Assert.AreEqual(3, table.Array.Count); // No change
    }

    [TestMethod]
    public void Remove_NegativeIndex_ReturnsNil()
    {
        var table = CreateTestTable(1, 2, 3);
        var result = CallTableFunction("remove", LuaValue.Table(table), LuaValue.Integer(-1));
        
        Assert.IsTrue(result.IsNil);
        Assert.AreEqual(3, table.Array.Count); // No change
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Remove_NoTable_ThrowsException()
    {
        CallTableFunction("remove");
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Remove_NonTable_ThrowsException()
    {
        CallTableFunction("remove", LuaValue.String("not a table"));
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Remove_NonIntegerPosition_ThrowsException()
    {
        var table = CreateTestTable(1, 2, 3);
        CallTableFunction("remove", LuaValue.Table(table), LuaValue.String("not a number"));
    }

    #endregion

    #region Move Function Tests - Complex Scenarios

    [TestMethod]
    public void Move_SameTable_CopiesElements()
    {
        var table = CreateTestTable(1, 2, 3, 4, 5);
        var result = CallTableFunction("move", 
            LuaValue.Table(table), 
            LuaValue.Integer(2), 
            LuaValue.Integer(4), 
            LuaValue.Integer(6));
        
        Assert.IsTrue(result.IsTable);
        Assert.AreEqual(1L, table.Get(LuaValue.Integer(1)).AsInteger());
        Assert.AreEqual(2L, table.Get(LuaValue.Integer(2)).AsInteger());
        Assert.AreEqual(3L, table.Get(LuaValue.Integer(3)).AsInteger());
        Assert.AreEqual(4L, table.Get(LuaValue.Integer(4)).AsInteger());
        Assert.AreEqual(5L, table.Get(LuaValue.Integer(5)).AsInteger());
        Assert.AreEqual(2L, table.Get(LuaValue.Integer(6)).AsInteger());
        Assert.AreEqual(3L, table.Get(LuaValue.Integer(7)).AsInteger());
        Assert.AreEqual(4L, table.Get(LuaValue.Integer(8)).AsInteger());
    }

    [TestMethod]
    public void Move_DifferentTables_CopiesElements()
    {
        var source = CreateTestTable(1, 2, 3);
        var dest = new LuaTable();
        
        var result = CallTableFunction("move", 
            LuaValue.Table(source), 
            LuaValue.Integer(1), 
            LuaValue.Integer(3), 
            LuaValue.Integer(1),
            LuaValue.Table(dest));
        
        Assert.IsTrue(result.IsTable);
        Assert.AreEqual(1L, dest.Get(LuaValue.Integer(1)).AsInteger());
        Assert.AreEqual(2L, dest.Get(LuaValue.Integer(2)).AsInteger());
        Assert.AreEqual(3L, dest.Get(LuaValue.Integer(3)).AsInteger());
    }

    [TestMethod]
    public void Move_SingleElement_WorksCorrectly()
    {
        var table = CreateTestTable(1, 2, 3);
        CallTableFunction("move", 
            LuaValue.Table(table), 
            LuaValue.Integer(2), 
            LuaValue.Integer(2), 
            LuaValue.Integer(5));
        
        Assert.AreEqual(2L, table.Get(LuaValue.Integer(5)).AsInteger());
    }

    [TestMethod]
    public void Move_StartGreaterThanEnd_DoesNothing()
    {
        var table = CreateTestTable(1, 2, 3);
        var originalCount = table.Array.Count;
        
        CallTableFunction("move", 
            LuaValue.Table(table), 
            LuaValue.Integer(3), 
            LuaValue.Integer(1), 
            LuaValue.Integer(5));
        
        Assert.AreEqual(originalCount, table.Array.Count);
        Assert.IsTrue(table.Get(LuaValue.Integer(5)).IsNil);
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Move_MissingArguments_ThrowsException()
    {
        var table = CreateTestTable(1, 2, 3);
        CallTableFunction("move", LuaValue.Table(table), LuaValue.Integer(1), LuaValue.Integer(2));
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Move_NonTableSource_ThrowsException()
    {
        CallTableFunction("move", 
            LuaValue.String("not a table"), 
            LuaValue.Integer(1), 
            LuaValue.Integer(2), 
            LuaValue.Integer(3));
    }

    #endregion

    #region Concat Function Tests - String Building

    [TestMethod]
    public void Concat_SimpleArray_JoinsElements()
    {
        var table = CreateTestTable("a", "b", "c");
        var result = CallTableFunction("concat", LuaValue.Table(table));
        
        Assert.AreEqual("abc", result.AsString());
    }

    [TestMethod]
    public void Concat_WithSeparator_JoinsWithSeparator()
    {
        var table = CreateTestTable("a", "b", "c");
        var result = CallTableFunction("concat", LuaValue.Table(table), LuaValue.String("-"));
        
        Assert.AreEqual("a-b-c", result.AsString());
    }

    [TestMethod]
    public void Concat_WithRange_JoinsSubset()
    {
        var table = CreateTestTable("a", "b", "c", "d", "e");
        var result = CallTableFunction("concat", 
            LuaValue.Table(table), 
            LuaValue.String(","), 
            LuaValue.Integer(2), 
            LuaValue.Integer(4));
        
        Assert.AreEqual("b,c,d", result.AsString());
    }

    [TestMethod]
    public void Concat_EmptyTable_ReturnsEmptyString()
    {
        var table = new LuaTable();
        var result = CallTableFunction("concat", LuaValue.Table(table));
        
        Assert.AreEqual("", result.AsString());
    }

    [TestMethod]
    public void Concat_SingleElement_ReturnsElement()
    {
        var table = CreateTestTable("hello");
        var result = CallTableFunction("concat", LuaValue.Table(table));
        
        Assert.AreEqual("hello", result.AsString());
    }

    [TestMethod]
    public void Concat_StartGreaterThanEnd_ReturnsEmpty()
    {
        var table = CreateTestTable("a", "b", "c");
        var result = CallTableFunction("concat", 
            LuaValue.Table(table), 
            LuaValue.String(","), 
            LuaValue.Integer(3), 
            LuaValue.Integer(1));
        
        Assert.AreEqual("", result.AsString());
    }

    [TestMethod]
    public void Concat_NumberElements_ConvertsToString()
    {
        var table = CreateTestTable(1, 2, 3);
        var result = CallTableFunction("concat", LuaValue.Table(table), LuaValue.String(","));
        
        Assert.AreEqual("1,2,3", result.AsString());
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Concat_NoTable_ThrowsException()
    {
        CallTableFunction("concat");
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Concat_NonTable_ThrowsException()
    {
        CallTableFunction("concat", LuaValue.String("not a table"));
    }

    #endregion

    #region Sort Function Tests - Equivalence Classes

    [TestMethod]
    public void Sort_Numbers_SortsNumerically()
    {
        var table = CreateTestTable(3, 1, 4, 1, 5, 9, 2, 6);
        CallTableFunction("sort", LuaValue.Table(table));
        
        Assert.AreEqual(1L, table.Get(LuaValue.Integer(1)).AsInteger());
        Assert.AreEqual(1L, table.Get(LuaValue.Integer(2)).AsInteger());
        Assert.AreEqual(2L, table.Get(LuaValue.Integer(3)).AsInteger());
        Assert.AreEqual(3L, table.Get(LuaValue.Integer(4)).AsInteger());
        Assert.AreEqual(4L, table.Get(LuaValue.Integer(5)).AsInteger());
        Assert.AreEqual(5L, table.Get(LuaValue.Integer(6)).AsInteger());
        Assert.AreEqual(6L, table.Get(LuaValue.Integer(7)).AsInteger());
        Assert.AreEqual(9L, table.Get(LuaValue.Integer(8)).AsInteger());
    }

    [TestMethod]
    public void Sort_Strings_SortsLexicographically()
    {
        var table = CreateTestTable("zebra", "apple", "banana", "cherry");
        CallTableFunction("sort", LuaValue.Table(table));
        
        Assert.AreEqual("apple", table.Get(LuaValue.Integer(1)).AsString());
        Assert.AreEqual("banana", table.Get(LuaValue.Integer(2)).AsString());
        Assert.AreEqual("cherry", table.Get(LuaValue.Integer(3)).AsString());
        Assert.AreEqual("zebra", table.Get(LuaValue.Integer(4)).AsString());
    }

    [TestMethod]
    public void Sort_EmptyTable_DoesNothing()
    {
        var table = new LuaTable();
        CallTableFunction("sort", LuaValue.Table(table));
        
        Assert.AreEqual(0, table.Array.Count);
    }

    [TestMethod]
    public void Sort_SingleElement_DoesNothing()
    {
        var table = CreateTestTable(42);
        CallTableFunction("sort", LuaValue.Table(table));
        
        Assert.AreEqual(42L, table.Get(LuaValue.Integer(1)).AsInteger());
        Assert.AreEqual(1, table.Array.Count);
    }

    [TestMethod]
    public void Sort_AlreadySorted_RemainsUnchanged()
    {
        var table = CreateTestTable(1, 2, 3, 4, 5);
        CallTableFunction("sort", LuaValue.Table(table));
        
        for (int i = 1; i <= 5; i++)
        {
            Assert.AreEqual((long)i, table.Get(LuaValue.Integer(i)).AsInteger());
        }
    }

    [TestMethod]
    public void Sort_ReverseSorted_GetsReversed()
    {
        var table = CreateTestTable(5, 4, 3, 2, 1);
        CallTableFunction("sort", LuaValue.Table(table));
        
        for (int i = 1; i <= 5; i++)
        {
            Assert.AreEqual((long)i, table.Get(LuaValue.Integer(i)).AsInteger());
        }
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Sort_NoTable_ThrowsException()
    {
        CallTableFunction("sort");
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Sort_NonTable_ThrowsException()
    {
        CallTableFunction("sort", LuaValue.String("not a table"));
    }

    #endregion

    #region Pack Function Tests - Varargs Handling

    [TestMethod]
    public void Pack_NoArguments_ReturnsEmptyTableWithCount()
    {
        var result = CallTableFunction("pack");
        
        Assert.IsTrue(result.IsTable);
        var table = result.AsTable<LuaTable>();
        Assert.AreEqual(0L, table.Get(LuaValue.String("n")).AsInteger());
        Assert.AreEqual(0, table.Array.Count);
    }

    [TestMethod]
    public void Pack_SingleArgument_PacksCorrectly()
    {
        var result = CallTableFunction("pack", LuaValue.Integer(42));
        
        Assert.IsTrue(result.IsTable);
        var table = result.AsTable<LuaTable>();
        Assert.AreEqual(1L, table.Get(LuaValue.String("n")).AsInteger());
        Assert.AreEqual(42L, table.Get(LuaValue.Integer(1)).AsInteger());
    }

    [TestMethod]
    public void Pack_MultipleArguments_PacksAll()
    {
        var result = CallTableFunction("pack", 
            LuaValue.Integer(1), 
            LuaValue.String("hello"), 
            LuaValue.Boolean(true), 
            LuaValue.Nil);
        
        Assert.IsTrue(result.IsTable);
        var table = result.AsTable<LuaTable>();
        Assert.AreEqual(4L, table.Get(LuaValue.String("n")).AsInteger());
        Assert.AreEqual(1L, table.Get(LuaValue.Integer(1)).AsInteger());
        Assert.AreEqual("hello", table.Get(LuaValue.Integer(2)).AsString());
        Assert.IsTrue(table.Get(LuaValue.Integer(3)).AsBoolean());
        Assert.IsTrue(table.Get(LuaValue.Integer(4)).IsNil);
    }

    [TestMethod]
    public void Pack_PreservesNilValues_IncludesInCount()
    {
        var result = CallTableFunction("pack", 
            LuaValue.Integer(1), 
            LuaValue.Nil, 
            LuaValue.Integer(3));
        
        Assert.IsTrue(result.IsTable);
        var table = result.AsTable<LuaTable>();
        Assert.AreEqual(3L, table.Get(LuaValue.String("n")).AsInteger());
        Assert.AreEqual(1L, table.Get(LuaValue.Integer(1)).AsInteger());
        Assert.IsTrue(table.Get(LuaValue.Integer(2)).IsNil);
        Assert.AreEqual(3L, table.Get(LuaValue.Integer(3)).AsInteger());
    }

    #endregion

    #region Unpack Function Tests - Array Expansion

    [TestMethod]
    public void Unpack_SimpleArray_ReturnsElements()
    {
        var table = CreateTestTable(1, 2, 3);
        var results = CallTableFunctionMultiple("unpack", LuaValue.Table(table));
        
        Assert.AreEqual(3, results.Length);
        Assert.AreEqual(1L, results[0].AsInteger());
        Assert.AreEqual(2L, results[1].AsInteger());
        Assert.AreEqual(3L, results[2].AsInteger());
    }

    [TestMethod]
    public void Unpack_WithRange_ReturnsSubset()
    {
        var table = CreateTestTable(1, 2, 3, 4, 5);
        var results = CallTableFunctionMultiple("unpack", 
            LuaValue.Table(table), 
            LuaValue.Integer(2), 
            LuaValue.Integer(4));
        
        Assert.AreEqual(3, results.Length);
        Assert.AreEqual(2L, results[0].AsInteger());
        Assert.AreEqual(3L, results[1].AsInteger());
        Assert.AreEqual(4L, results[2].AsInteger());
    }

    [TestMethod]
    public void Unpack_EmptyTable_ReturnsNoElements()
    {
        var table = new LuaTable();
        var results = CallTableFunctionMultiple("unpack", LuaValue.Table(table));
        
        Assert.AreEqual(0, results.Length);
    }

    [TestMethod]
    public void Unpack_WithNField_UsesNAsLength()
    {
        var table = new LuaTable();
        table.Set(LuaValue.Integer(1), LuaValue.String("a"));
        table.Set(LuaValue.Integer(2), LuaValue.String("b"));
        table.Set(LuaValue.String("n"), LuaValue.Integer(2));
        
        var results = CallTableFunctionMultiple("unpack", LuaValue.Table(table));
        
        Assert.AreEqual(2, results.Length);
        Assert.AreEqual("a", results[0].AsString());
        Assert.AreEqual("b", results[1].AsString());
    }

    [TestMethod]
    public void Unpack_StartGreaterThanEnd_ReturnsEmpty()
    {
        var table = CreateTestTable(1, 2, 3);
        var results = CallTableFunctionMultiple("unpack", 
            LuaValue.Table(table), 
            LuaValue.Integer(3), 
            LuaValue.Integer(1));
        
        Assert.AreEqual(0, results.Length);
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Unpack_NoTable_ThrowsException()
    {
        CallTableFunction("unpack");
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Unpack_NonTable_ThrowsException()
    {
        CallTableFunction("unpack", LuaValue.String("not a table"));
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Unpack_NonIntegerEndIndex_ThrowsException()
    {
        var table = CreateTestTable(1, 2, 3);
        CallTableFunction("unpack", 
            LuaValue.Table(table), 
            LuaValue.Integer(1), 
            LuaValue.String("not a number"));
    }

    #endregion

    #region Helper Methods

    private LuaTable CreateTestTable(params object[] values)
    {
        var table = new LuaTable();
        for (int i = 0; i < values.Length; i++)
        {
            var luaValue = values[i] switch
            {
                int intValue => LuaValue.Integer(intValue),
                long longValue => LuaValue.Integer(longValue),
                string stringValue => LuaValue.String(stringValue),
                bool boolValue => LuaValue.Boolean(boolValue),
                _ => LuaValue.Nil
            };
            table.Set(LuaValue.Integer(i + 1), luaValue);
        }
        return table;
    }

    private LuaValue CallTableFunction(string functionName, params LuaValue[] args)
    {
        var tableTable = _env.GetVariable("table").AsTable<LuaTable>();
        var function = tableTable.Get(LuaValue.String(functionName)).AsFunction();
        var results = function.Call(args);
        return results.Length > 0 ? results[0] : LuaValue.Nil;
    }

    private LuaValue[] CallTableFunctionMultiple(string functionName, params LuaValue[] args)
    {
        var tableTable = _env.GetVariable("table").AsTable<LuaTable>();
        var function = tableTable.Get(LuaValue.String(functionName)).AsFunction();
        return function.Call(args);
    }

    #endregion
}