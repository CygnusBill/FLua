using System;
using System.Reflection;
using FLua.Runtime;

class TestRunner
{
    static void Main()
    {
        var assembly = Assembly.LoadFile("/Users/bill/Repos/FLua/test_inline_simple.dll");
        var type = assembly.GetType("CompiledLuaScript.LuaScript");
        var method = type.GetMethod("Execute");
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        var result = method.Invoke(null, new object[] { env });
        Console.WriteLine("Execution completed");
    }
}