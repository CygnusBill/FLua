using System;
using System.Collections.Generic;
using System.Numerics;
using FLua.Runtime;

namespace CompiledLuaScript;

public static class LuaScript
{
    public static LuaValue[] Execute(LuaEnvironment env)
    {
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "Hello from compiled Lua!" })        ;
var x = 42        ;
        env.SetVariable("x", x);
var y = LuaOperations.Add(env.GetVariable("x"), 8)        ;
        env.SetVariable("y", y);
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "Result:", env.GetVariable("y") })        ;
        return new LuaValue[0];
    }
}
