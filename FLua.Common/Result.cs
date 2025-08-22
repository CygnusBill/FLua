using System;
using System.Collections.Generic;
using System.Linq;

namespace FLua.Common
{
    /// <summary>
    /// Represents the result of an operation that can either succeed with a value or fail with an error
    /// </summary>
    public readonly struct Result<T>
    {
        private readonly T? _value;
        private readonly string? _error;
        private readonly bool _isSuccess;

        private Result(T value)
        {
            _value = value;
            _error = null;
            _isSuccess = true;
        }

        private Result(string error)
        {
            _value = default;
            _error = error ?? "Unknown error";
            _isSuccess = false;
        }

        /// <summary>
        /// Gets whether the result represents a successful operation
        /// </summary>
        public bool IsSuccess => _isSuccess;

        /// <summary>
        /// Gets whether the result represents a failed operation
        /// </summary>
        public bool IsFailure => !_isSuccess;

        /// <summary>
        /// Gets the success value. Throws if the result is a failure.
        /// </summary>
        public T Value => _isSuccess ? _value! : throw new InvalidOperationException($"Cannot access value of failed result: {_error}");

        /// <summary>
        /// Gets the error message. Throws if the result is a success.
        /// </summary>
        public string Error => !_isSuccess ? _error! : throw new InvalidOperationException("Cannot access error of successful result");

        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static Result<T> Success(T value) => new(value);

        /// <summary>
        /// Creates a failed result
        /// </summary>
        public static Result<T> Failure(string error) => new(error);

        /// <summary>
        /// Attempts to get the value if successful
        /// </summary>
        public bool TryGetValue(out T value)
        {
            if (_isSuccess)
            {
                value = _value!;
                return true;
            }
            value = default!;
            return false;
        }

        /// <summary>
        /// Gets the value if successful, otherwise returns the default value
        /// </summary>
        public T GetValueOrDefault(T defaultValue = default!) => _isSuccess ? _value! : defaultValue;

        /// <summary>
        /// Executes an action based on success or failure
        /// </summary>
        public void Match(Action<T> onSuccess, Action<string> onFailure)
        {
            if (_isSuccess)
                onSuccess(_value!);
            else
                onFailure(_error!);
        }

        /// <summary>
        /// Transforms the result based on success or failure
        /// </summary>
        public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
        {
            return _isSuccess ? onSuccess(_value!) : onFailure(_error!);
        }

        /// <summary>
        /// Maps the success value to a new value
        /// </summary>
        public Result<TResult> Map<TResult>(Func<T, TResult> mapper)
        {
            return _isSuccess ? Result<TResult>.Success(mapper(_value!)) : Result<TResult>.Failure(_error!);
        }

        /// <summary>
        /// Chains another result-producing operation
        /// </summary>
        public Result<TResult> Bind<TResult>(Func<T, Result<TResult>> binder)
        {
            return _isSuccess ? binder(_value!) : Result<TResult>.Failure(_error!);
        }

        /// <summary>
        /// Chains another result-producing operation (alias for Bind)
        /// </summary>
        public Result<TResult> FlatMap<TResult>(Func<T, Result<TResult>> mapper) => Bind(mapper);

        /// <summary>
        /// Implicit conversion from value to success result
        /// </summary>
        public static implicit operator Result<T>(T value) => Success(value);

        /// <summary>
        /// Returns a string representation of the result
        /// </summary>
        public override string ToString()
        {
            return _isSuccess ? $"Success({_value})" : $"Failure({_error})";
        }
    }

    /// <summary>
    /// Represents the result of an operation that can either succeed with a value or fail with a typed error
    /// </summary>
    public readonly struct Result<TValue, TError>
    {
        private readonly TValue? _value;
        private readonly TError? _error;
        private readonly bool _isSuccess;

        private Result(TValue value)
        {
            _value = value;
            _error = default;
            _isSuccess = true;
        }

        private Result(TError error)
        {
            _value = default;
            _error = error;
            _isSuccess = false;
        }

        /// <summary>
        /// Gets whether the result represents a successful operation
        /// </summary>
        public bool IsSuccess => _isSuccess;

        /// <summary>
        /// Gets whether the result represents a failed operation
        /// </summary>
        public bool IsFailure => !_isSuccess;

        /// <summary>
        /// Gets the success value. Throws if the result is a failure.
        /// </summary>
        public TValue Value => _isSuccess ? _value! : throw new InvalidOperationException($"Cannot access value of failed result: {_error}");

        /// <summary>
        /// Gets the error. Throws if the result is a success.
        /// </summary>
        public TError Error => !_isSuccess ? _error! : throw new InvalidOperationException("Cannot access error of successful result");

        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static Result<TValue, TError> Success(TValue value) => new(value);

        /// <summary>
        /// Creates a failed result
        /// </summary>
        public static Result<TValue, TError> Failure(TError error) => new(error);

        /// <summary>
        /// Executes an action based on success or failure
        /// </summary>
        public void Match(Action<TValue> onSuccess, Action<TError> onFailure)
        {
            if (_isSuccess)
                onSuccess(_value!);
            else
                onFailure(_error!);
        }

        /// <summary>
        /// Transforms the result based on success or failure
        /// </summary>
        public TResult Match<TResult>(Func<TValue, TResult> onSuccess, Func<TError, TResult> onFailure)
        {
            return _isSuccess ? onSuccess(_value!) : onFailure(_error!);
        }

        /// <summary>
        /// Maps the success value to a new value
        /// </summary>
        public Result<TResult, TError> Map<TResult>(Func<TValue, TResult> mapper)
        {
            return _isSuccess ? Result<TResult, TError>.Success(mapper(_value!)) : Result<TResult, TError>.Failure(_error!);
        }

        /// <summary>
        /// Maps the error value to a new error type
        /// </summary>
        public Result<TValue, TNewError> MapError<TNewError>(Func<TError, TNewError> mapper)
        {
            return _isSuccess ? Result<TValue, TNewError>.Success(_value!) : Result<TValue, TNewError>.Failure(mapper(_error!));
        }

        /// <summary>
        /// Chains another result-producing operation
        /// </summary>
        public Result<TResult, TError> Bind<TResult>(Func<TValue, Result<TResult, TError>> binder)
        {
            return _isSuccess ? binder(_value!) : Result<TResult, TError>.Failure(_error!);
        }

        /// <summary>
        /// Implicit conversion from value to success result
        /// </summary>
        public static implicit operator Result<TValue, TError>(TValue value) => Success(value);

        /// <summary>
        /// Implicit conversion from error to failure result
        /// </summary>
        public static implicit operator Result<TValue, TError>(TError error) => Failure(error);

        /// <summary>
        /// Converts to simple Result with string error
        /// </summary>
        public Result<TValue> ToSimpleResult()
        {
            return _isSuccess ? Result<TValue>.Success(_value!) : Result<TValue>.Failure(_error?.ToString() ?? "Unknown error");
        }

        /// <summary>
        /// Returns a string representation of the result
        /// </summary>
        public override string ToString()
        {
            return _isSuccess ? $"Success({_value})" : $"Failure({_error})";
        }
    }

    /// <summary>
    /// Utility methods for working with Results
    /// </summary>
    public static class Result
    {
        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static Result<T> Success<T>(T value) => Result<T>.Success(value);

        /// <summary>
        /// Creates a failed result
        /// </summary>
        public static Result<T> Failure<T>(string error) => Result<T>.Failure(error);

        /// <summary>
        /// Wraps a potentially throwing operation in a Result
        /// </summary>
        public static Result<T> Try<T>(Func<T> operation, string? errorPrefix = null)
        {
            try
            {
                return Success(operation());
            }
            catch (Exception ex)
            {
                var message = errorPrefix != null ? $"{errorPrefix}: {ex.Message}" : ex.Message;
                return Failure<T>(message);
            }
        }

        /// <summary>
        /// Combines multiple results into a single result containing all success values
        /// </summary>
        public static Result<IEnumerable<T>> Combine<T>(params Result<T>[] results)
        {
            var values = new List<T>();
            var errors = new List<string>();

            foreach (var result in results)
            {
                if (result.IsSuccess)
                    values.Add(result.Value);
                else
                    errors.Add(result.Error);
            }

            return errors.Any() 
                ? Failure<IEnumerable<T>>(string.Join("; ", errors))
                : Success<IEnumerable<T>>(values);
        }

        /// <summary>
        /// Returns the first successful result, or all errors combined if all fail
        /// </summary>
        public static Result<T> FirstSuccess<T>(params Result<T>[] results)
        {
            var errors = new List<string>();

            foreach (var result in results)
            {
                if (result.IsSuccess)
                    return result;
                errors.Add(result.Error);
            }

            return Failure<T>(string.Join("; ", errors));
        }
    }
}