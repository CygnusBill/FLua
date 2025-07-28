using System;
using System.Collections.Generic;
using System.Numerics;
using FLua.Runtime;

namespace CompiledLuaScript;

public static class LuaScript
{
    public static LuaValue[] Execute(LuaEnvironment env)
    {
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "testing local variables" })        ;
        // TODO: Implement LocalFunctionDef
var result1 = ((LuaFunction)env.GetVariable("f")).Call(new LuaValue[] { 10 })[0]        ;
        env.SetVariable("result1", result1);
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "f(10) result:", env.GetVariable("result1") })        ;
        {
var i = 10            ;
            env.SetVariable("i", i);
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "outer i:", env.GetVariable("i") })            ;
            {
var i = 100                ;
                env.SetVariable("i", i);
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "inner i:", env.GetVariable("i") })                ;
            }
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "outer i again:", env.GetVariable("i") })            ;
        }
var a = 1        ;
        env.SetVariable("a", a);
var b = 2        ;
        env.SetVariable("b", b);
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "a =", env.GetVariable("a"), "b =", env.GetVariable("b") })        ;
var x = 10        ;
        env.SetVariable("x", x);
var y = 20        ;
        env.SetVariable("y", y);
var z = LuaValue.Nil        ;
        env.SetVariable("z", z);
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "x =", env.GetVariable("x"), "y =", env.GetVariable("y"), "z =", env.GetVariable("z") })        ;
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "Local variables test completed!" })        ;
        return new LuaValue[0];
    }
}
