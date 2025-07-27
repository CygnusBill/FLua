using System;
using System.Collections.Generic;
using System.Numerics;
using FLua.Runtime;

namespace test_roslyn_comprehensive
{
    public static class LuaScript
    {
        public static LuaValue[] Execute(LuaEnvironment env)
        {
            var x = new LuaInteger(10L);
            env.SetVariable("x", x);
            var y = new LuaInteger(20L);
            env.SetVariable("y", y);
            var z = LuaOperations.Add(x, y);
            env.SetVariable("z", z);
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("x + y ="), z });
            var a = new LuaInteger(5L);
            env.SetVariable("a", a);
            var b = new LuaInteger(3L);
            env.SetVariable("b", b);
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("5 + 3 ="), LuaOperations.Add(a, b) });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("5 - 3 ="), LuaOperations.Subtract(a, b) });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("5 * 3 ="), LuaOperations.Multiply(a, b) });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("5 / 3 ="), LuaOperations.FloatDivide(a, b) });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("5 % 3 ="), LuaOperations.Modulo(a, b) });
            var str1 = new LuaString("Hello");
            env.SetVariable("str1", str1);
            var str2 = new LuaString("World");
            env.SetVariable("str2", str2);
            var combined = LuaOperations.Concat(str1, LuaOperations.Concat(new LuaString(" "), str2));
            env.SetVariable("combined", combined);
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("Combined string:"), combined });
            var t = new LuaBoolean(true);
            env.SetVariable("t", t);
            var f = new LuaBoolean(false);
            env.SetVariable("f", f);
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("true and false ="), LuaOperations.And(t, f) });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("true or false ="), LuaOperations.Or(t, f) });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("10 < 20 ="), LuaOperations.Less(new LuaInteger(10L), new LuaInteger(20L)) });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("10 > 20 ="), LuaOperations.Greater(new LuaInteger(10L), new LuaInteger(20L)) });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("10 == 10 ="), LuaOperations.Equal(new LuaInteger(10L), new LuaInteger(10L)) });
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("10 ~= 20 ="), LuaOperations.NotEqual(new LuaInteger(10L), new LuaInteger(20L)) });
            LuaValue[] add(params LuaValue[] args)
            {
                var a_1 = args.Length > 0 ? args[0] : LuaValue.Nil;
                env.SetVariable("a", a_1);
                var b_2 = args.Length > 1 ? args[1] : LuaValue.Nil;
                env.SetVariable("b", b_2);
                return new LuaValue[]
                {
                    LuaOperations.Add(a_1, b_2)
                };
            }

            var add_func = new LuaUserFunction(add);
            env.SetVariable("add", add_func);
            var result = ((LuaFunction)add_func).Call(new LuaValue[] { new LuaInteger(100L), new LuaInteger(200L) })[0];
            env.SetVariable("result", result);
            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("add(100, 200) ="), result });
            var shadow = new LuaString("outer");
            env.SetVariable("shadow", shadow);
            {
                var shadow_3 = new LuaString("inner");
                env.SetVariable("shadow", shadow_3);
                ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("Inside block, shadow ="), shadow_3 });
            }

            ((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("Outside block, shadow ="), shadow });
            return new LuaValue[]
            {
                new LuaInteger(42L),
                new LuaString("success")
            };
        }
    }
}