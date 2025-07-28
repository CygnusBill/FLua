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
            var x = 10L;
            env.SetVariable("x", x);
            var result = 0L;
            env.SetVariable("result", result);
            if (LuaOperations.Greater(x, 5L).IsTruthy)
            {
                result = 1L;
                env.SetVariable("result", result);
            }
            else if (LuaOperations.Equal(x, 5L).IsTruthy)
            {
                result = 2L;
                env.SetVariable("result", result);
            }
            else
            {
                result = 3L;
                env.SetVariable("result", result);
            }

            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "Result:", result });
            return new LuaValue[]
            {
            };
        }
    }
}