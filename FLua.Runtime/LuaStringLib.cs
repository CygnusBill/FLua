using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FLua.Runtime
{
    /// <summary>
    /// Lua String Library implementation
    /// </summary>
    public static class LuaStringLib
    {
        /// <summary>
        /// Adds the string library to the Lua environment
        /// </summary>
        public static void AddStringLibrary(LuaEnvironment env)
        {
            var stringTable = new LuaTable();
            
            // Basic string functions
            stringTable.Set(LuaValue.String("len"), new BuiltinFunction(Len));
            stringTable.Set(LuaValue.String("sub"), new BuiltinFunction(Sub));
            stringTable.Set(LuaValue.String("upper"), new BuiltinFunction(Upper));
            stringTable.Set(LuaValue.String("lower"), new BuiltinFunction(Lower));
            stringTable.Set(LuaValue.String("reverse"), new BuiltinFunction(Reverse));
            
            // Character functions
            stringTable.Set(LuaValue.String("char"), new BuiltinFunction(Char));
            stringTable.Set(LuaValue.String("byte"), new BuiltinFunction(Byte));
            
            // Repetition
            stringTable.Set(LuaValue.String("rep"), new BuiltinFunction(Rep));
            
            // Pattern matching (simplified - full Lua patterns are complex)
            stringTable.Set(LuaValue.String("find"), new BuiltinFunction(Find));
            stringTable.Set(LuaValue.String("match"), new BuiltinFunction(Match));
            stringTable.Set(LuaValue.String("gsub"), new BuiltinFunction(GSub));
            stringTable.Set(LuaValue.String("gmatch"), new BuiltinFunction(GMatch));
            
            // Formatting
            stringTable.Set(LuaValue.String("format"), new BuiltinFunction(Format));
            
            // Binary packing/unpacking (Lua 5.3+)
            stringTable.Set(LuaValue.String("pack"), new BuiltinFunction(Pack));
            stringTable.Set(LuaValue.String("unpack"), new BuiltinFunction(Unpack));
            stringTable.Set(LuaValue.String("packsize"), new BuiltinFunction(PackSize));
            
            env.SetVariable("string", stringTable);
        }
        
        #region Basic String Functions
        
        private static LuaValue[] Len(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'len' (string expected)");
            
            var str = args[0];
            if (str.IsString)
                return [LuaValue.Integer(str.AsString().Length)];
            
            if (str.IsNumber)
                return [LuaValue.Integer(str.ToString().Length)];
            
            throw new LuaRuntimeException("bad argument #1 to 'len' (string expected)");
        }
        
        private static LuaValue[] Sub(LuaValue[] args)
        {
            if (args.Length < 2)
                throw new LuaRuntimeException("bad argument #2 to 'sub' (number expected)");
            
            var str = args[0];
            var start = args[1];
            LuaValue end = LuaValue.Nil;
            if(args.Length > 2)
            {
                end = args[2];
            }
            
            var stringValue = str.AsString();
            if (!start.IsInteger)
                throw new LuaRuntimeException("bad argument #2 to 'sub' (number expected)");
            
            var startIndex = (int)start.AsInteger();
            var endIndex = end != LuaValue.Nil && end.IsInteger ? (int)end.AsInteger() : stringValue.Length;
            
            // Convert Lua 1-based indexing to C# 0-based
            if (startIndex < 0)
                startIndex = stringValue.Length + startIndex + 1;
            if (endIndex < 0)
                endIndex = stringValue.Length + endIndex + 1;
            
            startIndex = Math.Max(1, Math.Min(startIndex, stringValue.Length + 1));
            endIndex = Math.Max(0, Math.Min(endIndex, stringValue.Length));
            
            if (startIndex > endIndex || startIndex > stringValue.Length)
                return [LuaValue.String("")];
            
            var length = (int)(endIndex - startIndex + 1);
            if (length <= 0)
                return [LuaValue.String("")];
            
            try
            {
                var result = stringValue.Substring(startIndex - 1, Math.Min(length, stringValue.Length - startIndex + 1));
                return [LuaValue.String(result)];
            }
            catch
            {
                return [LuaValue.String("")];
            }
        }
        
        private static LuaValue[] Upper(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'upper' (string expected)");
            
            var str = args[0];
            return [LuaValue.String(str.AsString().ToUpperInvariant())];
        }
        
        private static LuaValue[] Lower(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'lower' (string expected)");
            
            var str = args[0];
            return [LuaValue.String(str.AsString().ToLowerInvariant())];
        }
        
        private static LuaValue[] Reverse(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'reverse' (string expected)");
            
            var str = args[0];
            var chars = str.AsString().ToCharArray();
            Array.Reverse(chars);
            return [LuaValue.String(new string(chars))];
        }
        
        #endregion
        
        #region Character Functions
        
        private static LuaValue[] Char(LuaValue[] args)
        {
            var chars = new char[args.Length];
            
            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].IsInteger)
                    throw new LuaRuntimeException($"bad argument #{i + 1} to 'char' (number expected)");
                
                var value = args[i].AsInteger();
                if (value < 0 || value > 255)
                    throw new LuaRuntimeException($"bad argument #{i + 1} to 'char' (out of range)");
                
                chars[i] = (char)value;
            }
            
            return [LuaValue.String(new string(chars))];
        }
        
        private static LuaValue[] Byte(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'byte' (string expected)");
            
            var str = args[0].AsString();
            var start = args.Length > 1 && args[1].IsInteger ? args[1].AsInteger() : 1;
            var end = args.Length > 2 && args[2].IsInteger ? args[2].AsInteger() : start;
            
            if (start < 0)
                start = str.Length + start + 1;
            if (end < 0)
                end = str.Length + end + 1;
            
            start = Math.Max(1, Math.Min(start, str.Length));
            end = Math.Max(start, Math.Min(end, str.Length));
            
            if (start > str.Length)
                return []; // Return no values
            
            var results = new List<LuaValue>();
            for (long i = start; i <= end && i <= str.Length; i++)
            {
                results.Add(LuaValue.Integer((byte)str[(int)i - 1]));
            }
            
            return results.ToArray();
        }
        
        #endregion
        
        #region Repetition Functions
        
        private static LuaValue[] Rep(LuaValue[] args)
        {
            if (args.Length < 2)
                throw new LuaRuntimeException("bad argument #2 to 'rep' (number expected)");
            
            var str = args[0].AsString();
            var count = args[1];
            var separator = args.Length > 2 ? args[2].AsString() : "";
            
            if (!count.IsInteger)
                throw new LuaRuntimeException("bad argument #2 to 'rep' (number expected)");
            
            var n = (int)count.AsInteger();
            if (n <= 0)
                return [LuaValue.String("")];
            
            if (n == 1)
                return [LuaValue.String(str)];
            
            try
            {
                if (string.IsNullOrEmpty(separator))
                {
                    var sb = new StringBuilder(str.Length * n);
                    for (int i = 0; i < n; i++)
                    {
                        sb.Append(str);
                    }
                    return [LuaValue.String(sb.ToString())];
                }
                else
                {
                    var sb = new StringBuilder();
                    for (int i = 0; i < n; i++)
                    {
                        if (i > 0)
                            sb.Append(separator);
                        sb.Append(str);
                    }
                    return [LuaValue.String(sb.ToString())];
                }
            }
            catch (OutOfMemoryException)
            {
                throw new LuaRuntimeException("resulting string too large");
            }
        }
        
        #endregion
        
        #region Pattern Matching Functions (Simplified)
        
        private static LuaValue[] Find(LuaValue[] args)
        {
            if (args.Length < 2)
                throw new LuaRuntimeException("bad argument #2 to 'find' (string expected)");
            
            var str = args[0].AsString();
            var pattern = args[1].AsString();
            var start = args.Length > 2 && args[2].IsInteger ? (int)args[2].AsInteger() : 1;
            var plain = args.Length > 3 ? args[3].IsTruthy() : false;
            
            if (start < 0)
                start = str.Length + start + 1;
            start = Math.Max(1, Math.Min(start, str.Length + 1));
            
            if (start > str.Length)
                return [LuaValue.Nil];
            
            try
            {
                var match = LuaPatterns.Find(str, pattern, start, plain);
                
                if (match != null)
                {
                    var results = new List<LuaValue>
                    {
                        LuaValue.Integer(match.Start), // Already 1-based
                        LuaValue.Integer(match.End)    // Already 1-based
                    };
                    
                    // Add captured groups
                    foreach (var capture in match.Captures)
                    {
                        results.Add(LuaValue.String(capture));
                    }
                    
                    return results.ToArray();
                }
            }
            catch (ArgumentException)
            {
                throw new LuaRuntimeException("invalid pattern");
            }
            
            return [LuaValue.Nil];
        }
        
        private static LuaValue[] Match(LuaValue[] args)
        {
            if (args.Length < 2)
                throw new LuaRuntimeException("bad argument #2 to 'match' (string expected)");
            
            var str = args[0].AsString();
            var pattern = args[1].AsString();
            var start = args.Length > 2 && args[2].IsInteger ? (int)args[2].AsInteger() : 1;
            
            if (start < 0)
                start = str.Length + start + 1;
            start = Math.Max(1, Math.Min(start, str.Length + 1));
            
            try
            {
                var regexPattern = ConvertLuaPatternToRegex(pattern);
                var regex = new Regex(regexPattern);
                var match = regex.Match(str, start - 1);
                
                if (match.Success)
                {
                    if (match.Groups.Count > 1)
                    {
                        // Return captured groups
                        var results = new List<LuaValue>();
                        for (int i = 1; i < match.Groups.Count; i++)
                        {
                            results.Add(LuaValue.String(match.Groups[i].Value));
                        }
                        return results.ToArray();
                    }
                    else
                    {
                        // Return the entire match
                        return [LuaValue.String(match.Value)];
                    }
                }
            }
            catch (ArgumentException)
            {
                throw new LuaRuntimeException("invalid pattern");
            }
            
            return [];
        }
        
        private static LuaValue[] GSub(LuaValue[] args)
        {
            if (args.Length < 3)
                throw new LuaRuntimeException("bad argument #3 to 'gsub' (string/function/table expected)");
            
            var str = args[0].AsString();
            var pattern = args[1].AsString();
            var replacement = args[2];
            var limit = args.Length > 3 && args[3].IsInteger ? (int)args[3].AsInteger() : int.MaxValue;
            
            try
            {
                var regexPattern = ConvertLuaPatternToRegex(pattern);
                var regex = new Regex(regexPattern);
                var matches = regex.Matches(str);
                
                if (matches.Count == 0)
                    return [LuaValue.String(str), LuaValue.Integer(0)];
                
                var result = str;
                var count = 0;
                
                // Simple string replacement (more complex function/table replacements would need additional logic)
                if (replacement.IsString)
                {
                    var replStr = replacement.AsString();
                    result = regex.Replace(str, replStr, Math.Min(limit, matches.Count));
                    count = Math.Min(limit, matches.Count);
                }
                else if (replacement.IsFunction || replacement.IsTable)
                {
                    // For now, just do basic replacement - full implementation would call functions/lookup tables
                    result = regex.Replace(str, "", Math.Min(limit, matches.Count));
                    count = Math.Min(limit, matches.Count);
                }
                
                return [LuaValue.String(result), LuaValue.Integer(count)];
            }
            catch (ArgumentException)
            {
                throw new LuaRuntimeException("invalid pattern");
            }
        }
        
        private static LuaValue[] GMatch(LuaValue[] args)
        {
            if (args.Length < 2)
                throw new LuaRuntimeException("bad argument #2 to 'gmatch' (string expected)");
            
            var str = args[0].AsString();
            var pattern = args[1].AsString();
            
            // Return an iterator function
            var matches = new List<string>();
            try
            {
                var regexPattern = ConvertLuaPatternToRegex(pattern);
                var regex = new Regex(regexPattern);
                var regexMatches = regex.Matches(str);
                
                foreach (Match match in regexMatches)
                {
                    matches.Add(match.Value);
                }
            }
            catch (ArgumentException)
            {
                throw new LuaRuntimeException("invalid pattern");
            }
            
            var index = 0;
            var iterator = new BuiltinFunction(iterArgs =>
            {
                if (index < matches.Count)
                {
                    return [LuaValue.String(matches[index++])];
                }
                return [];
            });
            
            return [LuaValue.Function(iterator)];
        }
        
        /// <summary>
        /// Converts a simplified Lua pattern to a .NET regex pattern
        /// Note: This is a basic implementation - full Lua patterns are more complex
        /// </summary>
        private static string ConvertLuaPatternToRegex(string luaPattern)
        {
            // This is a very simplified conversion
            // Full Lua pattern implementation would be much more complex
            return luaPattern
                .Replace(".", "\\.")  
                .Replace("*", ".*")
                .Replace("?", ".?")
                .Replace("+", ".+")
                .Replace("^", "^")
                .Replace("$", "$")
                .Replace("[", "[")
                .Replace("]", "]")
                .Replace("(", "(")
                .Replace(")", ")");
        }
        
        #endregion
        
        #region Formatting Functions
        
        private static LuaValue[] Format(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'format' (string expected)");
            
            var format = args[0].AsString();
            var values = args.Skip(1).ToArray();
            
            try
            {
                var result = new StringBuilder();
                var valueIndex = 0;
                var pos = 0;
                
                // Pattern for format specifiers: %[-+ #0]?[width]?[.precision]?[specifier]
                var formatPattern = @"%([+\- #0]*)(\d*)(?:\.(\d+))?([diouxXeEfFgGaAcspq%])";
                var regex = new Regex(formatPattern);
                
                foreach (Match match in regex.Matches(format))
                {
                    // Append text before the format specifier
                    result.Append(format.Substring(pos, match.Index - pos));
                    pos = match.Index + match.Length;
                    
                    var flags = match.Groups[1].Value;
                    var widthStr = match.Groups[2].Value;
                    var precisionStr = match.Groups[3].Value;
                    var specifier = match.Groups[4].Value;
                    
                    if (specifier == "%")
                    {
                        result.Append("%");
                        continue;
                    }
                    
                    if (valueIndex >= values.Length)
                        throw new LuaRuntimeException($"bad argument #{valueIndex + 2} to 'format' (no value)");
                    
                    var value = values[valueIndex++];
                    var width = string.IsNullOrEmpty(widthStr) ? 0 : int.Parse(widthStr);
                    var precision = string.IsNullOrEmpty(precisionStr) ? -1 : int.Parse(precisionStr);
                    
                    // Format the value
                    string formatted = FormatValue(value, specifier, flags, width, precision);
                    result.Append(formatted);
                }
                
                // Append remaining text
                result.Append(format.Substring(pos));
                
                return [LuaValue.String(result.ToString())];
            }
            catch (Exception ex) when (!(ex is LuaRuntimeException))
            {
                throw new LuaRuntimeException($"invalid format string: {ex.Message}");
            }
        }
        
        private static string FormatValue(LuaValue value, string specifier, string flags, int width, int precision)
        {
            bool leftAlign = flags.Contains('-');
            bool showSign = flags.Contains('+');
            bool spaceSign = flags.Contains(' ');
            bool altForm = flags.Contains('#');
            bool zeroPad = flags.Contains('0') && !leftAlign;
            
            string result = specifier switch
            {
                "d" or "i" => FormatInteger(value.IsInteger ? value.AsInteger() : 0, showSign, spaceSign),
                "o" => FormatOctal(value.IsInteger ? value.AsInteger() : 0, altForm),
                "u" => FormatUnsigned(value.IsInteger ? value.AsInteger() : 0),
                "x" => FormatHex(value.IsInteger ? value.AsInteger() : 0, false, altForm),
                "X" => FormatHex(value.IsInteger ? value.AsInteger() : 0, true, altForm),
                "f" or "F" => FormatFloat(value.IsNumber ? value.AsDouble() : 0, precision >= 0 ? precision : 6, specifier == "F"),
                "e" => FormatExponential(value.IsNumber ? value.AsDouble() : 0, precision >= 0 ? precision : 6, false),
                "E" => FormatExponential(value.IsNumber ? value.AsDouble() : 0, precision >= 0 ? precision : 6, true),
                "g" => FormatGeneral(value.IsNumber ? value.AsDouble() : 0, precision >= 0 ? precision : 6, false, altForm),
                "G" => FormatGeneral(value.IsNumber ? value.AsDouble() : 0, precision >= 0 ? precision : 6, true, altForm),
                "c" => FormatCharacter(value),
                "s" => FormatString(value, precision),
                "q" => FormatQuotedString(value),
                "p" => FormatPointer(value),
                _ => value.ToString() ?? ""
            };
            
            // Apply width and alignment
            if (width > 0 && result.Length < width)
            {
                if (leftAlign)
                {
                    result = result.PadRight(width);
                }
                else if (zeroPad && IsNumericSpecifier(specifier))
                {
                    // For zero padding, we need to handle signs specially
                    if (result.StartsWith("-") || result.StartsWith("+") || result.StartsWith(" "))
                    {
                        result = result[0] + new string('0', width - result.Length) + result.Substring(1);
                    }
                    else if (result.StartsWith("0x") || result.StartsWith("0X"))
                    {
                        result = result.Substring(0, 2) + new string('0', width - result.Length) + result.Substring(2);
                    }
                    else
                    {
                        result = new string('0', width - result.Length) + result;
                    }
                }
                else
                {
                    result = result.PadLeft(width);
                }
            }
            
            return result;
        }
        
        private static bool IsNumericSpecifier(string spec)
        {
            return spec == "d" || spec == "i" || spec == "o" || spec == "u" || 
                   spec == "x" || spec == "X" || spec == "f" || spec == "F" || 
                   spec == "e" || spec == "E" || spec == "g" || spec == "G";
        }
        
        private static string FormatInteger(long value, bool showSign, bool spaceSign)
        {
            var result = value.ToString();
            if (value >= 0)
            {
                if (showSign) result = "+" + result;
                else if (spaceSign) result = " " + result;
            }
            return result;
        }
        
        private static string FormatUnsigned(long value)
        {
            return ((ulong)value).ToString();
        }
        
        private static string FormatOctal(long value, bool altForm)
        {
            var result = Convert.ToString(value, 8);
            if (altForm && !result.StartsWith("0") && result != "0")
                result = "0" + result;
            return result;
        }
        
        private static string FormatHex(long value, bool upper, bool altForm)
        {
            var result = Convert.ToString(value, 16);
            if (upper) result = result.ToUpperInvariant();
            if (altForm && value != 0)
                result = (upper ? "0X" : "0x") + result;
            return result;
        }
        
        private static string FormatFloat(double value, int precision, bool upper)
        {
            return value.ToString($"F{precision}");
        }
        
        private static string FormatExponential(double value, int precision, bool upper)
        {
            return value.ToString(upper ? $"E{precision}" : $"e{precision}");
        }
        
        private static string FormatGeneral(double value, int precision, bool upper, bool altForm)
        {
            var result = value.ToString(upper ? $"G{precision}" : $"g{precision}");
            // In alternate form, trailing zeros are not removed
            if (altForm && !result.Contains('.') && !result.Contains('e') && !result.Contains('E'))
            {
                result += ".";
            }
            return result;
        }
        
        private static string FormatCharacter(LuaValue value)
        {
            var code = value.IsInteger ? value.AsInteger() : 0;
            if (code < 0 || code > 255)
                throw new LuaRuntimeException($"bad argument to format (char out of range)");
            return new string((char)code, 1);
        }
        
        private static string FormatString(LuaValue value, int precision)
        {
            var str = value.AsString() ?? "";
            if (precision >= 0 && precision < str.Length)
                str = str.Substring(0, precision);
            return str;
        }
        
        private static string FormatQuotedString(LuaValue value)
        {
            var str = value.AsString() ?? "";
            var result = new StringBuilder();
            result.Append('"');
            
            foreach (char c in str)
            {
                switch (c)
                {
                    case '\n': result.Append("\\n"); break;
                    case '\r': result.Append("\\r"); break;
                    case '\t': result.Append("\\t"); break;
                    case '\b': result.Append("\\b"); break;
                    case '\f': result.Append("\\f"); break;
                    case '"': result.Append("\\\""); break;
                    case '\\': result.Append("\\\\"); break;
                    default:
                        if (c >= 32 && c < 127)
                            result.Append(c);
                        else
                            result.Append($"\\{(int)c:D3}");
                        break;
                }
            }
            
            result.Append('"');
            return result.ToString();
        }
        
        private static string FormatPointer(LuaValue value)
        {
            return $"0x{value.GetHashCode():x8}";
        }
        
        #endregion
        
        #region Binary Packing/Unpacking Functions
        
        private static LuaValue[] Pack(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'pack' (string expected)");
                
            var format = args[0].AsString();
            var values = args.Skip(1).ToList();
            var result = new List<byte>();
            var valueIndex = 0;
            
            try
            {
                var pos = 0;
                while (pos < format.Length)
                {
                    var (size, needsValue) = ProcessPackFormat(format, ref pos, result, 
                        () => 
                        {
                            if (valueIndex >= values.Count)
                                throw new LuaRuntimeException($"bad argument #{valueIndex + 2} to 'pack' (no value)");
                            return values[valueIndex++];
                        });
                }
                
                // Convert byte array to string (Lua strings can contain binary data)
                return [LuaValue.String(Encoding.GetEncoding("ISO-8859-1").GetString(result.ToArray()))];
            }
            catch (Exception ex) when (!(ex is LuaRuntimeException))
            {
                throw new LuaRuntimeException($"invalid format string: {ex.Message}");
            }
        }
        
        private static LuaValue[] Unpack(LuaValue[] args)
        {
            if (args.Length < 2)
                throw new LuaRuntimeException("bad argument #2 to 'unpack' (string expected)");
                
            var format = args[0].AsString();
            var data = Encoding.GetEncoding("ISO-8859-1").GetBytes(args[1].AsString());
            var startPos = args.Length > 2 && args[2].IsInteger ? (int)args[2].AsInteger() - 1 : 0; // Lua uses 1-based indexing
            
            var results = new List<LuaValue>();
            var dataPos = startPos;
            
            try
            {
                var formatPos = 0;
                while (formatPos < format.Length)
                {
                    var value = ProcessUnpackFormat(format, ref formatPos, data, ref dataPos);
                    if (value.HasValue)
                        results.Add(value.Value);
                }
                
                // Return unpacked values plus the position after last read byte (1-based)
                results.Add(LuaValue.Integer(dataPos + 1));
                return results.ToArray();
            }
            catch (Exception ex) when (!(ex is LuaRuntimeException))
            {
                throw new LuaRuntimeException($"invalid format string: {ex.Message}");
            }
        }
        
        private static LuaValue[] PackSize(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'packsize' (string expected)");
                
            var format = args[0].AsString();
            
            try
            {
                var totalSize = 0;
                var pos = 0;
                
                while (pos < format.Length)
                {
                    var size = GetFormatSize(format, ref pos);
                    if (size < 0)
                        throw new LuaRuntimeException("variable-length format");
                    totalSize += size;
                }
                
                return [LuaValue.Integer(totalSize)];
            }
            catch (Exception ex) when (!(ex is LuaRuntimeException))
            {
                throw new LuaRuntimeException($"invalid format string: {ex.Message}");
            }
        }
        
        private static int GetFormatSize(string format, ref int pos)
        {
            if (pos >= format.Length)
                return 0;
                
            // Skip whitespace
            while (pos < format.Length && char.IsWhiteSpace(format[pos]))
                pos++;
                
            if (pos >= format.Length)
                return 0;
                
            var c = format[pos++];
            
            // Handle alignment specifiers
            if (c == '<' || c == '>' || c == '=' || c == '!')
                return 0;
                
            // Get optional size
            var size = 0;
            while (pos < format.Length && char.IsDigit(format[pos]))
            {
                size = size * 10 + (format[pos] - '0');
                pos++;
            }
            
            switch (c)
            {
                case 'b': return 1;  // signed byte
                case 'B': return 1;  // unsigned byte
                case 'h': return 2;  // signed short
                case 'H': return 2;  // unsigned short
                case 'l': return 8;  // signed long (Lua uses 8 bytes)
                case 'L': return 8;  // unsigned long
                case 'j': return 8;  // lua_Integer (8 bytes)
                case 'J': return 8;  // lua_Unsigned (8 bytes)
                case 'T': return 8;  // size_t
                case 'i': return size > 0 ? size : 4;  // signed int with given size
                case 'I': return size > 0 ? size : 4;  // unsigned int with given size
                case 'f': return 4;  // float
                case 'd': return 8;  // double
                case 'n': return 8;  // Lua number (double)
                case 'c': return size > 0 ? size : 1;  // fixed-length string
                case 'z': return -1; // zero-terminated string (variable length)
                case 's': return -1; // string with length prefix (variable length)
                case 'x': return 1;  // padding byte
                case 'X': return 0;  // alignment (no size)
                case ' ': return 0;  // ignored
                default:
                    throw new LuaRuntimeException($"invalid format option '{c}'");
            }
        }
        
        private static (int size, bool needsValue) ProcessPackFormat(string format, ref int pos, 
            List<byte> output, Func<LuaValue> getValue)
        {
            if (pos >= format.Length)
                return (0, false);
                
            // Skip whitespace
            while (pos < format.Length && char.IsWhiteSpace(format[pos]))
                pos++;
                
            if (pos >= format.Length)
                return (0, false);
                
            var c = format[pos++];
            
            // Handle endianness/alignment specifiers
            if (c == '<') { /* little endian - default */ return (0, false); }
            if (c == '>') { /* big endian - TODO */ return (0, false); }
            if (c == '=') { /* native endian */ return (0, false); }
            if (c == '!') { /* TODO: alignment */ return (0, false); }
            
            // Get optional size
            var size = 0;
            while (pos < format.Length && char.IsDigit(format[pos]))
            {
                size = size * 10 + (format[pos] - '0');
                pos++;
            }
            
            switch (c)
            {
                case 'b': // signed byte
                    {
                        var value = getValue();
                        var num = value.IsInteger ? value.AsInteger() : 0;
                        output.Add((byte)(sbyte)num);
                        return (1, true);
                    }
                    
                case 'B': // unsigned byte
                    {
                        var value = getValue();
                        var num = value.IsInteger ? value.AsInteger() : 0;
                        output.Add((byte)num);
                        return (1, true);
                    }
                    
                case 'h': // signed short
                    {
                        var value = getValue();
                        var num = value.IsInteger ? value.AsInteger() : 0;
                        var bytes = BitConverter.GetBytes((short)num);
                        output.AddRange(bytes);
                        return (2, true);
                    }
                    
                case 'H': // unsigned short
                    {
                        var value = getValue();
                        var num = value.IsInteger ? value.AsInteger() : 0;
                        var bytes = BitConverter.GetBytes((ushort)num);
                        output.AddRange(bytes);
                        return (2, true);
                    }
                    
                case 'l': // signed long (8 bytes in Lua)
                case 'j': // lua_Integer
                    {
                        var value = getValue();
                        var num = value.IsInteger ? value.AsInteger() : 0;
                        var bytes = BitConverter.GetBytes(num);
                        output.AddRange(bytes);
                        return (8, true);
                    }
                    
                case 'L': // unsigned long
                case 'J': // lua_Unsigned
                case 'T': // size_t
                    {
                        var value = getValue();
                        var num = value.IsInteger ? value.AsInteger() : 0;
                        var bytes = BitConverter.GetBytes((ulong)num);
                        output.AddRange(bytes);
                        return (8, true);
                    }
                    
                case 'i': // signed int with size
                case 'I': // unsigned int with size
                    {
                        var value = getValue();
                        var num = value.IsInteger ? value.AsInteger() : 0;
                        var intSize = size > 0 ? size : 4;
                        
                        if (intSize > 8)
                            throw new LuaRuntimeException($"integral size ({intSize}) out of limits [1,8]");
                            
                        // Pack the integer in the specified number of bytes
                        for (int i = 0; i < intSize; i++)
                        {
                            output.Add((byte)(num & 0xFF));
                            num >>= 8;
                        }
                        return (intSize, true);
                    }
                    
                case 'f': // float
                    {
                        var value = getValue();
                        var num = value.IsNumber ? value.AsDouble() : 0;
                        var bytes = BitConverter.GetBytes((float)num);
                        output.AddRange(bytes);
                        return (4, true);
                    }
                    
                case 'd': // double
                case 'n': // Lua number
                    {
                        var value = getValue();
                        var num = value.IsNumber ? value.AsDouble() : 0;
                        var bytes = BitConverter.GetBytes(num);
                        output.AddRange(bytes);
                        return (8, true);
                    }
                    
                case 'c': // fixed-length string
                    {
                        var value = getValue();
                        var str = value.AsString() ?? "";
                        var strSize = size > 0 ? size : 1;
                        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(str);
                        
                        // Pad or truncate to exact size
                        for (int i = 0; i < strSize; i++)
                        {
                            if (i < bytes.Length)
                                output.Add(bytes[i]);
                            else
                                output.Add(0); // pad with zeros
                        }
                        return (strSize, true);
                    }
                    
                case 'z': // zero-terminated string
                    {
                        var value = getValue();
                        var str = value.AsString() ?? "";
                        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(str);
                        output.AddRange(bytes);
                        output.Add(0); // null terminator
                        return (bytes.Length + 1, true);
                    }
                    
                case 's': // string with length prefix
                    {
                        var value = getValue();
                        var str = value.AsString() ?? "";
                        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(str);
                        var lenSize = size > 0 ? size : 8; // default to 8-byte length
                        
                        if (lenSize > 8)
                            throw new LuaRuntimeException($"string length size ({lenSize}) out of limits [1,8]");
                            
                        // Pack the length
                        var len = (ulong)bytes.Length;
                        for (int i = 0; i < lenSize; i++)
                        {
                            output.Add((byte)(len & 0xFF));
                            len >>= 8;
                        }
                        
                        // Pack the string data
                        output.AddRange(bytes);
                        return (lenSize + bytes.Length, true);
                    }
                    
                case 'x': // padding byte
                    output.Add(0);
                    return (1, false);
                    
                case 'X': // alignment
                    // TODO: Implement alignment
                    return (0, false);
                    
                case ' ': // ignored
                    return (0, false);
                    
                default:
                    throw new LuaRuntimeException($"invalid format option '{c}'");
            }
        }
        
        private static LuaValue? ProcessUnpackFormat(string format, ref int pos, byte[] data, ref int dataPos)
        {
            if (pos >= format.Length)
                return null;
                
            // Skip whitespace
            while (pos < format.Length && char.IsWhiteSpace(format[pos]))
                pos++;
                
            if (pos >= format.Length)
                return null;
                
            var c = format[pos++];
            
            // Handle endianness/alignment specifiers
            if (c == '<' || c == '>' || c == '=' || c == '!')
                return null;
                
            // Get optional size
            var size = 0;
            while (pos < format.Length && char.IsDigit(format[pos]))
            {
                size = size * 10 + (format[pos] - '0');
                pos++;
            }
            
            switch (c)
            {
                case 'b': // signed byte
                    if (dataPos >= data.Length)
                        throw new LuaRuntimeException("data string too short");
                    return LuaValue.Integer((sbyte)data[dataPos++]);
                    
                case 'B': // unsigned byte
                    if (dataPos >= data.Length)
                        throw new LuaRuntimeException("data string too short");
                    return LuaValue.Integer(data[dataPos++]);
                    
                case 'h': // signed short
                    if (dataPos + 2 > data.Length)
                        throw new LuaRuntimeException("data string too short");
                    var shortVal = BitConverter.ToInt16(data, dataPos);
                    dataPos += 2;
                    return LuaValue.Integer(shortVal);
                    
                case 'H': // unsigned short
                    if (dataPos + 2 > data.Length)
                        throw new LuaRuntimeException("data string too short");
                    var ushortVal = BitConverter.ToUInt16(data, dataPos);
                    dataPos += 2;
                    return LuaValue.Integer(ushortVal);
                    
                case 'l': // signed long
                case 'j': // lua_Integer
                    if (dataPos + 8 > data.Length)
                        throw new LuaRuntimeException("data string too short");
                    var longVal = BitConverter.ToInt64(data, dataPos);
                    dataPos += 8;
                    return LuaValue.Integer(longVal);
                    
                case 'L': // unsigned long
                case 'J': // lua_Unsigned
                case 'T': // size_t
                    if (dataPos + 8 > data.Length)
                        throw new LuaRuntimeException("data string too short");
                    var ulongVal = BitConverter.ToUInt64(data, dataPos);
                    dataPos += 8;
                    return LuaValue.Integer((long)ulongVal);
                    
                case 'i': // signed int with size
                case 'I': // unsigned int with size
                    {
                        var intSize = size > 0 ? size : 4;
                        if (intSize > 8)
                            throw new LuaRuntimeException($"integral size ({intSize}) out of limits [1,8]");
                        if (dataPos + intSize > data.Length)
                            throw new LuaRuntimeException("data string too short");
                            
                        long value = 0;
                        for (int i = intSize - 1; i >= 0; i--)
                        {
                            value = (value << 8) | data[dataPos + i];
                        }
                        
                        // Sign extend if needed
                        if (c == 'i' && intSize < 8)
                        {
                            var signBit = 1L << (intSize * 8 - 1);
                            if ((value & signBit) != 0)
                            {
                                value |= -1L << (intSize * 8);
                            }
                        }
                        
                        dataPos += intSize;
                        return LuaValue.Integer(value);
                    }
                    
                case 'f': // float
                    if (dataPos + 4 > data.Length)
                        throw new LuaRuntimeException("data string too short");
                    var floatVal = BitConverter.ToSingle(data, dataPos);
                    dataPos += 4;
                    return LuaValue.Number(floatVal);
                    
                case 'd': // double
                case 'n': // Lua number
                    if (dataPos + 8 > data.Length)
                        throw new LuaRuntimeException("data string too short");
                    var doubleVal = BitConverter.ToDouble(data, dataPos);
                    dataPos += 8;
                    return LuaValue.Number(doubleVal);
                    
                case 'c': // fixed-length string
                    {
                        var strSize = size > 0 ? size : 1;
                        if (dataPos + strSize > data.Length)
                            throw new LuaRuntimeException("data string too short");
                        var str = Encoding.GetEncoding("ISO-8859-1").GetString(data, dataPos, strSize);
                        dataPos += strSize;
                        return LuaValue.String(str);
                    }
                    
                case 'z': // zero-terminated string
                    {
                        var start = dataPos;
                        while (dataPos < data.Length && data[dataPos] != 0)
                            dataPos++;
                        if (dataPos >= data.Length)
                            throw new LuaRuntimeException("unfinished string");
                        var str = Encoding.GetEncoding("ISO-8859-1").GetString(data, start, dataPos - start);
                        dataPos++; // skip null terminator
                        return LuaValue.String(str);
                    }
                    
                case 's': // string with length prefix
                    {
                        var lenSize = size > 0 ? size : 8;
                        if (lenSize > 8)
                            throw new LuaRuntimeException($"string length size ({lenSize}) out of limits [1,8]");
                        if (dataPos + lenSize > data.Length)
                            throw new LuaRuntimeException("data string too short");
                            
                        // Read the length
                        ulong len = 0;
                        for (int i = lenSize - 1; i >= 0; i--)
                        {
                            len = (len << 8) | data[dataPos + i];
                        }
                        dataPos += lenSize;
                        
                        // Read the string
                        if (dataPos + (int)len > data.Length)
                            throw new LuaRuntimeException("data string too short");
                        var str = Encoding.GetEncoding("ISO-8859-1").GetString(data, dataPos, (int)len);
                        dataPos += (int)len;
                        return LuaValue.String(str);
                    }
                    
                case 'x': // padding byte
                    if (dataPos >= data.Length)
                        throw new LuaRuntimeException("data string too short");
                    dataPos++;
                    return null;
                    
                case 'X': // alignment
                    // TODO: Implement alignment
                    return null;
                    
                case ' ': // ignored
                    return null;
                    
                default:
                    throw new LuaRuntimeException($"invalid format option '{c}'");
            }
        }
        
        #endregion
    }
} 