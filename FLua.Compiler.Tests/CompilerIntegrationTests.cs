using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Compiler;
using FLua.Runtime;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System;

namespace FLua.Compiler.Tests;

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

    #region Equivalence Partitioning Tests

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

    #endregion

    #region Boundary Value Analysis Tests

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
    public void CompileAndExecute_MaxIntegerValue_HandlesCorrectly()
    {
        // Testing Approach: Boundary Value Analysis - Maximum integer value
        // Arrange
        string luaCode = $"local max = {long.MaxValue}";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "max_int.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var compileResult = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsTrue(compileResult.Success, "Maximum integer should compile successfully");
        
        // Verify value
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("CompiledLuaScript.LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        method.Invoke(null, new object[] { env });
        var maxValue = env.GetVariable("max");
        Assert.IsTrue(maxValue is LuaInteger, "max should be LuaInteger");
        Assert.AreEqual(long.MaxValue, ((LuaInteger)maxValue).Value, "Should handle max integer value");
    }

    #endregion

    #region Error Condition Testing

    [TestMethod]
    public void Compile_InvalidLuaSyntax_ReturnsError()
    {
        // Testing Approach: Error Condition Testing - Invalid syntax handling
        // Arrange
        string invalidLuaCode = "local x = ; ; ;"; // Invalid syntax
        
        // Act & Assert
        Assert.ThrowsException<Exception>(() => 
        {
            var ast = FLua.Parser.ParserHelper.ParseString(invalidLuaCode);
        }, "Invalid Lua syntax should throw parse exception");
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

    #endregion

    #region Decision Table Testing

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

    #endregion

    #region State Transition Testing

    [TestMethod]
    public void Compile_DebugAndReleaseOptimization_BothSucceed()
    {
        // Testing Approach: State Transition Testing - Different optimization states
        // Arrange
        string luaCode = "local test = 123";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        
        // Test Debug optimization
        var debugPath = Path.Combine(_tempDir, "debug.dll");
        var debugOptions = new CompilerOptions(debugPath, Optimization: OptimizationLevel.Debug);
        var debugResult = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), debugOptions);
        
        // Test Release optimization
        var releasePath = Path.Combine(_tempDir, "release.dll");
        var releaseOptions = new CompilerOptions(releasePath, Optimization: OptimizationLevel.Release);
        var releaseResult = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), releaseOptions);

        // Assert
        Assert.IsTrue(debugResult.Success, "Debug compilation should succeed");
        Assert.IsTrue(releaseResult.Success, "Release compilation should succeed");
        Assert.IsTrue(File.Exists(debugPath), "Debug DLL should exist");
        Assert.IsTrue(File.Exists(releasePath), "Release DLL should exist");
        
        // Both should produce working assemblies
        var debugAssembly = Assembly.LoadFile(debugPath);
        var releaseAssembly = Assembly.LoadFile(releasePath);
        Assert.IsNotNull(debugAssembly.GetType("CompiledLuaScript.LuaScript"), 
            "Debug assembly should have LuaScript class");
        Assert.IsNotNull(releaseAssembly.GetType("CompiledLuaScript.LuaScript"), 
            "Release assembly should have LuaScript class");
    }

    #endregion

    #region Compatibility Testing

    [TestMethod]
    public void CompileAndExecute_WithCustomAssemblyName_WorksCorrectly()
    {
        // Testing Approach: Compatibility Testing - Custom configuration options
        // Arrange
        string luaCode = "local custom = 42";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "custom.dll");
        var options = new CompilerOptions(outputPath, AssemblyName: "MyCustomLuaModule");

        // Act
        var result = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsTrue(result.Success, "Custom assembly name should work");
        
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("MyCustomLuaModule.LuaScript");
        Assert.IsNotNull(type, "Should use custom namespace/assembly name");
    }

    #endregion
}