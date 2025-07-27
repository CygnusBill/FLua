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
            LuaValue i = new LuaInteger(0L);
            env.SetVariable("i", i);
            LuaValue sum = new LuaInteger(0L);
            env.SetVariable("sum", sum);
            while (LuaOperations.Less(i, new LuaInteger(5L)).IsTruthy)
            {
                sum = LuaOperations.Add(sum, i);
                env.SetVariable("sum", sum);
                i = LuaOperations.Add(i, new LuaInteger(1L));
                env.SetVariable("i", i);
            }

            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("Sum:"), sum });
            return new LuaValue[]
            {
            };
        }
    }
}