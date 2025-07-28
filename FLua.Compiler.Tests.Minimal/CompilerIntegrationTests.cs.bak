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
        var type = assembly.GetType("CompiledLuaScript+LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        try
        {
            method.Invoke(null, new object[] { env });
        }
        catch (Exception ex)
        {
            var inner = ex.InnerException ?? ex;
            Assert.Fail($"Compiled script should execute without exceptions: {inner.GetType().Name}: {inner.Message}\nStack: {inner.StackTrace}");
        }
    }

    [TestMethod]
    public void CompileAndExecute_VariableAssignment_WorksCorrectly()
    {
        // Testing Approach: Equivalence Partitioning - Variable assignment and arithmetic
        // Arrange
        string luaCode = @"
            x = 42
            y = x + 8
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
        var type = assembly.GetType("CompiledLuaScript+LuaScript");
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
        var type = assembly.GetType("CompiledLuaScript+LuaScript");
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
        var type = assembly.GetType("CompiledLuaScript+LuaScript");
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

    [TestMethod]
    public void CompileAndExecute_IfStatement_WorksCorrectly()
    {
        // Testing Approach: Control Flow Testing - If/else statements
        // Arrange
        string luaCode = @"
            local x = 10
            result = 0
            
            if x > 5 then
                result = 1
            elseif x == 5 then
                result = 2
            else
                result = 3
            end
        ";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "if_statement.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var compileResult = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsTrue(compileResult.Success, "If statement should compile successfully");
        
        // Execute and verify
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("CompiledLuaScript+LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        method.Invoke(null, new object[] { env });
        
        var result = env.GetVariable("result");
        Assert.IsTrue(result is LuaInteger, "result should be LuaInteger");
        Assert.AreEqual(1L, ((LuaInteger)result).Value, "result should equal 1");
    }

    [TestMethod]
    public void CompileAndExecute_WhileLoop_WorksCorrectly()
    {
        // Testing Approach: Control Flow Testing - While loops
        // Arrange
        string luaCode = @"
            i = 0
            sum = 0
            
            while i < 5 do
                sum = sum + i
                i = i + 1
            end
        ";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "while_loop.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var compileResult = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsTrue(compileResult.Success, "While loop should compile successfully");
        
        // Execute and verify
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("CompiledLuaScript+LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        method.Invoke(null, new object[] { env });
        
        var sum = env.GetVariable("sum");
        Assert.IsTrue(sum is LuaInteger, "sum should be LuaInteger");
        Assert.AreEqual(10L, ((LuaInteger)sum).Value, "sum should equal 10 (0+1+2+3+4)");
    }

    [TestMethod]
    public void CompileAndExecute_RepeatUntilLoop_WorksCorrectly()
    {
        // Testing Approach: Control Flow Testing - Repeat/until loops
        // Arrange
        string luaCode = @"
            i = 0
            count = 0
            
            repeat
                count = count + 1
                i = i + 1
            until i >= 3
        ";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "repeat_loop.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var compileResult = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsTrue(compileResult.Success, "Repeat/until loop should compile successfully");
        
        // Execute and verify
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("CompiledLuaScript+LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        method.Invoke(null, new object[] { env });
        
        var count = env.GetVariable("count");
        Assert.IsTrue(count is LuaInteger, "count should be LuaInteger");
        Assert.AreEqual(3L, ((LuaInteger)count).Value, "count should equal 3");
    }

    [TestMethod]
    public void CompileAndExecute_NumericFor_WorksCorrectly()
    {
        // Testing Approach: Control Flow Testing - Numeric for loops
        // Arrange
        string luaCode = @"
            sum = 0
            
            for i = 1, 5 do
                sum = sum + i
            end
        ";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "numeric_for.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var compileResult = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsTrue(compileResult.Success, "Numeric for loop should compile successfully");
        
        // Execute and verify
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("CompiledLuaScript+LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        method.Invoke(null, new object[] { env });
        
        var sum = env.GetVariable("sum");
        Assert.IsTrue(sum is LuaInteger, "sum should be LuaInteger");
        Assert.AreEqual(15L, ((LuaInteger)sum).Value, "sum should equal 15 (1+2+3+4+5)");
    }

    [TestMethod]
    public void CompileAndExecute_NumericForWithStep_WorksCorrectly()
    {
        // Testing Approach: Boundary Value Analysis - For loop with custom step
        // Arrange
        string luaCode = @"
            count = 0
            
            for i = 10, 1, -2 do
                count = count + 1
            end
        ";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "numeric_for_step.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var compileResult = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsTrue(compileResult.Success, "Numeric for loop with step should compile successfully");
        
        // Execute and verify
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("CompiledLuaScript+LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        method.Invoke(null, new object[] { env });
        
        var count = env.GetVariable("count");
        Assert.IsTrue(count is LuaInteger, "count should be LuaInteger");
        Assert.AreEqual(5L, ((LuaInteger)count).Value, "count should equal 5 (10,8,6,4,2)");
    }

    [TestMethod]
    public void CompileAndExecute_BreakStatement_WorksCorrectly()
    {
        // Testing Approach: Control Flow Testing - Break statements
        // Arrange
        string luaCode = @"
            count = 0
            
            while true do
                count = count + 1
                if count >= 3 then
                    break
                end
            end
        ";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "break_statement.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var compileResult = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsTrue(compileResult.Success, "Break statement should compile successfully");
        
        // Execute and verify
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("CompiledLuaScript+LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        method.Invoke(null, new object[] { env });
        
        var count = env.GetVariable("count");
        Assert.IsTrue(count is LuaInteger, "count should be LuaInteger");
        Assert.AreEqual(3L, ((LuaInteger)count).Value, "count should equal 3");
    }

    [TestMethod]
    public void CompileAndExecute_EmptyTableConstructor_WorksCorrectly()
    {
        // Testing Approach: Boundary Value Analysis - Empty table
        // Arrange
        string luaCode = @"
            local t = {}
        ";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "empty_table.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var compileResult = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsTrue(compileResult.Success, "Empty table constructor should compile successfully");
        
        // Execute and verify
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("CompiledLuaScript+LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        method.Invoke(null, new object[] { env });
        
        var t = env.GetVariable("t");
        Assert.IsTrue(t is LuaTable, "t should be LuaTable");
        // Verify it's empty by checking a random key
        var value = ((LuaTable)t).Get(new LuaInteger(1));
        Assert.IsTrue(value is LuaNil, "Empty table should return nil for any key");
    }

    [TestMethod]
    public void CompileAndExecute_ArrayStyleTable_WorksCorrectly()
    {
        // Testing Approach: Normal Case Testing - Array-style table constructor
        // Arrange
        string luaCode = @"
local t = {10, 20, 30}
local a = t[1]
local b = t[2]
local c = t[3]
local sum = a + b + c
        ";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "array_table.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var compileResult = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsTrue(compileResult.Success, "Array-style table should compile successfully");
        
        // Execute and verify
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("CompiledLuaScript+LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        method.Invoke(null, new object[] { env });
        
        var sum = env.GetVariable("sum");
        Assert.IsTrue(sum is LuaInteger, "sum should be LuaInteger");
        Assert.AreEqual(60L, ((LuaInteger)sum).Value, "sum should equal 60 (10+20+30)");
    }

    [TestMethod]
    public void CompileAndExecute_NamedFieldTable_WorksCorrectly()
    {
        // Testing Approach: Equivalence Partitioning - Named field table constructor
        // Arrange
        string luaCode = @"
local t = {x = 10, y = 20}
local a = t.x
local b = t.y
local sum = a + b
        ";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "named_field_table.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var compileResult = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsTrue(compileResult.Success, "Named field table should compile successfully");
        
        // Execute and verify
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("CompiledLuaScript+LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        method.Invoke(null, new object[] { env });
        
        var sum = env.GetVariable("sum");
        Assert.IsTrue(sum is LuaInteger, "sum should be LuaInteger");
        Assert.AreEqual(30L, ((LuaInteger)sum).Value, "sum should equal 30 (10+20)");
    }

    [TestMethod]
    public void CompileAndExecute_TableAssignment_WorksCorrectly()
    {
        // Testing Approach: State Transition Testing - Table modification
        // Arrange
        string luaCode = @"
local t = {10, 20}
t[1] = 100
t[3] = 300
local a = t[1]
local b = t[2]
local c = t[3]
local sum = a + b + c
        ";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "table_assignment.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var compileResult = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsTrue(compileResult.Success, "Table assignment should compile successfully");
        
        // Execute and verify
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("CompiledLuaScript+LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        method.Invoke(null, new object[] { env });
        
        var sum = env.GetVariable("sum");
        Assert.IsTrue(sum is LuaInteger, "sum should be LuaInteger");
        Assert.AreEqual(420L, ((LuaInteger)sum).Value, "sum should equal 420 (100+20+300)");
    }

    [TestMethod]
    public void CompileAndExecute_TableMethodCall_WorksCorrectly()
    {
        // Testing Approach: Integration Testing - Table method calls
        // Arrange
        string luaCode = @"
local str = ""hello""
local len = str:len()
        ";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "table_method_call.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var compileResult = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsTrue(compileResult.Success, "Table method call should compile successfully");
        
        // Execute and verify
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("CompiledLuaScript+LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        method.Invoke(null, new object[] { env });
        
        var len = env.GetVariable("len");
        Assert.IsTrue(len is LuaInteger, "len should be LuaInteger");
        Assert.AreEqual(5L, ((LuaInteger)len).Value, "len should equal 5");
    }

    [TestMethod]
    public void CompileAndExecute_MixedTableConstructor_WorksCorrectly()
    {
        // Testing Approach: Combinatorial Testing - Mixed table constructor
        // Arrange
        string luaCode = @"
local t = {10, x = 20, [""key""] = 30, 40}
local a = t[1]
local b = t.x
local c = t[""key""]
local d = t[2]
local sum = a + b + c + d
        ";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "mixed_table.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var compileResult = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsTrue(compileResult.Success, "Mixed table constructor should compile successfully");
        
        // Execute and verify
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("CompiledLuaScript+LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        method.Invoke(null, new object[] { env });
        
        var sum = env.GetVariable("sum");
        Assert.IsTrue(sum is LuaInteger, "sum should be LuaInteger");
        Assert.AreEqual(100L, ((LuaInteger)sum).Value, "sum should equal 100 (10+20+30+40)");
    }

    [TestMethod]
    public void CompileAndExecute_MultipleAssignmentFromFunction_WorksCorrectly()
    {
        // Testing Approach: Decision Table Testing - Multiple return value scenarios
        // Arrange
        string luaCode = @"
local function multi()
    return 10, 20, 30
end

local function dual()
    return 1, 2
end

-- Test 1: Equal variables and returns
local a, b, c = multi()

-- Test 2: Fewer variables than returns
local x, y = multi()

-- Test 3: More variables than returns
local p, q, r = dual()
        ";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "multi_assign.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var compileResult = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsTrue(compileResult.Success, "Multiple assignment from function should compile successfully");
        
        // Execute and verify
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("CompiledLuaScript+LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        method.Invoke(null, new object[] { env });
        
        // Test 1: Equal variables and returns
        Assert.AreEqual(10L, ((LuaInteger)env.GetVariable("a")).Value, "a should be 10");
        Assert.AreEqual(20L, ((LuaInteger)env.GetVariable("b")).Value, "b should be 20");
        Assert.AreEqual(30L, ((LuaInteger)env.GetVariable("c")).Value, "c should be 30");
        
        // Test 2: Fewer variables than returns
        Assert.AreEqual(10L, ((LuaInteger)env.GetVariable("x")).Value, "x should be 10");
        Assert.AreEqual(20L, ((LuaInteger)env.GetVariable("y")).Value, "y should be 20");
        
        // Test 3: More variables than returns
        Assert.AreEqual(1L, ((LuaInteger)env.GetVariable("p")).Value, "p should be 1");
        Assert.AreEqual(2L, ((LuaInteger)env.GetVariable("q")).Value, "q should be 2");
        Assert.IsTrue(env.GetVariable("r") is LuaNil, "r should be nil");
    }

    [TestMethod]
    public void CompileAndExecute_TableAccessAndStringMethods_WorksCorrectly()
    {
        // Testing Approach: State Transition Testing - Table access and string methods
        // Arrange
        string luaCode = @"
-- Test table access
local t = {value = 42, name = ""test""}
local v = t.value
local n = t.name

-- Test string method with multiple assignment
local s = ""hello world""
local parts = {s:find("" "")}
local idx = parts[1]

-- Test chained access
local nested = {inner = {data = 100}}
local d = nested.inner.data
        ";
        var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
        var outputPath = Path.Combine(_tempDir, "table_custom_method.dll");
        var options = new CompilerOptions(outputPath);

        // Act
        var compileResult = _compiler.Compile(Microsoft.FSharp.Collections.ListModule.ToArray(ast), options);

        // Assert
        Assert.IsTrue(compileResult.Success, "Table custom method should compile successfully");
        
        // Execute and verify
        var assembly = Assembly.LoadFile(outputPath);
        var type = assembly.GetType("CompiledLuaScript+LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        method.Invoke(null, new object[] { env });
        
        Assert.AreEqual(42L, ((LuaInteger)env.GetVariable("v")).Value, "v should be 42");
        Assert.AreEqual("test", ((LuaString)env.GetVariable("n")).Value, "n should be 'test'");
        Assert.AreEqual(6L, ((LuaInteger)env.GetVariable("idx")).Value, "idx should be 6 (position of space)");
        Assert.AreEqual(100L, ((LuaInteger)env.GetVariable("d")).Value, "d should be 100");
    }
}