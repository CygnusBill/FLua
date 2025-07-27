using System;
using System.Collections.Generic;
using System.Numerics;
using FLua.Runtime;

namespace CompiledLuaScript
{
    public static class LuaScript
    {
        public static LuaValue[] Execute(LuaEnvironment env)
        {
            LuaValue t = () =>
            {
                var table_0 = new LuaTable();
                table_0.Set(new LuaInteger(1L), new LuaInteger(10L));
                table_0.Set(new LuaInteger(2L), new LuaInteger(20L));
                table_0.Set(new LuaInteger(3L), new LuaInteger(30L));
                return table_0;
            }();
            env.SetVariable("t", t);
            LuaValue a = ((LuaTable)t).Get(new LuaInteger(1L));
            env.SetVariable("a", a);
            LuaValue b = ((LuaTable)t).Get(new LuaInteger(2L));
            env.SetVariable("b", b);
            LuaValue c = ((LuaTable)t).Get(new LuaInteger(3L));
            env.SetVariable("c", c);
            LuaValue sum = LuaOperations.Add(LuaOperations.Add(a, b), c);
            env.SetVariable("sum", sum);
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { sum });
            return new LuaValue[]
            {
            };
        }
    }
}