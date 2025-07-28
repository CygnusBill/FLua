using System;
using System.Collections.Generic;
using System.Numerics;
using FLua.Runtime;

namespace test_console_app
{
    public static class LuaScript
    {
        public static LuaValue[] Execute(LuaEnvironment env)
        {
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "FLua Console Application Test" });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "=============================" });
            var x = 42L;
            env.SetVariable("x", x);
            var y = 8L;
            env.SetVariable("y", y);
            var result = LuaOperations.Add(x, y);
            env.SetVariable("result", result);
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { LuaOperations.Concat("Calculation: ", LuaOperations.Concat(x, LuaOperations.Concat(" + ", LuaOperations.Concat(y, LuaOperations.Concat(" = ", result))))) });
            LuaValue[] greet(params LuaValue[] args)
            {
                var name = args.Length > 0 ? args[0] : LuaValue.Nil;
                env.SetVariable("name", name);
                return new LuaValue[]
                {
                    LuaOperations.Concat("Hello, ", LuaOperations.Concat(name, "!"))
                };
            }

            var greet_func = new LuaUserFunction(greet);
            env.SetVariable("greet", greet_func);
            var message = ((LuaFunction)greet_func).Call(new LuaValue[] { "World" })[0];
            env.SetVariable("message", message);
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { message });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "Test completed successfully!" });
            return new LuaValue[]
            {
                0L
            };
        }

        public static int Main(string[] args)
        {
            return LuaConsoleRunner.Run(Execute, args);
        }
    }
}