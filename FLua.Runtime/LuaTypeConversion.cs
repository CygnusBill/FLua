using System;
using System.Globalization;

namespace FLua.Runtime
{
    /// <summary>
    /// Provides centralized type conversion functionality for Lua values.
    /// This ensures consistent conversion behavior between interpreter and future compiler.
    /// </summary>
    public static class LuaTypeConversion
    {
        /// <summary>
        /// Converts a Lua value to a number (double).
        /// Returns null if the conversion is not possible.
        /// </summary>
        public static double? ToNumber(LuaValue value)
        {
            switch (value)
            {
                case LuaNumber num:
                    return num.Value;
                    
                case LuaInteger integer:
                    return (double)integer.Value;
                    
                case LuaString str:
                    // Try to parse the string as a number
                    if (double.TryParse(str.Value.Trim(), NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out double result))
                    {
                        return result;
                    }
                    // Try hexadecimal format
                    if (str.Value.Trim().StartsWith("0x", StringComparison.OrdinalIgnoreCase) || 
                        str.Value.Trim().StartsWith("0X", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var hexStr = str.Value.Trim().Substring(2);
                            if (long.TryParse(hexStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long hexValue))
                            {
                                return (double)hexValue;
                            }
                        }
                        catch { }
                    }
                    return null;
                    
                default:
                    return null;
            }
        }

        /// <summary>
        /// Converts a Lua value to an integer (long).
        /// Returns null if the conversion is not possible.
        /// </summary>
        public static long? ToInteger(LuaValue value)
        {
            switch (value)
            {
                case LuaInteger integer:
                    return integer.Value;
                    
                case LuaNumber num:
                    // Only convert if the number is an exact integer
                    double d = num.Value;
                    if (Math.Floor(d) == d && d >= long.MinValue && d <= long.MaxValue)
                    {
                        return (long)d;
                    }
                    return null;
                    
                case LuaString str:
                    // Try to parse the string as an integer
                    if (long.TryParse(str.Value.Trim(), NumberStyles.Integer | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long result))
                    {
                        return result;
                    }
                    // Try hexadecimal format
                    if (str.Value.Trim().StartsWith("0x", StringComparison.OrdinalIgnoreCase) || 
                        str.Value.Trim().StartsWith("0X", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            var hexStr = str.Value.Trim().Substring(2);
                            if (long.TryParse(hexStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long hexValue))
                            {
                                return hexValue;
                            }
                        }
                        catch { }
                    }
                    // Try parsing as double first and check if it's an integer
                    var numValue = ToNumber(value);
                    if (numValue.HasValue)
                    {
                        double dValue = numValue.Value;
                        if (Math.Floor(dValue) == dValue && dValue >= long.MinValue && dValue <= long.MaxValue)
                        {
                            return (long)dValue;
                        }
                    }
                    return null;
                    
                default:
                    return null;
            }
        }

        /// <summary>
        /// Converts a Lua value to a string.
        /// This follows Lua's string conversion rules.
        /// </summary>
        public static string ToString(LuaValue value)
        {
            switch (value)
            {
                case LuaString str:
                    return str.Value;
                    
                case LuaNumber num:
                    // Format numbers according to Lua rules
                    double d = num.Value;
                    if (Math.Floor(d) == d && d >= long.MinValue && d <= long.MaxValue)
                    {
                        // Integer-valued numbers are formatted without decimal point
                        return ((long)d).ToString();
                    }
                    return d.ToString(CultureInfo.InvariantCulture);
                    
                case LuaInteger integer:
                    return integer.Value.ToString();
                    
                case LuaBoolean boolean:
                    return boolean.Value ? "true" : "false";
                    
                case LuaNil _:
                    return "nil";
                    
                case LuaTable _:
                    return "table";
                    
                case LuaFunction _:
                    return "function";
                    
                case LuaCoroutine _:
                    return "thread";
                    
                default:
                    return value.ToString() ?? "unknown";
            }
        }

        /// <summary>
        /// Converts a Lua value to a boolean.
        /// In Lua, only nil and false are falsy, everything else is truthy.
        /// </summary>
        public static bool ToBoolean(LuaValue value)
        {
            return LuaValue.IsValueTruthy(value);
        }

        /// <summary>
        /// Gets the Lua type name of a value.
        /// This is what the type() function returns.
        /// </summary>
        public static string GetTypeName(LuaValue value)
        {
            switch (value)
            {
                case LuaNil _:
                    return "nil";
                case LuaBoolean _:
                    return "boolean";
                case LuaNumber _:
                case LuaInteger _:
                    return "number";
                case LuaString _:
                    return "string";
                case LuaTable _:
                    return "table";
                case LuaFunction _:
                    return "function";
                case LuaCoroutine _:
                    return "thread";
                default:
                    return "userdata"; // Default for unknown types
            }
        }

        /// <summary>
        /// Attempts to coerce a value to a number for arithmetic operations.
        /// This is more permissive than ToNumber and matches Lua's behavior.
        /// </summary>
        public static double? CoerceToNumber(LuaValue value)
        {
            // For now, this is the same as ToNumber, but we might want to
            // add metamethod support here in the future
            return ToNumber(value);
        }

        /// <summary>
        /// Attempts to coerce a value to an integer for bitwise operations.
        /// This is more permissive than ToInteger and matches Lua's behavior.
        /// </summary>
        public static long? CoerceToInteger(LuaValue value)
        {
            // First try direct conversion
            var intResult = ToInteger(value);
            if (intResult.HasValue)
                return intResult;

            // Try converting through number first
            var numResult = ToNumber(value);
            if (numResult.HasValue)
            {
                double d = numResult.Value;
                // Lua truncates towards zero for bitwise operations
                if (d >= 0)
                    return (long)Math.Floor(d);
                else
                    return (long)Math.Ceiling(d);
            }

            return null;
        }

        /// <summary>
        /// Formats a value for concatenation.
        /// Numbers are converted to strings, other types may error.
        /// </summary>
        public static string? ToConcatString(LuaValue value)
        {
            switch (value)
            {
                case LuaString str:
                    return str.Value;
                case LuaNumber _:
                case LuaInteger _:
                    return ToString(value);
                default:
                    // Other types require metamethods for concatenation
                    return null;
            }
        }
    }
}