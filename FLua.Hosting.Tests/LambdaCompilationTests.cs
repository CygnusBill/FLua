using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Hosting;
using FLua.Hosting.Security;
using FLua.Runtime;
using System;

namespace FLua.Hosting.Tests;

/// <summary>
/// Tests for lambda compilation functionality following Lee Copeland standards:
/// - Equivalence Partitioning: Different Lua expressions and return types
/// - Boundary Value Analysis: Empty scripts, complex expressions
/// - Error Condition Testing: Invalid Lua code, type mismatches
/// </summary>
[TestClass]
public class LambdaCompilationTests
{
    private ILuaHost _host = null!;

    [TestInitialize]
    public void Setup()
    {
        _host = new LuaHost();
    }

    #region Basic Lambda Compilation

    [TestMethod]
    public void CompileToFunction_SimpleExpression_ReturnsCorrectValue()
    {
        // Testing Approach: Equivalence Partitioning - Simple arithmetic
        // Arrange
        string luaCode = "return 2 + 3";

        // Act
        var func = _host.CompileToFunction<double>(luaCode);
        var result = func();

        // Assert
        Assert.AreEqual(5.0, result);
    }

    [TestMethod]
    public void CompileToFunction_StringResult_ReturnsString()
    {
        // Testing Approach: Equivalence Partitioning - String return type
        // Arrange
        string luaCode = "return 'Hello, ' .. 'World!'";

        // Act
        var func = _host.CompileToFunction<string>(luaCode);
        var result = func();

        // Assert
        Assert.AreEqual("Hello, World!", result);
    }

    [TestMethod]
    public void CompileToFunction_BooleanResult_ReturnsBoolean()
    {
        // Testing Approach: Equivalence Partitioning - Boolean return type
        // Arrange
        string luaCode = "return 5 > 3";

        // Act
        var func = _host.CompileToFunction<bool>(luaCode);
        var result = func();

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void CompileToFunction_WithMathLibrary_UsesStandardLibrary()
    {
        // Testing Approach: Integration Testing - Standard library usage
        // Arrange
        string luaCode = "return math.sqrt(16)";

        // Act
        var func = _host.CompileToFunction<double>(luaCode);
        var result = func();

        // Assert
        Assert.AreEqual(4.0, result);
    }

    #endregion

    #region Error Condition Testing

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void CompileToFunction_InvalidSyntax_ThrowsException()
    {
        // Testing Approach: Error Condition Testing - Syntax errors
        // Arrange
        string luaCode = "return 2 +";

        // Act & Assert
        _host.CompileToFunction<double>(luaCode);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void CompileToFunction_TypeMismatch_ThrowsException()
    {
        // Testing Approach: Error Condition Testing - Type conversion errors
        // Arrange
        string luaCode = "return 'not a number'";

        // Act
        var func = _host.CompileToFunction<double>(luaCode);
        
        // This should throw when the function is invoked
        func();
    }

    #endregion

    #region Boundary Value Testing

    [TestMethod]
    public void CompileToFunction_EmptyReturn_ReturnsDefaultValue()
    {
        // Testing Approach: Boundary Value Analysis - Empty return
        // Arrange
        string luaCode = "return";

        // Act
        var func = _host.CompileToFunction<LuaValue>(luaCode);
        var result = func();

        // Assert
        Assert.AreEqual(LuaValue.Nil, result);
    }

    [TestMethod]
    public void CompileToFunction_MultipleReturns_ReturnsFirstValue()
    {
        // Testing Approach: Boundary Value Analysis - Multiple return values
        // Arrange
        string luaCode = "return 1, 2, 3";

        // Act
        var func = _host.CompileToFunction<double>(luaCode);
        var result = func();

        // Assert
        Assert.AreEqual(1.0, result);
    }

    #endregion

    #region Security and Trust Level Testing

    [TestMethod]
    public void CompileToFunction_WithRestrictedTrustLevel_LimitsAvailableFunctions()
    {
        // Testing Approach: Security Testing - Trust level enforcement
        // Arrange
        string luaCode = "return type(42)";
        var options = new LuaHostOptions { TrustLevel = TrustLevel.Untrusted };

        // Act
        var func = _host.CompileToFunction<string>(luaCode, options);
        var result = func();

        // Assert
        Assert.AreEqual("number", result);
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void CompileToFunction_UntrustedWithDangerousFunction_ThrowsException()
    {
        // Testing Approach: Security Testing - Dangerous function blocking
        // Arrange
        string luaCode = "return load('return 42')()";
        var options = new LuaHostOptions { TrustLevel = TrustLevel.Untrusted };

        // Act
        var func = _host.CompileToFunction<double>(luaCode, options);
        
        // This should throw when executed because load is not available
        func();
    }

    #endregion
}