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
                    new LuaInteger(10L),
                    new LuaInteger(20L),
                    new LuaInteger(30L)
                };
            }

            var multi_func = new LuaUserFunction(multi);
            env.SetVariable("multi", multi_func);
            LuaValue[] _results_0 = ((LuaFunction)multi_func).Call(new LuaValue[] { });
            LuaValue a = _results_0.Length > 0 ? _results_0[0] : LuaNil.Instance;
            env.SetVariable("a", a);
            LuaValue b = _results_0.Length > 1 ? _results_0[1] : LuaNil.Instance;
            env.SetVariable("b", b);
            LuaValue c = _results_0.Length > 2 ? _results_0[2] : LuaNil.Instance;
            env.SetVariable("c", c);
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("a ="), a });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("b ="), b });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("c ="), c });
            LuaValue[] _results_1 = ((LuaFunction)multi_func).Call(new LuaValue[] { });
            LuaValue x = _results_1.Length > 0 ? _results_1[0] : LuaNil.Instance;
            env.SetVariable("x", x);
            LuaValue y = _results_1.Length > 1 ? _results_1[1] : LuaNil.Instance;
            env.SetVariable("y", y);
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("x ="), x });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("y ="), y });
            LuaValue[] dual(params LuaValue[] args)
            {
                return new LuaValue[]
                {
                    new LuaInteger(1L),
                    new LuaInteger(2L)
                };
            }

            var dual_func = new LuaUserFunction(dual);
            env.SetVariable("dual", dual_func);
            LuaValue[] _results_2 = ((LuaFunction)dual_func).Call(new LuaValue[] { });
            LuaValue p = _results_2.Length > 0 ? _results_2[0] : LuaNil.Instance;
            env.SetVariable("p", p);
            LuaValue q = _results_2.Length > 1 ? _results_2[1] : LuaNil.Instance;
            env.SetVariable("q", q);
            LuaValue r = _results_2.Length > 2 ? _results_2[2] : LuaNil.Instance;
            env.SetVariable("r", r);
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("p ="), p });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("q ="), q });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("r ="), r });
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