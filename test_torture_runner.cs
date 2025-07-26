using System;
using System.Reflection;
using FLua.Runtime;

class Program
{
    static void Main()
    {
        try
        {
            // Load the compiled Lua library
            var assembly = Assembly.LoadFile("/Users/bill/Repos/FLua/test_torture.dll");
            var luaScriptType = assembly.GetType("CompiledLuaScript.LuaScript");
            var executeMethod = luaScriptType.GetMethod("Execute");
            
            // Create environment with standard library
            var env = LuaEnvironment.CreateStandardEnvironment();
            
            // Execute the compiled Lua script
            var result = (LuaValue[])executeMethod.Invoke(null, new object[] { env });
            
            Console.WriteLine("Compiled Lua torture test executed successfully!");
            
            // Check some final variable values
            var x = env.GetVariable("x");
            var y = env.GetVariable("y");
            var combined = env.GetVariable("combined");
            
            Console.WriteLine($"Final x = {x}");
            Console.WriteLine($"Final y = {y}");
            Console.WriteLine($"Final combined = {combined}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing compiled Lua: {ex}");
        }
    }
}