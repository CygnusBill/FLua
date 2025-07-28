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
            LuaValue[] multi(params LuaValue[] args)
            {
                return new LuaValue[]
                {
                    10L,
                    20L,
                    30L
                };
            }

            var multi_func = new LuaUserFunction(multi);
            env.SetVariable("multi", multi_func);
            LuaValue[] _results_0 = ((LuaFunction)multi_func).Call(new LuaValue[] { });
            LuaValue a = _results_0.Length > 0 ? _results_0[0] : LuaValue.Nil;
            env.SetVariable("a", a);
            LuaValue b = _results_0.Length > 1 ? _results_0[1] : LuaValue.Nil;
            env.SetVariable("b", b);
            LuaValue c = _results_0.Length > 2 ? _results_0[2] : LuaValue.Nil;
            env.SetVariable("c", c);
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "a =", a });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "b =", b });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "c =", c });
            LuaValue[] _results_1 = ((LuaFunction)multi_func).Call(new LuaValue[] { });
            LuaValue x = _results_1.Length > 0 ? _results_1[0] : LuaValue.Nil;
            env.SetVariable("x", x);
            LuaValue y = _results_1.Length > 1 ? _results_1[1] : LuaValue.Nil;
            env.SetVariable("y", y);
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "x =", x });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "y =", y });
            LuaValue[] dual(params LuaValue[] args)
            {
                return new LuaValue[]
                {
                    1L,
                    2L
                };
            }

            var dual_func = new LuaUserFunction(dual);
            env.SetVariable("dual", dual_func);
            LuaValue[] _results_2 = ((LuaFunction)dual_func).Call(new LuaValue[] { });
            LuaValue p = _results_2.Length > 0 ? _results_2[0] : LuaValue.Nil;
            env.SetVariable("p", p);
            LuaValue q = _results_2.Length > 1 ? _results_2[1] : LuaValue.Nil;
            env.SetVariable("q", q);
            LuaValue r = _results_2.Length > 2 ? _results_2[2] : LuaValue.Nil;
            env.SetVariable("r", r);
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "p =", p });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "q =", q });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "r =", r });
            return new LuaValue[]
            {
            };
        }

        public static int Main(string[] args)
        {
            return LuaConsoleRunner.Run(Execute, args);
        }
    }
}