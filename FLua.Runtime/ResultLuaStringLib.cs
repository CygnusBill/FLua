using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FLua.Common;

namespace FLua.Runtime
{
    /// <summary>
    /// Result-based version of Lua String Library implementation
    /// Converts all exception-based error handling to Result pattern
    /// </summary>
    public static class ResultLuaStringLib
    {
        #region Basic String Functions
        
        public static Result<LuaValue[]> LenResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'len' (string expected)");
            
            var str = args[0];
            if (str.IsString)
                return Result<LuaValue[]>.Success([LuaValue.Integer(str.AsString().Length)]);
            
            if (str.IsNumber)
                return Result<LuaValue[]>.Success([LuaValue.Integer(str.ToString().Length)]);
            
            return Result<LuaValue[]>.Failure("bad argument #1 to 'len' (string expected)");
        }
        
        public static Result<LuaValue[]> SubResult(LuaValue[] args)
        {
            if (args.Length < 2)
                return Result<LuaValue[]>.Failure("bad argument #2 to 'sub' (number expected)");
            
            var str = args[0];
            var start = args[1];
            LuaValue end = LuaValue.Nil;
            if(args.Length > 2)
            {
                end = args[2];
            }
            
            var stringValue = str.AsString();
            if (!start.IsInteger)
                return Result<LuaValue[]>.Failure("bad argument #2 to 'sub' (number expected)");
            
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
                return Result<LuaValue[]>.Success([LuaValue.String("")]);
            
            var length = (int)(endIndex - startIndex + 1);
            if (length <= 0)
                return Result<LuaValue[]>.Success([LuaValue.String("")]);
            
            try
            {
                var result = stringValue.Substring(startIndex - 1, Math.Min(length, stringValue.Length - startIndex + 1));
                return Result<LuaValue[]>.Success([LuaValue.String(result)]);
            }
            catch
            {
                return Result<LuaValue[]>.Success([LuaValue.String("")]);
            }
        }
        
        public static Result<LuaValue[]> UpperResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'upper' (string expected)");
            
            var str = args[0];
            return Result<LuaValue[]>.Success([LuaValue.String(str.AsString().ToUpperInvariant())]);
        }
        
        public static Result<LuaValue[]> LowerResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'lower' (string expected)");
            
            var str = args[0];
            return Result<LuaValue[]>.Success([LuaValue.String(str.AsString().ToLowerInvariant())]);
        }
        
        public static Result<LuaValue[]> ReverseResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'reverse' (string expected)");
            
            var str = args[0];
            var chars = str.AsString().ToCharArray();
            Array.Reverse(chars);
            return Result<LuaValue[]>.Success([LuaValue.String(new string(chars))]);
        }
        
        #endregion
        
        #region Character Functions
        
        public static Result<LuaValue[]> CharResult(LuaValue[] args)
        {
            var chars = new char[args.Length];
            
            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].IsInteger)
                    return Result<LuaValue[]>.Failure($"bad argument #{i + 1} to 'char' (number expected)");
                
                var value = args[i].AsInteger();
                if (value < 0 || value > 255)
                    return Result<LuaValue[]>.Failure($"bad argument #{i + 1} to 'char' (out of range)");
                
                chars[i] = (char)value;
            }
            
            return Result<LuaValue[]>.Success([LuaValue.String(new string(chars))]);
        }
        
        public static Result<LuaValue[]> ByteResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'byte' (string expected)");
            
            var str = args[0].AsString();
            var start = args.Length > 1 && args[1].IsInteger ? args[1].AsInteger() : 1;
            var end = args.Length > 2 && args[2].IsInteger ? args[2].AsInteger() : start;
            
            // Handle negative indices
            if (start < 0)
                start = str.Length + start + 1;
            if (end < 0)
                end = str.Length + end + 1;
            
            // Check if start position is out of bounds - return empty if so
            if (start > str.Length || start < 1)
                return Result<LuaValue[]>.Success([]);
            
            // Clamp end position but don't clamp start (we already checked bounds)
            end = Math.Max(start, Math.Min(end, str.Length));
            
            var results = new List<LuaValue>();
            for (long i = start; i <= end && i <= str.Length; i++)
            {
                results.Add(LuaValue.Integer((byte)str[(int)i - 1]));
            }
            
            return Result<LuaValue[]>.Success(results.ToArray());
        }
        
        #endregion
        
        #region Repetition Functions
        
        public static Result<LuaValue[]> RepResult(LuaValue[] args)
        {
            if (args.Length < 2)
                return Result<LuaValue[]>.Failure("bad argument #2 to 'rep' (number expected)");
            
            var str = args[0].AsString();
            var count = args[1];
            var separator = args.Length > 2 ? args[2].AsString() : "";
            
            if (!count.IsInteger)
                return Result<LuaValue[]>.Failure("bad argument #2 to 'rep' (number expected)");
            
            var n = (int)count.AsInteger();
            if (n <= 0)
                return Result<LuaValue[]>.Success([LuaValue.String("")]);
            
            if (n == 1)
                return Result<LuaValue[]>.Success([LuaValue.String(str)]);
            
            try
            {
                if (string.IsNullOrEmpty(separator))
                {
                    var sb = new StringBuilder(str.Length * n);
                    for (int i = 0; i < n; i++)
                    {
                        sb.Append(str);
                    }
                    return Result<LuaValue[]>.Success([LuaValue.String(sb.ToString())]);
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
                    return Result<LuaValue[]>.Success([LuaValue.String(sb.ToString())]);
                }
            }
            catch (OutOfMemoryException)
            {
                return Result<LuaValue[]>.Failure("resulting string too large");
            }
        }
        
        #endregion
        
        #region Pattern Matching Functions (Simplified)
        
        public static Result<LuaValue[]> FindResult(LuaValue[] args)
        {
            if (args.Length < 2)
                return Result<LuaValue[]>.Failure("bad argument #2 to 'find' (string expected)");
            
            var str = args[0].AsString();
            var pattern = args[1].AsString();
            var start = args.Length > 2 && args[2].IsInteger ? (int)args[2].AsInteger() : 1;
            var plain = args.Length > 3 ? args[3].IsTruthy() : false;
            
            if (start < 0)
                start = str.Length + start + 1;
            start = Math.Max(1, Math.Min(start, str.Length + 1));
            
            if (start > str.Length)
                return Result<LuaValue[]>.Success([LuaValue.Nil]);
            
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
                    
                    return Result<LuaValue[]>.Success(results.ToArray());
                }
                
                return Result<LuaValue[]>.Success([LuaValue.Nil]);
            }
            catch (Exception ex) when (ex.Message.Contains("invalid pattern"))
            {
                return Result<LuaValue[]>.Failure("invalid pattern");
            }
        }
        
        public static Result<LuaValue[]> MatchResult(LuaValue[] args)
        {
            if (args.Length < 2)
                return Result<LuaValue[]>.Failure("bad argument #2 to 'match' (string expected)");
            
            var str = args[0].AsString();
            var pattern = args[1].AsString();
            var start = args.Length > 2 && args[2].IsInteger ? (int)args[2].AsInteger() : 1;
            
            if (start < 0)
                start = str.Length + start + 1;
            start = Math.Max(1, Math.Min(start, str.Length + 1));
            
            if (start > str.Length)
                return Result<LuaValue[]>.Success([LuaValue.Nil]);
            
            try
            {
                var match = LuaPatterns.Find(str, pattern, start, false);
                
                if (match != null && match.Captures.Count > 0)
                {
                    var results = new List<LuaValue>();
                    foreach (var capture in match.Captures)
                    {
                        results.Add(LuaValue.String(capture));
                    }
                    return Result<LuaValue[]>.Success(results.ToArray());
                }
                else if (match != null)
                {
                    // No captures, return the whole match
                    return Result<LuaValue[]>.Success([LuaValue.String(match.Value)]);
                }
                
                return Result<LuaValue[]>.Success([LuaValue.Nil]);
            }
            catch (Exception ex) when (ex.Message.Contains("invalid pattern"))
            {
                return Result<LuaValue[]>.Failure("invalid pattern");
            }
        }
        
        public static Result<LuaValue[]> GSubResult(LuaValue[] args)
        {
            if (args.Length < 3)
                return Result<LuaValue[]>.Failure("bad argument #3 to 'gsub' (string/function/table expected)");
            
            var str = args[0].AsString();
            var pattern = args[1].AsString();
            var replacement = args[2];
            var limit = args.Length > 3 && args[3].IsInteger ? (int)args[3].AsInteger() : int.MaxValue;
            
            if (!replacement.IsString && !replacement.IsFunction && !replacement.IsTable)
                return Result<LuaValue[]>.Failure("bad argument #3 to 'gsub' (string/function/table expected)");
            
            try
            {
                var (result, count) = LuaPatterns.GSub(str, pattern, replacement, limit);
                return Result<LuaValue[]>.Success([LuaValue.String(result), LuaValue.Integer(count)]);
            }
            catch (Exception ex) when (ex.Message.Contains("invalid pattern"))
            {
                return Result<LuaValue[]>.Failure("invalid pattern");
            }
        }
        
        public static Result<LuaValue[]> GMatchResult(LuaValue[] args)
        {
            if (args.Length < 2)
                return Result<LuaValue[]>.Failure("bad argument #2 to 'gmatch' (string expected)");
            
            var str = args[0].AsString();
            var pattern = args[1].AsString();
            
            try
            {
                var iterator = LuaPatterns.GMatch(str, pattern);
                return Result<LuaValue[]>.Success([iterator]);
            }
            catch (Exception ex) when (ex.Message.Contains("invalid pattern"))
            {
                return Result<LuaValue[]>.Failure("invalid pattern");
            }
        }
        
        #endregion
        
        #region Format Functions
        
        public static Result<LuaValue[]> FormatResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'format' (string expected)");
            
            var format = args[0].AsString();
            var values = args.Skip(1).ToArray();
            var valueIndex = 0;
            
            try
            {
                var result = ProcessFormat(format, values, ref valueIndex);
                return result.Map(s => new LuaValue[] { LuaValue.String(s) });
            }
            catch (FormatException ex)
            {
                return Result<LuaValue[]>.Failure($"invalid format string: {ex.Message}");
            }
        }
        
        private static Result<string> ProcessFormat(string format, LuaValue[] values, ref int valueIndex)
        {
            var result = new StringBuilder();
            var i = 0;
            
            while (i < format.Length)
            {
                if (format[i] == '%')
                {
                    if (i + 1 < format.Length && format[i + 1] == '%')
                    {
                        result.Append('%');
                        i += 2;
                        continue;
                    }
                    
                    if (valueIndex >= values.Length)
                        return Result<string>.Failure($"bad argument #{valueIndex + 2} to 'format' (no value)");
                    
                    var formatResult = ProcessFormatSpecifier(format, ref i, values[valueIndex]);
                    if (!formatResult.IsSuccess)
                        return formatResult;
                    
                    result.Append(formatResult.Value);
                    valueIndex++;
                }
                else
                {
                    result.Append(format[i]);
                    i++;
                }
            }
            
            return Result<string>.Success(result.ToString());
        }
        
        private static Result<string> ProcessFormatSpecifier(string format, ref int i, LuaValue value)
        {
            // Simplified format processing - this would need full implementation
            // for production use
            i++; // Skip '%'
            
            if (i >= format.Length)
                return Result<string>.Failure("incomplete format specifier");
            
            var spec = format[i];
            i++; // Move past specifier
            
            return spec switch
            {
                'd' or 'i' => FormatInteger(value),
                'f' => FormatFloat(value),
                's' => FormatString(value),
                'c' => FormatChar(value),
                _ => Result<string>.Failure($"invalid format option '{spec}'")
            };
        }
        
        private static Result<string> FormatInteger(LuaValue value)
        {
            if (value.IsInteger)
                return Result<string>.Success(value.AsInteger().ToString());
            if (value.IsNumber)
                return Result<string>.Success(((long)value.AsDouble()).ToString());
            return Result<string>.Failure("number expected");
        }
        
        private static Result<string> FormatFloat(LuaValue value)
        {
            if (value.IsNumber)
                return Result<string>.Success(value.AsDouble().ToString("F6"));
            return Result<string>.Failure("number expected");
        }
        
        private static Result<string> FormatString(LuaValue value)
        {
            return Result<string>.Success(value.AsString());
        }
        
        private static Result<string> FormatChar(LuaValue value)
        {
            if (!value.IsInteger)
                return Result<string>.Failure("number expected");
            
            var charValue = value.AsInteger();
            if (charValue < 0 || charValue > 255)
                return Result<string>.Failure("bad argument to format (char out of range)");
            
            return Result<string>.Success(((char)charValue).ToString());
        }
        
        #endregion
        
        #region Binary Packing Functions
        
        public static Result<LuaValue[]> PackResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'pack' (string expected)");
            
            var format = args[0].AsString();
            var values = args.Skip(1).ToArray();
            var valueIndex = 0;
            
            try
            {
                var result = ProcessPack(format, values, ref valueIndex);
                return result.Map(bytes => new LuaValue[] { LuaValue.String(Convert.ToBase64String(bytes)) });
            }
            catch (Exception ex)
            {
                return Result<LuaValue[]>.Failure($"invalid format string: {ex.Message}");
            }
        }
        
        public static Result<LuaValue[]> UnpackResult(LuaValue[] args)
        {
            if (args.Length < 2)
                return Result<LuaValue[]>.Failure("bad argument #2 to 'unpack' (string expected)");
            
            var format = args[0].AsString();
            var data = args[1].AsString();
            var offset = args.Length > 2 && args[2].IsInteger ? (int)args[2].AsInteger() - 1 : 0;
            
            try
            {
                var bytes = Convert.FromBase64String(data);
                var result = ProcessUnpack(format, bytes, offset);
                return result;
            }
            catch (Exception ex)
            {
                return Result<LuaValue[]>.Failure($"invalid format string: {ex.Message}");
            }
        }
        
        public static Result<LuaValue[]> PackSizeResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'packsize' (string expected)");
            
            var format = args[0].AsString();
            
            try
            {
                var size = CalculatePackSize(format);
                return size.Map(s => new LuaValue[] { LuaValue.Integer(s) });
            }
            catch (Exception ex)
            {
                return Result<LuaValue[]>.Failure($"invalid format string: {ex.Message}");
            }
        }
        
        private static Result<byte[]> ProcessPack(string format, LuaValue[] values, ref int valueIndex)
        {
            var result = new List<byte>();
            var i = 0;
            
            while (i < format.Length)
            {
                var c = format[i];
                i++;
                
                if (valueIndex >= values.Length)
                    return Result<byte[]>.Failure($"bad argument #{valueIndex + 2} to 'pack' (no value)");
                
                // Simplified pack processing
                switch (c)
                {
                    case 'b':
                        if (!values[valueIndex].IsInteger)
                            return Result<byte[]>.Failure("number expected");
                        result.Add((byte)values[valueIndex].AsInteger());
                        valueIndex++;
                        break;
                        
                    default:
                        return Result<byte[]>.Failure($"invalid format option '{c}'");
                }
            }
            
            return Result<byte[]>.Success(result.ToArray());
        }
        
        private static Result<LuaValue[]> ProcessUnpack(string format, byte[] data, int offset)
        {
            var results = new List<LuaValue>();
            var i = 0;
            var dataIndex = offset;
            
            while (i < format.Length && dataIndex < data.Length)
            {
                var c = format[i];
                i++;
                
                switch (c)
                {
                    case 'b':
                        if (dataIndex >= data.Length)
                            return Result<LuaValue[]>.Failure("data string too short");
                        results.Add(LuaValue.Integer(data[dataIndex]));
                        dataIndex++;
                        break;
                        
                    default:
                        return Result<LuaValue[]>.Failure($"invalid format option '{c}'");
                }
            }
            
            return Result<LuaValue[]>.Success(results.ToArray());
        }
        
        private static Result<int> CalculatePackSize(string format)
        {
            var size = 0;
            var i = 0;
            
            while (i < format.Length)
            {
                var c = format[i];
                i++;
                
                switch (c)
                {
                    case 'b':
                        size += 1;
                        break;
                        
                    case 's':
                        return Result<int>.Failure("variable-length format");
                        
                    default:
                        return Result<int>.Failure($"invalid format option '{c}'");
                }
            }
            
            return Result<int>.Success(size);
        }
        
        #endregion
    }
}