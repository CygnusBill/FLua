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
            LuaValue obj = LuaOperations.CreateTable(new LuaValue[] { new LuaString("value"), new LuaInteger(42L), new LuaString("getValue"), null });
            env.SetVariable("obj", obj);
            LuaValue result = ((LuaFunction)LuaOperations.GetMethod(env, obj, new LuaString("getValue"))).Call(new LuaValue[] { obj })[0];
            env.SetVariable("result", result);
            LuaValue multi = LuaOperations.CreateTable(new LuaValue[] { new LuaString("getTwo"), null });
            env.SetVariable("multi", multi);
            LuaValue x = ((LuaFunction)LuaOperations.GetMethod(env, multi, new LuaString("getTwo"))).Call(new LuaValue[] { multi })[0];
            env.SetVariable("x", x);
            LuaValue y = LuaValue.Nil;
            env.SetVariable("y", y);
            return new LuaValue[]
            {
            };
        }
    }
}