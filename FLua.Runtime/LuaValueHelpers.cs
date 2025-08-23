using System;

namespace FLua.Runtime
{
    /// <summary>
    /// Extension methods to provide compatibility with old LuaValue API
    /// </summary>
    public static class LuaValueExtensions
    {
        // Note: These can't be properties in extension methods, but we'll keep the method names
        // The calling code needs to be updated to use method syntax
    }

    /// <summary>
    /// Helper methods for working with the new LuaValue struct
    /// </summary>
    public static class LuaValueHelpers
    {
        /// <summary>
        /// Gets a numeric value from a LuaValue, handling both integer and float types
        /// </summary>
        public static double GetNumber(LuaValue value)
        {
            if (value.TryGetNumber(out var number))
                return number;
            throw new InvalidOperationException($"Cannot convert {value.Type} to number");
        }

        /// <summary>
        /// Gets an integer value from a LuaValue, with conversion from float if applicable
        /// </summary>
        public static long GetInteger(LuaValue value)
        {
            if (value.TryGetIntegerValue(out var integer))
                return integer;
            throw new InvalidOperationException($"Cannot convert {value.Type} to integer");
        }

        /// <summary>
        /// Creates a numeric LuaValue, always returning Float for double input
        /// </summary>
        public static LuaValue CreateNumber(double value)
        {
            return LuaValue.Float(value);
        }

        /// <summary>
        /// Creates a numeric LuaValue from an integer
        /// </summary>
        public static LuaValue CreateNumber(long value)
        {
            return LuaValue.Integer(value);
        }

        /// <summary>
        /// Checks if a value is a number (integer or float)
        /// </summary>
        public static bool IsNumber(LuaValue value)
        {
            return value.IsNumber;
        }

        /// <summary>
        /// Checks if a value is a string
        /// </summary>
        public static bool IsString(LuaValue value)
        {
            return value.Type == LuaType.String;
        }

        /// <summary>
        /// Checks if a value is a table
        /// </summary>
        public static bool IsTable(LuaValue value)
        {
            return value.Type == LuaType.Table;
        }

        /// <summary>
        /// Checks if a value is a function
        /// </summary>
        public static bool IsFunction(LuaValue value)
        {
            return value.Type == LuaType.Function;
        }
    }
}