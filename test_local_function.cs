using System;
using System.Collections.Generic;
using System.Numerics;
using FLua.Runtime;

namespace CompiledLuaScript;

public static class LuaScript
{
    public static LuaValue[] Execute(LuaEnvironment env)
    {
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("Testing local function definitions") })        ;
LuaValue[] add(params LuaValue[] args)         {
            var a = args.Length > 0 ? args[0] : LuaValue.Nil;
            env.SetVariable("a", a);
            var b = args.Length > 1 ? args[1] : LuaValue.Nil;
            env.SetVariable("b", b);
return new LuaValue[] { LuaOperations.Add(env.GetVariable("a"), env.GetVariable("b"))             };
            return new LuaValue[0];
        }
        var add_func = new LuaUserFunction(add);
        env.SetVariable("add", add_func);
var result = ((LuaFunction)env.GetVariable("add")).Call(new LuaValue[] { new LuaInteger(10), new LuaInteger(20) })[0]        ;
        env.SetVariable("result", result);
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("add(10, 20) ="), env.GetVariable("result") })        ;
LuaValue[] divmod(params LuaValue[] args)         {
            var a = args.Length > 0 ? args[0] : LuaValue.Nil;
            env.SetVariable("a", a);
            var b = args.Length > 1 ? args[1] : LuaValue.Nil;
            env.SetVariable("b", b);
return new LuaValue[] { LuaOperations.FloatDivide(env.GetVariable("a"), env.GetVariable("b")), LuaOperations.Modulo(env.GetVariable("a"), env.GetVariable("b"))             };
            return new LuaValue[0];
        }
        var divmod_func = new LuaUserFunction(divmod);
        env.SetVariable("divmod", divmod_func);
var div = ((LuaFunction)env.GetVariable("divmod")).Call(new LuaValue[] { new LuaInteger(17), new LuaInteger(5) })[0]        ;
        env.SetVariable("div", div);
var mod = LuaValue.Nil        ;
        env.SetVariable("mod", mod);
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("divmod(17, 5) ="), env.GetVariable("div"), env.GetVariable("mod") })        ;
var x = new LuaInteger(100)        ;
        env.SetVariable("x", x);
LuaValue[] addX(params LuaValue[] args)         {
            var y = args.Length > 0 ? args[0] : LuaValue.Nil;
            env.SetVariable("y", y);
return new LuaValue[] { LuaOperations.Add(env.GetVariable("x"), env.GetVariable("y"))             };
            return new LuaValue[0];
        }
        var addX_func = new LuaUserFunction(addX);
        env.SetVariable("addX", addX_func);
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("addX(50) ="), ((LuaFunction)env.GetVariable("addX")).Call(new LuaValue[] { new LuaInteger(50) })[0] })        ;
LuaValue[] outer(params LuaValue[] args)         {
            var a = args.Length > 0 ? args[0] : LuaValue.Nil;
            env.SetVariable("a", a);
LuaValue[] inner(params LuaValue[] args)             {
                var b = args.Length > 0 ? args[0] : LuaValue.Nil;
                env.SetVariable("b", b);
return new LuaValue[] { LuaOperations.Add(env.GetVariable("a"), env.GetVariable("b"))                 };
                return new LuaValue[0];
            }
            var inner_func = new LuaUserFunction(inner);
            env.SetVariable("inner", inner_func);
return new LuaValue[] { ((LuaFunction)env.GetVariable("inner")).Call(new LuaValue[] { new LuaInteger(10) })[0]             };
            return new LuaValue[0];
        }
        var outer_func = new LuaUserFunction(outer);
        env.SetVariable("outer", outer_func);
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("outer(5) ="), ((LuaFunction)env.GetVariable("outer")).Call(new LuaValue[] { new LuaInteger(5) })[0] })        ;
((LuaFunction)env.GetVariable("print")).Call(new LuaValue[] { new LuaString("Local function test completed!") })        ;
        return new LuaValue[0];
    }
}
