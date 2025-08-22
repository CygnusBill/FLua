using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Runtime;
using System;
using System.Collections.Generic;

namespace FLua.Runtime.LibraryTests
{
    /// <summary>
    /// Comprehensive test suite for LuaTypeConversion - critical type conversion utilities.
    /// This class had only 3.1% test coverage and provides essential Lua type conversion logic.
    /// Tests all conversion methods using Lee Copeland testing methodology.
    /// </summary>
    [TestClass]
    public class LuaTypeConversionTests
    {
        #region ToNumber Tests

        [TestMethod]
        public void ToNumber_IntegerValue_ReturnsDoubleValue()
        {
            // Testing Approach: Basic functionality - integer to number conversion
            var intValue = LuaValue.Integer(42);
            
            var result = LuaTypeConversion.ToNumber(intValue);
            
            Assert.AreEqual(42.0, result);
        }

        [TestMethod]
        public void ToNumber_FloatValue_ReturnsDoubleValue()
        {
            // Testing Approach: Basic functionality - float to number conversion
            var floatValue = LuaValue.Float(3.14);
            
            var result = LuaTypeConversion.ToNumber(floatValue);
            
            Assert.AreEqual(3.14, result);
        }

        [TestMethod]
        public void ToNumber_NumericString_ReturnsCorrectNumber()
        {
            // Testing Approach: String to number conversion - valid numeric string
            var stringValue = LuaValue.String("123.45");
            
            var result = LuaTypeConversion.ToNumber(stringValue);
            
            Assert.AreEqual(123.45, result);
        }

        [TestMethod]
        public void ToNumber_IntegerString_ReturnsCorrectNumber()
        {
            // Testing Approach: String to number conversion - integer string
            var stringValue = LuaValue.String("789");
            
            var result = LuaTypeConversion.ToNumber(stringValue);
            
            Assert.AreEqual(789.0, result);
        }

        [TestMethod]
        public void ToNumber_StringWithWhitespace_ReturnsCorrectNumber()
        {
            // Testing Approach: Boundary Value Analysis - string with leading/trailing whitespace
            var stringValue = LuaValue.String("  42.5  ");
            
            var result = LuaTypeConversion.ToNumber(stringValue);
            
            Assert.AreEqual(42.5, result);
        }

        [TestMethod]
        public void ToNumber_NegativeString_ReturnsCorrectNumber()
        {
            // Testing Approach: Boundary Value Analysis - negative number string
            var stringValue = LuaValue.String("-123.45");
            
            var result = LuaTypeConversion.ToNumber(stringValue);
            
            Assert.AreEqual(-123.45, result);
        }

        [TestMethod]
        public void ToNumber_ZeroString_ReturnsZero()
        {
            // Testing Approach: Boundary Value Analysis - zero string
            var stringValue = LuaValue.String("0");
            
            var result = LuaTypeConversion.ToNumber(stringValue);
            
            Assert.AreEqual(0.0, result);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ToNumber_InvalidString_ThrowsException()
        {
            // Testing Approach: Error condition - invalid number string
            var stringValue = LuaValue.String("not a number");
            
            LuaTypeConversion.ToNumber(stringValue); // Should throw
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ToNumber_BooleanValue_ThrowsException()
        {
            // Testing Approach: Error condition - non-numeric type
            var boolValue = LuaValue.Boolean(true);
            
            LuaTypeConversion.ToNumber(boolValue); // Should throw
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ToNumber_NilValue_ThrowsException()
        {
            // Testing Approach: Error condition - nil value
            var nilValue = LuaValue.Nil;
            
            LuaTypeConversion.ToNumber(nilValue); // Should throw
        }

        #endregion

        #region ToInteger Tests

        [TestMethod]
        public void ToInteger_IntegerValue_ReturnsLongValue()
        {
            // Testing Approach: Basic functionality - integer to integer conversion
            var intValue = LuaValue.Integer(42);
            
            var result = LuaTypeConversion.ToInteger(intValue);
            
            Assert.AreEqual(42L, result);
        }

        [TestMethod]
        public void ToInteger_FloatWithIntegerValue_ReturnsInteger()
        {
            // Testing Approach: Float to integer conversion - whole number
            var floatValue = LuaValue.Float(123.0);
            
            var result = LuaTypeConversion.ToInteger(floatValue);
            
            Assert.AreEqual(123L, result);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ToInteger_FloatWithFractionalPart_ThrowsException()
        {
            // Testing Approach: Error condition - float with fractional part
            var floatValue = LuaValue.Float(123.45);
            
            LuaTypeConversion.ToInteger(floatValue); // Should throw
        }

        [TestMethod]
        public void ToInteger_IntegerString_ReturnsCorrectInteger()
        {
            // Testing Approach: String to integer conversion - valid integer string
            var stringValue = LuaValue.String("789");
            
            var result = LuaTypeConversion.ToInteger(stringValue);
            
            Assert.AreEqual(789L, result);
        }

        [TestMethod]
        public void ToInteger_NegativeIntegerString_ReturnsCorrectInteger()
        {
            // Testing Approach: Boundary Value Analysis - negative integer string
            var stringValue = LuaValue.String("-456");
            
            var result = LuaTypeConversion.ToInteger(stringValue);
            
            Assert.AreEqual(-456L, result);
        }

        [TestMethod]
        public void ToInteger_ZeroString_ReturnsZero()
        {
            // Testing Approach: Boundary Value Analysis - zero string
            var stringValue = LuaValue.String("0");
            
            var result = LuaTypeConversion.ToInteger(stringValue);
            
            Assert.AreEqual(0L, result);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ToInteger_FloatString_ThrowsException()
        {
            // Testing Approach: Error condition - float string to integer
            var stringValue = LuaValue.String("123.45");
            
            LuaTypeConversion.ToInteger(stringValue); // Should throw
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ToInteger_InvalidString_ThrowsException()
        {
            // Testing Approach: Error condition - invalid number string
            var stringValue = LuaValue.String("not a number");
            
            LuaTypeConversion.ToInteger(stringValue); // Should throw
        }

        [TestMethod]
        public void ToInteger_MaxLongValue_ReturnsMaxValue()
        {
            // Testing Approach: Boundary Value Analysis - maximum long value
            var maxValue = LuaValue.Integer(long.MaxValue);
            
            var result = LuaTypeConversion.ToInteger(maxValue);
            
            Assert.AreEqual(long.MaxValue, result);
        }

        [TestMethod]
        public void ToInteger_MinLongValue_ReturnsMinValue()
        {
            // Testing Approach: Boundary Value Analysis - minimum long value
            var minValue = LuaValue.Integer(long.MinValue);
            
            var result = LuaTypeConversion.ToInteger(minValue);
            
            Assert.AreEqual(long.MinValue, result);
        }

        #endregion

        #region ToString Tests

        [TestMethod]
        public void ToString_StringValue_ReturnsString()
        {
            // Testing Approach: Basic functionality - string to string conversion
            var stringValue = LuaValue.String("hello world");
            
            var result = LuaTypeConversion.ToString(stringValue);
            
            Assert.AreEqual("hello world", result);
        }

        [TestMethod]
        public void ToString_IntegerValue_ReturnsStringRepresentation()
        {
            // Testing Approach: Number to string conversion - integer
            var intValue = LuaValue.Integer(42);
            
            var result = LuaTypeConversion.ToString(intValue);
            
            Assert.AreEqual("42", result);
        }

        [TestMethod]
        public void ToString_FloatValue_ReturnsStringRepresentation()
        {
            // Testing Approach: Number to string conversion - float
            var floatValue = LuaValue.Float(3.14);
            
            var result = LuaTypeConversion.ToString(floatValue);
            
            // Should contain the number in string form
            Assert.IsTrue(result.Contains("3.14"));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ToString_BooleanValue_ThrowsException()
        {
            // Testing Approach: Error condition - boolean to string
            var boolValue = LuaValue.Boolean(true);
            
            LuaTypeConversion.ToString(boolValue); // Should throw
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ToString_NilValue_ThrowsException()
        {
            // Testing Approach: Error condition - nil to string
            var nilValue = LuaValue.Nil;
            
            LuaTypeConversion.ToString(nilValue); // Should throw
        }

        #endregion

        #region ToBoolean Tests

        [TestMethod]
        public void ToBoolean_TrueValue_ReturnsTrue()
        {
            // Testing Approach: Basic functionality - true boolean
            var trueValue = LuaValue.Boolean(true);
            
            var result = LuaTypeConversion.ToBoolean(trueValue);
            
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ToBoolean_FalseValue_ReturnsFalse()
        {
            // Testing Approach: Basic functionality - false boolean
            var falseValue = LuaValue.Boolean(false);
            
            var result = LuaTypeConversion.ToBoolean(falseValue);
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ToBoolean_NilValue_ReturnsFalse()
        {
            // Testing Approach: Lua truthiness - nil is falsy
            var nilValue = LuaValue.Nil;
            
            var result = LuaTypeConversion.ToBoolean(nilValue);
            
            Assert.IsFalse(result);
        }

        [TestMethod]
        public void ToBoolean_NonBooleanNonNilValues_ReturnTrue()
        {
            // Testing Approach: Lua truthiness - everything else is truthy
            Assert.IsTrue(LuaTypeConversion.ToBoolean(LuaValue.Integer(0))); // 0 is truthy in Lua
            Assert.IsTrue(LuaTypeConversion.ToBoolean(LuaValue.String(""))); // Empty string is truthy in Lua
            Assert.IsTrue(LuaTypeConversion.ToBoolean(LuaValue.Float(0.0))); // 0.0 is truthy in Lua
            Assert.IsTrue(LuaTypeConversion.ToBoolean(LuaValue.String("false"))); // String "false" is truthy
        }

        #endregion

        #region GetTypeName Tests

        [TestMethod]
        public void GetTypeName_AllLuaTypes_ReturnsCorrectNames()
        {
            // Testing Approach: Equivalence Class Partitioning - all Lua types
            Assert.AreEqual("nil", LuaTypeConversion.GetTypeName(LuaValue.Nil));
            Assert.AreEqual("boolean", LuaTypeConversion.GetTypeName(LuaValue.Boolean(true)));
            Assert.AreEqual("number", LuaTypeConversion.GetTypeName(LuaValue.Integer(42)));
            Assert.AreEqual("number", LuaTypeConversion.GetTypeName(LuaValue.Float(3.14)));
            Assert.AreEqual("string", LuaTypeConversion.GetTypeName(LuaValue.String("hello")));
        }

        [TestMethod]
        public void GetTypeName_TableValue_ReturnsTable()
        {
            // Testing Approach: Type name for table
            var table = new LuaTable();
            var tableValue = LuaValue.Table(table);
            
            var result = LuaTypeConversion.GetTypeName(tableValue);
            
            Assert.AreEqual("table", result);
        }

        [TestMethod]
        public void GetTypeName_FunctionValue_ReturnsFunction()
        {
            // Testing Approach: Type name for function
            var function = new BuiltinFunction(args => new[] { LuaValue.Nil });
            var functionValue = LuaValue.Function(function);
            
            var result = LuaTypeConversion.GetTypeName(functionValue);
            
            Assert.AreEqual("function", result);
        }

        #endregion

        #region ToConcatString Tests

        [TestMethod]
        public void ToConcatString_StringValue_ReturnsString()
        {
            // Testing Approach: String concatenation - string value
            var stringValue = LuaValue.String("hello");
            
            var result = LuaTypeConversion.ToConcatString(stringValue);
            
            Assert.AreEqual("hello", result);
        }

        [TestMethod]
        public void ToConcatString_IntegerValue_ReturnsStringRepresentation()
        {
            // Testing Approach: Number concatenation - integer
            var intValue = LuaValue.Integer(42);
            
            var result = LuaTypeConversion.ToConcatString(intValue);
            
            Assert.AreEqual("42", result);
        }

        [TestMethod]
        public void ToConcatString_FloatValue_ReturnsStringRepresentation()
        {
            // Testing Approach: Number concatenation - float
            var floatValue = LuaValue.Float(3.14);
            
            var result = LuaTypeConversion.ToConcatString(floatValue);
            
            // Should contain the number representation
            Assert.IsTrue(result.Contains("3.14"));
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ToConcatString_BooleanValue_ThrowsException()
        {
            // Testing Approach: Error condition - boolean not valid for concatenation
            var boolValue = LuaValue.Boolean(true);
            
            LuaTypeConversion.ToConcatString(boolValue); // Should throw
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ToConcatString_NilValue_ThrowsException()
        {
            // Testing Approach: Error condition - nil not valid for concatenation
            var nilValue = LuaValue.Nil;
            
            LuaTypeConversion.ToConcatString(nilValue); // Should throw
        }

        #endregion

        #region StringToNumber Tests

        [TestMethod]
        public void StringToNumber_ValidIntegerString_ReturnsIntegerValue()
        {
            // Testing Approach: String parsing - valid integer
            var result = LuaTypeConversion.StringToNumber("42");
            
            Assert.IsNotNull(result);
            Assert.AreEqual(LuaType.Integer, result.Value.Type);
            Assert.AreEqual(42L, result.Value.AsInteger());
        }

        [TestMethod]
        public void StringToNumber_ValidFloatString_ReturnsFloatValue()
        {
            // Testing Approach: String parsing - valid float
            var result = LuaTypeConversion.StringToNumber("3.14");
            
            Assert.IsNotNull(result);
            Assert.AreEqual(LuaType.Float, result.Value.Type);
            Assert.AreEqual(3.14, result.Value.AsFloat(), 0.0001);
        }

        [TestMethod]
        public void StringToNumber_NegativeInteger_ReturnsCorrectValue()
        {
            // Testing Approach: Boundary Value Analysis - negative integer
            var result = LuaTypeConversion.StringToNumber("-123");
            
            Assert.IsNotNull(result);
            Assert.AreEqual(LuaType.Integer, result.Value.Type);
            Assert.AreEqual(-123L, result.Value.AsInteger());
        }

        [TestMethod]
        public void StringToNumber_NegativeFloat_ReturnsCorrectValue()
        {
            // Testing Approach: Boundary Value Analysis - negative float
            var result = LuaTypeConversion.StringToNumber("-2.718");
            
            Assert.IsNotNull(result);
            Assert.AreEqual(LuaType.Float, result.Value.Type);
            Assert.AreEqual(-2.718, result.Value.AsFloat(), 0.0001);
        }

        [TestMethod]
        public void StringToNumber_ZeroString_ReturnsZeroInteger()
        {
            // Testing Approach: Boundary Value Analysis - zero
            var result = LuaTypeConversion.StringToNumber("0");
            
            Assert.IsNotNull(result);
            Assert.AreEqual(LuaType.Integer, result.Value.Type);
            Assert.AreEqual(0L, result.Value.AsInteger());
        }

        [TestMethod]
        public void StringToNumber_StringWithWhitespace_ReturnsCorrectValue()
        {
            // Testing Approach: Whitespace handling
            var result = LuaTypeConversion.StringToNumber("  42.5  ");
            
            Assert.IsNotNull(result);
            Assert.AreEqual(LuaType.Float, result.Value.Type);
            Assert.AreEqual(42.5, result.Value.AsFloat(), 0.0001);
        }

        [TestMethod]
        public void StringToNumber_HexadecimalString_ReturnsCorrectValue()
        {
            // Testing Approach: Hexadecimal number parsing
            var result = LuaTypeConversion.StringToNumber("0xFF");
            
            Assert.IsNotNull(result);
            Assert.AreEqual(LuaType.Integer, result.Value.Type);
            Assert.AreEqual(255L, result.Value.AsInteger());
        }

        [TestMethod]
        public void StringToNumber_ScientificNotation_ReturnsCorrectValue()
        {
            // Testing Approach: Scientific notation parsing
            var result = LuaTypeConversion.StringToNumber("1.23e2");
            
            Assert.IsNotNull(result);
            Assert.AreEqual(LuaType.Float, result.Value.Type);
            Assert.AreEqual(123.0, result.Value.AsFloat(), 0.0001);
        }

        [TestMethod]
        public void StringToNumber_InvalidString_ReturnsNull()
        {
            // Testing Approach: Error condition - invalid string returns null
            var result = LuaTypeConversion.StringToNumber("not a number");
            
            Assert.IsNull(result);
        }

        [TestMethod]
        public void StringToNumber_EmptyString_ReturnsNull()
        {
            // Testing Approach: Edge case - empty string
            var result = LuaTypeConversion.StringToNumber("");
            
            Assert.IsNull(result);
        }

        [TestMethod]
        public void StringToNumber_NullString_ReturnsNull()
        {
            // Testing Approach: Edge case - null string
            var result = LuaTypeConversion.StringToNumber(null);
            
            Assert.IsNull(result);
        }

        #endregion

        #region Integration and Edge Case Tests

        [TestMethod]
        public void TypeConversion_NumberRoundTrip_MaintainsValue()
        {
            // Testing Approach: Round-trip testing - number conversions
            var originalInt = 42L;
            var originalFloat = 3.14;
            
            // Convert to LuaValue then back
            var intValue = LuaValue.Integer(originalInt);
            var floatValue = LuaValue.Float(originalFloat);
            
            var convertedInt = LuaTypeConversion.ToInteger(intValue);
            var convertedFloat = LuaTypeConversion.ToNumber(floatValue);
            
            Assert.AreEqual(originalInt, convertedInt);
            Assert.AreEqual(originalFloat, convertedFloat);
        }

        [TestMethod]
        public void TypeConversion_StringNumberRoundTrip_MaintainsValue()
        {
            // Testing Approach: String-number conversion round trip
            var intString = "789";
            var floatString = "12.34";
            
            // String to LuaValue to number
            var intResult = LuaTypeConversion.StringToNumber(intString);
            var floatResult = LuaTypeConversion.StringToNumber(floatString);
            
            Assert.IsNotNull(intResult);
            Assert.IsNotNull(floatResult);
            
            var intNumber = LuaTypeConversion.ToNumber(intResult.Value);
            var floatNumber = LuaTypeConversion.ToNumber(floatResult.Value);
            
            Assert.AreEqual(789.0, intNumber);
            Assert.AreEqual(12.34, floatNumber);
        }

        [TestMethod]
        public void TypeConversion_BooleanTruthiness_FollowsLuaRules()
        {
            // Testing Approach: Lua truthiness rules verification
            // Only nil and false are falsy in Lua
            Assert.IsFalse(LuaTypeConversion.ToBoolean(LuaValue.Nil));
            Assert.IsFalse(LuaTypeConversion.ToBoolean(LuaValue.Boolean(false)));
            
            // Everything else is truthy
            Assert.IsTrue(LuaTypeConversion.ToBoolean(LuaValue.Boolean(true)));
            Assert.IsTrue(LuaTypeConversion.ToBoolean(LuaValue.Integer(0)));
            Assert.IsTrue(LuaTypeConversion.ToBoolean(LuaValue.Float(0.0)));
            Assert.IsTrue(LuaTypeConversion.ToBoolean(LuaValue.String("")));
            Assert.IsTrue(LuaTypeConversion.ToBoolean(LuaValue.String("false")));
        }

        [TestMethod]
        public void TypeConversion_ConsistentTypeNames_AllTypes()
        {
            // Testing Approach: Type name consistency verification
            var typeNames = new Dictionary<LuaValue, string>
            {
                { LuaValue.Nil, "nil" },
                { LuaValue.Boolean(true), "boolean" },
                { LuaValue.Integer(42), "number" },
                { LuaValue.Float(3.14), "number" },
                { LuaValue.String("test"), "string" },
                { LuaValue.Table(new LuaTable()), "table" }
            };
            
            foreach (var kvp in typeNames)
            {
                Assert.AreEqual(kvp.Value, LuaTypeConversion.GetTypeName(kvp.Key));
            }
        }

        [TestMethod]
        public void TypeConversion_Performance_ManyConversions()
        {
            // Testing Approach: Performance test - many conversions
            for (int i = 0; i < 1000; i++)
            {
                var stringNum = i.ToString();
                var luaValue = LuaTypeConversion.StringToNumber(stringNum);
                
                if (luaValue != null && luaValue.Value.Type == LuaType.Integer)
                {
                    var backToNumber = LuaTypeConversion.ToNumber(luaValue.Value);
                    var typeName = LuaTypeConversion.GetTypeName(luaValue.Value);
                    var boolValue = LuaTypeConversion.ToBoolean(luaValue.Value);
                    
                    Assert.AreEqual((double)i, backToNumber);
                    Assert.AreEqual("number", typeName);
                    Assert.IsTrue(boolValue); // Numbers are truthy in Lua
                }
            }
        }

        #endregion
    }
}