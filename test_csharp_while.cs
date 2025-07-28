using System;
using FLua.Runtime;

class Program
{
    static void Main()
    {
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        // local i = 0
        LuaValue i = 0;
        
        // while i < 3 do
        while (LuaOperations.Less(i, 3).IsTruthy)
        {
            // print("i =", i)
            var print = env.GetVariable("print") as LuaFunction;
            print?.Call(new[] { "i =", i });
            
            // i = i + 1
            i = LuaOperations.Add(i, 1);
        }
        
        // print("Done, i =", i)
        var print2 = env.GetVariable("print") as LuaFunction;
        print2?.Call(new[] { "Done, i =", i });
    }
}