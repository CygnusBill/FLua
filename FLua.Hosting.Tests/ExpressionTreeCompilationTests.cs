using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Hosting;
using FLua.Hosting.Security;
using FLua.Runtime;
using System;
using System.Linq.Expressions;

namespace FLua.Hosting.Tests;

/// <summary>
/// Tests for expression tree compilation functionality following Lee Copeland standards:
/// - Equivalence Partitioning: Different Lua expressions and result types
/// - Boundary Value Analysis: Empty scripts, complex expressions
/// - Error Condition Testing: Invalid Lua code, unsupported constructs
/// </summary>
[TestClass]
public class ExpressionTreeCompilationTests
{
    private ILuaHost _host = null!;

    [TestInitialize]
    public void Setup()
    {
        _host = new LuaHost();
    }

    #region Basic Expression Tree Compilation

    [TestMethod]
    public void CompileToExpression_SimpleArithmetic_CreatesValidExpressionTree()
    {
        // Testing Approach: Equivalence Partitioning - Simple arithmetic
        // Arrange
        string luaCode = "return 10 + 5";

        // Act
        var expr = _host.CompileToExpression<double>(luaCode);
        var compiled = expr.Compile();
        var result = compiled();

        // Assert
        Assert.AreEqual(15.0, result);
        Assert.IsNotNull(expr.Body);
        Assert.AreEqual(ExpressionType.Invoke, expr.Body.NodeType);
    }

    [TestMethod]
    public void CompileToExpression_StringConcatenation_ReturnsString()
    {
        // Testing Approach: Equivalence Partitioning - String operations
        // Arrange
        string luaCode = "return 'Hello' .. ', ' .. 'World!'";

        // Act
        var expr = _host.CompileToExpression<string>(luaCode);
        var compiled = expr.Compile();
        var result = compiled();

        // Assert
        Assert.AreEqual("Hello, World!", result);
    }

    [TestMethod]
    public void CompileToExpression_BooleanLogic_ReturnsBoolean()
    {
        // Testing Approach: Equivalence Partitioning - Boolean operations
        // Arrange
        string luaCode = "return 10 > 5";

        // Act
        var expr = _host.CompileToExpression<bool>(luaCode);
        var compiled = expr.Compile();
        var result = compiled();

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void CompileToExpression_MathLibraryCall_UsesStandardLibrary()
    {
        // Testing Approach: Integration Testing - Standard library in expressions
        // Arrange
        string luaCode = "return math.floor(10.7)";

        // Act
        var expr = _host.CompileToExpression<double>(luaCode);
        var compiled = expr.Compile();
        var result = compiled();

        // Assert
        Assert.AreEqual(10.0, result);
    }

    #endregion

    #region Expression Tree Analysis

    [TestMethod]
    public void CompileToExpression_SimpleExpression_CanBeAnalyzed()
    {
        // Testing Approach: Structural Testing - Expression tree structure
        // Arrange
        string luaCode = "return 42";

        // Act
        var expr = _host.CompileToExpression<double>(luaCode);

        // Assert
        Assert.IsNotNull(expr);
        Assert.IsNotNull(expr.Body);
        // The expression should contain an invocation
        Assert.AreEqual(ExpressionType.Invoke, expr.Body.NodeType);
    }

    [TestMethod]
    public void CompileToExpression_WithLocalVariables_CreatesProperScope()
    {
        // Testing Approach: State Testing - Variable scoping
        // Arrange
        string luaCode = @"
            local x = 10
            local y = 20
            return x + y
        ";

        // Act
        var expr = _host.CompileToExpression<double>(luaCode);
        var compiled = expr.Compile();
        var result = compiled();

        // Assert
        Assert.AreEqual(30.0, result);
    }

    #endregion

    #region Error Condition Testing

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void CompileToExpression_InvalidSyntax_ThrowsException()
    {
        // Testing Approach: Error Condition Testing - Syntax errors
        // Arrange
        string luaCode = "return 10 +";

        // Act & Assert
        _host.CompileToExpression<double>(luaCode);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void CompileToExpression_TypeMismatch_ThrowsWhenExecuted()
    {
        // Testing Approach: Error Condition Testing - Type conversion errors
        // Arrange
        string luaCode = "return 'not a number'";

        // Act
        var expr = _host.CompileToExpression<double>(luaCode);
        var compiled = expr.Compile();
        
        // This should throw when executed
        compiled();
    }

    #endregion

    #region Boundary Value Testing

    [TestMethod]
    public void CompileToExpression_EmptyReturn_ReturnsDefaultValue()
    {
        // Testing Approach: Boundary Value Analysis - Empty return
        // Arrange
        string luaCode = "return";

        // Act
        var expr = _host.CompileToExpression<LuaValue>(luaCode);
        var compiled = expr.Compile();
        var result = compiled();

        // Assert
        Assert.AreEqual(LuaValue.Nil, result);
    }

    [TestMethod]
    public void CompileToExpression_MultipleReturns_ReturnsFirstValue()
    {
        // Testing Approach: Boundary Value Analysis - Multiple return values
        // Arrange
        string luaCode = "return 1, 2, 3";

        // Act
        var expr = _host.CompileToExpression<double>(luaCode);
        var compiled = expr.Compile();
        var result = compiled();

        // Assert
        Assert.AreEqual(1.0, result);
    }

    #endregion

    #region Security and Trust Level Testing

    [TestMethod]
    public void CompileToExpression_WithTrustLevel_RespectsSecurityRestrictions()
    {
        // Testing Approach: Security Testing - Trust level enforcement
        // Arrange
        string luaCode = "return type(42)";
        var options = new LuaHostOptions { TrustLevel = TrustLevel.Untrusted };

        // Act
        var expr = _host.CompileToExpression<string>(luaCode, options);
        var compiled = expr.Compile();
        var result = compiled();

        // Assert
        Assert.AreEqual("number", result);
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void CompileToExpression_UntrustedWithDangerousFunction_ThrowsWhenExecuted()
    {
        // Testing Approach: Security Testing - Dangerous function blocking
        // Arrange
        string luaCode = "return load('return 42')()";
        var options = new LuaHostOptions { TrustLevel = TrustLevel.Untrusted };

        // Act
        var expr = _host.CompileToExpression<double>(luaCode, options);
        var compiled = expr.Compile();
        
        // This should throw because load is not available
        compiled();
    }

    #endregion

    #region Complex Expression Testing

    [TestMethod]
    public void CompileToExpression_ComplexCalculation_EvaluatesCorrectly()
    {
        // Testing Approach: Integration Testing - Complex expressions
        // Arrange
        string luaCode = @"
            local function factorial(n)
                if n <= 1 then return 1 end
                return n * factorial(n - 1)
            end
            return factorial(5)
        ";

        // Act
        var expr = _host.CompileToExpression<double>(luaCode);
        var compiled = expr.Compile();
        var result = compiled();

        // Assert
        Assert.AreEqual(120.0, result);
    }

    [TestMethod]
    public void CompileToExpression_TableOperations_WorksWithTables()
    {
        // Testing Approach: Integration Testing - Table operations
        // Arrange
        string luaCode = @"
            local t = {a = 10, b = 20}
            return t.a + t.b
        ";

        // Act
        var expr = _host.CompileToExpression<double>(luaCode);
        var compiled = expr.Compile();
        var result = compiled();

        // Assert
        Assert.AreEqual(30.0, result);
    }

    #endregion
}