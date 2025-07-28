using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Interpreter;
using FLua.Runtime;

namespace FLua.Interpreter.Tests
{
    [TestClass]
    public class TestFeatures
    {
        private LuaInterpreter _interpreter = null!;
        
        [TestInitialize]
        public void Setup()
        {
            _interpreter = new LuaInterpreter();
        }
        
        [TestMethod]
        public void TestBasicExpressions()
        {
            // Arithmetic
            Assert.AreEqual(3.0, GetNumericValue(_interpreter.EvaluateExpression("1 + 2")));
            Assert.AreEqual(1.0, GetNumericValue(_interpreter.EvaluateExpression("3 - 2")));
            Assert.AreEqual(6.0, GetNumericValue(_interpreter.EvaluateExpression("2 * 3")));
            Assert.AreEqual(2.0, GetNumericValue(_interpreter.EvaluateExpression("4 / 2")));
            Assert.AreEqual(1.0, GetNumericValue(_interpreter.EvaluateExpression("5 % 2")));
            Assert.AreEqual(8.0, GetNumericValue(_interpreter.EvaluateExpression("2 ^ 3")));
            Assert.AreEqual(2.0, GetNumericValue(_interpreter.EvaluateExpression("5 // 2")));
            
            // Comparison
            Assert.IsTrue(_interpreter.EvaluateExpression("1 < 2").AsBoolean());
            Assert.IsTrue(_interpreter.EvaluateExpression("2 <= 2").AsBoolean());
            Assert.IsTrue(_interpreter.EvaluateExpression("3 > 2").AsBoolean());
            Assert.IsTrue(_interpreter.EvaluateExpression("3 >= 3").AsBoolean());
            Assert.IsTrue(_interpreter.EvaluateExpression("2 == 2").AsBoolean());
            Assert.IsTrue(_interpreter.EvaluateExpression("2 ~= 3").AsBoolean());
            
            // Logical
            Assert.IsTrue(_interpreter.EvaluateExpression("true and true").AsBoolean());
            Assert.IsFalse(_interpreter.EvaluateExpression("true and false").AsBoolean());
            Assert.IsTrue(_interpreter.EvaluateExpression("true or false").AsBoolean());
            Assert.IsFalse(_interpreter.EvaluateExpression("false or false").AsBoolean());
            Assert.IsFalse(_interpreter.EvaluateExpression("not true").AsBoolean());
            Assert.IsTrue(_interpreter.EvaluateExpression("not false").AsBoolean());
            
            // Bitwise
            Assert.AreEqual(3L, _interpreter.EvaluateExpression("1 | 2").AsInteger());
            Assert.AreEqual(0L, _interpreter.EvaluateExpression("1 & 2").AsInteger());
            Assert.AreEqual(3L, _interpreter.EvaluateExpression("1 ~ 2").AsInteger());
            Assert.AreEqual(4L, _interpreter.EvaluateExpression("1 << 2").AsInteger());
            Assert.AreEqual(1L, _interpreter.EvaluateExpression("4 >> 2").AsInteger());
            Assert.AreEqual(-2L, _interpreter.EvaluateExpression("~1").AsInteger());
            
            // Concatenation
            Assert.AreEqual("hello world", _interpreter.EvaluateExpression("'hello ' .. 'world'").AsString());
            
            // Length
            Assert.AreEqual(5L, _interpreter.EvaluateExpression("#'hello'").AsInteger());
            
            // Parentheses
            Assert.AreEqual(7.0, GetNumericValue(_interpreter.EvaluateExpression("1 + (2 * 3)")));
        }
        
        [TestMethod]
        public void TestSimpleVariables()
        {
            // Test simple variable assignment and retrieval
            _interpreter.ExecuteCode("x = 10");
            Assert.AreEqual(10.0, GetNumericValue(_interpreter.EvaluateExpression("x")));
            
            // Test local variable
            _interpreter.ExecuteCode("local y = 20");
            Assert.AreEqual(20.0, GetNumericValue(_interpreter.EvaluateExpression("y")));
            
            // Test reassignment
            _interpreter.ExecuteCode("x = 30");
            Assert.AreEqual(30.0, GetNumericValue(_interpreter.EvaluateExpression("x")));
        }
        
        [TestMethod]
        public void TestSimpleTable()
        {
            // Test table construction and access
            _interpreter.ExecuteCode("t = {10, 20, 30}");
            Assert.AreEqual(10.0, GetNumericValue(_interpreter.EvaluateExpression("t[1]")));
            Assert.AreEqual(20.0, GetNumericValue(_interpreter.EvaluateExpression("t[2]")));
            Assert.AreEqual(30.0, GetNumericValue(_interpreter.EvaluateExpression("t[3]")));
            
            // Test table length
            Assert.AreEqual(3L, _interpreter.EvaluateExpression("#t").AsInteger());
        }
        
        // Helper method to handle both LuaNumber and LuaInteger
        private double GetNumericValue(LuaValue value)
        {
            if (value.IsNumber)
                return value.AsDouble();
            throw new InvalidCastException($"Cannot convert {value.Type} to a numeric value");
        }
    }
} 