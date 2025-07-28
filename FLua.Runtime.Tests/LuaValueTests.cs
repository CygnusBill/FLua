using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Runtime;

namespace FLua.Runtime.Tests
{
    [TestClass]
    public class LuaValueTests
    {
        #region LuaNil Tests

        // Equivalence Class Testing: Testing the singleton behavior of LuaNil
        [TestMethod]
        public void LuaNil_Instance_ShouldBeSingleton()
        {
            var nil1 = LuaValue.Nil;
            var nil2 = LuaValue.Nil;
            Assert.AreEqual(nil1, nil2);
        }

        // State-Based Testing: Testing the falsy nature of nil
        [TestMethod]
        public void LuaNil_IsTruthy_ShouldBeFalse()
        {
            Assert.IsFalse(LuaValue.Nil.IsTruthy());
        }

        // Domain Testing: Testing string representation
        [TestMethod]
        public void LuaNil_ToString_ShouldReturnNil()
        {
            Assert.AreEqual("nil", LuaValue.Nil.ToString());
        }

        #endregion

        #region LuaBoolean Tests

        // Equivalence Class Testing: Testing true and false equivalence classes
        [TestMethod]
        public void LuaBoolean_TrueValue_ShouldBeTruthy()
        {
            var luaBool = LuaValue.Boolean(true);
            Assert.IsTrue(luaBool.IsTruthy());
            Assert.IsTrue(luaBool.AsBoolean());
            Assert.AreEqual("true", luaBool.ToString());
        }

        [TestMethod]
        public void LuaBoolean_FalseValue_ShouldBeFalsy()
        {
            var luaBool = LuaValue.Boolean(false);
            Assert.IsFalse(luaBool.IsTruthy());
            Assert.IsFalse(luaBool.AsBoolean());
            Assert.AreEqual("false", luaBool.ToString());
        }

        #endregion

        #region LuaInteger Tests

        // Boundary Value Testing: Testing integer boundaries
        [TestMethod]
        public void LuaInteger_MinValue_ShouldHandleCorrectly()
        {
            var luaInt = LuaValue.Integer(long.MinValue);
            Assert.AreEqual(long.MinValue, luaInt.AsInteger());
            Assert.AreEqual((double)long.MinValue, luaInt.AsNumber());
        }

        [TestMethod]
        public void LuaInteger_MaxValue_ShouldHandleCorrectly()
        {
            var luaInt = LuaValue.Integer(long.MaxValue);
            Assert.AreEqual(long.MaxValue, luaInt.AsInteger());
            Assert.AreEqual((double)long.MaxValue, luaInt.AsNumber());
        }

        // Equivalence Class Testing: Testing zero, positive, and negative integers
        [TestMethod]
        public void LuaInteger_Zero_ShouldHandleCorrectly()
        {
            var luaInt = LuaValue.Integer(0);
            Assert.AreEqual(0, luaInt.AsInteger());
            Assert.IsTrue(luaInt.IsTruthy()); // In Lua, 0 is truthy
            Assert.AreEqual("0", luaInt.ToString());
        }

        [TestMethod]
        public void LuaInteger_PositiveValue_ShouldHandleCorrectly()
        {
            var luaInt = LuaValue.Integer(42);
            Assert.AreEqual(42, luaInt.AsInteger());
            Assert.IsTrue(luaInt.IsTruthy());
            Assert.AreEqual("42", luaInt.ToString());
        }

        [TestMethod]
        public void LuaInteger_NegativeValue_ShouldHandleCorrectly()
        {
            var luaInt = LuaValue.Integer(-42);
            Assert.AreEqual(-42, luaInt.AsInteger());
            Assert.IsTrue(luaInt.IsTruthy());
            Assert.AreEqual("-42", luaInt.ToString());
        }

        #endregion

        #region LuaNumber Tests

        // Boundary Value Testing: Testing floating-point boundaries and special values
        [TestMethod]
        public void LuaNumber_PositiveInfinity_ShouldHandleCorrectly()
        {
            var luaNum = LuaValue.Number(double.PositiveInfinity);
            Assert.AreEqual(double.PositiveInfinity, luaNum.AsNumber());
            Assert.AreEqual(double.PositiveInfinity, luaNum.AsNumber());
            Assert.IsTrue(luaNum.IsTruthy());
        }

        [TestMethod]
        public void LuaNumber_NegativeInfinity_ShouldHandleCorrectly()
        {
            var luaNum = LuaValue.Number(double.NegativeInfinity);
            Assert.AreEqual(double.NegativeInfinity, luaNum.AsNumber());
            Assert.AreEqual(double.NegativeInfinity, luaNum.AsNumber());
            Assert.IsTrue(luaNum.IsTruthy());
        }

        [TestMethod]
        public void LuaNumber_NaN_ShouldHandleCorrectly()
        {
            var luaNum = LuaValue.Number(double.NaN);
            Assert.IsTrue(double.IsNaN(luaNum.AsNumber()));
            Assert.IsTrue(double.IsNaN(luaNum.AsNumber()));
            Assert.IsTrue(luaNum.IsTruthy());
        }

        // Domain Testing: Testing normal floating-point values
        [TestMethod]
        public void LuaNumber_RegularValue_ShouldHandleCorrectly()
        {
            var luaNum = LuaValue.Number(3.14159);
            Assert.AreEqual(3.14159, luaNum.AsNumber(), 0.000001);
            Assert.AreEqual(3.14159, luaNum.AsNumber(), 0.000001);
            Assert.IsTrue(luaNum.IsTruthy());
        }

        #endregion

        #region FromObject Factory Method Tests

        // Decision Table Testing: Testing all supported object types
        [TestMethod]
        public void FromObject_Null_ShouldReturnNil()
        {
            var result = LuaValue.FromObject(null!);
            Assert.AreSame(LuaValue.Nil, result);
        }

        [TestMethod]
        public void FromObject_Bool_ShouldReturnLuaBoolean()
        {
            var result = LuaValue.FromObject(true);
            Assert.IsTrue(result.IsTruthy());
        }

        [TestMethod]
        public void FromObject_Int_ShouldReturnLuaInteger()
        {
            var result = LuaValue.FromObject(42);
            Assert.IsTrue(result.IsInteger);
            Assert.AreEqual(42, result);
        }

        [TestMethod]
        public void FromObject_Long_ShouldReturnLuaInteger()
        {
            var result = LuaValue.FromObject(42L);
            Assert.IsTrue(result.IsInteger);
            Assert.AreEqual(42L, result);
        }

        [TestMethod]
        public void FromObject_Double_ShouldReturnLuaNumber()
        {
            var result = LuaValue.FromObject(3.14);
            Assert.IsTrue(result.IsTruthy());
            Assert.AreEqual(3.14, result.AsFloat(), 0.000001);
        }

        [TestMethod]
        public void FromObject_Float_ShouldReturnLuaNumber()
        {
            var result = LuaValue.FromObject(3.14f);
            Assert.IsTrue(result.IsFloat);
            Assert.AreEqual(3.14, result.AsFloat(), 0.000001);
        }

        [TestMethod]
        public void FromObject_String_ShouldReturnLuaString()
        {
            var result = LuaValue.FromObject("hello");
            Assert.IsTrue(result.IsTruthy());
            Assert.AreEqual("hello", result);
        }

        // Risk-Based Testing: Testing unsupported type conversion
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void FromObject_UnsupportedType_ShouldThrowException()
        {
            LuaValue.FromObject(new DateTime());
        }

        #endregion

        #region IsValueTruthy Static Method Tests

        // Decision Table Testing: Testing all truthiness combinations
        [TestMethod]
        public void IsValueTruthy_Null_ShouldReturnFalse()
        {
            Assert.IsFalse((null!));
        }

        [TestMethod]
        public void IsValueTruthy_Nil_ShouldReturnFalse()
        {
            Assert.IsFalse(!LuaValue.Nil.IsTruthy());
        }

        [TestMethod]
        public void IsValueTruthy_FalseBoolean_ShouldReturnFalse()
        {
            Assert.IsFalse(!LuaValue.Boolean(false).IsTruthy());
        }

        [TestMethod]
        public void IsValueTruthy_TrueBoolean_ShouldReturnTrue()
        {
            Assert.IsTrue(LuaValue.Boolean(true).IsTruthy());
        }

        [TestMethod]
        public void IsValueTruthy_Integer_ShouldReturnTrue()
        {
            Assert.IsTrue(LuaValue.Integer(0).IsTruthy()); // Even 0 is truthy in Lua
            Assert.IsTrue(LuaValue.Integer(42).IsTruthy());
            Assert.IsTrue(LuaValue.Integer(-1).IsTruthy());
        }

        [TestMethod]
        public void IsValueTruthy_Number_ShouldReturnTrue()
        {
            Assert.IsTrue(LuaValue.Number(0.0).IsTruthy()); // Even 0.0 is truthy in Lua
            Assert.IsTrue(LuaValue.Number(3.14).IsTruthy());
            Assert.IsTrue(LuaValue.Number(-2.7).IsTruthy());
        }

        #endregion
    }
} 