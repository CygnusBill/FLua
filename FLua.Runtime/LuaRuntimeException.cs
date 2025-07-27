using System;
using FLua.Common.Diagnostics;

namespace FLua.Runtime
{
    /// <summary>
    /// Exception thrown during Lua execution
    /// </summary>
    public class LuaRuntimeException : Exception
    {
        /// <summary>
        /// The diagnostic information for this exception
        /// </summary>
        public FLuaDiagnostic? Diagnostic { get; }
        
        /// <summary>
        /// The call stack at the time of the error
        /// </summary>
        public string? LuaStackTrace { get; set; }
        
        public LuaRuntimeException(string message) : base(message) { }
        
        public LuaRuntimeException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
        
        public LuaRuntimeException(FLuaDiagnostic diagnostic) 
            : base(diagnostic.Message)
        {
            Diagnostic = diagnostic;
        }
        
        public LuaRuntimeException(FLuaDiagnostic diagnostic, Exception innerException) 
            : base(diagnostic.Message, innerException)
        {
            Diagnostic = diagnostic;
        }
        
        /// <summary>
        /// Creates a nil value access exception
        /// </summary>
        public static LuaRuntimeException NilValueAccess(string operation, SourceLocation? location = null)
        {
            var diagnostic = DiagnosticBuilder.NilValueAccess(operation, location);
            return new LuaRuntimeException(diagnostic);
        }
        
        /// <summary>
        /// Creates a type mismatch exception
        /// </summary>
        public static LuaRuntimeException TypeMismatch(string operation, string expected, string actual, SourceLocation? location = null)
        {
            var diagnostic = DiagnosticBuilder.TypeMismatch(operation, expected, actual, location);
            return new LuaRuntimeException(diagnostic);
        }
        
        /// <summary>
        /// Creates an unknown variable exception
        /// </summary>
        public static LuaRuntimeException UnknownVariable(string name, SourceLocation? location = null)
        {
            var diagnostic = DiagnosticBuilder.UnknownVariable(name, location);
            return new LuaRuntimeException(diagnostic);
        }
        
        /// <summary>
        /// Creates a const assignment exception
        /// </summary>
        public static LuaRuntimeException ConstAssignment(string variable, SourceLocation? location = null)
        {
            var diagnostic = new FLuaDiagnostic(
                ErrorCodes.ConstAssignment,
                ErrorSeverity.Error,
                DiagnosticMessages.ConstAssignment(variable),
                location);
            return new LuaRuntimeException(diagnostic);
        }
        
        /// <summary>
        /// Creates an invalid operation exception
        /// </summary>
        public static LuaRuntimeException InvalidOperation(string operation, string type, SourceLocation? location = null)
        {
            var diagnostic = new FLuaDiagnostic(
                ErrorCodes.InvalidOperation,
                ErrorSeverity.Error,
                DiagnosticMessages.InvalidOperation(operation, type),
                location);
            return new LuaRuntimeException(diagnostic);
        }
        
        /// <summary>
        /// Creates a closed variable access exception
        /// </summary>
        public static LuaRuntimeException ClosedVariableAccess(string variable, SourceLocation? location = null)
        {
            var diagnostic = new FLuaDiagnostic(
                ErrorCodes.InvalidOperation,
                ErrorSeverity.Error,
                $"Attempt to use closed variable '{variable}'. Variables marked with <close> cannot be accessed after their scope ends.",
                location);
            diagnostic.Help = "Check that you're not accessing the variable after its scope has ended.";
            return new LuaRuntimeException(diagnostic);
        }
    }
} 