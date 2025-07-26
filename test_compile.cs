using System;
using System.Collections.Generic;
using System.Numerics;
using FLua.Runtime;

namespace CompiledLuaScript;

public static class LuaScript
{
    public static LuaValue[] Execute(LuaEnvironment env)
    {
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("Hello from compiled Lua!") })        ;
var x = new LuaInteger(42)        ;
        env.SetVariable("x", x);
var y = LuaOperations.Add(env.GetVariable("x"), new LuaInteger(8))        ;
        env.SetVariable("y", y);
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("Result:"), env.GetVariable("y") })        ;
        return new LuaValue[0];
    }
}
