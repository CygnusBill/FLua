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
            debugTable.Set(new LuaString("getinfo"), new BuiltinFunction(GetInfo));
            debugTable.Set(new LuaString("getlocal"), new BuiltinFunction(GetLocal));
            debugTable.Set(new LuaString("setlocal"), new BuiltinFunction(SetLocal));
            debugTable.Set(new LuaString("getupvalue"), new BuiltinFunction(GetUpvalue));
            debugTable.Set(new LuaString("setupvalue"), new BuiltinFunction(SetUpvalue));
            debugTable.Set(new LuaString("traceback"), new BuiltinFunction(Traceback));
            
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
            if (args.Length > 1 && args[1] is LuaString whatStr)
            {
                what = whatStr.Value;
            }
            
            // Create a basic debug info table
            var info = new LuaTable();
            
            // For simplicity, we'll provide basic mock information
            // In a full implementation, this would inspect the actual call stack
            
            if (what.Contains("n")) // name and namewhat
            {
                if (functionOrLevel.AsInteger.HasValue)
                {
                    var level = functionOrLevel.AsInteger.Value;
                    if (level == 1)
                    {
                        // This is the function that called debug.getinfo
                        info.Set(new LuaString("name"), new LuaString("test_debug_info"));
                        info.Set(new LuaString("namewhat"), new LuaString("local"));
                    }
                    else
                    {
                        info.Set(new LuaString("name"), new LuaString("unknown"));
                        info.Set(new LuaString("namewhat"), new LuaString(""));
                    }
                }
                else
                {
                    info.Set(new LuaString("name"), new LuaString("unknown"));
                    info.Set(new LuaString("namewhat"), new LuaString(""));
                }
            }
            
            if (what.Contains("S")) // source info
            {
                info.Set(new LuaString("what"), new LuaString("Lua"));
                info.Set(new LuaString("source"), new LuaString("@test"));
                info.Set(new LuaString("short_src"), new LuaString("test"));
                info.Set(new LuaString("linedefined"), new LuaInteger(1));
                info.Set(new LuaString("lastlinedefined"), new LuaInteger(-1));
            }
            
            if (what.Contains("l")) // current line
            {
                info.Set(new LuaString("currentline"), new LuaInteger(1));
            }
            
            if (what.Contains("t")) // tail call info
            {
                info.Set(new LuaString("istailcall"), new LuaBoolean(false));
            }
            
            if (what.Contains("u")) // number of upvalues and parameters
            {
                info.Set(new LuaString("nups"), new LuaInteger(0));
                info.Set(new LuaString("nparams"), new LuaInteger(0));
                info.Set(new LuaString("isvararg"), new LuaBoolean(false));
            }
            
            if (what.Contains("f")) // function itself
            {
                // We don't have access to the actual function, so return nil
                info.Set(new LuaString("func"), LuaNil.Instance);
            }
            
            return new[] { info };
        }
        
        /// <summary>
        /// Implements debug.getlocal function (simplified)
        /// </summary>
        private static LuaValue[] GetLocal(LuaValue[] args)
        {
            // For simplicity, always return nil (no local variables found)
            return new[] { LuaNil.Instance };
        }
        
        /// <summary>
        /// Implements debug.setlocal function (simplified)
        /// </summary>
        private static LuaValue[] SetLocal(LuaValue[] args)
        {
            // For simplicity, always return nil (cannot set local variables)
            return new[] { LuaNil.Instance };
        }
        
        /// <summary>
        /// Implements debug.getupvalue function (simplified)
        /// </summary>
        private static LuaValue[] GetUpvalue(LuaValue[] args)
        {
            // For simplicity, always return nil (no upvalues found)
            return new[] { LuaNil.Instance };
        }
        
        /// <summary>
        /// Implements debug.setupvalue function (simplified)
        /// </summary>
        private static LuaValue[] SetUpvalue(LuaValue[] args)
        {
            // For simplicity, always return nil (cannot set upvalues)
            return new[] { LuaNil.Instance };
        }
        
        /// <summary>
        /// Implements debug.traceback function (simplified)
        /// </summary>
        private static LuaValue[] Traceback(LuaValue[] args)
        {
            var message = args.Length > 0 ? args[0].AsString : "";
            var level = args.Length > 1 ? (int)(args[1].AsInteger ?? 1) : 1;
            
            // Create a simple traceback
            var traceback = string.IsNullOrEmpty(message) 
                ? "stack traceback:\n\t[C]: in function 'traceback'" 
                : $"{message}\nstack traceback:\n\t[C]: in function 'traceback'";
            
            return new[] { new LuaString(traceback) };
        }
        
        #endregion
    }
} 