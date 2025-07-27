using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Compiler;
using FLua.Runtime;
using System.IO;
using System.Reflection;
using System;

namespace FLua.Compiler.Tests.Minimal;

[TestClass]
public class InlineFunctionTests
{
    private CecilLuaCompiler _compiler;
    private string _tempDir;

    [TestInitialize]
    public void Setup()
    {
        _compiler = new CecilLuaCompiler();
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
    public void CompileAndExecute_SimpleInlineFunction_WorksCorrectly()
    {
        // Arrange
        string luaCode = @"
local add = function(a, b) return a + b end
local result = add(5, 3)
        ";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "inline_simple.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var compileResult = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsTrue(compileResult.Success, "Simple inline function should compile successfully");
        
        // Execute and verify
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("CompiledLuaScript+LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        method.Invoke(null, new object[] { env });
        
        var result = env.GetVariable("result");
        Assert.IsTrue(result is LuaInteger, "result should be LuaInteger");
        Assert.AreEqual(8L, ((LuaInteger)result).Value, "result should equal 8");
    }

    [TestMethod]
    public void CompileAndExecute_InlineFunctionInTable_WorksCorrectly()
    {
        // Arrange
        string luaCode = @"
local ops = {
    multiply = function(x, y) return x * y end,
    add = function(x, y) return x + y end
}
local result1 = ops.multiply(10, 5)
local result2 = ops.add(7, 3)
        ";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "inline_table.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var compileResult = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsTrue(compileResult.Success, "Inline functions in table should compile successfully");
        
        // Execute and verify
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("CompiledLuaScript+LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        method.Invoke(null, new object[] { env });
        
        var result1 = env.GetVariable("result1");
        Assert.IsTrue(result1 is LuaInteger, "result1 should be LuaInteger");
        Assert.AreEqual(50L, ((LuaInteger)result1).Value, "result1 should equal 50");
        
        var result2 = env.GetVariable("result2");
        Assert.IsTrue(result2 is LuaInteger, "result2 should be LuaInteger");
        Assert.AreEqual(10L, ((LuaInteger)result2).Value, "result2 should equal 10");
    }

    [TestMethod]
    public void CompileAndExecute_InlineFunctionAsArgument_WorksCorrectly()
    {
        // Arrange
        string luaCode = @"
local function apply(f, a, b)
    return f(a, b)
end

local result = apply(function(x, y) return x - y end, 10, 3)
        ";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "inline_arg.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var compileResult = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsTrue(compileResult.Success, "Inline function as argument should compile successfully");
        
        // Execute and verify
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("CompiledLuaScript+LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        method.Invoke(null, new object[] { env });
        
        var result = env.GetVariable("result");
        Assert.IsTrue(result is LuaInteger, "result should be LuaInteger");
        Assert.AreEqual(7L, ((LuaInteger)result).Value, "result should equal 7 (10-3)");
    }

    [TestMethod]
    public void CompileAndExecute_InlineFunctionNoParams_WorksCorrectly()
    {
        // Arrange
        string luaCode = @"
local greet = function() return ""Hello from inline!"" end
local message = greet()
        ";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "inline_noparams.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var compileResult = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsTrue(compileResult.Success, "Inline function with no params should compile successfully");
        
        // Execute and verify
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("CompiledLuaScript+LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        method.Invoke(null, new object[] { env });
        
        var message = env.GetVariable("message");
        Assert.IsTrue(message is LuaString, "message should be LuaString");
        Assert.AreEqual("Hello from inline!", ((LuaString)message).Value, "message should match");
    }
}