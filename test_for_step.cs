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
            LuaValue count = new LuaInteger(0L);
            env.SetVariable("count", count);
            {
                var i_start = new LuaInteger(10L);
                var i_stop = new LuaInteger(1L);
                var i_step = LuaOperations.Negate(new LuaInteger(2L));
                double i_start_num = LuaTypeConversion.ToNumber(i_start) ?? 0;
                double i_stop_num = LuaTypeConversion.ToNumber(i_stop) ?? 0;
                double i_step_num = LuaTypeConversion.ToNumber(i_step) ?? 1;
                LuaValue i = null;
                for (double i_num = i_start_num; (i_step_num > 0 && i_num <= i_stop_num) || (i_step_num < 0 && i_num >= i_stop_num); i_num += i_step_num)
                {
                    i = new LuaNumber(i_num);
                    env.SetVariable("i", i);
                    count = LuaOperations.Add(count, new LuaInteger(1L));
                    env.SetVariable("count", count);
                }
            }

            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("Count:"), count });
            return new LuaValue[]
            {
            };
        }
    }
}