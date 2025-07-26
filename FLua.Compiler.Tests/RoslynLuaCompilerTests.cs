using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Compiler;
using FLua.Runtime;
using FLua.Ast;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System;
using System.Linq;
using Microsoft.FSharp.Collections;
using LuaAttribute = FLua.Ast.Attribute;

namespace FLua.Compiler.Tests;

/// <summary>
/// Test suite for RoslynLuaCompiler following Lee Copeland testing standards:
/// - Boundary value analysis
/// - Equivalence partitioning  
/// - Error condition testing
/// - Integration testing
/// </summary>
[TestClass]
public class RoslynLuaCompilerTests
{
    private RoslynLuaCompiler _compiler;
    private string _tempDir;

    [TestInitialize]
    public void Setup()
    {
        _compiler = new RoslynLuaCompiler();
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    #region Equivalence Partitioning Tests

    [TestMethod]
    public void Compile_EmptyProgram_ProducesValidAssembly()
    {
        // Arrange
        var ast = new List<Statement>();
        var outputPath = Path.Combine(_tempDir, "empty.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var result = _compiler.Compile(ast, options);

        // Assert
        Assert.IsTrue(result.Success, "Empty program should compile successfully");
        Assert.IsTrue(File.Exists(outputPath), "Output file should be created");
        Assert.IsNull(result.Errors, "Should have no compilation errors");
    }

    [TestMethod]
    public void Compile_SimpleAssignment_GeneratesCorrectCode()
    {
        // Arrange
        var ast = CreateSimpleAssignmentAst();
        var outputPath = Path.Combine(_tempDir, "assignment.dll");
        var options = new CompilerOptions(outputPath, IncludeDebugInfo: true);

        // Act
        var result = _compiler.Compile(ast, options);

        // Assert
        Assert.IsTrue(result.Success, "Simple assignment should compile");
        Assert.IsTrue(File.Exists(outputPath), "DLL should be created");
        
        // Verify generated C# code contains expected patterns
        var csharpFile = Path.ChangeExtension(outputPath, ".cs");
        Assert.IsTrue(File.Exists(csharpFile), "Debug C# file should be created");
        var csharpCode = File.ReadAllText(csharpFile);
        Assert.IsTrue(csharpCode.Contains("new LuaInteger(42)"), "Should generate LuaInteger literal");
        Assert.IsTrue(csharpCode.Contains("env.SetVariable"), "Should set variable in environment");
    }

    [TestMethod]
    public void Compile_FunctionCall_HandlesCorrectly()
    {
        // Arrange
        var ast = CreateFunctionCallAst();
        var outputPath = Path.Combine(_tempDir, "function_call.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var result = _compiler.Compile(ast, options);

        // Assert
        Assert.IsTrue(result.Success, "Function call should compile");
        
        // Load and execute to verify correctness
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("CompiledLuaScript.LuaScript");
        var method = type.GetMethod("Execute");
        
        var env = LuaEnvironment.CreateStandardEnvironment();
        var executionResult = (LuaValue[])method.Invoke(null, new object[] { env });
        
        Assert.IsNotNull(executionResult, "Should return result array");
    }

    #endregion

    #region Boundary Value Analysis Tests

    [TestMethod]
    public void Compile_MaximumIntegerLiteral_HandlesCorrectly()
    {
        // Arrange
        var ast = CreateMaxIntegerAst();
        var outputPath = Path.Combine(_tempDir, "max_int.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var result = _compiler.Compile(ast, options);

        // Assert
        Assert.IsTrue(result.Success, "Maximum integer should compile");
    }

    [TestMethod]
    public void Compile_EmptyString_HandlesCorrectly()
    {
        // Arrange
        var ast = CreateEmptyStringAst();
        var outputPath = Path.Combine(_tempDir, "empty_string.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var result = _compiler.Compile(ast, options);

        // Assert
        Assert.IsTrue(result.Success, "Empty string should compile");
    }

    [TestMethod]
    public void Compile_LongVariableName_HandlesCorrectly()
    {
        // Arrange
        var longName = new string('a', 1000); // Very long variable name
        var ast = CreateLongVariableNameAst(longName);
        var outputPath = Path.Combine(_tempDir, "long_var.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var result = _compiler.Compile(ast, options);

        // Assert
        Assert.IsTrue(result.Success, "Long variable name should compile");
    }

    #endregion

    #region Error Condition Tests

    [TestMethod]
    public void Compile_InvalidOutputPath_ReturnsError()
    {
        // Arrange
        var ast = new List<Statement>();
        var invalidPath = "/invalid/nonexistent/path/test.dll";
        var options = new CompilerOptions(invalidPath);

        // Act
        var result = _compiler.Compile(ast, options);

        // Assert
        Assert.IsFalse(result.Success, "Should fail with invalid output path");
        Assert.IsNotNull(result.Errors, "Should have error messages");
        Assert.IsTrue(result.Errors.Any(), "Should contain at least one error");
    }

    [TestMethod]
    public void Compile_NullAst_ThrowsException()
    {
        // Arrange
        var outputPath = Path.Combine(_tempDir, "null_test.dll");
        var options = new CompilerOptions(outputPath);

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            _compiler.Compile(null, options));
    }

    [TestMethod]
    public void Compile_NullOptions_ThrowsException()
    {
        // Arrange
        var ast = new List<Statement>();

        // Act & Assert
        Assert.ThrowsException<ArgumentNullException>(() => 
            _compiler.Compile(ast, null));
    }

    #endregion

    #region Integration Tests

    [TestMethod]
    public void Compile_LibraryTarget_ProducesLibraryAssembly()
    {
        // Arrange
        var ast = CreateSimpleAssignmentAst();
        var outputPath = Path.Combine(_tempDir, "library.dll");
        var options = new CompilerOptions(outputPath, CompilationTarget.Library);

        // Act
        var result = _compiler.Compile(ast, options);

        // Assert
        Assert.IsTrue(result.Success, "Library compilation should succeed");
        Assert.AreEqual(CompilationTarget.Library, options.Target);
        
        // Verify it's actually a library
        var assembly = Assembly.LoadFile(outputPath);
        Assert.IsNull(assembly.EntryPoint, "Library should not have entry point");
    }

    [TestMethod]
    public void Compile_ConsoleTarget_ProducesConsoleAssembly()
    {
        // Arrange
        var ast = CreateSimpleAssignmentAst();
        var outputPath = Path.Combine(_tempDir, "console.exe");
        var options = new CompilerOptions(outputPath, CompilationTarget.ConsoleApp);

        // Act
        var result = _compiler.Compile(ast, options);

        // Assert
        Assert.IsTrue(result.Success, "Console compilation should succeed");
        Assert.AreEqual(CompilationTarget.ConsoleApp, options.Target);
        
        // Verify it has entry point
        var assembly = Assembly.LoadFile(outputPath);
        Assert.IsNotNull(assembly.EntryPoint, "Console app should have entry point");
    }

    [TestMethod]
    public void Compile_WithCustomReferences_IncludesReferences()
    {
        // Arrange
        var ast = CreateSimpleAssignmentAst();
        var outputPath = Path.Combine(_tempDir, "custom_refs.dll");
        var customRefs = new[] { typeof(System.Text.Json.JsonSerializer).Assembly.Location };
        var options = new CompilerOptions(outputPath, References: customRefs);

        // Act
        var result = _compiler.Compile(ast, options);

        // Assert
        Assert.IsTrue(result.Success, "Compilation with custom references should succeed");
    }

    [TestMethod]
    public void Compile_DebugAndReleaseOptimization_BothWork()
    {
        // Arrange
        var ast = CreateSimpleAssignmentAst();
        
        // Test Debug
        var debugPath = Path.Combine(_tempDir, "debug.dll");
        var debugOptions = new CompilerOptions(debugPath, Optimization: OptimizationLevel.Debug);
        var debugResult = _compiler.Compile(ast, debugOptions);
        
        // Test Release
        var releasePath = Path.Combine(_tempDir, "release.dll");
        var releaseOptions = new CompilerOptions(releasePath, Optimization: OptimizationLevel.Release);
        var releaseResult = _compiler.Compile(ast, releaseOptions);

        // Assert
        Assert.IsTrue(debugResult.Success, "Debug compilation should succeed");
        Assert.IsTrue(releaseResult.Success, "Release compilation should succeed");
        Assert.IsTrue(File.Exists(debugPath), "Debug DLL should exist");
        Assert.IsTrue(File.Exists(releasePath), "Release DLL should exist");
    }

    #endregion

    #region Execution Tests (End-to-End)

    [TestMethod]
    public void CompiledAssembly_ExecutesCorrectly()
    {
        // Arrange
        var ast = CreateArithmeticTestAst(); // x = 5 + 3
        var outputPath = Path.Combine(_tempDir, "arithmetic.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var compileResult = _compiler.Compile(ast, options);
        Assert.IsTrue(compileResult.Success, "Compilation should succeed");

        // Load and execute
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("CompiledLuaScript.LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        var result = (LuaValue[])method.Invoke(null, new object[] { env });

        // Assert
        Assert.IsNotNull(result, "Execution should return result");
        var xValue = env.GetVariable("x");
        Assert.IsTrue(xValue is LuaInteger, "x should be LuaInteger");
        Assert.AreEqual(8L, ((LuaInteger)xValue).Value, "x should equal 8");
    }

    #endregion

    #region Test Data Factories

    private List<Statement> CreateSimpleAssignmentAst()
    {
        // local x = 42
        var assignment = Statement.CreateLocalAssignment(
            new[] { ("x", LuaAttribute.NoAttribute) },
            new[] { Expr.CreateLiteral(Literal.CreateInteger(new System.Numerics.BigInteger(42))) }
        );
        return new List<Statement> { assignment };
    }

    private List<Statement> CreateFunctionCallAst()
    {
        // print("hello")
        var printCall = Statement.CreateFunctionCall(
            Expr.CreateFunctionCall(
                Expr.CreateVar("print"),
                ListModule.OfArray(new[] { Expr.CreateLiteral(Literal.CreateString("hello")) })
            )
        );
        return new List<Statement> { printCall };
    }

    private List<Statement> CreateMaxIntegerAst()
    {
        // local max = 9223372036854775807
        var assignment = Statement.CreateLocalAssignment(
            new[] { ("max", LuaAttribute.NoAttribute) },
            new[] { Expr.CreateLiteral(Literal.CreateInteger(new System.Numerics.BigInteger(long.MaxValue))) }
        );
        return new List<Statement> { assignment };
    }

    private List<Statement> CreateEmptyStringAst()
    {
        // local empty = ""
        var assignment = Statement.CreateLocalAssignment(
            new[] { ("empty", LuaAttribute.NoAttribute) },
            new[] { Expr.CreateLiteral(Literal.CreateString("")) }
        );
        return new List<Statement> { assignment };
    }

    private List<Statement> CreateLongVariableNameAst(string varName)
    {
        // local {varName} = 1
        var assignment = Statement.CreateLocalAssignment(
            new[] { (varName, LuaAttribute.NoAttribute) },
            new[] { Expr.CreateLiteral(Literal.CreateInteger(new System.Numerics.BigInteger(1))) }
        );
        return new List<Statement> { assignment };
    }

    private List<Statement> CreateArithmeticTestAst()
    {
        // local x = 5 + 3
        var addition = Expr.CreateBinary(
            Expr.CreateLiteral(Literal.CreateInteger(new System.Numerics.BigInteger(5))),
            BinaryOp.Add,
            Expr.CreateLiteral(Literal.CreateInteger(new System.Numerics.BigInteger(3)))
        );
        var assignment = Statement.CreateLocalAssignment(
            new[] { ("x", LuaAttribute.NoAttribute) },
            new[] { addition }
        );
        return new List<Statement> { assignment };
    }

    #endregion
}