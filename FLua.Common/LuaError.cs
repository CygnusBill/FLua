using System;
using System.Collections.Generic;
using System.Linq;
using FLua.Common.Diagnostics;

namespace FLua.Common
{
    /// <summary>
    /// Represents different types of Lua errors
    /// </summary>
    public enum LuaErrorType
    {
        TypeError,
        RuntimeError,
        SyntaxError,
        CompilerError,
        IOError,
        ArgumentError,
        SystemError
    }

    /// <summary>
    /// Represents a structured Lua error with type, message, and optional location
    /// </summary>
    public sealed record LuaError
    {
        public LuaErrorType Type { get; init; }
        public string Message { get; init; }
        public SourceLocation? Location { get; init; }
        public Exception? InnerException { get; init; }

        public LuaError(LuaErrorType type, string message, SourceLocation? location = null, Exception? innerException = null)
        {
            Type = type;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Location = location;
            InnerException = innerException;
        }

        /// <summary>
        /// Creates a type error
        /// </summary>
        public static LuaError TypeError(string message, SourceLocation? location = null)
            => new(LuaErrorType.TypeError, message, location);

        /// <summary>
        /// Creates a runtime error
        /// </summary>
        public static LuaError RuntimeError(string message, SourceLocation? location = null)
            => new(LuaErrorType.RuntimeError, message, location);

        /// <summary>
        /// Creates a syntax error
        /// </summary>
        public static LuaError SyntaxError(string message, SourceLocation? location = null)
            => new(LuaErrorType.SyntaxError, message, location);

        /// <summary>
        /// Creates a compiler error
        /// </summary>
        public static LuaError CompilerError(string message, SourceLocation? location = null)
            => new(LuaErrorType.CompilerError, message, location);

        /// <summary>
        /// Creates an I/O error
        /// </summary>
        public static LuaError IOError(string message, SourceLocation? location = null, Exception? innerException = null)
            => new(LuaErrorType.IOError, message, location, innerException);

        /// <summary>
        /// Creates an argument error
        /// </summary>
        public static LuaError ArgumentError(string message, SourceLocation? location = null)
            => new(LuaErrorType.ArgumentError, message, location);

        /// <summary>
        /// Creates a system error
        /// </summary>
        public static LuaError SystemError(string message, SourceLocation? location = null, Exception? innerException = null)
            => new(LuaErrorType.SystemError, message, location, innerException);

        /// <summary>
        /// Converts this error to a formatted string
        /// </summary>
        public override string ToString()
        {
            var result = $"{Type}: {Message}";
            
            if (Location != null)
            {
                result += $" at {Location.FileName}:{Location.Line}:{Location.Column}";
            }

            if (InnerException != null)
            {
                result += $" (Inner: {InnerException.Message})";
            }

            return result;
        }

        /// <summary>
        /// Converts this error to a standard Exception
        /// </summary>
        public Exception ToException()
        {
            var ex = InnerException != null 
                ? new InvalidOperationException(Message, InnerException)
                : new InvalidOperationException(Message);
            return ex;
        }
    }

    /// <summary>
    /// Represents multiple Lua errors collected together
    /// </summary>
    public sealed record LuaErrorCollection
    {
        public IReadOnlyList<LuaError> Errors { get; init; }

        public LuaErrorCollection(IEnumerable<LuaError> errors)
        {
            Errors = errors?.ToList() ?? throw new ArgumentNullException(nameof(errors));
        }

        public LuaErrorCollection(params LuaError[] errors)
        {
            Errors = errors ?? throw new ArgumentNullException(nameof(errors));
        }

        /// <summary>
        /// Gets whether there are any errors
        /// </summary>
        public bool HasErrors => Errors.Count > 0;

        /// <summary>
        /// Gets the number of errors
        /// </summary>
        public int Count => Errors.Count;

        /// <summary>
        /// Converts all errors to a single formatted message
        /// </summary>
        public override string ToString()
        {
            if (!HasErrors) return "No errors";
            
            if (Errors.Count == 1) return Errors[0].ToString();
            
            return string.Join("\n", Errors.Select((error, i) => $"{i + 1}. {error}"));
        }

        /// <summary>
        /// Converts all errors to Exceptions
        /// </summary>
        public IEnumerable<Exception> ToExceptions()
        {
            return Errors.Select(error => error.ToException());
        }

        /// <summary>
        /// Throws the first error as an exception, or does nothing if no errors
        /// </summary>
        public void ThrowIfAny()
        {
            if (HasErrors)
            {
                throw Errors[0].ToException();
            }
        }
    }

    /// <summary>
    /// Type aliases for common Result patterns with LuaError
    /// </summary>
    public static class LuaResult
    {
        /// <summary>
        /// Result type for operations that can fail with a LuaError
        /// </summary>
        public static Result<T, LuaError> Success<T>(T value) => Result<T, LuaError>.Success(value);

        /// <summary>
        /// Result type for operations that can fail with a LuaError
        /// </summary>
        public static Result<T, LuaError> Failure<T>(LuaError error) => Result<T, LuaError>.Failure(error);

        /// <summary>
        /// Result type for operations that can fail with multiple LuaErrors
        /// </summary>
        public static Result<T, LuaErrorCollection> Success<T>(T value, LuaErrorCollection _) => Result<T, LuaErrorCollection>.Success(value);

        /// <summary>
        /// Result type for operations that can fail with multiple LuaErrors
        /// </summary>
        public static Result<T, LuaErrorCollection> Failure<T>(LuaErrorCollection errors) => Result<T, LuaErrorCollection>.Failure(errors);
    }
}