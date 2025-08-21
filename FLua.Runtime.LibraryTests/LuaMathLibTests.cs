using FLua.Runtime;
using FLua.Common;

namespace FLua.Runtime.LibraryTests;

/// <summary>
/// Comprehensive unit tests for LuaMathLib using Lee Copeland's testing methodology.
/// Includes boundary value analysis, equivalence class partitioning, and error path testing.
/// </summary>
[TestClass]
public class LuaMathLibTests
{
    private LuaEnvironment _env = null!;

    [TestInitialize]
    public void Setup()
    {
        _env = new LuaEnvironment();
        LuaMathLib.AddMathLibrary(_env);
    }

    #region Constants Tests

    [TestMethod]
    public void Constants_ShouldHaveCorrectValues()
    {
        var mathTable = _env.GetVariable("math").AsTable<LuaTable>();
        
        Assert.AreEqual(Math.PI, mathTable.Get(LuaValue.String("pi")).AsDouble(), 1e-15);
        Assert.AreEqual(double.PositiveInfinity, mathTable.Get(LuaValue.String("huge")).AsDouble());
        Assert.AreEqual(long.MinValue, mathTable.Get(LuaValue.String("mininteger")).AsInteger());
        Assert.AreEqual(long.MaxValue, mathTable.Get(LuaValue.String("maxinteger")).AsInteger());
    }

    #endregion

    #region Abs Function Tests - Boundary Value Analysis

    [TestMethod]
    public void Abs_PositiveInteger_ReturnsPositive()
    {
        var result = CallMathFunction("abs", LuaValue.Integer(42));
        Assert.AreEqual(42L, result.AsInteger());
    }

    [TestMethod]
    public void Abs_NegativeInteger_ReturnsPositive()
    {
        var result = CallMathFunction("abs", LuaValue.Integer(-42));
        Assert.AreEqual(42L, result.AsInteger());
    }

    [TestMethod]
    public void Abs_Zero_ReturnsZero()
    {
        var result = CallMathFunction("abs", LuaValue.Integer(0));
        Assert.AreEqual(0L, result.AsInteger());
    }

    [TestMethod]
    public void Abs_MinValue_ReturnsMinValue()
    {
        // Boundary case: long.MinValue abs is itself in Lua
        var result = CallMathFunction("abs", LuaValue.Integer(long.MinValue));
        Assert.AreEqual(long.MinValue, result.AsInteger());
    }

    [TestMethod]
    public void Abs_MaxValue_ReturnsMaxValue()
    {
        var result = CallMathFunction("abs", LuaValue.Integer(long.MaxValue));
        Assert.AreEqual(long.MaxValue, result.AsInteger());
    }

    [TestMethod]
    public void Abs_PositiveFloat_ReturnsPositive()
    {
        var result = CallMathFunction("abs", LuaValue.Float(3.14));
        Assert.AreEqual(3.14, result.AsDouble(), 1e-15);
    }

    [TestMethod]
    public void Abs_NegativeFloat_ReturnsPositive()
    {
        var result = CallMathFunction("abs", LuaValue.Float(-3.14));
        Assert.AreEqual(3.14, result.AsDouble(), 1e-15);
    }

    [TestMethod]
    public void Abs_NaN_ReturnsNaN()
    {
        var result = CallMathFunction("abs", LuaValue.Float(double.NaN));
        Assert.IsTrue(double.IsNaN(result.AsDouble()));
    }

    [TestMethod]
    public void Abs_PositiveInfinity_ReturnsPositiveInfinity()
    {
        var result = CallMathFunction("abs", LuaValue.Float(double.PositiveInfinity));
        Assert.AreEqual(double.PositiveInfinity, result.AsDouble());
    }

    [TestMethod]
    public void Abs_NegativeInfinity_ReturnsPositiveInfinity()
    {
        var result = CallMathFunction("abs", LuaValue.Float(double.NegativeInfinity));
        Assert.AreEqual(double.PositiveInfinity, result.AsDouble());
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Abs_NoArguments_ThrowsException()
    {
        CallMathFunction("abs");
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Abs_NonNumericArgument_ThrowsException()
    {
        CallMathFunction("abs", LuaValue.String("not a number"));
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Abs_NilArgument_ThrowsException()
    {
        CallMathFunction("abs", LuaValue.Nil);
    }

    #endregion

    #region Max/Min Functions Tests - Equivalence Class Partitioning

    [TestMethod]
    public void Max_SingleArgument_ReturnsSameValue()
    {
        var result = CallMathFunction("max", LuaValue.Integer(42));
        Assert.AreEqual(42L, result.AsInteger());
    }

    [TestMethod]
    public void Max_TwoIntegers_ReturnsLarger()
    {
        var result = CallMathFunction("max", LuaValue.Integer(10), LuaValue.Integer(20));
        Assert.AreEqual(20L, result.AsInteger());
    }

    [TestMethod]
    public void Max_TwoFloats_ReturnsLarger()
    {
        var result = CallMathFunction("max", LuaValue.Float(3.14), LuaValue.Float(2.71));
        Assert.AreEqual(3.14, result.AsDouble(), 1e-15);
    }

    [TestMethod]
    public void Max_MixedTypes_ReturnsLarger()
    {
        var result = CallMathFunction("max", LuaValue.Integer(10), LuaValue.Float(5.5));
        Assert.AreEqual(10.0, result.AsDouble(), 1e-15);
    }

    [TestMethod]
    public void Max_MultipleArguments_ReturnsLargest()
    {
        var result = CallMathFunction("max", 
            LuaValue.Integer(5), LuaValue.Integer(10), LuaValue.Integer(3), LuaValue.Integer(8));
        Assert.AreEqual(10L, result.AsInteger());
    }

    [TestMethod]
    public void Max_EqualValues_ReturnsFirst()
    {
        var result = CallMathFunction("max", LuaValue.Integer(42), LuaValue.Integer(42));
        Assert.AreEqual(42L, result.AsInteger());
    }

    [TestMethod]
    public void Max_WithNaN_ReturnsNaN()
    {
        var result = CallMathFunction("max", LuaValue.Float(double.NaN), LuaValue.Float(42.0));
        Assert.IsTrue(double.IsNaN(result.AsDouble()));
    }

    [TestMethod]
    public void Max_WithInfinity_ReturnsInfinity()
    {
        var result = CallMathFunction("max", LuaValue.Float(double.PositiveInfinity), LuaValue.Float(42.0));
        Assert.AreEqual(double.PositiveInfinity, result.AsDouble());
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Max_NoArguments_ThrowsException()
    {
        CallMathFunction("max");
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Max_NonNumericFirstArgument_ThrowsException()
    {
        CallMathFunction("max", LuaValue.String("not a number"), LuaValue.Integer(42));
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Max_NonNumericSecondArgument_ThrowsException()
    {
        CallMathFunction("max", LuaValue.Integer(42), LuaValue.String("not a number"));
    }

    [TestMethod]
    public void Min_TwoIntegers_ReturnsSmaller()
    {
        var result = CallMathFunction("min", LuaValue.Integer(10), LuaValue.Integer(20));
        Assert.AreEqual(10L, result.AsInteger());
    }

    [TestMethod]
    public void Min_WithNegativeInfinity_ReturnsNegativeInfinity()
    {
        var result = CallMathFunction("min", LuaValue.Float(double.NegativeInfinity), LuaValue.Float(42.0));
        Assert.AreEqual(double.NegativeInfinity, result.AsDouble());
    }

    #endregion

    #region Floor/Ceil Functions Tests - Boundary Value Analysis

    [TestMethod]
    public void Floor_Integer_ReturnsSameValue()
    {
        var result = CallMathFunction("floor", LuaValue.Integer(42));
        Assert.AreEqual(42L, result.AsInteger());
    }

    [TestMethod]
    public void Floor_PositiveFloat_ReturnsFloor()
    {
        var result = CallMathFunction("floor", LuaValue.Float(3.7));
        Assert.AreEqual(3L, result.AsInteger());
    }

    [TestMethod]
    public void Floor_NegativeFloat_ReturnsFloor()
    {
        var result = CallMathFunction("floor", LuaValue.Float(-3.7));
        Assert.AreEqual(-4L, result.AsInteger());
    }

    [TestMethod]
    public void Floor_ExactFloat_ReturnsInteger()
    {
        var result = CallMathFunction("floor", LuaValue.Float(5.0));
        Assert.AreEqual(5L, result.AsInteger());
    }

    [TestMethod]
    public void Floor_LargeFloat_ReturnsFloat()
    {
        var largeValue = (double)long.MaxValue + 1000.0;
        var result = CallMathFunction("floor", LuaValue.Float(largeValue));
        Assert.AreEqual(Math.Floor(largeValue), result.AsDouble(), 1e-15);
    }

    [TestMethod]
    public void Floor_Zero_ReturnsZero()
    {
        var result = CallMathFunction("floor", LuaValue.Float(0.0));
        Assert.AreEqual(0L, result.AsInteger());
    }

    [TestMethod]
    public void Floor_NearZeroPositive_ReturnsZero()
    {
        var result = CallMathFunction("floor", LuaValue.Float(0.1));
        Assert.AreEqual(0L, result.AsInteger());
    }

    [TestMethod]
    public void Floor_NearZeroNegative_ReturnsNegativeOne()
    {
        var result = CallMathFunction("floor", LuaValue.Float(-0.1));
        Assert.AreEqual(-1L, result.AsInteger());
    }

    [TestMethod]
    public void Ceil_PositiveFloat_ReturnsCeil()
    {
        var result = CallMathFunction("ceil", LuaValue.Float(3.2));
        Assert.AreEqual(4L, result.AsInteger());
    }

    [TestMethod]
    public void Ceil_NegativeFloat_ReturnsCeil()
    {
        var result = CallMathFunction("ceil", LuaValue.Float(-3.2));
        Assert.AreEqual(-3L, result.AsInteger());
    }

    #endregion

    #region FMod Function Tests - Error Conditions

    [TestMethod]
    public void FMod_ValidArguments_ReturnsRemainder()
    {
        var result = CallMathFunction("fmod", LuaValue.Float(10.0), LuaValue.Float(3.0));
        Assert.AreEqual(1.0, result.AsDouble(), 1e-15);
    }

    [TestMethod]
    public void FMod_IntegerArguments_ReturnsInteger()
    {
        var result = CallMathFunction("fmod", LuaValue.Integer(10), LuaValue.Integer(3));
        Assert.AreEqual(1L, result.AsInteger());
    }

    [TestMethod]
    public void FMod_NegativeDividend_ReturnsNegativeRemainder()
    {
        var result = CallMathFunction("fmod", LuaValue.Float(-10.0), LuaValue.Float(3.0));
        Assert.AreEqual(-1.0, result.AsDouble(), 1e-15);
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void FMod_DivisionByZero_ThrowsException()
    {
        CallMathFunction("fmod", LuaValue.Float(10.0), LuaValue.Float(0.0));
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void FMod_MissingSecondArgument_ThrowsException()
    {
        CallMathFunction("fmod", LuaValue.Float(10.0));
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void FMod_NonNumericFirstArgument_ThrowsException()
    {
        CallMathFunction("fmod", LuaValue.String("not a number"), LuaValue.Float(3.0));
    }

    #endregion

    #region Trigonometric Functions Tests - Special Values

    [TestMethod]
    public void Sin_Zero_ReturnsZero()
    {
        var result = CallMathFunction("sin", LuaValue.Float(0.0));
        Assert.AreEqual(0.0, result.AsDouble(), 1e-15);
    }

    [TestMethod]
    public void Sin_PiOver2_ReturnsOne()
    {
        var result = CallMathFunction("sin", LuaValue.Float(Math.PI / 2));
        Assert.AreEqual(1.0, result.AsDouble(), 1e-15);
    }

    [TestMethod]
    public void Sin_Pi_ReturnsZero()
    {
        var result = CallMathFunction("sin", LuaValue.Float(Math.PI));
        Assert.AreEqual(0.0, result.AsDouble(), 1e-15);
    }

    [TestMethod]
    public void Cos_Zero_ReturnsOne()
    {
        var result = CallMathFunction("cos", LuaValue.Float(0.0));
        Assert.AreEqual(1.0, result.AsDouble(), 1e-15);
    }

    [TestMethod]
    public void Cos_PiOver2_ReturnsZero()
    {
        var result = CallMathFunction("cos", LuaValue.Float(Math.PI / 2));
        Assert.AreEqual(0.0, result.AsDouble(), 1e-14);
    }

    [TestMethod]
    public void Tan_Zero_ReturnsZero()
    {
        var result = CallMathFunction("tan", LuaValue.Float(0.0));
        Assert.AreEqual(0.0, result.AsDouble(), 1e-15);
    }

    [TestMethod]
    public void ATan_TwoArguments_ReturnsAtan2()
    {
        var result = CallMathFunction("atan", LuaValue.Float(1.0), LuaValue.Float(1.0));
        Assert.AreEqual(Math.PI / 4, result.AsDouble(), 1e-15);
    }

    [TestMethod]
    public void Deg_Pi_Returns180()
    {
        var result = CallMathFunction("deg", LuaValue.Float(Math.PI));
        Assert.AreEqual(180.0, result.AsDouble(), 1e-13);
    }

    [TestMethod]
    public void Rad_180_ReturnsPi()
    {
        var result = CallMathFunction("rad", LuaValue.Float(180.0));
        Assert.AreEqual(Math.PI, result.AsDouble(), 1e-15);
    }

    #endregion

    #region Exponential/Logarithmic Functions Tests

    [TestMethod]
    public void Exp_Zero_ReturnsOne()
    {
        var result = CallMathFunction("exp", LuaValue.Float(0.0));
        Assert.AreEqual(1.0, result.AsDouble(), 1e-15);
    }

    [TestMethod]
    public void Exp_One_ReturnsE()
    {
        var result = CallMathFunction("exp", LuaValue.Float(1.0));
        Assert.AreEqual(Math.E, result.AsDouble(), 1e-15);
    }

    [TestMethod]
    public void Log_E_ReturnsOne()
    {
        var result = CallMathFunction("log", LuaValue.Float(Math.E));
        Assert.AreEqual(1.0, result.AsDouble(), 1e-15);
    }

    [TestMethod]
    public void Log_WithBase_ReturnsCorrectValue()
    {
        var result = CallMathFunction("log", LuaValue.Float(8.0), LuaValue.Float(2.0));
        Assert.AreEqual(3.0, result.AsDouble(), 1e-15);
    }

    [TestMethod]
    public void Sqrt_Four_ReturnsTwo()
    {
        var result = CallMathFunction("sqrt", LuaValue.Float(4.0));
        Assert.AreEqual(2.0, result.AsDouble(), 1e-15);
    }

    [TestMethod]
    public void Sqrt_Zero_ReturnsZero()
    {
        var result = CallMathFunction("sqrt", LuaValue.Float(0.0));
        Assert.AreEqual(0.0, result.AsDouble(), 1e-15);
    }

    [TestMethod]
    public void Pow_TwoToThePowerOfThree_ReturnsEight()
    {
        var result = CallMathFunction("pow", LuaValue.Float(2.0), LuaValue.Float(3.0));
        Assert.AreEqual(8.0, result.AsDouble(), 1e-15);
    }

    [TestMethod]
    public void Pow_AnyNumberToThePowerOfZero_ReturnsOne()
    {
        var result = CallMathFunction("pow", LuaValue.Float(42.0), LuaValue.Float(0.0));
        Assert.AreEqual(1.0, result.AsDouble(), 1e-15);
    }

    #endregion

    #region Random Functions Tests

    [TestMethod]
    public void Random_NoArguments_ReturnsBetweenZeroAndOne()
    {
        var result = CallMathFunction("random");
        var value = result.AsDouble();
        Assert.IsTrue(value >= 0.0 && value < 1.0);
    }

    [TestMethod]
    public void Random_SingleArgument_ReturnsBetweenOneAndN()
    {
        var result = CallMathFunction("random", LuaValue.Integer(10));
        var value = result.AsInteger();
        Assert.IsTrue(value >= 1 && value <= 10);
    }

    [TestMethod]
    public void Random_TwoArguments_ReturnsBetweenMAndN()
    {
        var result = CallMathFunction("random", LuaValue.Integer(5), LuaValue.Integer(15));
        var value = result.AsInteger();
        Assert.IsTrue(value >= 5 && value <= 15);
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Random_NegativeArgument_ThrowsException()
    {
        CallMathFunction("random", LuaValue.Integer(-5));
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Random_ZeroArgument_ThrowsException()
    {
        CallMathFunction("random", LuaValue.Integer(0));
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Random_InvalidRange_ThrowsException()
    {
        CallMathFunction("random", LuaValue.Integer(10), LuaValue.Integer(5));
    }

    #endregion

    #region Type/Conversion Functions Tests

    [TestMethod]
    public void Type_Integer_ReturnsIntegerString()
    {
        var result = CallMathFunction("type", LuaValue.Integer(42));
        Assert.AreEqual("integer", result.AsString());
    }

    [TestMethod]
    public void Type_Float_ReturnsFloatString()
    {
        var result = CallMathFunction("type", LuaValue.Float(3.14));
        Assert.AreEqual("float", result.AsString());
    }

    [TestMethod]
    public void Type_NonNumber_ReturnsNil()
    {
        var result = CallMathFunction("type", LuaValue.String("not a number"));
        Assert.IsTrue(result.IsNil);
    }

    [TestMethod]
    public void ToInteger_Integer_ReturnsSameValue()
    {
        var result = CallMathFunction("tointeger", LuaValue.Integer(42));
        Assert.AreEqual(42L, result.AsInteger());
    }

    [TestMethod]
    public void ToInteger_ExactFloat_ReturnsInteger()
    {
        var result = CallMathFunction("tointeger", LuaValue.Float(42.0));
        Assert.AreEqual(42L, result.AsInteger());
    }

    [TestMethod]
    public void ToInteger_NonExactFloat_ReturnsNil()
    {
        var result = CallMathFunction("tointeger", LuaValue.Float(42.5));
        Assert.IsTrue(result.IsNil);
    }

    [TestMethod]
    public void ToInteger_StringInteger_ReturnsInteger()
    {
        var result = CallMathFunction("tointeger", LuaValue.String("42"));
        Assert.AreEqual(42L, result.AsInteger());
    }

    [TestMethod]
    public void ToInteger_StringWithSpaces_ReturnsInteger()
    {
        var result = CallMathFunction("tointeger", LuaValue.String("  42  "));
        Assert.AreEqual(42L, result.AsInteger());
    }

    [TestMethod]
    public void ToInteger_NonNumericString_ReturnsNil()
    {
        var result = CallMathFunction("tointeger", LuaValue.String("not a number"));
        Assert.IsTrue(result.IsNil);
    }

    [TestMethod]
    public void Ult_UnsignedComparison_ReturnsCorrectResult()
    {
        var result = CallMathFunction("ult", LuaValue.Integer(-1), LuaValue.Integer(1));
        Assert.IsFalse(result.AsBoolean()); // -1 as unsigned is larger than 1, so -1 < 1 is false
    }

    #endregion

    #region Helper Methods

    private LuaValue CallMathFunction(string functionName, params LuaValue[] args)
    {
        var mathTable = _env.GetVariable("math").AsTable<LuaTable>();
        var function = mathTable.Get(LuaValue.String(functionName)).AsFunction();
        var results = function.Call(args);
        return results.Length > 0 ? results[0] : LuaValue.Nil;
    }

    #endregion
}