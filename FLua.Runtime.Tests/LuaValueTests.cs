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
            var nil1 = LuaNil.Instance;
            var nil2 = LuaNil.Instance;
            Assert.AreSame(nil1, nil2);
        }

        // State-Based Testing: Testing the falsy nature of nil
        [TestMethod]
        public void LuaNil_IsTruthy_ShouldBeFalse()
        {
            Assert.IsFalse(LuaNil.Instance.IsTruthy);
        }

        // Domain Testing: Testing string representation
        [TestMethod]
        public void LuaNil_ToString_ShouldReturnNil()
        {
            Assert.AreEqual("nil", LuaNil.Instance.ToString());
        }

        #endregion

        #region LuaBoolean Tests

        // Equivalence Class Testing: Testing true and false equivalence classes
        [TestMethod]
        public void LuaBoolean_TrueValue_ShouldBeTruthy()
        {
            var luaBool = new LuaBoolean(true);
            Assert.IsTrue(luaBool.IsTruthy);
            Assert.IsTrue(luaBool.Value);
            Assert.AreEqual("true", luaBool.ToString());
        }

        [TestMethod]
        public void LuaBoolean_FalseValue_ShouldBeFalsy()
        {
            var luaBool = new LuaBoolean(false);
            Assert.IsFalse(luaBool.IsTruthy);
            Assert.IsFalse(luaBool.Value);
            Assert.AreEqual("false", luaBool.ToString());
        }

        #endregion

        #region LuaInteger Tests

        // Boundary Value Testing: Testing integer boundaries
        [TestMethod]
        public void LuaInteger_MinValue_ShouldHandleCorrectly()
        {
            var luaInt = new LuaInteger(long.MinValue);
            Assert.AreEqual(long.MinValue, luaInt.Value);
            Assert.AreEqual(long.MinValue, luaInt.AsInteger);
            Assert.AreEqual((double)long.MinValue, luaInt.AsNumber);
        }

        [TestMethod]
        public void LuaInteger_MaxValue_ShouldHandleCorrectly()
        {
            var luaInt = new LuaInteger(long.MaxValue);
            Assert.AreEqual(long.MaxValue, luaInt.Value);
            Assert.AreEqual(long.MaxValue, luaInt.AsInteger);
            Assert.AreEqual((double)long.MaxValue, luaInt.AsNumber);
        }

        // Equivalence Class Testing: Testing zero, positive, and negative integers
        [TestMethod]
        public void LuaInteger_Zero_ShouldHandleCorrectly()
        {
            var luaInt = new LuaInteger(0);
            Assert.AreEqual(0, luaInt.Value);
            Assert.IsTrue(luaInt.IsTruthy); // In Lua, 0 is truthy
            Assert.AreEqual("0", luaInt.ToString());
        }

        [TestMethod]
        public void LuaInteger_PositiveValue_ShouldHandleCorrectly()
        {
            var luaInt = new LuaInteger(42);
            Assert.AreEqual(42, luaInt.Value);
            Assert.IsTrue(luaInt.IsTruthy);
            Assert.AreEqual("42", luaInt.ToString());
        }

        [TestMethod]
        public void LuaInteger_NegativeValue_ShouldHandleCorrectly()
        {
            var luaInt = new LuaInteger(-42);
            Assert.AreEqual(-42, luaInt.Value);
            Assert.IsTrue(luaInt.IsTruthy);
            Assert.AreEqual("-42", luaInt.ToString());
        }

        #endregion

        #region LuaNumber Tests

        // Boundary Value Testing: Testing floating-point boundaries and special values
        [TestMethod]
        public void LuaNumber_PositiveInfinity_ShouldHandleCorrectly()
        {
            var luaNum = new LuaNumber(double.PositiveInfinity);
            Assert.AreEqual(double.PositiveInfinity, luaNum.Value);
            Assert.AreEqual(double.PositiveInfinity, luaNum.AsNumber);
            Assert.IsTrue(luaNum.IsTruthy);
        }

        [TestMethod]
        public void LuaNumber_NegativeInfinity_ShouldHandleCorrectly()
        {
            var luaNum = new LuaNumber(double.NegativeInfinity);
            Assert.AreEqual(double.NegativeInfinity, luaNum.Value);
            Assert.AreEqual(double.NegativeInfinity, luaNum.AsNumber);
            Assert.IsTrue(luaNum.IsTruthy);
        }

        [TestMethod]
        public void LuaNumber_NaN_ShouldHandleCorrectly()
        {
            var luaNum = new LuaNumber(double.NaN);
            Assert.IsTrue(double.IsNaN(luaNum.Value));
            Assert.IsTrue(double.IsNaN(luaNum.AsNumber!.Value));
            Assert.IsTrue(luaNum.IsTruthy);
        }

        // Domain Testing: Testing normal floating-point values
        [TestMethod]
        public void LuaNumber_RegularValue_ShouldHandleCorrectly()
        {
            var luaNum = new LuaNumber(3.14159);
            Assert.AreEqual(3.14159, luaNum.Value, 0.000001);
            Assert.AreEqual(3.14159, luaNum.AsNumber!.Value, 0.000001);
            Assert.IsTrue(luaNum.IsTruthy);
        }

        #endregion

        #region FromObject Factory Method Tests

        // Decision Table Testing: Testing all supported object types
        [TestMethod]
        public void FromObject_Null_ShouldReturnNil()
        {
            var result = LuaValue.FromObject(null!);
            Assert.AreSame(LuaNil.Instance, result);
        }

        [TestMethod]
        public void FromObject_Bool_ShouldReturnLuaBoolean()
        {
            var result = LuaValue.FromObject(true);
            Assert.IsInstanceOfType(result, typeof(LuaBoolean));
            Assert.IsTrue(((LuaBoolean)result).Value);
        }

        [TestMethod]
        public void FromObject_Int_ShouldReturnLuaInteger()
        {
            var result = LuaValue.FromObject(42);
            Assert.IsInstanceOfType(result, typeof(LuaInteger));
            Assert.AreEqual(42, ((LuaInteger)result).Value);
        }

        [TestMethod]
        public void FromObject_Long_ShouldReturnLuaInteger()
        {
            var result = LuaValue.FromObject(42L);
            Assert.IsInstanceOfType(result, typeof(LuaInteger));
            Assert.AreEqual(42L, ((LuaInteger)result).Value);
        }

        [TestMethod]
        public void FromObject_Double_ShouldReturnLuaNumber()
        {
            var result = LuaValue.FromObject(3.14);
            Assert.IsInstanceOfType(result, typeof(LuaNumber));
            Assert.AreEqual(3.14, ((LuaNumber)result).Value, 0.000001);
        }

        [TestMethod]
        public void FromObject_Float_ShouldReturnLuaNumber()
        {
            var result = LuaValue.FromObject(3.14f);
            Assert.IsInstanceOfType(result, typeof(LuaNumber));
            Assert.AreEqual(3.14f, ((LuaNumber)result).Value, 0.000001);
        }

        [TestMethod]
        public void FromObject_String_ShouldReturnLuaString()
        {
            var result = LuaValue.FromObject("hello");
            Assert.IsInstanceOfType(result, typeof(LuaString));
            Assert.AreEqual("hello", ((LuaString)result).Value);
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
            Assert.IsFalse(LuaValue.IsValueTruthy(null!));
        }

        [TestMethod]
        public void IsValueTruthy_Nil_ShouldReturnFalse()
        {
            Assert.IsFalse(LuaValue.IsValueTruthy(LuaNil.Instance));
        }

        [TestMethod]
        public void IsValueTruthy_FalseBoolean_ShouldReturnFalse()
        {
            Assert.IsFalse(LuaValue.IsValueTruthy(new LuaBoolean(false)));
        }

        [TestMethod]
        public void IsValueTruthy_TrueBoolean_ShouldReturnTrue()
        {
            Assert.IsTrue(LuaValue.IsValueTruthy(new LuaBoolean(true)));
        }

        [TestMethod]
        public void IsValueTruthy_Integer_ShouldReturnTrue()
        {
            Assert.IsTrue(LuaValue.IsValueTruthy(new LuaInteger(0))); // Even 0 is truthy in Lua
            Assert.IsTrue(LuaValue.IsValueTruthy(new LuaInteger(42)));
            Assert.IsTrue(LuaValue.IsValueTruthy(new LuaInteger(-1)));
        }

        [TestMethod]
        public void IsValueTruthy_Number_ShouldReturnTrue()
        {
            Assert.IsTrue(LuaValue.IsValueTruthy(new LuaNumber(0.0))); // Even 0.0 is truthy in Lua
            Assert.IsTrue(LuaValue.IsValueTruthy(new LuaNumber(3.14)));
            Assert.IsTrue(LuaValue.IsValueTruthy(new LuaNumber(-2.7)));
        }

        #endregion
    }
} 