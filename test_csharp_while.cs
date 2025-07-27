using System;
using FLua.Runtime;

class Program
{
    static void Main()
    {
        var env = LuaEnvironment.CreateStandardEnvironment();
        
        // local i = 0
        LuaValue i = new LuaInteger(0);
        
        // while i < 3 do
        while (LuaOperations.Less(i, new LuaInteger(3)).IsTruthy)
        {
            // print("i =", i)
            var print = env.GetVariable("print") as LuaFunction;
            print?.Call(new[] { new LuaString("i ="), i });
            
            // i = i + 1
            i = LuaOperations.Add(i, new LuaInteger(1));
        }
        
        // print("Done, i =", i)
        var print2 = env.GetVariable("print") as LuaFunction;
        print2?.Call(new[] { new LuaString("Done, i ="), i });
    }
}