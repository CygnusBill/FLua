using System;
using System.Globalization;

namespace FLua.Runtime
{
    /// <summary>
    /// Provides type conversion utilities for Lua values.
    /// Most conversions are now handled by the LuaValue struct itself.
    /// </summary>
    public static class LuaTypeConversion
    {
        /// <summary>
        /// Converts a Lua value to a number (double).
        /// This includes parsing hex strings which LuaValue doesn't handle.
        /// </summary>
        public static double? ToNumber(LuaValue value)
        {
            if (value.IsNumber)
                return value.AsNumber();
                
            if (value.IsString)
            {
                var str = value.AsString().Trim();
                
                // Try to parse the string as a number
                if (double.TryParse(str, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out double result))
                {
                    return result;
                }
                
                // Try hexadecimal format
                if (str.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || 
                    str.StartsWith("0X", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var hexStr = str.Substring(2);
                        if (long.TryParse(hexStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long hexValue))
                        {
                            return (double)hexValue;
                        }
                    }
                    catch { }
                }
            }
            
            return null;
        }

        /// <summary>
        /// Converts a Lua value to an integer (long).
        /// This includes parsing hex strings which LuaValue doesn't handle.
        /// </summary>
        public static long? ToInteger(LuaValue value)
        {
            if (value.IsInteger)
                return value.AsInteger();
                
            if (value.IsFloat)
            {
                // Only convert if the number is an exact integer
                double d = value.AsFloat();
                if (Math.Floor(d) == d && d >= long.MinValue && d <= long.MaxValue)
                {
                    return (long)d;
                }
                return null;
            }
            
            if (value.IsString)
            {
                var str = value.AsString().Trim();
                
                // Try to parse the string as an integer
                if (long.TryParse(str, NumberStyles.Integer | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long result))
                {
                    return result;
                }
                
                // Try hexadecimal format
                if (str.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || 
                    str.StartsWith("0X", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var hexStr = str.Substring(2);
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
            }
            
            return null;
        }

        /// <summary>
        /// Converts a Lua value to a string.
        /// Just delegates to the built-in ToString() method.
        /// </summary>
        public static string ToString(LuaValue value)
        {
            return value.ToString();
        }

        /// <summary>
        /// Converts a Lua value to a boolean.
        /// In Lua, only nil and false are falsy, everything else is truthy.
        /// </summary>
        public static bool ToBoolean(LuaValue value)
        {
            return value.IsTruthy();
        }

        /// <summary>
        /// Gets the type name of a Lua value as a string.
        /// </summary>
        public static string GetTypeName(LuaValue value)
        {
            return value.Type switch
            {
                LuaType.Nil => "nil",
                LuaType.Boolean => "boolean",
                LuaType.Integer => "number",
                LuaType.Float => "number",
                LuaType.String => "string",
                LuaType.Table => "table",
                LuaType.Function => "function",
                LuaType.Thread => "thread",
                _ => "userdata"
            };
        }

        /// <summary>
        /// Converts a value to a string for concatenation.
        /// Returns null if the value cannot be concatenated.
        /// </summary>
        public static string? ToConcatString(LuaValue value)
        {
            if (value.IsString)
                return value.AsString();
                
            if (value.IsNumber)
                return value.ToString();
                
            // Check for __concat metamethod
            if (value.IsTable)
            {
                var table = value.AsTable<LuaTable>();
                if (table.Metatable != null)
                {
                    var concat = table.Metatable.RawGet(LuaValue.String("__concat"));
                    if (!concat.IsNil)
                        return null; // Has metamethod, should use it instead
                }
            }
            
            return null;
        }

        /// <summary>
        /// Parses a string to a number according to Lua rules.
        /// </summary>
        public static LuaValue? StringToNumber(string str, int? base_ = null)
        {
            str = str.Trim();
            
            if (base_ == null)
            {
                // Auto-detect base
                if (str.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || 
                    str.StartsWith("0X", StringComparison.OrdinalIgnoreCase))
                {
                    base_ = 16;
                    str = str.Substring(2);
                }
                else
                {
                    base_ = 10;
                }
            }
            
            if (base_ == 10)
            {
                if (long.TryParse(str, NumberStyles.Integer | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long intResult))
                {
                    return LuaValue.Integer(intResult);
                }
                if (double.TryParse(str, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out double floatResult))
                {
                    return LuaValue.Number(floatResult);
                }
            }
            else if (base_ == 16)
            {
                if (long.TryParse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out long hexResult))
                {
                    return LuaValue.Integer(hexResult);
                }
            }
            
            return null;
        }
    }
}