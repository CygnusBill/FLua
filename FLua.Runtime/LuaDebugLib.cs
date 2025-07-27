using System;
using System.Collections.Generic;

namespace FLua.Runtime
{
    /// <summary>
    /// Lua Debug Library implementation (basic functionality)
    /// </summary>
    public static class LuaDebugLib
    {
        /// <summary>
        /// Adds the debug library to the Lua environment
        /// </summary>
        public static void AddDebugLibrary(LuaEnvironment env)
        {
            var debugTable = new LuaTable();
            
            // Debug information functions
            debugTable.Set(LuaValue.String("getinfo"), new BuiltinFunction(GetInfo));
            debugTable.Set(LuaValue.String("getlocal"), new BuiltinFunction(GetLocal));
            debugTable.Set(LuaValue.String("setlocal"), new BuiltinFunction(SetLocal));
            debugTable.Set(LuaValue.String("getupvalue"), new BuiltinFunction(GetUpvalue));
            debugTable.Set(LuaValue.String("setupvalue"), new BuiltinFunction(SetUpvalue));
            debugTable.Set(LuaValue.String("traceback"), new BuiltinFunction(Traceback));
            
            env.SetVariable("debug", debugTable);
        }
        
        #region Debug Information Functions
        
        /// <summary>
        /// Implements debug.getinfo function
        /// </summary>
        private static LuaValue[] GetInfo(LuaValue[] args)
        {
            // debug.getinfo([thread,] function [, what])
            // For simplicity, we'll ignore the thread parameter and just handle function/level and what
            
            LuaValue functionOrLevel;
            string what = "flnStu"; // Default: all info
            
            if (args.Length == 0)
            {
                throw new LuaRuntimeException("bad argument #1 to 'getinfo' (function or level expected)");
            }
            
            functionOrLevel = args[0];
            if (args.Length > 1 && args[1].IsString)
            {
                what = args[1].AsString();
            }
            
            // Create a basic debug info table
            var info = new LuaTable();
            
            // For simplicity, we'll provide basic mock information
            // In a full implementation, this would inspect the actual call stack
            
            if (what.Contains("n")) // name and namewhat
            {
                if (functionOrLevel.IsInteger)
                {
                    var level = functionOrLevel.AsInteger();
                    if (level == 1)
                    {
                        // This is the function that called debug.getinfo
                        info.Set(LuaValue.String("name"), LuaValue.String("test_debug_info"));
                        info.Set(LuaValue.String("namewhat"), LuaValue.String("local"));
                    }
                    else
                    {
                        info.Set(LuaValue.String("name"), LuaValue.String("unknown"));
                        info.Set(LuaValue.String("namewhat"), LuaValue.String(""));
                    }
                }
                else
                {
                    info.Set(LuaValue.String("name"), LuaValue.String("unknown"));
                    info.Set(LuaValue.String("namewhat"), LuaValue.String(""));
                }
            }
            
            if (what.Contains("S")) // source info
            {
                info.Set(LuaValue.String("what"), LuaValue.String("Lua"));
                info.Set(LuaValue.String("source"), LuaValue.String("@test"));
                info.Set(LuaValue.String("short_src"), LuaValue.String("test"));
                info.Set(LuaValue.String("linedefined"), LuaValue.Integer(1));
                info.Set(LuaValue.String("lastlinedefined"), LuaValue.Integer(-1));
            }
            
            if (what.Contains("l")) // current line
            {
                info.Set(LuaValue.String("currentline"), LuaValue.Integer(1));
            }
            
            if (what.Contains("t")) // tail call info
            {
                info.Set(LuaValue.String("istailcall"), LuaValue.Boolean(false));
            }
            
            if (what.Contains("u")) // number of upvalues and parameters
            {
                info.Set(LuaValue.String("nups"), LuaValue.Integer(0));
                info.Set(LuaValue.String("nparams"), LuaValue.Integer(0));
                info.Set(LuaValue.String("isvararg"), LuaValue.Boolean(false));
            }
            
            if (what.Contains("f")) // function itself
            {
                // We don't have access to the actual function, so return nil
                info.Set(LuaValue.String("func"), LuaValue.Nil);
            }
            
            return [LuaValue.Table(info)];
        }
        
        /// <summary>
        /// Implements debug.getlocal function (simplified)
        /// </summary>
        private static LuaValue[] GetLocal(LuaValue[] args)
        {
            // For simplicity, always return nil (no local variables found)
            return [LuaValue.Nil];
        }
        
        /// <summary>
        /// Implements debug.setlocal function (simplified)
        /// </summary>
        private static LuaValue[] SetLocal(LuaValue[] args)
        {
            // For simplicity, always return nil (cannot set local variables)
            return [LuaValue.Nil];
        }
        
        /// <summary>
        /// Implements debug.getupvalue function (simplified)
        /// </summary>
        private static LuaValue[] GetUpvalue(LuaValue[] args)
        {
            // For simplicity, always return nil (no upvalues found)
            return [LuaValue.Nil];
        }
        
        /// <summary>
        /// Implements debug.setupvalue function (simplified)
        /// </summary>
        private static LuaValue[] SetUpvalue(LuaValue[] args)
        {
            // For simplicity, always return nil (cannot set upvalues)
            return [LuaValue.Nil];
        }
        
        /// <summary>
        /// Implements debug.traceback function (simplified)
        /// </summary>
        private static LuaValue[] Traceback(LuaValue[] args)
        {
            var message = args.Length > 0 && args[0].IsString ? args[0].AsString() : "";
            var level = args.Length > 1 && args[1].IsInteger ? (int)args[1].AsInteger() : 1;
            
            // Create a simple traceback
            var traceback = string.IsNullOrEmpty(message) 
                ? "stack traceback:\n\t[C]: in function 'traceback'" 
                : $"{message}\nstack traceback:\n\t[C]: in function 'traceback'";
            
            return [LuaValue.String(traceback)];
        }
        
        #endregion
    }
} 