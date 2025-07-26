using System;
using System.Reflection;
using FLua.Runtime;

class Program
{
    static void Main()
    {
        // Load the compiled Lua library
        var assembly = Assembly.LoadFile("/Users/bill/Repos/FLua/simple_test.dll");
        var luaScriptType = assembly.GetType("CompiledLuaScript.LuaScript");
        var executeMethod = luaScriptType.GetMethod("Execute");
        
        // Create environment with print function
        var env = new LuaEnvironment();
        LuaEnvironment.SetupStandardLibrary(env);
        
        // Execute the compiled Lua script
        var result = (LuaValue[])executeMethod.Invoke(null, new object[] { env });
        
        Console.WriteLine("Compiled Lua script executed successfully!");
    }
}