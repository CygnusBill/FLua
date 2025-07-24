using System;

namespace FLua.Runtime
{
    /// <summary>
    /// Exception thrown during Lua execution
    /// </summary>
    public class LuaRuntimeException : Exception
    {
        public LuaRuntimeException(string message) : base(message) { }
    }
} 