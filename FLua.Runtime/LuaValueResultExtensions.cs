using System;
using FLua.Common;

namespace FLua.Runtime
{
    /// <summary>
    /// Extension methods that provide Result-based safe access to LuaValue contents
    /// </summary>
    public static class LuaValueResultExtensions
    {
        /// <summary>
        /// Safely gets boolean value as Result
        /// </summary>
        public static Result<bool> TryAsBoolean(this LuaValue value)
        {
            return value.TryGetBoolean(out bool result)
                ? Result<bool>.Success(result)
                : Result<bool>.Failure($"Value is not a boolean, it's {value.Type}");
        }

        /// <summary>
        /// Safely gets integer value as Result
        /// </summary>
        public static Result<long> TryAsInteger(this LuaValue value)
        {
            return value.TryGetInteger(out long result)
                ? Result<long>.Success(result)
                : Result<long>.Failure($"Value is not an integer, it's {value.Type}");
        }

        /// <summary>
        /// Safely gets float value as Result
        /// </summary>
        public static Result<double> TryAsFloat(this LuaValue value)
        {
            return value.TryGetFloat(out double result)
                ? Result<double>.Success(result)
                : Result<double>.Failure($"Value is not a float, it's {value.Type}");
        }

        /// <summary>
        /// Safely gets numeric value as Result (handles both integer and float)
        /// </summary>
        public static Result<double> TryAsDouble(this LuaValue value)
        {
            return value.TryGetNumber(out double result)
                ? Result<double>.Success(result)
                : Result<double>.Failure($"Value is not a number, it's {value.Type}");
        }

        /// <summary>
        /// Safely gets integer value as Result, with conversion from float if lossless
        /// </summary>
        public static Result<long> TryAsIntegerValue(this LuaValue value)
        {
            return value.TryGetIntegerValue(out long result)
                ? Result<long>.Success(result)
                : Result<long>.Failure($"Value cannot be converted to integer losslessly, it's {value.Type}");
        }

        /// <summary>
        /// Safely gets string value as Result
        /// </summary>
        public static Result<string> TryAsString(this LuaValue value)
        {
            return value.TryGetString(out string? result) && result != null
                ? Result<string>.Success(result)
                : Result<string>.Failure($"Value is not a string, it's {value.Type}");
        }

        /// <summary>
        /// Safely gets table value as Result
        /// </summary>
        public static Result<T> TryAsTable<T>(this LuaValue value) where T : class
        {
            return value.TryGetTable<T>(out T? result) && result != null
                ? Result<T>.Success(result)
                : Result<T>.Failure($"Value is not a table of type {typeof(T).Name}, it's {value.Type}");
        }

        /// <summary>
        /// Safely gets function value as Result
        /// </summary>
        public static Result<LuaFunction> TryAsFunction(this LuaValue value)
        {
            if (value.IsFunction)
            {
                try
                {
                    var function = value.AsFunction();
                    return Result<LuaFunction>.Success(function);
                }
                catch (Exception ex)
                {
                    return Result<LuaFunction>.Failure($"Failed to get function: {ex.Message}");
                }
            }

            return Result<LuaFunction>.Failure($"Value is not a function, it's {value.Type}");
        }

        /// <summary>
        /// Safely gets function value as Result with generic type
        /// </summary>
        public static Result<T> TryAsFunction<T>(this LuaValue value) where T : LuaFunction
        {
            if (value.IsFunction)
            {
                try
                {
                    var function = value.AsFunction<T>();
                    return Result<T>.Success(function);
                }
                catch (Exception ex)
                {
                    return Result<T>.Failure($"Failed to get function as {typeof(T).Name}: {ex.Message}");
                }
            }

            return Result<T>.Failure($"Value is not a function, it's {value.Type}");
        }

        /// <summary>
        /// Safely gets userdata value as Result
        /// </summary>
        public static Result<T> TryAsUserData<T>(this LuaValue value) where T : class
        {
            if (value.IsUserData)
            {
                try
                {
                    var userData = value.AsUserData<T>();
                    return Result<T>.Success(userData);
                }
                catch (Exception ex)
                {
                    return Result<T>.Failure($"Failed to get userdata as {typeof(T).Name}: {ex.Message}");
                }
            }

            return Result<T>.Failure($"Value is not userdata, it's {value.Type}");
        }

        /// <summary>
        /// Safely gets thread value as Result
        /// </summary>
        public static Result<T> TryAsThread<T>(this LuaValue value) where T : class
        {
            if (value.IsThread)
            {
                try
                {
                    var thread = value.AsThread<T>();
                    return Result<T>.Success(thread);
                }
                catch (Exception ex)
                {
                    return Result<T>.Failure($"Failed to get thread as {typeof(T).Name}: {ex.Message}");
                }
            }

            return Result<T>.Failure($"Value is not a thread, it's {value.Type}");
        }

        /// <summary>
        /// Safely gets light userdata value as Result
        /// </summary>
        public static Result<IntPtr> TryAsLightUserData(this LuaValue value)
        {
            return value.TryGetLightUserData(out IntPtr result)
                ? Result<IntPtr>.Success(result)
                : Result<IntPtr>.Failure($"Value is not light userdata, it's {value.Type}");
        }

        /// <summary>
        /// Converts LuaValue to number using Lua's conversion rules
        /// </summary>
        public static Result<double> TryConvertToNumber(this LuaValue value)
        {
            return value.TryToNumber(out double result)
                ? Result<double>.Success(result)
                : Result<double>.Failure($"Cannot convert {value.Type} to number");
        }

        /// <summary>
        /// Converts LuaValue to integer using Lua's conversion rules
        /// </summary>
        public static Result<long> TryConvertToInteger(this LuaValue value)
        {
            return value.TryToInteger(out long result)
                ? Result<long>.Success(result)
                : Result<long>.Failure($"Cannot convert {value.Type} to integer");
        }

        /// <summary>
        /// Validates that a LuaValue has the expected type
        /// </summary>
        public static Result<LuaValue> EnsureType(this LuaValue value, LuaType expectedType)
        {
            return value.Type == expectedType
                ? Result<LuaValue>.Success(value)
                : Result<LuaValue>.Failure($"Expected {expectedType}, got {value.Type}");
        }

        /// <summary>
        /// Validates that a LuaValue is not nil
        /// </summary>
        public static Result<LuaValue> EnsureNotNil(this LuaValue value, string? context = null)
        {
            if (value.IsNil)
            {
                var message = context != null 
                    ? $"Value is nil in context: {context}"
                    : "Value is nil";
                return Result<LuaValue>.Failure(message);
            }

            return Result<LuaValue>.Success(value);
        }

        /// <summary>
        /// Validates that a LuaValue is truthy
        /// </summary>
        public static Result<LuaValue> EnsureTruthy(this LuaValue value, string? context = null)
        {
            if (!value.IsTruthy())
            {
                var message = context != null 
                    ? $"Value is falsy in context: {context}"
                    : "Value is falsy";
                return Result<LuaValue>.Failure(message);
            }

            return Result<LuaValue>.Success(value);
        }

        /// <summary>
        /// Safely performs arithmetic operations using Result pattern
        /// </summary>
        public static Result<LuaValue> TryAdd(this LuaValue left, LuaValue right)
        {
            if (!left.IsNumber || !right.IsNumber)
            {
                return Result<LuaValue>.Failure("Cannot perform arithmetic on non-numeric values");
            }

            try
            {
                return Result<LuaValue>.Success(LuaValue.Add(left, right));
            }
            catch (Exception ex)
            {
                return Result<LuaValue>.Failure($"Arithmetic operation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Safely performs subtraction using Result pattern
        /// </summary>
        public static Result<LuaValue> TrySubtract(this LuaValue left, LuaValue right)
        {
            if (!left.IsNumber || !right.IsNumber)
            {
                return Result<LuaValue>.Failure("Cannot perform arithmetic on non-numeric values");
            }

            try
            {
                return Result<LuaValue>.Success(LuaValue.Subtract(left, right));
            }
            catch (Exception ex)
            {
                return Result<LuaValue>.Failure($"Arithmetic operation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Safely performs multiplication using Result pattern
        /// </summary>
        public static Result<LuaValue> TryMultiply(this LuaValue left, LuaValue right)
        {
            if (!left.IsNumber || !right.IsNumber)
            {
                return Result<LuaValue>.Failure("Cannot perform arithmetic on non-numeric values");
            }

            try
            {
                return Result<LuaValue>.Success(LuaValue.Multiply(left, right));
            }
            catch (Exception ex)
            {
                return Result<LuaValue>.Failure($"Arithmetic operation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Safely performs division using Result pattern
        /// </summary>
        public static Result<LuaValue> TryDivide(this LuaValue left, LuaValue right)
        {
            if (!left.IsNumber || !right.IsNumber)
            {
                return Result<LuaValue>.Failure("Cannot perform arithmetic on non-numeric values");
            }

            if (right.TryGetNumber(out double rightValue) && rightValue == 0.0)
            {
                return Result<LuaValue>.Failure("Division by zero");
            }

            try
            {
                return Result<LuaValue>.Success(LuaValue.Divide(left, right));
            }
            catch (Exception ex)
            {
                return Result<LuaValue>.Failure($"Division failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Safely performs modulo using Result pattern
        /// </summary>
        public static Result<LuaValue> TryModulo(this LuaValue left, LuaValue right)
        {
            if (!left.IsNumber || !right.IsNumber)
            {
                return Result<LuaValue>.Failure("Cannot perform arithmetic on non-numeric values");
            }

            if (right.TryGetNumber(out double rightValue) && rightValue == 0.0)
            {
                return Result<LuaValue>.Failure("Modulo by zero");
            }

            try
            {
                return Result<LuaValue>.Success(LuaValue.Modulo(left, right));
            }
            catch (Exception ex)
            {
                return Result<LuaValue>.Failure($"Modulo operation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Safely performs power operation using Result pattern
        /// </summary>
        public static Result<LuaValue> TryPower(this LuaValue left, LuaValue right)
        {
            if (!left.IsNumber || !right.IsNumber)
            {
                return Result<LuaValue>.Failure("Cannot perform arithmetic on non-numeric values");
            }

            try
            {
                return Result<LuaValue>.Success(LuaValue.Power(left, right));
            }
            catch (Exception ex)
            {
                return Result<LuaValue>.Failure($"Power operation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Safely performs unary minus using Result pattern
        /// </summary>
        public static Result<LuaValue> TryNegate(this LuaValue value)
        {
            if (!value.IsNumber)
            {
                return Result<LuaValue>.Failure("Cannot negate non-numeric value");
            }

            try
            {
                return Result<LuaValue>.Success(LuaValue.UnaryMinus(value));
            }
            catch (Exception ex)
            {
                return Result<LuaValue>.Failure($"Negation failed: {ex.Message}");
            }
        }
    }
}