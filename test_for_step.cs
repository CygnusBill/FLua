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
            LuaValue count = 0L;
            env.SetVariable("count", count);
            {
                var i_start = 10L;
                var i_stop = 1L;
                var i_step = LuaOperations.Negate(2L);
                double i_start_num = LuaTypeConversion.ToNumber(i_start) ?? 0;
                double i_stop_num = LuaTypeConversion.ToNumber(i_stop) ?? 0;
                double i_step_num = LuaTypeConversion.ToNumber(i_step) ?? 1;
                LuaValue i = null;
                for (double i_num = i_start_num; (i_step_num > 0 && i_num <= i_stop_num) || (i_step_num < 0 && i_num >= i_stop_num); i_num += i_step_num)
                {
                    i = i_num;
                    env.SetVariable("i", i);
                    count = LuaOperations.Add(count, 1L);
                    env.SetVariable("count", count);
                }
            }

            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "Count:", count });
            return new LuaValue[]
            {
            };
        }
    }
}