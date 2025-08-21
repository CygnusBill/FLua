using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Runtime;
using System;
using System.Collections.Generic;

namespace FLua.Runtime.LibraryTests
{
    /// <summary>
    /// Comprehensive test suite for LuaValue - the core value type in FLua Runtime.
    /// Tests all constructors, type checking, value retrieval, arithmetic operations,
    /// comparisons, conversions, and edge cases using Lee Copeland testing methodology.
    /// </summary>
    [TestClass]
    public class LuaValueTests
    {
        #region Constructor and Factory Method Tests
        
        [TestMethod]
        public void Nil_CreateNil_ReturnsNilValue()
        {
            // Testing Approach: Basic functionality - nil value creation
            var nilValue = LuaValue.CreateNil();
            
            Assert.AreEqual(LuaType.Nil, nilValue.Type);
            Assert.IsTrue(nilValue.IsNil);
        }
        
        [TestMethod]
        public void Nil_StaticField_ReturnsNilValue()
        {
            // Testing Approach: Static field verification
            var nilValue = LuaValue.Nil;
            
            Assert.AreEqual(LuaType.Nil, nilValue.Type);
            Assert.IsTrue(nilValue.IsNil);
        }

        [TestMethod]
        public void Boolean_True_ReturnsCorrectValue()
        {
            // Testing Approach: Boolean value creation - true case
            var boolValue = LuaValue.Boolean(true);
            
            Assert.AreEqual(LuaType.Boolean, boolValue.Type);
            Assert.IsTrue(boolValue.IsBoolean);
            Assert.IsTrue(boolValue.AsBoolean());
        }

        [TestMethod]
        public void Boolean_False_ReturnsCorrectValue()
        {
            // Testing Approach: Boolean value creation - false case
            var boolValue = LuaValue.Boolean(false);
            
            Assert.AreEqual(LuaType.Boolean, boolValue.Type);
            Assert.IsTrue(boolValue.IsBoolean);
            Assert.IsFalse(boolValue.AsBoolean());
        }

        [TestMethod]
        public void Integer_PositiveValue_ReturnsCorrectValue()
        {
            // Testing Approach: Integer value creation - positive boundary
            var intValue = LuaValue.Integer(42);
            
            Assert.AreEqual(LuaType.Integer, intValue.Type);
            Assert.IsTrue(intValue.IsInteger);
            Assert.AreEqual(42L, intValue.AsInteger());
        }

        [TestMethod]
        public void Integer_NegativeValue_ReturnsCorrectValue()
        {
            // Testing Approach: Integer value creation - negative boundary
            var intValue = LuaValue.Integer(-123);
            
            Assert.AreEqual(LuaType.Integer, intValue.Type);
            Assert.IsTrue(intValue.IsInteger);
            Assert.AreEqual(-123L, intValue.AsInteger());
        }

        [TestMethod]
        public void Integer_Zero_ReturnsCorrectValue()
        {
            // Testing Approach: Boundary Value Analysis - zero case
            var intValue = LuaValue.Integer(0);
            
            Assert.AreEqual(LuaType.Integer, intValue.Type);
            Assert.IsTrue(intValue.IsInteger);
            Assert.AreEqual(0L, intValue.AsInteger());
        }

        [TestMethod]
        public void Integer_MaxValue_ReturnsCorrectValue()
        {
            // Testing Approach: Boundary Value Analysis - maximum integer
            var intValue = LuaValue.Integer(long.MaxValue);
            
            Assert.AreEqual(LuaType.Integer, intValue.Type);
            Assert.IsTrue(intValue.IsInteger);
            Assert.AreEqual(long.MaxValue, intValue.AsInteger());
        }

        [TestMethod]
        public void Integer_MinValue_ReturnsCorrectValue()
        {
            // Testing Approach: Boundary Value Analysis - minimum integer
            var intValue = LuaValue.Integer(long.MinValue);
            
            Assert.AreEqual(LuaType.Integer, intValue.Type);
            Assert.IsTrue(intValue.IsInteger);
            Assert.AreEqual(long.MinValue, intValue.AsInteger());
        }

        [TestMethod]
        public void Float_PositiveValue_ReturnsCorrectValue()
        {
            // Testing Approach: Float value creation - positive case
            var floatValue = LuaValue.Float(3.14);
            
            Assert.AreEqual(LuaType.Float, floatValue.Type);
            Assert.IsTrue(floatValue.IsFloat);
            Assert.AreEqual(3.14, floatValue.AsFloat(), 0.0001);
        }

        [TestMethod]
        public void Float_NegativeValue_ReturnsCorrectValue()
        {
            // Testing Approach: Float value creation - negative case
            var floatValue = LuaValue.Float(-2.718);
            
            Assert.AreEqual(LuaType.Float, floatValue.Type);
            Assert.IsTrue(floatValue.IsFloat);
            Assert.AreEqual(-2.718, floatValue.AsFloat(), 0.0001);
        }

        [TestMethod]
        public void Float_Zero_ReturnsCorrectValue()
        {
            // Testing Approach: Boundary Value Analysis - zero float
            var floatValue = LuaValue.Float(0.0);
            
            Assert.AreEqual(LuaType.Float, floatValue.Type);
            Assert.IsTrue(floatValue.IsFloat);
            Assert.AreEqual(0.0, floatValue.AsFloat());
        }

        [TestMethod]
        public void Float_NaN_ReturnsCorrectValue()
        {
            // Testing Approach: Edge case - NaN handling
            var floatValue = LuaValue.Float(double.NaN);
            
            Assert.AreEqual(LuaType.Float, floatValue.Type);
            Assert.IsTrue(floatValue.IsFloat);
            Assert.IsTrue(double.IsNaN(floatValue.AsFloat()));
        }

        [TestMethod]
        public void Float_PositiveInfinity_ReturnsCorrectValue()
        {
            // Testing Approach: Edge case - infinity handling
            var floatValue = LuaValue.Float(double.PositiveInfinity);
            
            Assert.AreEqual(LuaType.Float, floatValue.Type);
            Assert.IsTrue(floatValue.IsFloat);
            Assert.IsTrue(double.IsPositiveInfinity(floatValue.AsFloat()));
        }

        [TestMethod]
        public void Float_NegativeInfinity_ReturnsCorrectValue()
        {
            // Testing Approach: Edge case - negative infinity handling
            var floatValue = LuaValue.Float(double.NegativeInfinity);
            
            Assert.AreEqual(LuaType.Float, floatValue.Type);
            Assert.IsTrue(floatValue.IsFloat);
            Assert.IsTrue(double.IsNegativeInfinity(floatValue.AsFloat()));
        }

        [TestMethod]
        public void String_NormalString_ReturnsCorrectValue()
        {
            // Testing Approach: String value creation - normal case
            var strValue = LuaValue.String("hello world");
            
            Assert.AreEqual(LuaType.String, strValue.Type);
            Assert.IsTrue(strValue.IsString);
            Assert.AreEqual("hello world", strValue.AsString());
        }

        [TestMethod]
        public void String_EmptyString_ReturnsCorrectValue()
        {
            // Testing Approach: Boundary Value Analysis - empty string
            var strValue = LuaValue.String("");
            
            Assert.AreEqual(LuaType.String, strValue.Type);
            Assert.IsTrue(strValue.IsString);
            Assert.AreEqual("", strValue.AsString());
        }

        [TestMethod]
        public void String_NullString_ReturnsNilValue()
        {
            // Testing Approach: Error condition - null string handling
            var strValue = LuaValue.String(null);
            
            Assert.AreEqual(LuaType.Nil, strValue.Type);
            Assert.IsTrue(strValue.IsNil);
        }

        [TestMethod]
        public void String_UnicodeString_ReturnsCorrectValue()
        {
            // Testing Approach: Unicode support testing
            var unicodeString = "こんにちは世界 🌍 Ñoël";
            var strValue = LuaValue.String(unicodeString);
            
            Assert.AreEqual(LuaType.String, strValue.Type);
            Assert.IsTrue(strValue.IsString);
            Assert.AreEqual(unicodeString, strValue.AsString());
        }

        [TestMethod]
        public void String_LongString_ReturnsCorrectValue()
        {
            // Testing Approach: Boundary Value Analysis - large string
            var longString = new string('a', 10000);
            var strValue = LuaValue.String(longString);
            
            Assert.AreEqual(LuaType.String, strValue.Type);
            Assert.IsTrue(strValue.IsString);
            Assert.AreEqual(longString, strValue.AsString());
        }

        #endregion

        #region Type Checking Property Tests

        [TestMethod]
        public void IsNumber_IntegerValue_ReturnsTrue()
        {
            // Testing Approach: Type checking - integer as number
            var intValue = LuaValue.Integer(42);
            
            Assert.IsTrue(intValue.IsNumber);
            Assert.IsTrue(intValue.IsInteger);
            Assert.IsFalse(intValue.IsFloat);
        }

        [TestMethod]
        public void IsNumber_FloatValue_ReturnsTrue()
        {
            // Testing Approach: Type checking - float as number
            var floatValue = LuaValue.Float(3.14);
            
            Assert.IsTrue(floatValue.IsNumber);
            Assert.IsFalse(floatValue.IsInteger);
            Assert.IsTrue(floatValue.IsFloat);
        }

        [TestMethod]
        public void IsNumber_NonNumericTypes_ReturnsFalse()
        {
            // Testing Approach: Type checking - non-numeric types
            Assert.IsFalse(LuaValue.Boolean(true).IsNumber);
            Assert.IsFalse(LuaValue.String("123").IsNumber);
            Assert.IsFalse(LuaValue.Nil.IsNumber);
        }

        [TestMethod]
        public void TypeProperties_AllTypes_ReturnCorrectValues()
        {
            // Testing Approach: Equivalence Class Partitioning - all type checks
            var nilValue = LuaValue.Nil;
            var boolValue = LuaValue.Boolean(true);
            var intValue = LuaValue.Integer(42);
            var floatValue = LuaValue.Float(3.14);
            var stringValue = LuaValue.String("test");

            // Nil checks
            Assert.IsTrue(nilValue.IsNil);
            Assert.IsFalse(nilValue.IsBoolean);
            Assert.IsFalse(nilValue.IsInteger);
            Assert.IsFalse(nilValue.IsFloat);
            Assert.IsFalse(nilValue.IsNumber);
            Assert.IsFalse(nilValue.IsString);

            // Boolean checks
            Assert.IsFalse(boolValue.IsNil);
            Assert.IsTrue(boolValue.IsBoolean);
            Assert.IsFalse(boolValue.IsInteger);
            Assert.IsFalse(boolValue.IsFloat);
            Assert.IsFalse(boolValue.IsNumber);
            Assert.IsFalse(boolValue.IsString);

            // Integer checks
            Assert.IsFalse(intValue.IsNil);
            Assert.IsFalse(intValue.IsBoolean);
            Assert.IsTrue(intValue.IsInteger);
            Assert.IsFalse(intValue.IsFloat);
            Assert.IsTrue(intValue.IsNumber);
            Assert.IsFalse(intValue.IsString);

            // Float checks
            Assert.IsFalse(floatValue.IsNil);
            Assert.IsFalse(floatValue.IsBoolean);
            Assert.IsFalse(floatValue.IsInteger);
            Assert.IsTrue(floatValue.IsFloat);
            Assert.IsTrue(floatValue.IsNumber);
            Assert.IsFalse(floatValue.IsString);

            // String checks
            Assert.IsFalse(stringValue.IsNil);
            Assert.IsFalse(stringValue.IsBoolean);
            Assert.IsFalse(stringValue.IsInteger);
            Assert.IsFalse(stringValue.IsFloat);
            Assert.IsFalse(stringValue.IsNumber);
            Assert.IsTrue(stringValue.IsString);
        }

        #endregion

        #region Arithmetic Operation Tests

        [TestMethod]
        public void Add_IntegerInteger_ReturnsCorrectSum()
        {
            // Testing Approach: Arithmetic operations - integer addition
            var left = LuaValue.Integer(10);
            var right = LuaValue.Integer(20);
            
            var result = left.Add(right);
            
            Assert.AreEqual(LuaType.Integer, result.Type);
            Assert.AreEqual(30L, result.AsInteger());
        }

        [TestMethod]
        public void Add_IntegerFloat_ReturnsFloatSum()
        {
            // Testing Approach: Mixed type arithmetic - integer + float
            var left = LuaValue.Integer(10);
            var right = LuaValue.Float(3.14);
            
            var result = left.Add(right);
            
            Assert.AreEqual(LuaType.Float, result.Type);
            Assert.AreEqual(13.14, result.AsFloat(), 0.0001);
        }

        [TestMethod]
        public void Add_FloatFloat_ReturnsFloatSum()
        {
            // Testing Approach: Float arithmetic
            var left = LuaValue.Float(3.14);
            var right = LuaValue.Float(2.86);
            
            var result = left.Add(right);
            
            Assert.AreEqual(LuaType.Float, result.Type);
            Assert.AreEqual(6.0, result.AsFloat(), 0.0001);
        }

        [TestMethod]
        public void Add_OperatorOverload_WorksCorrectly()
        {
            // Testing Approach: Operator overload verification
            var left = LuaValue.Integer(15);
            var right = LuaValue.Integer(25);
            
            var result = left + right;
            
            Assert.AreEqual(LuaType.Integer, result.Type);
            Assert.AreEqual(40L, result.AsInteger());
        }

        [TestMethod]
        public void Subtract_IntegerInteger_ReturnsCorrectDifference()
        {
            // Testing Approach: Subtraction operation
            var left = LuaValue.Integer(50);
            var right = LuaValue.Integer(30);
            
            var result = left.Subtract(right);
            
            Assert.AreEqual(LuaType.Integer, result.Type);
            Assert.AreEqual(20L, result.AsInteger());
        }

        [TestMethod]
        public void Subtract_NegativeResult_ReturnsCorrectValue()
        {
            // Testing Approach: Subtraction resulting in negative
            var left = LuaValue.Integer(10);
            var right = LuaValue.Integer(30);
            
            var result = left.Subtract(right);
            
            Assert.AreEqual(LuaType.Integer, result.Type);
            Assert.AreEqual(-20L, result.AsInteger());
        }

        [TestMethod]
        public void Multiply_IntegerInteger_ReturnsCorrectProduct()
        {
            // Testing Approach: Multiplication operation
            var left = LuaValue.Integer(6);
            var right = LuaValue.Integer(7);
            
            var result = left.Multiply(right);
            
            Assert.AreEqual(LuaType.Integer, result.Type);
            Assert.AreEqual(42L, result.AsInteger());
        }

        [TestMethod]
        public void Multiply_IntegerFloat_ReturnsFloatProduct()
        {
            // Testing Approach: Mixed type multiplication
            var left = LuaValue.Integer(5);
            var right = LuaValue.Float(2.5);
            
            var result = left.Multiply(right);
            
            Assert.AreEqual(LuaType.Float, result.Type);
            Assert.AreEqual(12.5, result.AsFloat(), 0.0001);
        }

        [TestMethod]
        public void Divide_IntegerInteger_ReturnsFloatQuotient()
        {
            // Testing Approach: Division always returns float in Lua
            var left = LuaValue.Integer(10);
            var right = LuaValue.Integer(4);
            
            var result = left.Divide(right);
            
            Assert.AreEqual(LuaType.Float, result.Type);
            Assert.AreEqual(2.5, result.AsFloat(), 0.0001);
        }

        [TestMethod]
        public void FloorDivide_FloatFloat_ReturnsIntegerResult()
        {
            // Testing Approach: Floor division operation
            var left = LuaValue.Float(10.7);
            var right = LuaValue.Float(3.0);
            
            var result = left.FloorDivide(right);
            
            Assert.AreEqual(LuaType.Integer, result.Type);
            Assert.AreEqual(3L, result.AsInteger());
        }

        [TestMethod]
        public void Modulo_IntegerInteger_ReturnsCorrectRemainder()
        {
            // Testing Approach: Modulo operation
            var left = LuaValue.Integer(17);
            var right = LuaValue.Integer(5);
            
            var result = left.Modulo(right);
            
            Assert.AreEqual(LuaType.Integer, result.Type);
            Assert.AreEqual(2L, result.AsInteger());
        }

        [TestMethod]
        public void Power_IntegerInteger_ReturnsFloatResult()
        {
            // Testing Approach: Power operation (always returns float)
            var left = LuaValue.Integer(2);
            var right = LuaValue.Integer(3);
            
            var result = left.Power(right);
            
            Assert.AreEqual(LuaType.Float, result.Type);
            Assert.AreEqual(8.0, result.AsFloat(), 0.0001);
        }

        [TestMethod]
        public void UnaryMinus_PositiveInteger_ReturnsNegative()
        {
            // Testing Approach: Unary minus operation
            var value = LuaValue.Integer(42);
            
            var result = value.UnaryMinus();
            
            Assert.AreEqual(LuaType.Integer, result.Type);
            Assert.AreEqual(-42L, result.AsInteger());
        }

        [TestMethod]
        public void UnaryMinus_NegativeFloat_ReturnsPositive()
        {
            // Testing Approach: Unary minus on negative float
            var value = LuaValue.Float(-3.14);
            
            var result = value.UnaryMinus();
            
            Assert.AreEqual(LuaType.Float, result.Type);
            Assert.AreEqual(3.14, result.AsFloat(), 0.0001);
        }

        #endregion

        #region Comparison and Equality Tests

        [TestMethod]
        public void Equals_SameIntegerValues_ReturnsTrue()
        {
            // Testing Approach: Equality comparison - same integer values
            var left = LuaValue.Integer(42);
            var right = LuaValue.Integer(42);
            
            Assert.IsTrue(left.Equals(right));
            Assert.IsTrue(left == right);
            Assert.IsFalse(left != right);
        }

        [TestMethod]
        public void Equals_DifferentIntegerValues_ReturnsFalse()
        {
            // Testing Approach: Equality comparison - different integers
            var left = LuaValue.Integer(42);
            var right = LuaValue.Integer(24);
            
            Assert.IsFalse(left.Equals(right));
            Assert.IsFalse(left == right);
            Assert.IsTrue(left != right);
        }

        [TestMethod]
        public void Equals_SameStringValues_ReturnsTrue()
        {
            // Testing Approach: String equality
            var left = LuaValue.String("hello");
            var right = LuaValue.String("hello");
            
            Assert.IsTrue(left.Equals(right));
            Assert.IsTrue(left == right);
        }

        [TestMethod]
        public void Equals_DifferentTypes_ReturnsFalse()
        {
            // Testing Approach: Cross-type equality (should be false)
            var intValue = LuaValue.Integer(42);
            var stringValue = LuaValue.String("42");
            var floatValue = LuaValue.Float(42.0);
            
            Assert.IsFalse(intValue.Equals(stringValue));
            Assert.IsFalse(intValue.Equals(floatValue)); // Different types in Lua
            Assert.IsFalse(stringValue.Equals(floatValue));
        }

        [TestMethod]
        public void Equals_BothNil_ReturnsTrue()
        {
            // Testing Approach: Nil equality
            var left = LuaValue.Nil;
            var right = LuaValue.CreateNil();
            
            Assert.IsTrue(left.Equals(right));
            Assert.IsTrue(left == right);
        }

        [TestMethod]
        public void GetHashCode_SameValues_ReturnsSameHash()
        {
            // Testing Approach: Hash code consistency
            var left = LuaValue.String("test");
            var right = LuaValue.String("test");
            
            Assert.AreEqual(left.GetHashCode(), right.GetHashCode());
        }

        [TestMethod]
        public void GetHashCode_DifferentValues_ReturnsDifferentHashes()
        {
            // Testing Approach: Hash code uniqueness (best effort)
            var value1 = LuaValue.String("test1");
            var value2 = LuaValue.String("test2");
            var value3 = LuaValue.Integer(42);
            
            // Note: Hash codes can collide, but different values should usually have different hashes
            Assert.AreNotEqual(value1.GetHashCode(), value2.GetHashCode());
            Assert.AreNotEqual(value1.GetHashCode(), value3.GetHashCode());
        }

        #endregion

        #region Truthiness Tests

        [TestMethod]
        public void IsTruthy_NilValue_ReturnsFalse()
        {
            // Testing Approach: Lua truthiness - nil is falsy
            var nilValue = LuaValue.Nil;
            
            Assert.IsFalse(nilValue.IsTruthy());
        }

        [TestMethod]
        public void IsTruthy_FalseBoolean_ReturnsFalse()
        {
            // Testing Approach: Lua truthiness - false boolean is falsy
            var falseValue = LuaValue.Boolean(false);
            
            Assert.IsFalse(falseValue.IsTruthy());
        }

        [TestMethod]
        public void IsTruthy_TrueBoolean_ReturnsTrue()
        {
            // Testing Approach: Lua truthiness - true boolean is truthy
            var trueValue = LuaValue.Boolean(true);
            
            Assert.IsTrue(trueValue.IsTruthy());
        }

        [TestMethod]
        public void IsTruthy_AllOtherTypes_ReturnTrue()
        {
            // Testing Approach: Lua truthiness - everything except nil and false is truthy
            Assert.IsTrue(LuaValue.Integer(0).IsTruthy()); // 0 is truthy in Lua
            Assert.IsTrue(LuaValue.Float(0.0).IsTruthy()); // 0.0 is truthy in Lua
            Assert.IsTrue(LuaValue.String("").IsTruthy()); // Empty string is truthy in Lua
            Assert.IsTrue(LuaValue.String("false").IsTruthy()); // String "false" is truthy
        }

        #endregion

        #region Type Conversion Tests

        [TestMethod]
        public void TryGetInteger_IntegerValue_ReturnsTrue()
        {
            // Testing Approach: Type conversion - integer to integer
            var intValue = LuaValue.Integer(42);
            
            var success = intValue.TryGetInteger(out var result);
            
            Assert.IsTrue(success);
            Assert.AreEqual(42L, result);
        }

        [TestMethod]
        public void TryGetInteger_NonIntegerValue_ReturnsFalse()
        {
            // Testing Approach: Type conversion failure
            var stringValue = LuaValue.String("hello");
            
            var success = stringValue.TryGetInteger(out var result);
            
            Assert.IsFalse(success);
            Assert.AreEqual(0L, result);
        }

        [TestMethod]
        public void TryGetFloat_FloatValue_ReturnsTrue()
        {
            // Testing Approach: Float type conversion
            var floatValue = LuaValue.Float(3.14);
            
            var success = floatValue.TryGetFloat(out var result);
            
            Assert.IsTrue(success);
            Assert.AreEqual(3.14, result, 0.0001);
        }

        [TestMethod]
        public void TryGetFloat_IntegerValue_ReturnsTrue()
        {
            // Testing Approach: Integer to float conversion
            var intValue = LuaValue.Integer(42);
            
            var success = intValue.TryGetFloat(out var result);
            
            Assert.IsTrue(success);
            Assert.AreEqual(42.0, result, 0.0001);
        }

        [TestMethod]
        public void TryGetString_StringValue_ReturnsTrue()
        {
            // Testing Approach: String type conversion
            var stringValue = LuaValue.String("hello");
            
            var success = stringValue.TryGetString(out var result);
            
            Assert.IsTrue(success);
            Assert.AreEqual("hello", result);
        }

        [TestMethod]
        public void TryGetBoolean_BooleanValue_ReturnsTrue()
        {
            // Testing Approach: Boolean type conversion
            var boolValue = LuaValue.Boolean(true);
            
            var success = boolValue.TryGetBoolean(out var result);
            
            Assert.IsTrue(success);
            Assert.IsTrue(result);
        }

        #endregion

        #region Number Conversion Tests (Lua-style)

        [TestMethod]
        public void TryToNumber_StringWithInteger_ReturnsInteger()
        {
            // Testing Approach: Lua number conversion - string to integer
            var stringValue = LuaValue.String("42");
            
            var success = stringValue.TryToNumber(out var result);
            
            Assert.IsTrue(success);
            Assert.AreEqual(LuaType.Integer, result.Type);
            Assert.AreEqual(42L, result.AsInteger());
        }

        [TestMethod]
        public void TryToNumber_StringWithFloat_ReturnsFloat()
        {
            // Testing Approach: Lua number conversion - string to float
            var stringValue = LuaValue.String("3.14");
            
            var success = stringValue.TryToNumber(out var result);
            
            Assert.IsTrue(success);
            Assert.AreEqual(LuaType.Float, result.Type);
            Assert.AreEqual(3.14, result.AsFloat(), 0.0001);
        }

        [TestMethod]
        public void TryToNumber_StringWithWhitespace_ReturnsNumber()
        {
            // Testing Approach: Lua number conversion with whitespace
            var stringValue = LuaValue.String("  42  ");
            
            var success = stringValue.TryToNumber(out var result);
            
            Assert.IsTrue(success);
            Assert.AreEqual(LuaType.Integer, result.Type);
            Assert.AreEqual(42L, result.AsInteger());
        }

        [TestMethod]
        public void TryToNumber_InvalidString_ReturnsFalse()
        {
            // Testing Approach: Error condition - invalid number string
            var stringValue = LuaValue.String("not a number");
            
            var success = stringValue.TryToNumber(out var result);
            
            Assert.IsFalse(success);
            Assert.AreEqual(LuaType.Nil, result.Type);
        }

        [TestMethod]
        public void TryToInteger_FloatWithIntegerValue_ReturnsInteger()
        {
            // Testing Approach: Float to integer conversion (whole numbers)
            var floatValue = LuaValue.Float(42.0);
            
            var success = floatValue.TryToInteger(out var result);
            
            Assert.IsTrue(success);
            Assert.AreEqual(42L, result);
        }

        [TestMethod]
        public void TryToInteger_FloatWithFractionalPart_ReturnsFalse()
        {
            // Testing Approach: Float to integer conversion failure (fractional)
            var floatValue = LuaValue.Float(42.5);
            
            var success = floatValue.TryToInteger(out var result);
            
            Assert.IsFalse(success);
            Assert.AreEqual(0L, result);
        }

        #endregion

        #region ToString and String Representation Tests

        [TestMethod]
        public void ToLuaString_IntegerValue_ReturnsCorrectString()
        {
            // Testing Approach: Lua-style string representation
            var intValue = LuaValue.Integer(42);
            
            var luaString = intValue.ToLuaString();
            
            Assert.AreEqual("42", luaString);
        }

        [TestMethod]
        public void ToLuaString_FloatValue_ReturnsCorrectString()
        {
            // Testing Approach: Lua-style float representation
            var floatValue = LuaValue.Float(3.14);
            
            var luaString = floatValue.ToLuaString();
            
            // Lua format may vary, but should represent the number
            Assert.IsTrue(luaString.Contains("3.14") || luaString == "3.14");
        }

        [TestMethod]
        public void ToLuaString_StringValue_ReturnsString()
        {
            // Testing Approach: String to Lua string representation
            var stringValue = LuaValue.String("hello");
            
            var luaString = stringValue.ToLuaString();
            
            Assert.AreEqual("hello", luaString);
        }

        [TestMethod]
        public void ToLuaString_BooleanValues_ReturnsCorrectStrings()
        {
            // Testing Approach: Boolean to Lua string
            var trueValue = LuaValue.Boolean(true);
            var falseValue = LuaValue.Boolean(false);
            
            Assert.AreEqual("true", trueValue.ToLuaString());
            Assert.AreEqual("false", falseValue.ToLuaString());
        }

        [TestMethod]
        public void ToLuaString_NilValue_ReturnsNilString()
        {
            // Testing Approach: Nil to Lua string
            var nilValue = LuaValue.Nil;
            
            var luaString = nilValue.ToLuaString();
            
            Assert.AreEqual("nil", luaString);
        }

        #endregion

        #region Implicit Conversion Tests

        [TestMethod]
        public void ImplicitConversion_IntToLuaValue_WorksCorrectly()
        {
            // Testing Approach: Implicit conversion operators
            LuaValue value = 42;
            
            Assert.AreEqual(LuaType.Integer, value.Type);
            Assert.AreEqual(42L, value.AsInteger());
        }

        [TestMethod]
        public void ImplicitConversion_DoubleToLuaValue_WorksCorrectly()
        {
            // Testing Approach: Implicit conversion from double
            LuaValue value = 3.14;
            
            Assert.AreEqual(LuaType.Float, value.Type);
            Assert.AreEqual(3.14, value.AsFloat(), 0.0001);
        }

        [TestMethod]
        public void ImplicitConversion_StringToLuaValue_WorksCorrectly()
        {
            // Testing Approach: Implicit conversion from string
            LuaValue value = "hello";
            
            Assert.AreEqual(LuaType.String, value.Type);
            Assert.AreEqual("hello", value.AsString());
        }

        [TestMethod]
        public void ImplicitConversion_BoolToLuaValue_WorksCorrectly()
        {
            // Testing Approach: Implicit conversion from bool
            LuaValue trueValue = true;
            LuaValue falseValue = false;
            
            Assert.AreEqual(LuaType.Boolean, trueValue.Type);
            Assert.IsTrue(trueValue.AsBoolean());
            
            Assert.AreEqual(LuaType.Boolean, falseValue.Type);
            Assert.IsFalse(falseValue.AsBoolean());
        }

        #endregion

        #region Edge Cases and Error Conditions

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void AsBoolean_NonBooleanValue_ThrowsException()
        {
            // Testing Approach: Error condition - wrong type access
            var intValue = LuaValue.Integer(42);
            
            intValue.AsBoolean(); // Should throw
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void AsInteger_NonIntegerValue_ThrowsException()
        {
            // Testing Approach: Error condition - wrong type access
            var stringValue = LuaValue.String("hello");
            
            stringValue.AsInteger(); // Should throw
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void AsString_NilValue_ThrowsException()
        {
            // Testing Approach: Error condition - nil to string
            var nilValue = LuaValue.Nil;
            
            nilValue.AsString(); // Should throw
        }

        [TestMethod]
        public void AsIntegerValue_IntegerValue_ReturnsValue()
        {
            // Testing Approach: AsIntegerValue vs AsInteger difference
            var intValue = LuaValue.Integer(42);
            
            var result = intValue.AsIntegerValue();
            
            Assert.AreEqual(42L, result);
        }

        [TestMethod]
        public void AsDouble_FloatValue_ReturnsDoubleValue()
        {
            // Testing Approach: AsDouble method
            var floatValue = LuaValue.Float(3.14);
            
            var result = floatValue.AsDouble();
            
            Assert.AreEqual(3.14, result, 0.0001);
        }

        [TestMethod]
        public void AsDouble_IntegerValue_ReturnsDoubleValue()
        {
            // Testing Approach: AsDouble from integer
            var intValue = LuaValue.Integer(42);
            
            var result = intValue.AsDouble();
            
            Assert.AreEqual(42.0, result, 0.0001);
        }

        #endregion

        #region Performance and Memory Tests

        [TestMethod]
        public void ValueCreation_ManyValues_HandlesCorrectly()
        {
            // Testing Approach: Performance test - many value creations
            var values = new List<LuaValue>();
            
            for (int i = 0; i < 1000; i++)
            {
                values.Add(LuaValue.Integer(i));
                values.Add(LuaValue.Float(i * 0.1));
                values.Add(LuaValue.String($"string_{i}"));
                values.Add(LuaValue.Boolean(i % 2 == 0));
            }
            
            Assert.AreEqual(4000, values.Count);
            
            // Verify a few random values
            Assert.AreEqual(500L, values[2000].AsInteger());
            Assert.AreEqual("string_100", values[402].AsString());
        }

        [TestMethod]
        public void Release_CalledOnValues_DoesNotThrow()
        {
            // Testing Approach: Memory management - Release method
            var intValue = LuaValue.Integer(42);
            var stringValue = LuaValue.String("test");
            var floatValue = LuaValue.Float(3.14);
            
            // Release should not throw exceptions
            intValue.Release();
            stringValue.Release();
            floatValue.Release();
            
            // Values should still be usable after Release (depends on implementation)
            Assert.IsTrue(intValue.IsInteger);
            Assert.IsTrue(stringValue.IsString);
            Assert.IsTrue(floatValue.IsFloat);
        }

        #endregion
    }
}