using System;
using System.Collections.Generic;
using System.Numerics;
using FLua.Runtime;

namespace CompiledLuaScript;

public static class LuaScript
{
    public static LuaValue[] Execute(LuaEnvironment env)
    {
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "Testing simple return" })        ;
if (new LuaBoolean(true)        .IsTruthy)
        {
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "This will print" })            ;
return new LuaValue[] { 42, "hello"             };
        }
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "This should not print" })        ;
        return new LuaValue[0];
    }
}
