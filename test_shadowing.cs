using System;
using System.Collections.Generic;
using System.Numerics;
using FLua.Runtime;

namespace CompiledLuaScript;

public static class LuaScript
{
    public static LuaValue[] Execute(LuaEnvironment env)
    {
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "Testing variable shadowing" })        ;
var x = 10        ;
        env.SetVariable("x", x);
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "outer x =", x })        ;
        {
var x_1 = 20            ;
            env.SetVariable("x", x_1);
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "inner x =", x_1 })            ;
            {
var x_2 = 30                ;
                env.SetVariable("x", x_2);
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "inner inner x =", x_2 })                ;
            }
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "back to inner x =", x_1 })            ;
        }
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "back to outer x =", x })        ;
var y = 100        ;
        env.SetVariable("y", y);
LuaValue[] test(params LuaValue[] args)         {
var y_3 = 200            ;
            env.SetVariable("y", y_3);
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "function y =", y_3 })            ;
if (new LuaBoolean(true)            .IsTruthy)
            {
var y_4 = 300                ;
                env.SetVariable("y", y_4);
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "if block y =", y_4 })                ;
            }
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "back to function y =", y_3 })            ;
            return new LuaValue[0];
        }
        var test_func = new LuaUserFunction(test);
        env.SetVariable("test", test_func);
((LuaFunction)test).Call(new LuaValue[] {  })        ;
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "outer y =", y })        ;
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { "Variable shadowing test completed!" })        ;
        return new LuaValue[0];
    }
}
