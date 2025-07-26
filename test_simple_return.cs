using System;
using System.Collections.Generic;
using System.Numerics;
using FLua.Runtime;

namespace CompiledLuaScript;

public static class LuaScript
{
    public static LuaValue[] Execute(LuaEnvironment env)
    {
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("Testing simple return") })        ;
if (new LuaBoolean(true)        .IsTruthy)
        {
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("This will print") })            ;
return new LuaValue[] { new LuaInteger(42), new LuaString("hello")             };
        }
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("This should not print") })        ;
        return new LuaValue[0];
    }
}
