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
            LuaValue obj = LuaOperations.CreateTable(new LuaValue[] { "value", 42L, "getValue", null });
            env.SetVariable("obj", obj);
            LuaValue result = ((LuaFunction)LuaOperations.GetMethod(env, obj, "getValue")).Call(new LuaValue[] { obj })[0];
            env.SetVariable("result", result);
            LuaValue multi = LuaOperations.CreateTable(new LuaValue[] { "getTwo", null });
            env.SetVariable("multi", multi);
            LuaValue x = ((LuaFunction)LuaOperations.GetMethod(env, multi, "getTwo")).Call(new LuaValue[] { multi })[0];
            env.SetVariable("x", x);
            LuaValue y = LuaValue.Nil;
            env.SetVariable("y", y);
            return new LuaValue[]
            {
            };
        }
    }
}