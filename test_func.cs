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
            LuaValue[] test(params LuaValue[] args)
            {
                return new LuaValue[]
                {
                    LuaValue.Integer(42L)
                };
            }

            var test_func = new BuiltinFunction(test);
            env.SetVariable("test", test_func);
            LuaValue x = test_func.AsFunction().Call(new LuaValue[] { })[0];
            env.SetVariable("x", x);
            env.GetVariable("print").AsFunction().Call(new LuaValue[] { x });
            return new LuaValue[]
            {
            };
        }
    }
}