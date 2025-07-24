using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Runtime;
using System;

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
            var key = new LuaString("test");
            var value = new LuaString("value");
            
            table.Set(key, value);
            var retrieved = table.Get(key);
            
            Assert.AreEqual(value, retrieved);
        }

        // Equivalence Class Testing: Testing different key types
        [TestMethod]
        public void LuaTable_StringKey_ShouldStoreCorrectly()
        {
            var table = new LuaTable();
            table.Set(new LuaString("name"), new LuaString("Alice"));
            
            var result = table.Get(new LuaString("name"));
            Assert.AreEqual("Alice", ((LuaString)result).Value);
        }

        [TestMethod]
        public void LuaTable_IntegerKey_ShouldStoreInArray()
        {
            var table = new LuaTable();
            table.Set(new LuaInteger(1), new LuaString("first"));
            table.Set(new LuaInteger(2), new LuaString("second"));
            
            var result1 = table.Get(new LuaInteger(1));
            var result2 = table.Get(new LuaInteger(2));
            
            Assert.AreEqual("first", ((LuaString)result1).Value);
            Assert.AreEqual("second", ((LuaString)result2).Value);
        }

        [TestMethod]
        public void LuaTable_NonExistentKey_ShouldReturnNil()
        {
            var table = new LuaTable();
            var result = table.Get(new LuaString("nonexistent"));
            
            Assert.IsInstanceOfType(result, typeof(LuaNil));
        }

        #endregion

        #region Table Insert Function Tests

        // Scenario Testing: Testing table.insert functionality
        [TestMethod]
        public void TableInsert_AtEnd_ShouldAppendToArray()
        {
            var table_lib = (LuaTable)_env.GetVariable("table");
            var insertFunc = (LuaFunction)table_lib.Get(new LuaString("insert"));
            var testTable = new LuaTable();
            
            // Insert at end
            insertFunc.Call(new LuaValue[] { testTable, new LuaString("first") });
            insertFunc.Call(new LuaValue[] { testTable, new LuaString("second") });
            
            Assert.AreEqual("first", ((LuaString)testTable.Get(new LuaInteger(1))).Value);
            Assert.AreEqual("second", ((LuaString)testTable.Get(new LuaInteger(2))).Value);
        }

        [TestMethod]
        public void TableInsert_AtPosition_ShouldInsertCorrectly()
        {
            var table_lib = (LuaTable)_env.GetVariable("table");
            var insertFunc = (LuaFunction)table_lib.Get(new LuaString("insert"));
            var testTable = new LuaTable();
            
            testTable.Set(new LuaInteger(1), new LuaString("first"));
            testTable.Set(new LuaInteger(2), new LuaString("third"));
            
            // Insert at position 2
            insertFunc.Call(new LuaValue[] { testTable, new LuaInteger(2), new LuaString("second") });
            
            Assert.AreEqual("first", ((LuaString)testTable.Get(new LuaInteger(1))).Value);
            Assert.AreEqual("second", ((LuaString)testTable.Get(new LuaInteger(2))).Value);
            Assert.AreEqual("third", ((LuaString)testTable.Get(new LuaInteger(3))).Value);
        }

        // Boundary Value Testing: Testing insert at boundaries
        [TestMethod]
        public void TableInsert_AtBeginning_ShouldShiftElements()
        {
            var table_lib = (LuaTable)_env.GetVariable("table");
            var insertFunc = (LuaFunction)table_lib.Get(new LuaString("insert"));
            var testTable = new LuaTable();
            
            testTable.Set(new LuaInteger(1), new LuaString("second"));
            insertFunc.Call(new LuaValue[] { testTable, new LuaInteger(1), new LuaString("first") });
            
            Assert.AreEqual("first", ((LuaString)testTable.Get(new LuaInteger(1))).Value);
            Assert.AreEqual("second", ((LuaString)testTable.Get(new LuaInteger(2))).Value);
        }

        #endregion

        #region Table Remove Function Tests

        // Equivalence Class Testing: Testing table.remove functionality
        [TestMethod]
        public void TableRemove_LastElement_ShouldRemoveAndReturn()
        {
            var table_lib = (LuaTable)_env.GetVariable("table");
            var insertFunc = (LuaFunction)table_lib.Get(new LuaString("insert"));
            var removeFunc = (LuaFunction)table_lib.Get(new LuaString("remove"));
            var testTable = new LuaTable();
            
            insertFunc.Call(new LuaValue[] { testTable, new LuaString("first") });
            insertFunc.Call(new LuaValue[] { testTable, new LuaString("second") });
            
            var result = removeFunc.Call(new LuaValue[] { testTable });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("second", ((LuaString)result[0]).Value);
            Assert.IsInstanceOfType(testTable.Get(new LuaInteger(2)), typeof(LuaNil));
        }

        [TestMethod]
        public void TableRemove_AtPosition_ShouldRemoveAndShift()
        {
            var table_lib = (LuaTable)_env.GetVariable("table");
            var insertFunc = (LuaFunction)table_lib.Get(new LuaString("insert"));
            var removeFunc = (LuaFunction)table_lib.Get(new LuaString("remove"));
            var testTable = new LuaTable();
            
            insertFunc.Call(new LuaValue[] { testTable, new LuaString("first") });
            insertFunc.Call(new LuaValue[] { testTable, new LuaString("second") });
            insertFunc.Call(new LuaValue[] { testTable, new LuaString("third") });
            
            var result = removeFunc.Call(new LuaValue[] { testTable, new LuaInteger(2) });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("second", ((LuaString)result[0]).Value);
            Assert.AreEqual("first", ((LuaString)testTable.Get(new LuaInteger(1))).Value);
            Assert.AreEqual("third", ((LuaString)testTable.Get(new LuaInteger(2))).Value);
        }

        // Boundary Value Testing: Testing remove edge cases
        [TestMethod]
        public void TableRemove_EmptyTable_ShouldReturnNil()
        {
            var table_lib = (LuaTable)_env.GetVariable("table");
            var removeFunc = (LuaFunction)table_lib.Get(new LuaString("remove"));
            var testTable = new LuaTable();
            
            var result = removeFunc.Call(new LuaValue[] { testTable });
            
            Assert.AreEqual(1, result.Length);
            Assert.IsInstanceOfType(result[0], typeof(LuaNil));
        }

        #endregion

        #region Table Sort Function Tests

        // Domain Testing: Testing table.sort with different data types
        [TestMethod]
        public void TableSort_Numbers_ShouldSortAscending()
        {
            var table_lib = (LuaTable)_env.GetVariable("table");
            var insertFunc = (LuaFunction)table_lib.Get(new LuaString("insert"));
            var sortFunc = (LuaFunction)table_lib.Get(new LuaString("sort"));
            var testTable = new LuaTable();
            
            insertFunc.Call(new LuaValue[] { testTable, new LuaNumber(3.5) });
            insertFunc.Call(new LuaValue[] { testTable, new LuaNumber(1.2) });
            insertFunc.Call(new LuaValue[] { testTable, new LuaNumber(2.8) });
            
            sortFunc.Call(new LuaValue[] { testTable });
            
            Assert.AreEqual(1.2, ((LuaNumber)testTable.Get(new LuaInteger(1))).Value, 0.001);
            Assert.AreEqual(2.8, ((LuaNumber)testTable.Get(new LuaInteger(2))).Value, 0.001);
            Assert.AreEqual(3.5, ((LuaNumber)testTable.Get(new LuaInteger(3))).Value, 0.001);
        }

        [TestMethod]
        public void TableSort_Strings_ShouldSortAlphabetically()
        {
            var table_lib = (LuaTable)_env.GetVariable("table");
            var insertFunc = (LuaFunction)table_lib.Get(new LuaString("insert"));
            var sortFunc = (LuaFunction)table_lib.Get(new LuaString("sort"));
            var testTable = new LuaTable();
            
            insertFunc.Call(new LuaValue[] { testTable, new LuaString("charlie") });
            insertFunc.Call(new LuaValue[] { testTable, new LuaString("alice") });
            insertFunc.Call(new LuaValue[] { testTable, new LuaString("bob") });
            
            sortFunc.Call(new LuaValue[] { testTable });
            
            Assert.AreEqual("alice", ((LuaString)testTable.Get(new LuaInteger(1))).Value);
            Assert.AreEqual("bob", ((LuaString)testTable.Get(new LuaInteger(2))).Value);
            Assert.AreEqual("charlie", ((LuaString)testTable.Get(new LuaInteger(3))).Value);
        }

        // Edge Case Testing: Testing sort with special cases
        [TestMethod]
        public void TableSort_EmptyTable_ShouldNotFail()
        {
            var table_lib = (LuaTable)_env.GetVariable("table");
            var sortFunc = (LuaFunction)table_lib.Get(new LuaString("sort"));
            var testTable = new LuaTable();
            
            // Should not throw an exception
            sortFunc.Call(new LuaValue[] { testTable });
            
            Assert.AreEqual(0, testTable.Array.Count);
        }

        [TestMethod]
        public void TableSort_SingleElement_ShouldRemainUnchanged()
        {
            var table_lib = (LuaTable)_env.GetVariable("table");
            var insertFunc = (LuaFunction)table_lib.Get(new LuaString("insert"));
            var sortFunc = (LuaFunction)table_lib.Get(new LuaString("sort"));
            var testTable = new LuaTable();
            
            insertFunc.Call(new LuaValue[] { testTable, new LuaString("only") });
            sortFunc.Call(new LuaValue[] { testTable });
            
            Assert.AreEqual("only", ((LuaString)testTable.Get(new LuaInteger(1))).Value);
        }

        #endregion

        #region Table Concat Function Tests

        // Scenario Testing: Testing table.concat functionality
        [TestMethod]
        public void TableConcat_WithoutSeparator_ShouldConcatenateDirectly()
        {
            var table_lib = (LuaTable)_env.GetVariable("table");
            var insertFunc = (LuaFunction)table_lib.Get(new LuaString("insert"));
            var concatFunc = (LuaFunction)table_lib.Get(new LuaString("concat"));
            var testTable = new LuaTable();
            
            insertFunc.Call(new LuaValue[] { testTable, new LuaString("Hello") });
            insertFunc.Call(new LuaValue[] { testTable, new LuaString("World") });
            
            var result = concatFunc.Call(new LuaValue[] { testTable });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("HelloWorld", ((LuaString)result[0]).Value);
        }

        [TestMethod]
        public void TableConcat_WithSeparator_ShouldUseSeparator()
        {
            var table_lib = (LuaTable)_env.GetVariable("table");
            var insertFunc = (LuaFunction)table_lib.Get(new LuaString("insert"));
            var concatFunc = (LuaFunction)table_lib.Get(new LuaString("concat"));
            var testTable = new LuaTable();
            
            insertFunc.Call(new LuaValue[] { testTable, new LuaString("apple") });
            insertFunc.Call(new LuaValue[] { testTable, new LuaString("banana") });
            insertFunc.Call(new LuaValue[] { testTable, new LuaString("cherry") });
            
            var result = concatFunc.Call(new LuaValue[] { testTable, new LuaString(", ") });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("apple, banana, cherry", ((LuaString)result[0]).Value);
        }

        // Boundary Value Testing: Testing concat with range
        [TestMethod]
        public void TableConcat_WithRange_ShouldConcatenateSubset()
        {
            var table_lib = (LuaTable)_env.GetVariable("table");
            var insertFunc = (LuaFunction)table_lib.Get(new LuaString("insert"));
            var concatFunc = (LuaFunction)table_lib.Get(new LuaString("concat"));
            var testTable = new LuaTable();
            
            insertFunc.Call(new LuaValue[] { testTable, new LuaString("a") });
            insertFunc.Call(new LuaValue[] { testTable, new LuaString("b") });
            insertFunc.Call(new LuaValue[] { testTable, new LuaString("c") });
            insertFunc.Call(new LuaValue[] { testTable, new LuaString("d") });
            
            var result = concatFunc.Call(new LuaValue[] { 
                testTable, 
                new LuaString("-"), 
                new LuaInteger(2), 
                new LuaInteger(3) 
            });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("b-c", ((LuaString)result[0]).Value);
        }

        [TestMethod]
        public void TableConcat_EmptyTable_ShouldReturnEmptyString()
        {
            var table_lib = (LuaTable)_env.GetVariable("table");
            var concatFunc = (LuaFunction)table_lib.Get(new LuaString("concat"));
            var testTable = new LuaTable();
            
            var result = concatFunc.Call(new LuaValue[] { testTable });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("", ((LuaString)result[0]).Value);
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
            
            indexTable.Set(new LuaString("default"), new LuaString("fallback"));
            metatable.Set(new LuaString("__index"), indexTable);
            table.Metatable = metatable;
            
            var result = table.Get(new LuaString("default"));
            
            Assert.AreEqual("fallback", ((LuaString)result).Value);
        }

        // Risk-Based Testing: Testing metatable edge cases
        [TestMethod]
        public void LuaTable_NoMetatable_ShouldReturnNilForMissingKeys()
        {
            var table = new LuaTable();
            var result = table.Get(new LuaString("missing"));
            
            Assert.IsInstanceOfType(result, typeof(LuaNil));
        }

        #endregion
    }
} 