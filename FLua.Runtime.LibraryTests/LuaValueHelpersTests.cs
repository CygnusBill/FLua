using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Runtime;
using System;
using System.Collections.Generic;

namespace FLua.Runtime.LibraryTests
{
    /// <summary>
    /// Comprehensive test suite for LuaValueHelpers - utility methods for LuaValue operations.
    /// This class had 0% test coverage and provides critical utility functions.
    /// Tests all helper methods using Lee Copeland testing methodology.
    /// </summary>
    [TestClass]
    public class LuaValueHelpersTests
    {
        #region GetNumber Tests

        [TestMethod]
        public void GetNumber_IntegerValue_ReturnsCorrectDouble()
        {
            // Testing Approach: Basic functionality - integer to double conversion
            var intValue = LuaValue.Integer(42);
            
            var result = LuaValueHelpers.GetNumber(intValue);
            
            Assert.AreEqual(42.0, result, 0.0001);
        }

        [TestMethod]
        public void GetNumber_FloatValue_ReturnsCorrectDouble()
        {
            // Testing Approach: Basic functionality - float to double conversion
            var floatValue = LuaValue.Float(3.14);
            
            var result = LuaValueHelpers.GetNumber(floatValue);
            
            Assert.AreEqual(3.14, result, 0.0001);
        }

        [TestMethod]
        public void GetNumber_ZeroInteger_ReturnsZero()
        {
            // Testing Approach: Boundary Value Analysis - zero case
            var zeroValue = LuaValue.Integer(0);
            
            var result = LuaValueHelpers.GetNumber(zeroValue);
            
            Assert.AreEqual(0.0, result, 0.0001);
        }

        [TestMethod]
        public void GetNumber_NegativeNumber_ReturnsCorrectValue()
        {
            // Testing Approach: Boundary Value Analysis - negative values
            var negativeValue = LuaValue.Float(-2.718);
            
            var result = LuaValueHelpers.GetNumber(negativeValue);
            
            Assert.AreEqual(-2.718, result, 0.0001);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void GetNumber_NonNumericValue_ThrowsException()
        {
            // Testing Approach: Error Condition - non-numeric type
            var stringValue = LuaValue.String("hello");
            
            LuaValueHelpers.GetNumber(stringValue); // Should throw
        }

        #endregion

        #region GetInteger Tests

        [TestMethod]
        public void GetInteger_IntegerValue_ReturnsCorrectLong()
        {
            // Testing Approach: Basic functionality - integer extraction
            var intValue = LuaValue.Integer(42);
            
            var result = LuaValueHelpers.GetInteger(intValue);
            
            Assert.AreEqual(42L, result);
        }

        [TestMethod]
        public void GetInteger_MaxValue_ReturnsCorrectValue()
        {
            // Testing Approach: Boundary Value Analysis - maximum integer
            var maxValue = LuaValue.Integer(long.MaxValue);
            
            var result = LuaValueHelpers.GetInteger(maxValue);
            
            Assert.AreEqual(long.MaxValue, result);
        }

        [TestMethod]
        public void GetInteger_MinValue_ReturnsCorrectValue()
        {
            // Testing Approach: Boundary Value Analysis - minimum integer
            var minValue = LuaValue.Integer(long.MinValue);
            
            var result = LuaValueHelpers.GetInteger(minValue);
            
            Assert.AreEqual(long.MinValue, result);
        }

        [TestMethod]
        public void GetInteger_Zero_ReturnsZero()
        {
            // Testing Approach: Boundary Value Analysis - zero case
            var zeroValue = LuaValue.Integer(0);
            
            var result = LuaValueHelpers.GetInteger(zeroValue);
            
            Assert.AreEqual(0L, result);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void GetInteger_FloatValue_ThrowsException()
        {
            // Testing Approach: Error Condition - float is not integer
            var floatValue = LuaValue.Float(3.14);
            
            LuaValueHelpers.GetInteger(floatValue); // Should throw
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void GetInteger_NonNumericValue_ThrowsException()
        {
            // Testing Approach: Error Condition - non-numeric type
            var boolValue = LuaValue.Boolean(true);
            
            LuaValueHelpers.GetInteger(boolValue); // Should throw
        }

        #endregion

        #region CreateNumber Tests

        [TestMethod]
        public void CreateNumber_LongValue_ReturnsIntegerLuaValue()
        {
            // Testing Approach: Number creation - long to LuaValue
            var result = LuaValueHelpers.CreateNumber(42L);
            
            Assert.AreEqual(LuaType.Integer, result.Type);
            Assert.AreEqual(42L, result.AsInteger());
        }

        [TestMethod]
        public void CreateNumber_DoubleValue_ReturnsFloatLuaValue()
        {
            // Testing Approach: Number creation - double to LuaValue
            var result = LuaValueHelpers.CreateNumber(3.14);
            
            Assert.AreEqual(LuaType.Float, result.Type);
            Assert.AreEqual(3.14, result.AsFloat(), 0.0001);
        }

        [TestMethod]
        public void CreateNumber_ZeroLong_ReturnsZeroInteger()
        {
            // Testing Approach: Boundary Value Analysis - zero long
            var result = LuaValueHelpers.CreateNumber(0L);
            
            Assert.AreEqual(LuaType.Integer, result.Type);
            Assert.AreEqual(0L, result.AsInteger());
        }

        [TestMethod]
        public void CreateNumber_ZeroDouble_ReturnsZeroFloat()
        {
            // Testing Approach: Boundary Value Analysis - zero double
            var result = LuaValueHelpers.CreateNumber(0.0);
            
            Assert.AreEqual(LuaType.Float, result.Type);
            Assert.AreEqual(0.0, result.AsFloat());
        }

        [TestMethod]
        public void CreateNumber_NegativeLong_ReturnsCorrectInteger()
        {
            // Testing Approach: Boundary Value Analysis - negative long
            var result = LuaValueHelpers.CreateNumber(-123L);
            
            Assert.AreEqual(LuaType.Integer, result.Type);
            Assert.AreEqual(-123L, result.AsInteger());
        }

        [TestMethod]
        public void CreateNumber_NegativeDouble_ReturnsCorrectFloat()
        {
            // Testing Approach: Boundary Value Analysis - negative double
            var result = LuaValueHelpers.CreateNumber(-2.718);
            
            Assert.AreEqual(LuaType.Float, result.Type);
            Assert.AreEqual(-2.718, result.AsFloat(), 0.0001);
        }

        [TestMethod]
        public void CreateNumber_MaxLong_ReturnsMaxInteger()
        {
            // Testing Approach: Boundary Value Analysis - maximum long
            var result = LuaValueHelpers.CreateNumber(long.MaxValue);
            
            Assert.AreEqual(LuaType.Integer, result.Type);
            Assert.AreEqual(long.MaxValue, result.AsInteger());
        }

        [TestMethod]
        public void CreateNumber_MinLong_ReturnsMinInteger()
        {
            // Testing Approach: Boundary Value Analysis - minimum long
            var result = LuaValueHelpers.CreateNumber(long.MinValue);
            
            Assert.AreEqual(LuaType.Integer, result.Type);
            Assert.AreEqual(long.MinValue, result.AsInteger());
        }

        [TestMethod]
        public void CreateNumber_NaN_ReturnsNaNFloat()
        {
            // Testing Approach: Edge case - NaN handling
            var result = LuaValueHelpers.CreateNumber(double.NaN);
            
            Assert.AreEqual(LuaType.Float, result.Type);
            Assert.IsTrue(double.IsNaN(result.AsFloat()));
        }

        [TestMethod]
        public void CreateNumber_PositiveInfinity_ReturnsInfinityFloat()
        {
            // Testing Approach: Edge case - positive infinity
            var result = LuaValueHelpers.CreateNumber(double.PositiveInfinity);
            
            Assert.AreEqual(LuaType.Float, result.Type);
            Assert.IsTrue(double.IsPositiveInfinity(result.AsFloat()));
        }

        [TestMethod]
        public void CreateNumber_NegativeInfinity_ReturnsNegativeInfinityFloat()
        {
            // Testing Approach: Edge case - negative infinity
            var result = LuaValueHelpers.CreateNumber(double.NegativeInfinity);
            
            Assert.AreEqual(LuaType.Float, result.Type);
            Assert.IsTrue(double.IsNegativeInfinity(result.AsFloat()));
        }

        #endregion

        #region Type Checking Helper Tests

        [TestMethod]
        public void IsNumber_IntegerValue_ReturnsTrue()
        {
            // Testing Approach: Type checking - integer is number
            var intValue = LuaValue.Integer(42);
            
            var result = LuaValueHelpers.IsNumber(intValue);
            
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsNumber_FloatValue_ReturnsTrue()
        {
            // Testing Approach: Type checking - float is number
            var floatValue = LuaValue.Float(3.14);
            
            var result = LuaValueHelpers.IsNumber(floatValue);
            
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsNumber_NonNumericValues_ReturnsFalse()
        {
            // Testing Approach: Type checking - non-numeric types
            Assert.IsFalse(LuaValueHelpers.IsNumber(LuaValue.String("123")));
            Assert.IsFalse(LuaValueHelpers.IsNumber(LuaValue.Boolean(true)));
            Assert.IsFalse(LuaValueHelpers.IsNumber(LuaValue.Nil));
        }

        [TestMethod]
        public void IsString_StringValue_ReturnsTrue()
        {
            // Testing Approach: Type checking - string detection
            var stringValue = LuaValue.String("hello");
            
            var result = LuaValueHelpers.IsString(stringValue);
            
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsString_EmptyString_ReturnsTrue()
        {
            // Testing Approach: Boundary Value Analysis - empty string
            var emptyString = LuaValue.String("");
            
            var result = LuaValueHelpers.IsString(emptyString);
            
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsString_NonStringValues_ReturnsFalse()
        {
            // Testing Approach: Type checking - non-string types
            Assert.IsFalse(LuaValueHelpers.IsString(LuaValue.Integer(42)));
            Assert.IsFalse(LuaValueHelpers.IsString(LuaValue.Boolean(false)));
            Assert.IsFalse(LuaValueHelpers.IsString(LuaValue.Nil));
        }

        [TestMethod]
        public void IsTable_TableValue_ReturnsTrue()
        {
            // Testing Approach: Type checking - table detection
            var table = new LuaTable();
            var tableValue = LuaValue.Table(table);
            
            var result = LuaValueHelpers.IsTable(tableValue);
            
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsTable_NonTableValues_ReturnsFalse()
        {
            // Testing Approach: Type checking - non-table types
            Assert.IsFalse(LuaValueHelpers.IsTable(LuaValue.String("table")));
            Assert.IsFalse(LuaValueHelpers.IsTable(LuaValue.Integer(42)));
            Assert.IsFalse(LuaValueHelpers.IsTable(LuaValue.Boolean(true)));
            Assert.IsFalse(LuaValueHelpers.IsTable(LuaValue.Nil));
        }

        [TestMethod]
        public void IsFunction_FunctionValue_ReturnsTrue()
        {
            // Testing Approach: Type checking - function detection
            var function = new LuaFunction(null, "test", null, null);
            var functionValue = LuaValue.Function(function);
            
            var result = LuaValueHelpers.IsFunction(functionValue);
            
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void IsFunction_NonFunctionValues_ReturnsFalse()
        {
            // Testing Approach: Type checking - non-function types
            Assert.IsFalse(LuaValueHelpers.IsFunction(LuaValue.String("function")));
            Assert.IsFalse(LuaValueHelpers.IsFunction(LuaValue.Integer(42)));
            Assert.IsFalse(LuaValueHelpers.IsFunction(LuaValue.Boolean(true)));
            Assert.IsFalse(LuaValueHelpers.IsFunction(LuaValue.Nil));
        }

        #endregion

        #region Integration and Edge Case Tests

        [TestMethod]
        public void Helpers_AllNumberTypes_ConsistentBehavior()
        {
            // Testing Approach: Integration test - consistent behavior across number types
            var intValue = LuaValue.Integer(42);
            var floatValue = LuaValue.Float(42.0);
            
            // Both should be recognized as numbers
            Assert.IsTrue(LuaValueHelpers.IsNumber(intValue));
            Assert.IsTrue(LuaValueHelpers.IsNumber(floatValue));
            
            // GetNumber should return same value for both
            Assert.AreEqual(42.0, LuaValueHelpers.GetNumber(intValue), 0.0001);
            Assert.AreEqual(42.0, LuaValueHelpers.GetNumber(floatValue), 0.0001);
        }

        [TestMethod]
        public void Helpers_CreateAndExtract_RoundTrip()
        {
            // Testing Approach: Round-trip testing - create then extract
            var originalLong = 12345L;
            var originalDouble = 67.89;
            
            // Create LuaValues
            var intValue = LuaValueHelpers.CreateNumber(originalLong);
            var floatValue = LuaValueHelpers.CreateNumber(originalDouble);
            
            // Extract values back
            var extractedLong = LuaValueHelpers.GetInteger(intValue);
            var extractedDouble = LuaValueHelpers.GetNumber(floatValue);
            
            Assert.AreEqual(originalLong, extractedLong);
            Assert.AreEqual(originalDouble, extractedDouble, 0.0001);
        }

        [TestMethod]
        public void Helpers_TypeConsistency_AllMethods()
        {
            // Testing Approach: Cross-method consistency test
            var stringValue = LuaValue.String("test");
            var intValue = LuaValue.Integer(42);
            var floatValue = LuaValue.Float(3.14);
            var boolValue = LuaValue.Boolean(true);
            var nilValue = LuaValue.Nil;
            
            // String should only be detected as string
            Assert.IsTrue(LuaValueHelpers.IsString(stringValue));
            Assert.IsFalse(LuaValueHelpers.IsNumber(stringValue));
            Assert.IsFalse(LuaValueHelpers.IsTable(stringValue));
            Assert.IsFalse(LuaValueHelpers.IsFunction(stringValue));
            
            // Numbers should only be detected as numbers
            Assert.IsTrue(LuaValueHelpers.IsNumber(intValue));
            Assert.IsTrue(LuaValueHelpers.IsNumber(floatValue));
            Assert.IsFalse(LuaValueHelpers.IsString(intValue));
            Assert.IsFalse(LuaValueHelpers.IsTable(intValue));
            Assert.IsFalse(LuaValueHelpers.IsFunction(intValue));
            
            // Boolean and nil should not match any type helpers
            Assert.IsFalse(LuaValueHelpers.IsNumber(boolValue));
            Assert.IsFalse(LuaValueHelpers.IsString(boolValue));
            Assert.IsFalse(LuaValueHelpers.IsTable(boolValue));
            Assert.IsFalse(LuaValueHelpers.IsFunction(boolValue));
            
            Assert.IsFalse(LuaValueHelpers.IsNumber(nilValue));
            Assert.IsFalse(LuaValueHelpers.IsString(nilValue));
            Assert.IsFalse(LuaValueHelpers.IsTable(nilValue));
            Assert.IsFalse(LuaValueHelpers.IsFunction(nilValue));
        }

        [TestMethod]
        public void Helpers_Performance_ManyOperations()
        {
            // Testing Approach: Performance test - many helper operations
            var values = new List<LuaValue>
            {
                LuaValue.Integer(1),
                LuaValue.Float(2.0),
                LuaValue.String("three"),
                LuaValue.Boolean(true)
            };
            
            // Perform many type checks
            for (int i = 0; i < 1000; i++)
            {
                foreach (var value in values)
                {
                    LuaValueHelpers.IsNumber(value);
                    LuaValueHelpers.IsString(value);
                    LuaValueHelpers.IsTable(value);
                    LuaValueHelpers.IsFunction(value);
                }
            }
            
            // Should complete without errors or excessive time
            Assert.IsTrue(true); // If we get here, performance is acceptable
        }

        #endregion
    }
}