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
            var x = new LuaInteger(10L);
            env.SetVariable("x", x);
            var result = new LuaInteger(0L);
            env.SetVariable("result", result);
            if (LuaOperations.Greater(x, new LuaInteger(5L)).IsTruthy)
            {
                result = new LuaInteger(1L);
                env.SetVariable("result", result);
            }
            else if (LuaOperations.Equal(x, new LuaInteger(5L)).IsTruthy)
            {
                result = new LuaInteger(2L);
                env.SetVariable("result", result);
            }
            else
            {
                result = new LuaInteger(3L);
                env.SetVariable("result", result);
            }

            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("Result:"), result });
            return new LuaValue[]
            {
            };
        }
    }
}