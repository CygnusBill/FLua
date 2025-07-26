using System;
using System.Collections.Generic;
using System.Numerics;
using FLua.Runtime;

namespace CompiledLuaScript;

public static class LuaScript
{
    public static LuaValue[] Execute(LuaEnvironment env)
    {
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("testing local variables") })        ;
        // TODO: Implement LocalFunctionDef
var result1 = ((LuaFunction)env.GetVariable("f")).Call(new LuaValue[] { new LuaInteger(10) })[0]        ;
        env.SetVariable("result1", result1);
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("f(10) result:"), env.GetVariable("result1") })        ;
        {
var i = new LuaInteger(10)            ;
            env.SetVariable("i", i);
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("outer i:"), env.GetVariable("i") })            ;
            {
var i = new LuaInteger(100)                ;
                env.SetVariable("i", i);
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("inner i:"), env.GetVariable("i") })                ;
            }
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("outer i again:"), env.GetVariable("i") })            ;
        }
var a = new LuaInteger(1)        ;
        env.SetVariable("a", a);
var b = new LuaInteger(2)        ;
        env.SetVariable("b", b);
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("a ="), env.GetVariable("a"), new LuaString("b ="), env.GetVariable("b") })        ;
var x = new LuaInteger(10)        ;
        env.SetVariable("x", x);
var y = new LuaInteger(20)        ;
        env.SetVariable("y", y);
var z = LuaValue.Nil        ;
        env.SetVariable("z", z);
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("x ="), env.GetVariable("x"), new LuaString("y ="), env.GetVariable("y"), new LuaString("z ="), env.GetVariable("z") })        ;
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("Local variables test completed!") })        ;
        return new LuaValue[0];
    }
}
