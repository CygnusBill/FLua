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
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("Testing Roslyn code generator") });
            var x = new LuaInteger(42L);
            env.SetVariable("x", x);
            var y = LuaOperations.Add(x, new LuaInteger(8L));
            env.SetVariable("y", y);
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("x ="), x });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("y ="), y });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("Test completed!") });
            return new LuaValue[]
            {
            };
        }
    }
}