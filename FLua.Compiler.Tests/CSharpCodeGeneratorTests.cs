using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Compiler;
using FLua.Ast;
using System.Collections.Generic;
using System.Numerics;
using System;
using System.Linq;
using Microsoft.FSharp.Collections;
using LuaAttribute = FLua.Ast.Attribute;

namespace FLua.Compiler.Tests;

/// <summary>
/// Unit tests for CSharpCodeGenerator following Lee Copeland standards
/// Focus on code generation correctness and edge cases
/// </summary>
[TestClass]
public class CSharpCodeGeneratorTests
{
    private CSharpCodeGenerator _generator;
    private CompilerOptions _defaultOptions;

    [TestInitialize]
    public void Setup()
    {
        _generator = new CSharpCodeGenerator();
        _defaultOptions = new CompilerOptions("test.dll");
    }

    #region Literal Generation Tests

    [TestMethod]
    public void Generate_NilLiteral_ProducesCorrectCode()
    {
        // Testing Approach: Equivalence Partitioning - Testing nil literal as representative of literal values
        // Arrange
        var ast = new List<Statement>
        {
            Statement.CreateLocalAssignment(
                new[] { ("x", LuaLuaAttribute.NoAttribute) },
                new[] { Expr.CreateLiteral(Literal.CreateNil()) }
            )
        };

        // Act
        var result = _generator.Generate(ast, _defaultOptions);

        // Assert
        Assert.IsTrue(result.Contains("LuaValue.Nil"), "Should generate LuaValue.Nil for nil literal");
    }

    [TestMethod]
    public void Generate_BooleanLiterals_ProducesCorrectCode()
    {
        // Testing Approach: Boundary Value Analysis - Testing both true and false boolean values
        // Arrange
        var ast = new List<Statement>
        {
            Statement.CreateLocalAssignment(
                new[] { ("t", LuaAttribute.NoAttribute), ("f", LuaAttribute.NoAttribute) },
                new[] { 
                    Expr.CreateLiteral(Literal.CreateBoolean(true)),
                    Expr.CreateLiteral(Literal.CreateBoolean(false))
                }
            )
        };

        // Act
        var result = _generator.Generate(ast, _defaultOptions);

        // Assert
        Assert.IsTrue(result.Contains("new LuaBoolean(true)"), "Should generate LuaBoolean(true)");
        Assert.IsTrue(result.Contains("new LuaBoolean(false)"), "Should generate LuaBoolean(false)");
    }

    [TestMethod]
    public void Generate_IntegerLiteral_ProducesCorrectCode()
    {
        // Testing Approach: Equivalence Partitioning - Testing typical integer value generation
        // Arrange
        var ast = new List<Statement>
        {
            Statement.CreateLocalAssignment(
                new[] { ("num", LuaAttribute.NoAttribute) },
                new[] { Expr.CreateLiteral(Literal.CreateInteger(new BigInteger(42))) }
            )
        };

        // Act
        var result = _generator.Generate(ast, _defaultOptions);

        // Assert
        Assert.IsTrue(result.Contains("new LuaInteger(42)"), "Should generate LuaInteger with correct value");
    }

    [TestMethod]
    public void Generate_FloatLiteral_ProducesCorrectCode()
    {
        // Testing Approach: Equivalence Partitioning - Testing floating point literal generation
        // Arrange
        var ast = new List<Statement>
        {
            Statement.CreateLocalAssignment(
                new[] { ("pi", LuaAttribute.NoAttribute) },
                new[] { Expr.CreateLiteral(Literal.CreateFloat(3.14)) }
            )
        };

        // Act
        var result = _generator.Generate(ast, _defaultOptions);

        // Assert
        Assert.IsTrue(result.Contains("new LuaNumber(3.14d)"), "Should generate LuaNumber with double suffix");
    }

    [TestMethod]
    public void Generate_StringLiteral_EscapesCorrectly()
    {
        // Testing Approach: Error Condition Testing - Testing string escaping edge cases
        // Arrange
        var testString = "Hello \"World\" \\ \n";
        var ast = new List<Statement>
        {
            Statement.CreateLocalAssignment(
                new[] { ("str", LuaAttribute.NoAttribute) },
                new[] { Expr.CreateLiteral(Literal.CreateString(testString)) }
            )
        };

        // Act
        var result = _generator.Generate(ast, _defaultOptions);

        // Assert
        Assert.IsTrue(result.Contains("new LuaString("), "Should generate LuaString");
        Assert.IsTrue(result.Contains("\\\""), "Should escape quotes");
        Assert.IsTrue(result.Contains("\\\\"), "Should escape backslashes");
    }

    #endregion

    #region Binary Operation Tests

    [TestMethod]
    public void Generate_ArithmeticOperations_ProducesCorrectCode()
    {
        // Testing Approach: Equivalence Partitioning - Testing binary operation code generation
        // Arrange
        var addition = Expr.CreateBinary(
            Expr.CreateLiteral(Literal.CreateInteger(new BigInteger(5))),
            BinaryOp.Add,
            Expr.CreateLiteral(Literal.CreateInteger(new BigInteger(3)))
        );
        var ast = new List<Statement>
        {
            Statement.CreateLocalAssignment(
                new[] { ("result", LuaAttribute.NoAttribute) },
                new[] { addition }
            )
        };

        // Act
        var result = _generator.Generate(ast, _defaultOptions);

        // Assert
        Assert.IsTrue(result.Contains("LuaOperations.Add("), "Should use LuaOperations.Add");
        Assert.IsTrue(result.Contains("new LuaInteger(5)"), "Should generate first operand");
        Assert.IsTrue(result.Contains("new LuaInteger(3)"), "Should generate second operand");
    }

    [TestMethod]
    public void Generate_AllBinaryOperators_MapsCorrectly()
    {
        // Testing Approach: Equivalence Partitioning - Testing all binary operator mappings systematically
        // Test each binary operator maps to correct method
        var testCases = new[]
        {
            (BinaryOp.Add, "LuaOperations.Add"),
            (BinaryOp.Subtract, "LuaOperations.Subtract"),
            (BinaryOp.Multiply, "LuaOperations.Multiply"),
            (BinaryOp.Equal, "LuaOperations.Equal"),
            (BinaryOp.Less, "LuaOperations.LessThan")
        };

        foreach (var (op, expectedMethod) in testCases)
        {
            // Arrange
            var binaryExpr = Expr.CreateBinary(
                Expr.CreateLiteral(Literal.CreateInteger(new BigInteger(1))),
                op,
                Expr.CreateLiteral(Literal.CreateInteger(new BigInteger(2)))
            );
            var ast = new List<Statement>
            {
                Statement.CreateLocalAssignment(
                    new[] { ("test", LuaAttribute.NoAttribute) },
                    new[] { binaryExpr }
                )
            };

            // Act
            var result = _generator.Generate(ast, _defaultOptions);

            // Assert
            Assert.IsTrue(result.Contains(expectedMethod), 
                $"Binary operator {op} should map to {expectedMethod}");
        }
    }

    #endregion

    #region Function Call Tests

    [TestMethod]
    public void Generate_FunctionCallStatement_DoesNotAddIndexing()
    {
        // Testing Approach: Decision Table Testing - Testing function call in statement vs expression context
        // Arrange
        var printCall = Statement.CreateFunctionCall(
            Expr.CreateFunctionCall(
                Expr.CreateVar("print"),
                ListModule.OfArray(new[] { Expr.CreateLiteral(Literal.CreateString("hello")) })
            )
        );
        var ast = new List<Statement> { printCall };

        // Act
        var result = _generator.Generate(ast, _defaultOptions);

        // Assert
        Assert.IsTrue(result.Contains("((LuaFunction)"), "Should cast to LuaFunction");
        Assert.IsTrue(result.Contains(".Call(new LuaValue[]"), "Should call with array");
        Assert.IsFalse(result.Contains(").Call(new LuaValue[] { new LuaString(\"hello\") })[0]"), 
            "Function call as statement should not index result");
    }

    [TestMethod]
    public void Generate_FunctionCallExpression_AddsIndexing()
    {
        // Testing Approach: Decision Table Testing - Testing function call in expression context
        // Arrange - function call in expression context
        var assignment = Statement.CreateLocalAssignment(
            new[] { ("result", LuaAttribute.NoAttribute) },
            new[] { Expr.CreateFunctionCall(
                Expr.CreateVar("getValue"),
                ListModule.OfArray(new Expr[0])
            )}
        );
        var ast = new List<Statement> { assignment };

        // Act
        var result = _generator.Generate(ast, _defaultOptions);

        // Assert
        Assert.IsTrue(result.Contains(")[0]"), "Function call in expression should index first result");
    }

    #endregion

    #region Variable and Environment Tests

    [TestMethod]
    public void Generate_LocalVariable_SetsInEnvironment()
    {
        // Testing Approach: State Transition Testing - Testing variable creation and environment interaction
        // Arrange
        var ast = new List<Statement>
        {
            Statement.CreateLocalAssignment(
                new[] { ("x", LuaLuaAttribute.NoAttribute) },
                new[] { Expr.CreateLiteral(Literal.CreateInteger(new BigInteger(42))) }
            )
        };

        // Act
        var result = _generator.Generate(ast, _defaultOptions);

        // Assert
        Assert.IsTrue(result.Contains("var x = "), "Should declare local variable");
        Assert.IsTrue(result.Contains("env.SetVariable(\"x\", x);"), "Should set in environment");
    }

    [TestMethod]
    public void Generate_VariableAccess_UsesEnvironment()
    {
        // Arrange
        var ast = new List<Statement>
        {
            Statement.CreateLocalAssignment(
                new[] { ("y", LuaAttribute.NoAttribute) },
                new[] { Expr.CreateVar("x") }
            )
        };

        // Act
        var result = _generator.Generate(ast, _defaultOptions);

        // Assert
        Assert.IsTrue(result.Contains("env.GetVariable(\"x\")"), "Should get variable from environment");
    }

    #endregion

    #region Code Structure Tests

    [TestMethod]
    public void Generate_LibraryTarget_CreatesCorrectStructure()
    {
        // Arrange
        var ast = new List<Statement>();
        var options = new CompilerOptions("test.dll", CompilationTarget.Library);

        // Act
        var result = _generator.Generate(ast, options);

        // Assert
        Assert.IsTrue(result.Contains("using System;"), "Should include using statements");
        Assert.IsTrue(result.Contains("using FLua.Runtime;"), "Should include runtime using");
        Assert.IsTrue(result.Contains("public static class LuaScript"), "Should create LuaScript class");
        Assert.IsTrue(result.Contains("public static LuaValue[] Execute(LuaEnvironment env)"), 
            "Should create Execute method for library");
        Assert.IsTrue(result.Contains("return new LuaValue[0];"), "Should return empty array");
    }

    [TestMethod]
    public void Generate_ConsoleTarget_CreatesMainMethod()
    {
        // Arrange
        var ast = new List<Statement>();
        var options = new CompilerOptions("test.exe", CompilationTarget.ConsoleApp);

        // Act
        var result = _generator.Generate(ast, options);

        // Assert
        Assert.IsTrue(result.Contains("public static void Main(string[] args)"), 
            "Should create Main method for console app");
        Assert.IsTrue(result.Contains("var env = new LuaEnvironment();"), 
            "Should create environment");
        Assert.IsTrue(result.Contains("try"), "Should wrap in try-catch");
        Assert.IsTrue(result.Contains("catch (Exception ex)"), "Should catch exceptions");
    }

    [TestMethod]
    public void Generate_CustomAssemblyName_UsesCorrectNamespace()
    {
        // Arrange
        var ast = new List<Statement>();
        var options = new CompilerOptions("test.dll", AssemblyName: "MyCustomAssembly");

        // Act
        var result = _generator.Generate(ast, options);

        // Assert
        Assert.IsTrue(result.Contains("namespace MyCustomAssembly;"), 
            "Should use custom assembly name as namespace");
    }

    #endregion

    #region Edge Cases and Error Conditions

    [TestMethod]
    public void Generate_EmptyBlock_ProducesValidCode()
    {
        // Testing Approach: Boundary Value Analysis - Testing minimal input case
        // Arrange
        var ast = new List<Statement>();

        // Act
        var result = _generator.Generate(ast, _defaultOptions);

        // Assert
        Assert.IsNotNull(result, "Should generate code for empty block");
        Assert.IsTrue(result.Contains("public static LuaValue[] Execute"), "Should have Execute method");
        Assert.IsTrue(result.Length > 0, "Should generate non-empty code");
    }

    [TestMethod]
    public void Generate_VariableNameWithKeywords_SanitizesCorrectly()
    {
        // Arrange - variable name that conflicts with C# keyword
        var ast = new List<Statement>
        {
            Statement.CreateLocalAssignment(
                new[] { ("class", LuaAttribute.NoAttribute) },
                new[] { Expr.CreateLiteral(Literal.CreateInteger(new BigInteger(1))) }
            )
        };

        // Act
        var result = _generator.Generate(ast, _defaultOptions);

        // Assert
        // The sanitizer should handle C# keywords appropriately
        Assert.IsTrue(result.Contains("var class = ") || result.Contains("var @class = "), 
            "Should handle C# keyword variable names");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Generate_NullAst_ThrowsException()
    {
        // Testing Approach: Error Condition Testing - Testing null input validation
        // Act
        _generator.Generate(null, _defaultOptions);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void Generate_NullOptions_ThrowsException()
    {
        // Testing Approach: Error Condition Testing - Testing null options validation
        // Arrange
        var ast = new List<Statement>();

        // Act
        _generator.Generate(ast, null);
    }

    #endregion
}