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
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("FLua Console Application Test") });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("=============================") });
            var x = new LuaInteger(42L);
            env.SetVariable("x", x);
            var y = new LuaInteger(8L);
            env.SetVariable("y", y);
            var result = LuaOperations.Add(x, y);
            env.SetVariable("result", result);
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { LuaOperations.Concat(new LuaString("Calculation: "), LuaOperations.Concat(x, LuaOperations.Concat(new LuaString(" + "), LuaOperations.Concat(y, LuaOperations.Concat(new LuaString(" = "), result))))) });
            LuaValue[] greet(params LuaValue[] args)
            {
                var name = args.Length > 0 ? args[0] : LuaValue.Nil;
                env.SetVariable("name", name);
                return new LuaValue[]
                {
                    LuaOperations.Concat(new LuaString("Hello, "), LuaOperations.Concat(name, new LuaString("!")))
                };
            }

            var greet_func = new LuaUserFunction(greet);
            env.SetVariable("greet", greet_func);
            var message = ((LuaFunction)greet_func).Call(new LuaValue[] { new LuaString("World") })[0];
            env.SetVariable("message", message);
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { message });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("Test completed successfully!") });
            return new LuaValue[]
            {
                new LuaInteger(0L)
            };
        }

        public static int Main(string[] args)
        {
            return LuaConsoleRunner.Run(Execute, args);
        }
    }
}