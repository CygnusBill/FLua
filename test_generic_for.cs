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
            LuaValue t = LuaOperations.CreateTable(new LuaValue[] { LuaValue.String("a"), LuaValue.Integer(1L), LuaValue.String("b"), LuaValue.Integer(2L), LuaValue.String("c"), LuaValue.Integer(3L) });
            env.SetVariable("t", t);
            env.GetVariable("print").AsFunction().Call(new LuaValue[] { LuaValue.String("Test 1: pairs") });
            {
                LuaValue[] _iter_values = env.GetVariable("pairs").AsFunction().Call(new LuaValue[] { t });
                LuaValue _iter_func = _iter_values.Length > 0 ? _iter_values[0] : LuaValue.Nil;
                if (!_iter_func.IsFunction)
                    throw new LuaRuntimeException("bad argument #1 to 'for iterator' (function expected)");
                LuaValue _iter_state = _iter_values.Length > 1 ? _iter_values[1] : LuaValue.Nil;
                LuaValue _iter_control = _iter_values.Length > 2 ? _iter_values[2] : LuaValue.Nil;
                while (true)
                {
                    LuaValue[] _iter_result = _iter_func.AsFunction<LuaFunction>().Call(new LuaValue[] { _iter_state, _iter_control });
                    LuaValue _first_result = _iter_result.Length > 0 ? _iter_result[0] : LuaValue.Nil;
                    if (_first_result.IsNil)
                        break;
                    _iter_control = _first_result;
                    LuaValue k = _iter_result.Length > 0 ? _iter_result[0] : LuaValue.Nil;
                    env.SetVariable("k", k);
                    LuaValue v = _iter_result.Length > 1 ? _iter_result[1] : LuaValue.Nil;
                    env.SetVariable("v", v);
                    env.GetVariable("print").AsFunction().Call(new LuaValue[] { k, v });
                }
            }

            LuaValue arr = LuaOperations.CreateTable(new LuaValue[] { LuaValue.Integer(1L), LuaValue.Integer(10L), LuaValue.Integer(2L), LuaValue.Integer(20L), LuaValue.Integer(3L), LuaValue.Integer(30L) });
            env.SetVariable("arr", arr);
            env.GetVariable("print").AsFunction().Call(new LuaValue[] { LuaValue.String("\nTest 2: ipairs") });
            {
                LuaValue[] _iter_values = env.GetVariable("ipairs").AsFunction().Call(new LuaValue[] { arr });
                LuaValue _iter_func = _iter_values.Length > 0 ? _iter_values[0] : LuaValue.Nil;
                if (!_iter_func.IsFunction)
                    throw new LuaRuntimeException("bad argument #1 to 'for iterator' (function expected)");
                LuaValue _iter_state = _iter_values.Length > 1 ? _iter_values[1] : LuaValue.Nil;
                LuaValue _iter_control = _iter_values.Length > 2 ? _iter_values[2] : LuaValue.Nil;
                while (true)
                {
                    LuaValue[] _iter_result = _iter_func.AsFunction<LuaFunction>().Call(new LuaValue[] { _iter_state, _iter_control });
                    LuaValue _first_result = _iter_result.Length > 0 ? _iter_result[0] : LuaValue.Nil;
                    if (_first_result.IsNil)
                        break;
                    _iter_control = _first_result;
                    LuaValue i = _iter_result.Length > 0 ? _iter_result[0] : LuaValue.Nil;
                    env.SetVariable("i", i);
                    LuaValue v = _iter_result.Length > 1 ? _iter_result[1] : LuaValue.Nil;
                    env.SetVariable("v", v);
                    env.GetVariable("print").AsFunction().Call(new LuaValue[] { i, v });
                }
            }

            LuaValue[] multi_iter(params LuaValue[] args)
            {
                return new LuaValue[]
                {
                    LuaValue.Function(new BuiltinFunction(__anon_0)),
                    LuaValue.String("state"),
                    LuaValue.Nil
                };
            }

            var multi_iter_func = LuaValue.Function(new BuiltinFunction(multi_iter));
            env.SetVariable("multi_iter", multi_iter_func);
            env.GetVariable("print").AsFunction().Call(new LuaValue[] { LuaValue.String("\nTest 3: custom iterator") });
            {
                LuaValue[] _iter_values = multi_iter_func.AsFunction().Call(new LuaValue[] { });
                LuaValue _iter_func = _iter_values.Length > 0 ? _iter_values[0] : LuaValue.Nil;
                if (!_iter_func.IsFunction)
                    throw new LuaRuntimeException("bad argument #1 to 'for iterator' (function expected)");
                LuaValue _iter_state = _iter_values.Length > 1 ? _iter_values[1] : LuaValue.Nil;
                LuaValue _iter_control = _iter_values.Length > 2 ? _iter_values[2] : LuaValue.Nil;
                while (true)
                {
                    LuaValue[] _iter_result = _iter_func.AsFunction<LuaFunction>().Call(new LuaValue[] { _iter_state, _iter_control });
                    LuaValue _first_result = _iter_result.Length > 0 ? _iter_result[0] : LuaValue.Nil;
                    if (_first_result.IsNil)
                        break;
                    _iter_control = _first_result;
                    LuaValue k = _iter_result.Length > 0 ? _iter_result[0] : LuaValue.Nil;
                    env.SetVariable("k", k);
                    LuaValue v = _iter_result.Length > 1 ? _iter_result[1] : LuaValue.Nil;
                    env.SetVariable("v", v);
                    env.GetVariable("print").AsFunction().Call(new LuaValue[] { k, v });
                }
            }

            env.GetVariable("print").AsFunction().Call(new LuaValue[] { LuaValue.String("\nTest 4: break") });
            {
                LuaValue[] _iter_values = env.GetVariable("pairs").AsFunction().Call(new LuaValue[] { LuaOperations.CreateTable(new LuaValue[] { LuaValue.String("x"), LuaValue.Integer(10L), LuaValue.String("y"), LuaValue.Integer(20L), LuaValue.String("z"), LuaValue.Integer(30L) }) });
                LuaValue _iter_func = _iter_values.Length > 0 ? _iter_values[0] : LuaValue.Nil;
                if (!_iter_func.IsFunction)
                    throw new LuaRuntimeException("bad argument #1 to 'for iterator' (function expected)");
                LuaValue _iter_state = _iter_values.Length > 1 ? _iter_values[1] : LuaValue.Nil;
                LuaValue _iter_control = _iter_values.Length > 2 ? _iter_values[2] : LuaValue.Nil;
                while (true)
                {
                    LuaValue[] _iter_result = _iter_func.AsFunction<LuaFunction>().Call(new LuaValue[] { _iter_state, _iter_control });
                    LuaValue _first_result = _iter_result.Length > 0 ? _iter_result[0] : LuaValue.Nil;
                    if (_first_result.IsNil)
                        break;
                    _iter_control = _first_result;
                    LuaValue k = _iter_result.Length > 0 ? _iter_result[0] : LuaValue.Nil;
                    env.SetVariable("k", k);
                    LuaValue v = _iter_result.Length > 1 ? _iter_result[1] : LuaValue.Nil;
                    env.SetVariable("v", v);
                    env.GetVariable("print").AsFunction().Call(new LuaValue[] { k, v });
                    if (LuaOperations.Equal(k, LuaValue.String("y")).IsTruthy())
                    {
                        break;
                    }
                }
            }

            env.GetVariable("print").AsFunction().Call(new LuaValue[] { LuaValue.String("\nDone!") });
            return new LuaValue[]
            {
            };
        }

        private static LuaValue[] __anon_0(params LuaValue[] args)
        {
            var state = args.Length > 0 ? args[0] : LuaValue.Nil;
            var key = args.Length > 1 ? args[1] : LuaValue.Nil;
            if (LuaOperations.Equal(key, LuaValue.Nil).IsTruthy())
            {
                return new LuaValue[]
                {
                    LuaValue.Integer(1L),
                    LuaValue.String("one")
                };
            }
            else if (LuaOperations.Equal(key, LuaValue.Integer(1L)).IsTruthy())
            {
                return new LuaValue[]
                {
                    LuaValue.Integer(2L),
                    LuaValue.String("two")
                };
            }
            else
            {
                return new LuaValue[]
                {
                    LuaValue.Nil
                };
            }

            return new LuaValue[];
        }

        public static int Main(string[] args)
        {
            return LuaConsoleRunner.Run(Execute, args);
        }
    }
}