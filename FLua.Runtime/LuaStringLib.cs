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
            stringTable.Set(new LuaString("len"), new BuiltinFunction(Len));
            stringTable.Set(new LuaString("sub"), new BuiltinFunction(Sub));
            stringTable.Set(new LuaString("upper"), new BuiltinFunction(Upper));
            stringTable.Set(new LuaString("lower"), new BuiltinFunction(Lower));
            stringTable.Set(new LuaString("reverse"), new BuiltinFunction(Reverse));
            
            // Character functions
            stringTable.Set(new LuaString("char"), new BuiltinFunction(Char));
            stringTable.Set(new LuaString("byte"), new BuiltinFunction(Byte));
            
            // Repetition
            stringTable.Set(new LuaString("rep"), new BuiltinFunction(Rep));
            
            // Pattern matching (simplified - full Lua patterns are complex)
            stringTable.Set(new LuaString("find"), new BuiltinFunction(Find));
            stringTable.Set(new LuaString("match"), new BuiltinFunction(Match));
            stringTable.Set(new LuaString("gsub"), new BuiltinFunction(GSub));
            stringTable.Set(new LuaString("gmatch"), new BuiltinFunction(GMatch));
            
            // Formatting
            stringTable.Set(new LuaString("format"), new BuiltinFunction(Format));
            
            env.SetVariable("string", stringTable);
        }
        
        #region Basic String Functions
        
        private static LuaValue[] Len(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'len' (string expected)");
            
            var str = args[0];
            if (str is LuaString luaStr)
                return new[] { new LuaInteger(luaStr.Value.Length) };
            
            if (str is LuaNumber || str is LuaInteger)
                return new[] { new LuaInteger(str.ToString()?.Length ?? 0) };
            
            throw new LuaRuntimeException("bad argument #1 to 'len' (string expected)");
        }
        
        private static LuaValue[] Sub(LuaValue[] args)
        {
            if (args.Length < 2)
                throw new LuaRuntimeException("bad argument #2 to 'sub' (number expected)");
            
            var str = args[0];
            var start = args[1];
            var end = args.Length > 2 ? args[2] : null;
            
            var stringValue = str.AsString;
            if (!start.AsInteger.HasValue)
                throw new LuaRuntimeException("bad argument #2 to 'sub' (number expected)");
            
            var startIndex = (int)start.AsInteger.Value;
            var endIndex = end?.AsInteger ?? stringValue.Length;
            
            // Convert Lua 1-based indexing to C# 0-based
            if (startIndex < 0)
                startIndex = stringValue.Length + startIndex + 1;
            if (endIndex < 0)
                endIndex = stringValue.Length + endIndex + 1;
            
            startIndex = Math.Max(1, Math.Min(startIndex, stringValue.Length + 1));
            endIndex = Math.Max(0, Math.Min(endIndex, stringValue.Length));
            
            if (startIndex > endIndex || startIndex > stringValue.Length)
                return new[] { new LuaString("") };
            
            var length = (int)(endIndex - startIndex + 1);
            if (length <= 0)
                return new[] { new LuaString("") };
            
            try
            {
                var result = stringValue.Substring(startIndex - 1, Math.Min(length, stringValue.Length - startIndex + 1));
                return new[] { new LuaString(result) };
            }
            catch
            {
                return new[] { new LuaString("") };
            }
        }
        
        private static LuaValue[] Upper(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'upper' (string expected)");
            
            var str = args[0];
            return new[] { new LuaString(str.AsString.ToUpperInvariant()) };
        }
        
        private static LuaValue[] Lower(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'lower' (string expected)");
            
            var str = args[0];
            return new[] { new LuaString(str.AsString.ToLowerInvariant()) };
        }
        
        private static LuaValue[] Reverse(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'reverse' (string expected)");
            
            var str = args[0];
            var chars = str.AsString.ToCharArray();
            Array.Reverse(chars);
            return new[] { new LuaString(new string(chars)) };
        }
        
        #endregion
        
        #region Character Functions
        
        private static LuaValue[] Char(LuaValue[] args)
        {
            var chars = new char[args.Length];
            
            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].AsInteger.HasValue)
                    throw new LuaRuntimeException($"bad argument #{i + 1} to 'char' (number expected)");
                
                var value = args[i].AsInteger!.Value;
                if (value < 0 || value > 255)
                    throw new LuaRuntimeException($"bad argument #{i + 1} to 'char' (out of range)");
                
                chars[i] = (char)value;
            }
            
            return new[] { new LuaString(new string(chars)) };
        }
        
        private static LuaValue[] Byte(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'byte' (string expected)");
            
            var str = args[0].AsString;
            var start = args.Length > 1 ? args[1].AsInteger ?? 1 : 1;
            var end = args.Length > 2 ? args[2].AsInteger ?? start : start;
            
            if (start < 0)
                start = str.Length + start + 1;
            if (end < 0)
                end = str.Length + end + 1;
            
            start = Math.Max(1, Math.Min(start, str.Length));
            end = Math.Max(start, Math.Min(end, str.Length));
            
            if (start > str.Length)
                return new LuaValue[0]; // Return no values
            
            var results = new List<LuaValue>();
            for (long i = start; i <= end && i <= str.Length; i++)
            {
                results.Add(new LuaInteger((byte)str[(int)i - 1]));
            }
            
            return results.ToArray();
        }
        
        #endregion
        
        #region Repetition Functions
        
        private static LuaValue[] Rep(LuaValue[] args)
        {
            if (args.Length < 2)
                throw new LuaRuntimeException("bad argument #2 to 'rep' (number expected)");
            
            var str = args[0].AsString;
            var count = args[1];
            var separator = args.Length > 2 ? args[2].AsString : "";
            
            if (!count.AsInteger.HasValue)
                throw new LuaRuntimeException("bad argument #2 to 'rep' (number expected)");
            
            var n = (int)count.AsInteger.Value;
            if (n <= 0)
                return new[] { new LuaString("") };
            
            if (n == 1)
                return new[] { new LuaString(str) };
            
            try
            {
                if (string.IsNullOrEmpty(separator))
                {
                    var sb = new StringBuilder(str.Length * n);
                    for (int i = 0; i < n; i++)
                    {
                        sb.Append(str);
                    }
                    return new[] { new LuaString(sb.ToString()) };
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
                    return new[] { new LuaString(sb.ToString()) };
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
            
            var str = args[0].AsString;
            var pattern = args[1].AsString;
            var start = args.Length > 2 ? (int)(args[2].AsInteger ?? 1) : 1;
            var plain = args.Length > 3 ? LuaValue.IsValueTruthy(args[3]) : false;
            
            if (start < 0)
                start = str.Length + start + 1;
            start = Math.Max(1, Math.Min(start, str.Length + 1));
            
            if (start > str.Length)
                return new[] { LuaNil.Instance };
            
            try
            {
                int index;
                if (plain)
                {
                    // Plain text search
                    index = str.IndexOf(pattern, start - 1, StringComparison.Ordinal);
                }
                else
                {
                    // Convert Lua pattern to .NET regex (simplified)
                    var regexPattern = ConvertLuaPatternToRegex(pattern);
                    var regex = new Regex(regexPattern);
                    var match = regex.Match(str, start - 1);
                    
                    if (match.Success)
                    {
                        index = match.Index;
                        var results = new List<LuaValue>
                        {
                            new LuaInteger(index + 1), // Convert to 1-based
                            new LuaInteger(index + match.Length) // End position
                        };
                        
                        // Add captured groups
                        for (int i = 1; i < match.Groups.Count; i++)
                        {
                            results.Add(new LuaString(match.Groups[i].Value));
                        }
                        
                        return results.ToArray();
                    }
                    else
                    {
                        return new[] { LuaNil.Instance };
                    }
                }
                
                if (index >= 0)
                {
                    return new[]
                    {
                        new LuaInteger(index + 1), // Convert to 1-based
                        new LuaInteger(index + pattern.Length)
                    };
                }
            }
            catch (ArgumentException)
            {
                throw new LuaRuntimeException("invalid pattern");
            }
            
            return new[] { LuaNil.Instance };
        }
        
        private static LuaValue[] Match(LuaValue[] args)
        {
            if (args.Length < 2)
                throw new LuaRuntimeException("bad argument #2 to 'match' (string expected)");
            
            var str = args[0].AsString;
            var pattern = args[1].AsString;
            var start = args.Length > 2 ? (int)(args[2].AsInteger ?? 1) : 1;
            
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
                            results.Add(new LuaString(match.Groups[i].Value));
                        }
                        return results.ToArray();
                    }
                    else
                    {
                        // Return the entire match
                        return new[] { new LuaString(match.Value) };
                    }
                }
            }
            catch (ArgumentException)
            {
                throw new LuaRuntimeException("invalid pattern");
            }
            
            return new LuaValue[0];
        }
        
        private static LuaValue[] GSub(LuaValue[] args)
        {
            if (args.Length < 3)
                throw new LuaRuntimeException("bad argument #3 to 'gsub' (string/function/table expected)");
            
            var str = args[0].AsString;
            var pattern = args[1].AsString;
            var replacement = args[2];
            var limit = args.Length > 3 ? (int)(args[3].AsInteger ?? int.MaxValue) : int.MaxValue;
            
            try
            {
                var regexPattern = ConvertLuaPatternToRegex(pattern);
                var regex = new Regex(regexPattern);
                var matches = regex.Matches(str);
                
                if (matches.Count == 0)
                    return new LuaValue[] { new LuaString(str), new LuaInteger(0) };
                
                var result = str;
                var count = 0;
                
                // Simple string replacement (more complex function/table replacements would need additional logic)
                if (replacement is LuaString replStr)
                {
                    result = regex.Replace(str, replStr.Value, Math.Min(limit, matches.Count));
                    count = Math.Min(limit, matches.Count);
                }
                else if (replacement is LuaFunction || replacement is LuaTable)
                {
                    // For now, just do basic replacement - full implementation would call functions/lookup tables
                    result = regex.Replace(str, "", Math.Min(limit, matches.Count));
                    count = Math.Min(limit, matches.Count);
                }
                
                return new LuaValue[] { new LuaString(result), new LuaInteger(count) };
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
            
            var str = args[0].AsString;
            var pattern = args[1].AsString;
            
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
            var iterator = new LuaUserFunction(iterArgs =>
            {
                if (index < matches.Count)
                {
                    return new[] { new LuaString(matches[index++]) };
                }
                return new LuaValue[0];
            });
            
                            return new LuaValue[] { iterator };
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
            
            var format = args[0].AsString;
            var values = args.Skip(1).ToArray();
            
            try
            {
                // Simple printf-style formatting
                // This is a basic implementation - full printf compatibility would be more complex
                var result = format;
                var valueIndex = 0;
                
                // Replace %d, %s, %f etc. with actual values (including precision specifiers like %.2f)
                result = Regex.Replace(result, @"%(?:\.(\d+))?([diouxXeEfFgGaAcsp%])", match =>
                {
                    var precision = match.Groups[1].Success ? int.Parse(match.Groups[1].Value) : -1;
                    var specifier = match.Groups[2].Value;
                    
                    if (specifier == "%")
                        return "%";
                    
                    if (valueIndex >= values.Length)
                        throw new LuaRuntimeException("bad argument to 'format' (no value)");
                    
                    var value = values[valueIndex++];
                    
                    return specifier switch
                    {
                        "d" or "i" => value.AsInteger?.ToString() ?? "0",
                        "o" => Convert.ToString(value.AsInteger ?? 0, 8),
                        "u" => ((ulong)(value.AsInteger ?? 0)).ToString(),
                        "x" => Convert.ToString(value.AsInteger ?? 0, 16),
                        "X" => Convert.ToString(value.AsInteger ?? 0, 16).ToUpperInvariant(),
                        "f" or "F" => (value.AsNumber ?? 0).ToString($"F{(precision >= 0 ? precision : 6)}"),
                        "e" => (value.AsNumber ?? 0).ToString($"e{(precision >= 0 ? precision : 6)}"),
                        "E" => (value.AsNumber ?? 0).ToString($"E{(precision >= 0 ? precision : 6)}"),
                        "g" => (value.AsNumber ?? 0).ToString($"g{(precision >= 0 ? precision : 6)}"),
                        "G" => (value.AsNumber ?? 0).ToString($"G{(precision >= 0 ? precision : 6)}"),
                        "c" => new string((char)(value.AsInteger ?? 0), 1),
                        "s" => value.AsString ?? "",
                        "p" => $"0x{value.GetHashCode():x8}",
                        _ => value.ToString() ?? ""
                    };
                });
                
                return new[] { new LuaString(result) };
            }
            catch (Exception ex) when (!(ex is LuaRuntimeException))
            {
                throw new LuaRuntimeException("invalid format string");
            }
        }
        
        #endregion
    }
} 