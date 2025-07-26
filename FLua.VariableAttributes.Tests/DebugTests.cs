using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Ast;
using FLua.Runtime;
using FLua.Interpreter;

namespace FLua.VariableAttributes.Tests;

[TestClass]
public class DebugTests
{
    private LuaInterpreter _interpreter = null!;

    [TestInitialize]
    public void Setup()
    {
        _interpreter = new LuaInterpreter();
    }

    [TestMethod]
    public void DebugArithmeticTypes()
    {
        // Test what type simple arithmetic returns
        var result1 = _interpreter.ExecuteCode("return 10 + 20");
        Console.WriteLine($"Simple arithmetic: {result1[0].GetType().Name} = {result1[0]}");
        
        // Test what type const variable arithmetic returns
        var result2 = _interpreter.ExecuteCode(@"
            local a <const>, b <const> = 10, 20
            return a + b
        ");
        Console.WriteLine($"Const arithmetic: {result2[0].GetType().Name} = {result2[0]}");
        
        // Test function parameter
        var result3 = _interpreter.ExecuteCode(@"
            local function testConstParam(x <const>)
                return x * 2
            end
            
            return testConstParam(5)
        ");
        Console.WriteLine($"Function param: {result3[0].GetType().Name} = {result3[0]}");
        
        // Test the actual type checks that are failing
        Console.WriteLine($"Is result1 LuaInteger? {result1[0] is LuaInteger}");
        Console.WriteLine($"Is result1 LuaNumber? {result1[0] is LuaNumber}");
        Console.WriteLine($"Is result2 LuaInteger? {result2[0] is LuaInteger}");
        Console.WriteLine($"Is result2 LuaNumber? {result2[0] is LuaNumber}");
        Console.WriteLine($"Is result3 LuaInteger? {result3[0] is LuaInteger}");
        Console.WriteLine($"Is result3 LuaNumber? {result3[0] is LuaNumber}");
        
        // Just verify the types exist
        Assert.IsTrue(result1[0] is LuaValue);
        Assert.IsTrue(result2[0] is LuaValue);
        Assert.IsTrue(result3[0] is LuaValue);
    }
}
