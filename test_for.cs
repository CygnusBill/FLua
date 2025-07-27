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
            LuaValue sum = new LuaInteger(0L);
            env.SetVariable("sum", sum);
            {
                var i_start = new LuaInteger(1L);
                var i_stop = new LuaInteger(5L);
                var i_step = new LuaNumber(1);
                double i_start_num = LuaTypeConversion.ToNumber(i_start) ?? 0;
                double i_stop_num = LuaTypeConversion.ToNumber(i_stop) ?? 0;
                double i_step_num = LuaTypeConversion.ToNumber(i_step) ?? 1;
                LuaValue i = null;
                for (double i_num = i_start_num; (i_step_num > 0 && i_num <= i_stop_num) || (i_step_num < 0 && i_num >= i_stop_num); i_num += i_step_num)
                {
                    i = new LuaNumber(i_num);
                    env.SetVariable("i", i);
                    sum = LuaOperations.Add(sum, i);
                    env.SetVariable("sum", sum);
                }
            }

            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("Sum:"), sum });
            return new LuaValue[]
            {
            };
        }
    }
}