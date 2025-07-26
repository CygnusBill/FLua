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
            var assembly = Assembly.LoadFile("/Users/bill/Repos/FLua/test_local_function.dll");
            var luaScriptType = assembly.GetType("CompiledLuaScript.LuaScript");
            var executeMethod = luaScriptType.GetMethod("Execute");
            
            // Create environment with standard library
            var env = LuaEnvironment.CreateStandardEnvironment();
            
            Console.WriteLine("Executing compiled Lua torture test...");
            Console.WriteLine("=====================================");
            
            // Execute the compiled Lua script
            var result = (LuaValue[])executeMethod.Invoke(null, new object[] { env });
            
            Console.WriteLine("=====================================");
            Console.WriteLine("Compiled Lua test executed successfully!");
            
            // Display returned values
            if (result != null && result.Length > 0)
            {
                Console.WriteLine($"Returned {result.Length} value(s):");
                for (int i = 0; i < result.Length; i++)
                {
                    Console.WriteLine($"  [{i}] = {result[i]}");
                }
            }
            else
            {
                Console.WriteLine("No values returned");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing compiled Lua: {ex}");
        }
    }
}