using System;
using System.Collections.Generic;
using System.Numerics;
using FLua.Runtime;

namespace test_roslyn_gen
{
    public static class LuaScript
    {
        public static LuaValue[] Execute(LuaEnvironment env)
        {
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "Testing Roslyn code generator" });
            var x = 42L;
            env.SetVariable("x", x);
            var y = LuaOperations.Add(x, 8L);
            env.SetVariable("y", y);
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "x =", x });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "y =", y });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "Test completed!" });
            return new LuaValue[]
            {
            };
        }
    }
}