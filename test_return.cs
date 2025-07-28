using System;
using System.Collections.Generic;
using System.Numerics;
using FLua.Runtime;

namespace CompiledLuaScript;

public static class LuaScript
{
    public static LuaValue[] Execute(LuaEnvironment env)
    {
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "Testing return statements" })        ;
        // TODO: Implement LocalFunctionDef
var result = ((LuaFunction)env.GetVariable("add")).Call(new LuaValue[] { 10, 20 })[0]        ;
        env.SetVariable("result", result);
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "add(10, 20) =", env.GetVariable("result") })        ;
        // TODO: Implement LocalFunctionDef
var div = ((LuaFunction)env.GetVariable("divmod")).Call(new LuaValue[] { 17, 5 })[0]        ;
        env.SetVariable("div", div);
var mod = LuaValue.Nil        ;
        env.SetVariable("mod", mod);
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "divmod(17, 5) =", env.GetVariable("div"), env.GetVariable("mod") })        ;
        // TODO: Implement LocalFunctionDef
var nothing = ((LuaFunction)env.GetVariable("doNothing")).Call(new LuaValue[] {  })[0]        ;
        env.SetVariable("nothing", nothing);
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "doNothing() returned:", LuaOperations.Not(env.GetVariable("hing")) })        ;
        // TODO: Implement LocalFunctionDef
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "abs(-5) =", ((LuaFunction)env.GetVariable("abs")).Call(new LuaValue[] { LuaOperations.Negate(5) })[0] })        ;
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "abs(5) =", ((LuaFunction)env.GetVariable("abs")).Call(new LuaValue[] { 5 })[0] })        ;
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "Return statement test completed!" })        ;
        return new LuaValue[0];
    }
}
