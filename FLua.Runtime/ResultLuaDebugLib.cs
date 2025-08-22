using System;
using System.Collections.Generic;
using FLua.Common;

namespace FLua.Runtime
{
    /// <summary>
    /// Result-based version of Lua Debug Library implementation
    /// Converts all exception-based error handling to Result pattern
    /// </summary>
    public static class ResultLuaDebugLib
    {
        #region Debug Library Functions

        public static Result<LuaValue[]> GetInfoResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'getinfo' (function or level expected)");

            var target = args[0];
            var what = args.Length > 1 ? args[1].AsString() : "flnStu";

            if (!target.IsFunction && !target.IsInteger)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'getinfo' (function or level expected)");

            // Create a debug info table
            var info = new LuaTable();
            
            if (target.IsFunction)
            {
                var func = target.AsFunction();
                
                // Add function information
                info.Set(LuaValue.String("name"), LuaValue.String(func.ToString() ?? "function"));
                info.Set(LuaValue.String("namewhat"), LuaValue.String("global"));
                info.Set(LuaValue.String("what"), LuaValue.String("Lua"));
                info.Set(LuaValue.String("source"), LuaValue.String("=[C]"));
                info.Set(LuaValue.String("short_src"), LuaValue.String("[C]"));
                info.Set(LuaValue.String("linedefined"), LuaValue.Integer(-1));
                info.Set(LuaValue.String("lastlinedefined"), LuaValue.Integer(-1));
                info.Set(LuaValue.String("nups"), LuaValue.Integer(0));
                info.Set(LuaValue.String("nparams"), LuaValue.Integer(0));
                info.Set(LuaValue.String("isvararg"), LuaValue.Boolean(true));
                info.Set(LuaValue.String("func"), target);
            }
            else if (target.IsInteger)
            {
                var level = (int)target.AsInteger();
                
                // For stack level queries, return minimal info
                info.Set(LuaValue.String("name"), LuaValue.Nil);
                info.Set(LuaValue.String("namewhat"), LuaValue.String(""));
                info.Set(LuaValue.String("what"), LuaValue.String("main"));
                info.Set(LuaValue.String("source"), LuaValue.String("=[C]"));
                info.Set(LuaValue.String("short_src"), LuaValue.String("[C]"));
                info.Set(LuaValue.String("linedefined"), LuaValue.Integer(0));
                info.Set(LuaValue.String("lastlinedefined"), LuaValue.Integer(-1));
                info.Set(LuaValue.String("currentline"), LuaValue.Integer(-1));
            }

            return Result<LuaValue[]>.Success([LuaValue.Table(info)]);
        }

        public static Result<LuaValue[]> GetLocalResult(LuaValue[] args)
        {
            // Debug library local variable access not fully implemented
            return Result<LuaValue[]>.Success([LuaValue.Nil]);
        }

        public static Result<LuaValue[]> SetLocalResult(LuaValue[] args)
        {
            // Debug library local variable setting not fully implemented
            return Result<LuaValue[]>.Success([LuaValue.Nil]);
        }

        public static Result<LuaValue[]> GetUpvalueResult(LuaValue[] args)
        {
            // Debug library upvalue access not fully implemented
            return Result<LuaValue[]>.Success([LuaValue.Nil]);
        }

        public static Result<LuaValue[]> SetUpvalueResult(LuaValue[] args)
        {
            // Debug library upvalue setting not fully implemented
            return Result<LuaValue[]>.Success([LuaValue.Nil]);
        }

        public static Result<LuaValue[]> TracebackResult(LuaValue[] args)
        {
            var message = args.Length > 0 && !args[0].IsNil ? args[0].AsString() : "";
            var level = args.Length > 1 && args[1].IsInteger ? (int)args[1].AsInteger() : 1;

            // Create a simple traceback string
            var traceback = string.IsNullOrEmpty(message) 
                ? "stack traceback:\n\t[C]: in main chunk"
                : $"{message}\nstack traceback:\n\t[C]: in main chunk";

            return Result<LuaValue[]>.Success([LuaValue.String(traceback)]);
        }

        #endregion
    }
}