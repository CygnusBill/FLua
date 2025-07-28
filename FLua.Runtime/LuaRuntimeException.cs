using System;

namespace FLua.Runtime
{
    /// <summary>
    /// Exception thrown during Lua execution
    /// </summary>
    public class LuaRuntimeException : Exception
    {
        /// <summary>
        /// The error code for this exception (e.g., "CONST_ASSIGN", "NIL_ACCESS")
        /// </summary>
        public string? ErrorCode { get; }
        
        /// <summary>
        /// The call stack at the time of the error
        /// </summary>
        public string? LuaStackTrace { get; set; }
        
        /// <summary>
        /// Line number where the error occurred (if available)
        /// </summary>
        public int? Line { get; set; }
        
        /// <summary>
        /// Column number where the error occurred (if available)
        /// </summary>
        public int? Column { get; set; }
        
        /// <summary>
        /// Source file name where the error occurred (if available)
        /// </summary>
        public string? SourceFile { get; set; }
        
        public LuaRuntimeException(string message) : base(message) { }
        
        public LuaRuntimeException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
        
        public LuaRuntimeException(string errorCode, string message) 
            : base(message)
        {
            ErrorCode = errorCode;
        }
        
        public LuaRuntimeException(string errorCode, string message, Exception innerException) 
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
        
        /// <summary>
        /// Creates a nil value access exception
        /// </summary>
        public static LuaRuntimeException NilValueAccess(string operation)
        {
            return new LuaRuntimeException("NIL_ACCESS", $"Attempt to perform '{operation}' on a nil value");
        }
        
        /// <summary>
        /// Creates a type mismatch exception
        /// </summary>
        public static LuaRuntimeException TypeMismatch(string operation, string expected, string actual)
        {
            return new LuaRuntimeException("TYPE_MISMATCH", $"Type mismatch in '{operation}': expected {expected}, got {actual}");
        }
        
        /// <summary>
        /// Creates an unknown variable exception
        /// </summary>
        public static LuaRuntimeException UnknownVariable(string name)
        {
            return new LuaRuntimeException("UNKNOWN_VAR", $"Unknown variable '{name}'");
        }
        
        /// <summary>
        /// Creates a const assignment exception
        /// </summary>
        public static LuaRuntimeException ConstAssignment(string variable)
        {
            return new LuaRuntimeException("CONST_ASSIGN", $"Attempt to assign to const variable '{variable}'");
        }
        
        /// <summary>
        /// Creates an invalid operation exception
        /// </summary>
        public static LuaRuntimeException InvalidOperation(string operation, string type)
        {
            return new LuaRuntimeException("INVALID_OP", $"Invalid operation '{operation}' on type '{type}'");
        }
        
        /// <summary>
        /// Creates a closed variable access exception
        /// </summary>
        public static LuaRuntimeException ClosedVariableAccess(string variable)
        {
            return new LuaRuntimeException("CLOSED_VAR", $"Attempt to use closed variable '{variable}'. Variables marked with <close> cannot be accessed after their scope ends.");
        }
        
        /// <summary>
        /// Sets the source location information
        /// </summary>
        public LuaRuntimeException WithLocation(string? sourceFile, int? line, int? column)
        {
            SourceFile = sourceFile;
            Line = line;
            Column = column;
            return this;
        }
    }
}