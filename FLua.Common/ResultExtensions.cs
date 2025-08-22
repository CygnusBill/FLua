using System;
using System.Collections.Generic;
using System.Linq;

namespace FLua.Common
{
    /// <summary>
    /// Extension methods for working with Result types more fluently
    /// </summary>
    public static class ResultExtensions
    {
        /// <summary>
        /// Executes an action if the result is successful
        /// </summary>
        public static Result<T> OnSuccess<T>(this Result<T> result, Action<T> onSuccess)
        {
            if (result.IsSuccess)
                onSuccess(result.Value);
            return result;
        }

        /// <summary>
        /// Executes an action if the result is a failure
        /// </summary>
        public static Result<T> OnFailure<T>(this Result<T> result, Action<string> onFailure)
        {
            if (result.IsFailure)
                onFailure(result.Error);
            return result;
        }

        /// <summary>
        /// Logs success and failure results (useful for debugging)
        /// </summary>
        public static Result<T> Log<T>(this Result<T> result, string context = "")
        {
            if (result.IsSuccess)
                Console.WriteLine($"[SUCCESS {context}] {result.Value}");
            else
                Console.WriteLine($"[FAILURE {context}] {result.Error}");
            return result;
        }

        /// <summary>
        /// Converts a nullable value to a Result
        /// </summary>
        public static Result<T> ToResult<T>(this T? value, string errorMessage = "Value is null") where T : class
        {
            return value != null ? Result<T>.Success(value) : Result<T>.Failure(errorMessage);
        }

        /// <summary>
        /// Converts a nullable value type to a Result
        /// </summary>
        public static Result<T> ToResult<T>(this T? value, string errorMessage = "Value is null") where T : struct
        {
            return value.HasValue ? Result<T>.Success(value.Value) : Result<T>.Failure(errorMessage);
        }

        /// <summary>
        /// Combines multiple results - if any fail, returns the first failure
        /// </summary>
        public static Result<T[]> CombineAll<T>(this IEnumerable<Result<T>> results)
        {
            var resultArray = results.ToArray();
            var values = new T[resultArray.Length];
            
            for (int i = 0; i < resultArray.Length; i++)
            {
                if (resultArray[i].IsFailure)
                    return Result<T[]>.Failure(resultArray[i].Error);
                values[i] = resultArray[i].Value;
            }
            
            return Result<T[]>.Success(values);
        }

        /// <summary>
        /// Filters successful results and returns their values
        /// </summary>
        public static IEnumerable<T> SuccessfulValues<T>(this IEnumerable<Result<T>> results)
        {
            return results.Where(r => r.IsSuccess).Select(r => r.Value);
        }

        /// <summary>
        /// Filters failed results and returns their errors
        /// </summary>
        public static IEnumerable<string> FailureErrors<T>(this IEnumerable<Result<T>> results)
        {
            return results.Where(r => r.IsFailure).Select(r => r.Error);
        }

        /// <summary>
        /// Attempts to cast a result value to a different type
        /// </summary>
        public static Result<TResult> Cast<T, TResult>(this Result<T> result) where TResult : class
        {
            return result.Bind(value =>
            {
                if (value is TResult castedValue)
                    return Result<TResult>.Success(castedValue);
                return Result<TResult>.Failure($"Cannot cast {typeof(T).Name} to {typeof(TResult).Name}");
            });
        }

        /// <summary>
        /// Provides a default value if the result failed
        /// </summary>
        public static T OrElse<T>(this Result<T> result, T defaultValue)
        {
            return result.IsSuccess ? result.Value : defaultValue;
        }

        /// <summary>
        /// Provides a default value from a function if the result failed
        /// </summary>
        public static T OrElse<T>(this Result<T> result, Func<T> defaultValueFactory)
        {
            return result.IsSuccess ? result.Value : defaultValueFactory();
        }

        /// <summary>
        /// Chains an operation that doesn't produce a Result
        /// </summary>
        public static Result<TResult> Then<T, TResult>(this Result<T> result, Func<T, TResult> next)
        {
            return result.Map(next);
        }

        /// <summary>
        /// Chains an operation that may throw an exception
        /// </summary>
        public static Result<TResult> ThenTry<T, TResult>(this Result<T> result, Func<T, TResult> next, string? errorPrefix = null)
        {
            return result.Bind(value => Result.Try(() => next(value), errorPrefix));
        }

        /// <summary>
        /// Validates a result with a predicate
        /// </summary>
        public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, string errorMessage)
        {
            return result.Bind(value => 
                predicate(value) ? Result<T>.Success(value) : Result<T>.Failure(errorMessage));
        }

        /// <summary>
        /// Validates a result with a predicate that can provide a custom error message
        /// </summary>
        public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, Func<T, string> errorMessageFactory)
        {
            return result.Bind(value => 
                predicate(value) ? Result<T>.Success(value) : Result<T>.Failure(errorMessageFactory(value)));
        }
    }

    /// <summary>
    /// Extension methods for typed Result types
    /// </summary>
    public static class TypedResultExtensions
    {
        /// <summary>
        /// Executes an action if the result is successful
        /// </summary>
        public static Result<TValue, TError> OnSuccess<TValue, TError>(
            this Result<TValue, TError> result, 
            Action<TValue> onSuccess)
        {
            if (result.IsSuccess)
                onSuccess(result.Value);
            return result;
        }

        /// <summary>
        /// Executes an action if the result is a failure
        /// </summary>
        public static Result<TValue, TError> OnFailure<TValue, TError>(
            this Result<TValue, TError> result, 
            Action<TError> onFailure)
        {
            if (result.IsFailure)
                onFailure(result.Error);
            return result;
        }

        /// <summary>
        /// Provides a default value if the result failed
        /// </summary>
        public static TValue OrElse<TValue, TError>(this Result<TValue, TError> result, TValue defaultValue)
        {
            return result.IsSuccess ? result.Value : defaultValue;
        }

        /// <summary>
        /// Provides a default value from a function if the result failed
        /// </summary>
        public static TValue OrElse<TValue, TError>(this Result<TValue, TError> result, Func<TError, TValue> defaultValueFactory)
        {
            return result.IsSuccess ? result.Value : defaultValueFactory(result.Error);
        }
    }
}