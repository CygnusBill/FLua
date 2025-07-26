using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Compiler;
using FLua.Runtime;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System;
using System.Linq;

namespace FLua.Compiler.Tests.Minimal;

/// <summary>
/// Integration tests for the complete Lua compilation pipeline following Lee Copeland standards:
/// - Equivalence Partitioning: Testing different types of Lua programs
/// - Boundary Value Analysis: Testing edge cases and limits
/// - Error Condition Testing: Testing invalid inputs and error paths
/// - End-to-End Testing: Full compilation and execution validation
/// </summary>
[TestClass]
public class CompilerIntegrationTests
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

    [TestMethod]
    public void CompileAndExecute_SimplePrint_WorksCorrectly()
    {
        // Testing Approach: Equivalence Partitioning - Basic function call compilation
        // Arrange
        string luaCode = "print(\"Hello World\")";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "simple_print.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var compileResult = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);
        
        // Assert
        Assert.IsTrue(compileResult.Success, "Simple print should compile successfully");
        Assert.IsTrue(File.Exists(outputPath), "Output DLL should be created");
        
        // Execute and verify
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("CompiledLuaScript.LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        try
        {
            method.Invoke(null, new object[] { env });
        }
        catch (Exception ex)
        {
            Assert.Fail($"Compiled script should execute without exceptions: {ex.Message}");
        }
    }

    [TestMethod]
    public void CompileAndExecute_VariableAssignment_WorksCorrectly()
    {
        // Testing Approach: Equivalence Partitioning - Variable assignment and arithmetic
        // Arrange
        string luaCode = @"
            local x = 42
            local y = x + 8
            print('Result:', y)
        ";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "variables.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var compileResult = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsTrue(compileResult.Success, "Variable assignment should compile successfully");
        
        // Execute and verify variable values
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("CompiledLuaScript.LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        method.Invoke(null, new object[] { env });
        
        var yValue = env.GetVariable("y");
        Assert.IsTrue(yValue is LuaInteger, "y should be LuaInteger");
        Assert.AreEqual(50L, ((LuaInteger)yValue).Value, "y should equal 50");
    }

    [TestMethod]
    public void CompileAndExecute_EmptyProgram_WorksCorrectly()
    {
        // Testing Approach: Boundary Value Analysis - Minimal input case
        // Arrange
        string luaCode = "";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "empty.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var compileResult = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsTrue(compileResult.Success, "Empty program should compile successfully");
        
        // Execute empty program
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("CompiledLuaScript.LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        var result = (LuaValue[])method.Invoke(null, new object[] { env });
        Assert.IsNotNull(result, "Should return result array");
        Assert.AreEqual(0, result.Length, "Empty program should return empty array");
    }

    [TestMethod]
    public void Compile_InvalidOutputPath_ReturnsError()
    {
        // Testing Approach: Error Condition Testing - Invalid file system path
        // Arrange
        string luaCode = "print('test')";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var invalidPath = "/invalid/nonexistent/directory/test.dll";
        var options = new CompilerOptions(invalidPath);

        // Act
        var result = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsFalse(result.Success, "Should fail with invalid output path");
        Assert.IsNotNull(result.Errors, "Should have error messages");
        Assert.IsTrue(result.Errors.Any(), "Should contain at least one error");
    }

    [TestMethod]
    public void Compile_LibraryTarget_ProducesLibrary()
    {
        // Testing Approach: Decision Table Testing - Library vs Console target
        // Arrange
        string luaCode = "local x = 1";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "library.dll");
        var options = new CompilerOptions(outputPath, CompilationTarget.Library);

        // Act
        var result = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsTrue(result.Success, "Library compilation should succeed");
        
        var assembly = Assembly.LoadFile(outputPath);
        Assert.IsNull(assembly.EntryPoint, "Library should not have entry point");
        
        // Verify library has Execute method
        var type = assembly.GetType("CompiledLuaScript.LuaScript");
        var executeMethod = type.GetMethod("Execute");
        Assert.IsNotNull(executeMethod, "Library should have Execute method");
        Assert.IsTrue(executeMethod.IsStatic, "Execute method should be static");
    }

    [TestMethod]
    public void Compile_ConsoleTarget_ProducesConsoleApp()
    {
        // Testing Approach: Decision Table Testing - Console application target
        // Arrange
        string luaCode = "print('Hello Console')";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "console.exe");
        var options = new CompilerOptions(outputPath, CompilationTarget.ConsoleApp);

        // Act
        var result = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsTrue(result.Success, "Console compilation should succeed");
        
        var assembly = Assembly.LoadFile(outputPath);
        Assert.IsNotNull(assembly.EntryPoint, "Console app should have entry point");
        Assert.AreEqual("Main", assembly.EntryPoint.Name, "Entry point should be Main method");
    }
}