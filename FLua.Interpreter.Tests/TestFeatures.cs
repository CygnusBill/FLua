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
            Assert.IsTrue(((LuaBoolean)_interpreter.EvaluateExpression("1 < 2")).Value);
            Assert.IsTrue(((LuaBoolean)_interpreter.EvaluateExpression("2 <= 2")).Value);
            Assert.IsTrue(((LuaBoolean)_interpreter.EvaluateExpression("3 > 2")).Value);
            Assert.IsTrue(((LuaBoolean)_interpreter.EvaluateExpression("3 >= 3")).Value);
            Assert.IsTrue(((LuaBoolean)_interpreter.EvaluateExpression("2 == 2")).Value);
            Assert.IsTrue(((LuaBoolean)_interpreter.EvaluateExpression("2 ~= 3")).Value);
            
            // Logical
            Assert.IsTrue(((LuaBoolean)_interpreter.EvaluateExpression("true and true")).Value);
            Assert.IsFalse(((LuaBoolean)_interpreter.EvaluateExpression("true and false")).Value);
            Assert.IsTrue(((LuaBoolean)_interpreter.EvaluateExpression("true or false")).Value);
            Assert.IsFalse(((LuaBoolean)_interpreter.EvaluateExpression("false or false")).Value);
            Assert.IsFalse(((LuaBoolean)_interpreter.EvaluateExpression("not true")).Value);
            Assert.IsTrue(((LuaBoolean)_interpreter.EvaluateExpression("not false")).Value);
            
            // Bitwise
            Assert.AreEqual(3L, ((LuaInteger)_interpreter.EvaluateExpression("1 | 2")).Value);
            Assert.AreEqual(0L, ((LuaInteger)_interpreter.EvaluateExpression("1 & 2")).Value);
            Assert.AreEqual(3L, ((LuaInteger)_interpreter.EvaluateExpression("1 ~ 2")).Value);
            Assert.AreEqual(4L, ((LuaInteger)_interpreter.EvaluateExpression("1 << 2")).Value);
            Assert.AreEqual(1L, ((LuaInteger)_interpreter.EvaluateExpression("4 >> 2")).Value);
            Assert.AreEqual(-2L, ((LuaInteger)_interpreter.EvaluateExpression("~1")).Value);
            
            // Concatenation
            Assert.AreEqual("hello world", ((LuaString)_interpreter.EvaluateExpression("'hello ' .. 'world'")).Value);
            
            // Length
            Assert.AreEqual(5L, ((LuaInteger)_interpreter.EvaluateExpression("#'hello'")).Value);
            
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
            Assert.AreEqual(3L, ((LuaInteger)_interpreter.EvaluateExpression("#t")).Value);
        }
        
        // Helper method to handle both LuaNumber and LuaInteger
        private double GetNumericValue(LuaValue value)
        {
            if (value is LuaNumber number)
            {
                return number.Value;
            }
            else if (value is LuaInteger integer)
            {
                return integer.Value;
            }
            throw new InvalidCastException($"Cannot convert {value.GetType().Name} to a numeric value");
        }
    }
} 