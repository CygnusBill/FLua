using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FLua.Runtime.Tests
{
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

        #region Table Creation and Basic Operations

        // State-Based Testing: Testing table initialization and state
        [TestMethod]
        public void LuaTable_NewTable_ShouldBeEmpty()
        {
            var table = new LuaTable();
            Assert.AreEqual(0, table.Array.Count);
            Assert.AreEqual(0, table.Dictionary.Count);
        }

        [TestMethod]
        public void LuaTable_SetAndGet_ShouldMaintainState()
        {
            var table = new LuaTable();
            var key = "test";
            var value = "value";
            
            table.Set(key, value);
            var retrieved = table.Get(key);
            
            Assert.AreEqual(value, retrieved);
        }

        // Equivalence Class Testing: Testing different key types
        [TestMethod]
        public void LuaTable_StringKey_ShouldStoreCorrectly()
        {
            var table = new LuaTable();
            table.Set("name", "Alice");
            
            var result = table.Get("name");
            Assert.AreEqual("Alice", result);
        }

        [TestMethod]
        public void LuaTable_IntegerKey_ShouldStoreInArray()
        {
            var table = new LuaTable();
            table.Set(1, "first");
            table.Set(2, "second");
            
            var result1 = table.Get(1);
            var result2 = table.Get(2);
            
            Assert.AreEqual("first", result1);
            Assert.AreEqual("second", result2);
        }

        [TestMethod]
        public void LuaTable_NonExistentKey_ShouldReturnNil()
        {
            var table = new LuaTable();
            var result = table.Get("nonexistent");
            
            Assert.IsTrue(result.IsNil);
        }

        #endregion

        #region Table Insert Function Tests

        // Scenario Testing: Testing table.insert functionality
        [TestMethod]
        public void TableInsert_AtEnd_ShouldAppendToArray()
        {
            var tableLib = _env.GetVariable("table").AsTable<LuaTable>();
            var insertFunc = tableLib.Get("insert").AsFunction<LuaFunction>();
            var testTable = new LuaTable();
            
            // Insert at end
            insertFunc.Call(testTable, "first");
            insertFunc.Call(testTable, "second");
            
            Assert.AreEqual("first", testTable.Get(1));
            Assert.AreEqual("second", testTable.Get(2));
        }

        [TestMethod]
        public void TableInsert_AtPosition_ShouldInsertCorrectly()
        {
            var tableLib = _env.GetVariable("table").AsTable<LuaTable>();
            var insertFunc = tableLib.Get("insert").AsFunction<LuaFunction>();
            var testTable = new LuaTable();
            
            testTable.Set(1, "first");
            testTable.Set(2, "third");
            
            // Insert at position 2
            insertFunc.Call(testTable, 2, "second");
            
            Assert.AreEqual("first", testTable.Get(1));
            Assert.AreEqual("second", testTable.Get(2));
            Assert.AreEqual("third", testTable.Get(3));
        }

        // Boundary Value Testing: Testing insert at boundaries
        [TestMethod]
        public void TableInsert_AtBeginning_ShouldShiftElements()
        {
            var tableLib = _env.GetVariable("table").AsTable<LuaTable>();
            var insertFunc = tableLib.Get("insert").AsFunction<LuaFunction>();
            var testTable = new LuaTable();
            
            testTable.Set(1, "second");
            insertFunc.Call(testTable, 1, "first");
            
            Assert.AreEqual("first", testTable.Get(1));
            Assert.AreEqual("second", testTable.Get(2));
        }

        #endregion

        #region Table Remove Function Tests

        // Equivalence Class Testing: Testing table.remove functionality
        [TestMethod]
        public void TableRemove_LastElement_ShouldRemoveAndReturn()
        {
            var tableLib = _env.GetVariable("table").AsTable<LuaTable>();
            var insertFunc = tableLib.Get("insert").AsFunction<LuaFunction>();
            var removeFunc = tableLib.Get("remove").AsFunction<LuaFunction>();
            var testTable = new LuaTable();
            
            insertFunc.Call(testTable, "first");
            insertFunc.Call(testTable, "second");
            
            var result = removeFunc.Call(testTable);
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("second", result[0]);
            Assert.IsTrue(testTable.Get(2).IsNil);
        }

        [TestMethod]
        public void TableRemove_AtPosition_ShouldRemoveAndShift()
        {
            var tableLib = _env.GetVariable("table").AsTable<LuaTable>();
            var insertFunc = tableLib.Get("insert").AsFunction<LuaFunction>();
            var removeFunc = tableLib.Get("remove").AsFunction<LuaFunction>();
            var testTable = new LuaTable();
            
            insertFunc.Call(testTable, "first");
            insertFunc.Call(testTable, "second");
            insertFunc.Call(testTable, "third");
            
            var result = removeFunc.Call(testTable, 2);
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("second", result[0]);
            Assert.AreEqual("first", testTable.Get(1));
            Assert.AreEqual("third", testTable.Get(2));
        }

        // Boundary Value Testing: Testing remove edge cases
        [TestMethod]
        public void TableRemove_EmptyTable_ShouldReturnNil()
        {
            var tableLib = _env.GetVariable("table").AsTable<LuaTable>();
            var removeFunc = tableLib.Get("remove").AsFunction<LuaFunction>();
            var testTable = new LuaTable();
            
            var result = removeFunc.Call(testTable);
            
            Assert.AreEqual(1, result.Length);
            Assert.IsTrue(result[0].IsNil);
        }

        #endregion

        #region Table Sort Function Tests

        // Domain Testing: Testing table.sort with different data types
        [TestMethod]
        public void TableSort_Numbers_ShouldSortAscending()
        {
            var tableLib = _env.GetVariable("table").AsTable<LuaTable>();
            var insertFunc = tableLib.Get("insert").AsFunction<LuaFunction>();
            var sortFunc = tableLib.Get("sort").AsFunction<LuaFunction>();
            var testTable = new LuaTable();
            
            insertFunc.Call(testTable, 3.5);
            insertFunc.Call(testTable, 1.2);
            insertFunc.Call(testTable, 2.8);
            
            sortFunc.Call(testTable);
            
            Assert.AreEqual(1.2, testTable.Get(LuaValue.Integer(1)).AsDouble(), 0.001);
            Assert.AreEqual(2.8, testTable.Get(LuaValue.Integer(2)).AsDouble(), 0.001);
            Assert.AreEqual(3.5, testTable.Get(LuaValue.Integer(3)).AsDouble(), 0.001);
        }

        [TestMethod]
        public void TableSort_Strings_ShouldSortAlphabetically()
        {
            var tableLib = _env.GetVariable("table").AsTable<LuaTable>();
            var insertFunc = tableLib.Get("insert").AsFunction<LuaFunction>();
            var sortFunc = tableLib.Get("sort").AsFunction<LuaFunction>();
            var testTable = new LuaTable();
            
            insertFunc.Call(testTable, "charlie");
            insertFunc.Call(testTable, "alice");
            insertFunc.Call(testTable, "bob");
            
            sortFunc.Call(testTable);
            
            Assert.AreEqual("alice", testTable.Get(LuaValue.Integer(1)).AsString());
            Assert.AreEqual("bob", testTable.Get(LuaValue.Integer(2)).AsString());
            Assert.AreEqual("charlie", testTable.Get(LuaValue.Integer(3)).AsString());
        }

        // Edge Case Testing: Testing sort with special cases
        [TestMethod]
        public void TableSort_EmptyTable_ShouldNotFail()
        {
            var tableLib = _env.GetVariable("table").AsTable<LuaTable>();
            var sortFunc = tableLib.Get("sort").AsFunction<LuaFunction>();
            var testTable = new LuaTable();
            
            // Should not throw an exception
            sortFunc.Call(testTable);
            
            Assert.AreEqual(0, testTable.Array.Count);
        }

        [TestMethod]
        public void TableSort_SingleElement_ShouldRemainUnchanged()
        {
            var tableLib = _env.GetVariable("table").AsTable<LuaTable>();
            var insertFunc = tableLib.Get("insert").AsFunction<LuaFunction>();
            var sortFunc = tableLib.Get("sort").AsFunction<LuaFunction>();
            var testTable = new LuaTable();
            
            insertFunc.Call(testTable, "only");
            sortFunc.Call(testTable);
            
            Assert.AreEqual("only", testTable.Get(1));
        }

        #endregion

        #region Table Concat Function Tests

        // Scenario Testing: Testing table.concat functionality
        [TestMethod]
        public void TableConcat_WithoutSeparator_ShouldConcatenateDirectly()
        {
            var tableLib = _env.GetVariable("table").AsTable<LuaTable>();
            var insertFunc = tableLib.Get("insert").AsFunction<LuaFunction>();
            var concatFunc = tableLib.Get("concat").AsFunction<LuaFunction>();
            var testTable = new LuaTable();
            
            insertFunc.Call(testTable, "Hello");
            insertFunc.Call(testTable, "World");
            
            var result = concatFunc.Call(testTable);
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("HelloWorld", result[0]);
        }

        [TestMethod]
        public void TableConcat_WithSeparator_ShouldUseSeparator()
        {
            var tableLib = _env.GetVariable("table").AsTable<LuaTable>();
            var insertFunc = tableLib.Get("insert").AsFunction<LuaFunction>();
            var concatFunc = tableLib.Get("concat").AsFunction<LuaFunction>();
            var testTable = new LuaTable();
            
            insertFunc.Call(testTable, "apple");
            insertFunc.Call(testTable, "banana");
            insertFunc.Call(testTable, "cherry");
            
            var result = concatFunc.Call(testTable, ", ");
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("apple, banana, cherry", result[0]);
        }

        // Boundary Value Testing: Testing concat with range
        [TestMethod]
        public void TableConcat_WithRange_ShouldConcatenateSubset()
        {
            var tableLib = _env.GetVariable("table").AsTable<LuaTable>();
            var insertFunc = tableLib.Get("insert").AsFunction<LuaFunction>();
            var concatFunc = tableLib.Get("concat").AsFunction<LuaFunction>();
            var testTable = new LuaTable();
            
            insertFunc.Call(testTable, "a");
            insertFunc.Call(testTable, "b");
            insertFunc.Call(testTable, "c");
            insertFunc.Call(testTable, "d");
            
            var result = concatFunc.Call(testTable, "-", 2, 3);
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("b-c", result[0]);
        }

        [TestMethod]
        public void TableConcat_EmptyTable_ShouldReturnEmptyString()
        {
            var tableLib = _env.GetVariable("table").AsTable<LuaTable>();
            var concatFunc = tableLib.Get("concat").AsFunction<LuaFunction>();
            var testTable = new LuaTable();
            
            var result = concatFunc.Call(testTable);
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("", result[0]);
        }

        #endregion

        #region Metatable Tests

        // State-Based Testing: Testing metatable functionality
        [TestMethod]
        public void LuaTable_SetMetatable_ShouldStoreMetatable()
        {
            var table = new LuaTable();
            var metatable = new LuaTable();
            
            table.Metatable = metatable;
            
            Assert.AreSame(metatable, table.Metatable);
        }

        [TestMethod]
        public void LuaTable_MetatableIndexAccess_ShouldCallMetamethod()
        {
            var table = new LuaTable();
            var metatable = new LuaTable();
            var indexTable = new LuaTable();
            
            indexTable.Set("default", "fallback");
            metatable.Set("__index", indexTable);
            table.Metatable = metatable;
            
            var result = table.Get("default");
            
            Assert.AreEqual("fallback", result);
        }

        // Risk-Based Testing: Testing metatable edge cases
        [TestMethod]
        public void LuaTable_NoMetatable_ShouldReturnNilForMissingKeys()
        {
            var table = new LuaTable();
            var result = table.Get("missing");
            
            Assert.IsTrue(result.IsNil);
        }

        #endregion
    }
} 