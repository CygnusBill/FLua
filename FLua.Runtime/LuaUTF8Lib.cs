using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FLua.Runtime
{
    /// <summary>
    /// Lua UTF8 Library implementation
    /// </summary>
    public static class LuaUTF8Lib
    {
        /// <summary>
        /// UTF-8 character pattern for pattern matching
        /// </summary>
        public const string CharPattern = "[\0-\x7F\xC2-\xF4][\x80-\xBF]*";
        
        /// <summary>
        /// Adds the utf8 library to the Lua environment
        /// </summary>
        public static void AddUTF8Library(LuaEnvironment env)
        {
            var utf8Table = new LuaTable();
            
            // Core functions
            utf8Table.Set(LuaValue.String("len"), new BuiltinFunction(Len));
            utf8Table.Set(LuaValue.String("char"), new BuiltinFunction(Char));
            utf8Table.Set(LuaValue.String("codepoint"), new BuiltinFunction(CodePoint));
            utf8Table.Set(LuaValue.String("offset"), new BuiltinFunction(Offset));
            utf8Table.Set(LuaValue.String("codes"), new BuiltinFunction(Codes));
            
            // Pattern for character matching
            utf8Table.Set("charpattern", CharPattern);
            
            env.SetVariable("utf8", utf8Table);
        }
        
        #region Core Functions
        
        private static LuaValue[] Len(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'len' (string expected)");
            
            var str = args[0].AsString();
            var start = args.Length > 1 && args[1].IsInteger ? (int)args[1].AsInteger() : 1;
            var end = args.Length > 2 && args[2].IsInteger ? (int)args[2].AsInteger() : str.Length;
            var lax = args.Length > 3 ? args[3].IsTruthy() : false;
            
            // Convert to 0-based indexing
            start = Math.Max(0, start - 1);
            end = Math.Min(str.Length, end);
            
            if (start >= end)
                return [LuaValue.Integer(0)];
            
            try
            {
                var substring = str.Substring(start, end - start);
                var info = new StringInfo(substring);
                var length = info.LengthInTextElements;
                
                // Validate UTF-8 if not in lax mode
                if (!lax && !IsValidUTF8(substring))
                {
                    // Find the position of the first invalid byte
                    var bytes = Encoding.UTF8.GetBytes(substring);
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        if (!IsValidUTF8ByteSequence(bytes, i))
                        {
                            return [LuaValue.Nil, LuaValue.Integer(start + i + 1)];
                        }
                    }
                }
                
                return [LuaValue.Integer(length)];
            }
            catch (ArgumentException)
            {
                // Invalid UTF-8 sequence
                return [LuaValue.Nil, LuaValue.Integer(start + 1)];
            }
        }
        
        private static LuaValue[] Char(LuaValue[] args)
        {
            var codepoints = new List<int>();
            
            for (int i = 0; i < args.Length; i++)
            {
                if (!args[i].IsInteger)
                    throw new LuaRuntimeException($"bad argument #{i + 1} to 'char' (number expected)");
                
                var codepoint = (int)args[i].AsInteger();
                if (codepoint < 0 || codepoint > 0x10FFFF)
                    throw new LuaRuntimeException($"bad argument #{i + 1} to 'char' (value out of range)");
                
                codepoints.Add(codepoint);
            }
            
            try
            {
                var result = new StringBuilder();
                foreach (var codepoint in codepoints)
                {
                    result.Append(char.ConvertFromUtf32(codepoint));
                }
                
                return [LuaValue.String(result.ToString())];
            }
            catch (ArgumentException ex)
            {
                throw new LuaRuntimeException($"invalid code point: {ex.Message}");
            }
        }
        
        private static LuaValue[] CodePoint(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'codepoint' (string expected)");
            
            var str = args[0].AsString();
            var start = args.Length > 1 && args[1].IsInteger ? (int)args[1].AsInteger() : 1;
            var end = args.Length > 2 && args[2].IsInteger ? (int)args[2].AsInteger() : start;
            var lax = args.Length > 3 ? args[3].IsTruthy() : false;
            
            // Convert to 0-based indexing
            start = Math.Max(0, start - 1);
            end = Math.Min(str.Length, end);
            
            if (start >= str.Length)
                return [];
            
            try
            {
                var results = new List<LuaValue>();
                var info = new StringInfo(str);
                var textElements = StringInfo.GetTextElementEnumerator(str);
                
                int position = 0;
                int elementIndex = 0;
                
                while (textElements.MoveNext())
                {
                    if (position >= start && position < end)
                    {
                        var element = textElements.GetTextElement();
                        var codepoint = char.ConvertToUtf32(element, 0);
                        results.Add(LuaValue.Integer(codepoint));
                        
                        if (position >= end - 1)
                            break;
                    }
                    
                    position++;
                    elementIndex++;
                }
                
                return results.ToArray();
            }
            catch (ArgumentException)
            {
                if (lax)
                {
                    // In lax mode, return individual bytes as codepoints
                    var results = new List<LuaValue>();
                    for (int i = start; i < end && i < str.Length; i++)
                    {
                        results.Add(LuaValue.Integer((byte)str[i]));
                    }
                    return results.ToArray();
                }
                else
                {
                    throw new LuaRuntimeException("invalid UTF-8 code");
                }
            }
        }
        
        private static LuaValue[] Offset(LuaValue[] args)
        {
            if (args.Length < 2)
                throw new LuaRuntimeException("bad argument #2 to 'offset' (number expected)");
            
            var str = args[0].AsString();
            var n = args[1];
            var i = args.Length > 2 && args[2].IsInteger ? (int)args[2].AsInteger() : 1;
            
            if (!n.IsInteger)
                throw new LuaRuntimeException("bad argument #2 to 'offset' (number expected)");
            
            var offset = (int)n.AsInteger();
            
            // Convert to 0-based indexing
            i = Math.Max(0, i - 1);
            
            if (i >= str.Length)
                return [];
            
            try
            {
                if (offset == 0)
                {
                    // Find the start of the character at position i
                    var charStart = FindCharacterStart(str, i);
                    var charEnd = FindCharacterEnd(str, charStart);
                    return [LuaValue.Integer(charStart + 1), LuaValue.Integer(charEnd)];
                }
                else
                {
                    var substr = str.Substring(i);
                    var textElements = StringInfo.GetTextElementEnumerator(substr);
                    
                    int currentPos = i;
                    int count = 0;
                    
                    if (offset > 0)
                    {
                        // Move forward
                        while (textElements.MoveNext() && count < offset)
                        {
                            currentPos += textElements.GetTextElement().Length;
                            count++;
                        }
                    }
                    else
                    {
                        // Move backward
                        var allElements = new List<string>();
                        while (textElements.MoveNext())
                        {
                            allElements.Add(textElements.GetTextElement());
                        }
                        
                        for (int j = allElements.Count - 1; j >= 0 && count > offset; j--)
                        {
                            currentPos -= allElements[j].Length;
                            count--;
                        }
                    }
                    
                    if (currentPos < 0 || currentPos > str.Length)
                        return [];
                    
                    return [LuaValue.Integer(currentPos + 1)];
                }
            }
            catch (ArgumentException)
            {
                throw new LuaRuntimeException("invalid UTF-8 code");
            }
        }
        
        private static LuaValue[] Codes(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'codes' (string expected)");
            
            var str = args[0].AsString();
            var lax = args.Length > 1 ? args[1].IsTruthy() : false;
            
            try
            {
                var codepoints = new List<(int position, int codepoint)>();
                var info = new StringInfo(str);
                var textElements = StringInfo.GetTextElementEnumerator(str);
                
                int position = 1; // Lua uses 1-based indexing
                while (textElements.MoveNext())
                {
                    var element = textElements.GetTextElement();
                    var codepoint = char.ConvertToUtf32(element, 0);
                    codepoints.Add((position, codepoint));
                    position++;
                }
                
                // Return an iterator function
                var index = 0;
                var iterator = new BuiltinFunction(iterArgs =>
                {
                    if (index < codepoints.Count)
                    {
                        var (pos, cp) = codepoints[index++];
                        return [LuaValue.Integer(pos), LuaValue.Integer(cp)];
                    }
                    return [];
                });
                
                return [LuaValue.Function(iterator)];
            }
            catch (ArgumentException)
            {
                if (lax)
                {
                    // In lax mode, iterate over bytes
                    var bytes = Encoding.UTF8.GetBytes(str);
                    var index = 0;
                    var iterator = new BuiltinFunction(iterArgs =>
                    {
                        if (index < bytes.Length)
                        {
                            return [LuaValue.Integer(index + 1), LuaValue.Integer(bytes[index++])];
                        }
                        return [];
                    });
                    
                    return [LuaValue.Function(iterator)];
                }
                else
                {
                    throw new LuaRuntimeException("invalid UTF-8 code");
                }
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Checks if a string contains valid UTF-8
        /// </summary>
        private static bool IsValidUTF8(string str)
        {
            try
            {
                var bytes = Encoding.UTF8.GetBytes(str);
                var decoded = Encoding.UTF8.GetString(bytes);
                return decoded == str;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Checks if a byte sequence at a given position is valid UTF-8
        /// </summary>
        private static bool IsValidUTF8ByteSequence(byte[] bytes, int index)
        {
            if (index >= bytes.Length)
                return false;
            
            var b = bytes[index];
            
            // Single byte (ASCII)
            if (b <= 0x7F)
                return true;
            
            // Multi-byte sequence
            int expectedLength;
            if ((b & 0xE0) == 0xC0)
                expectedLength = 2;
            else if ((b & 0xF0) == 0xE0)
                expectedLength = 3;
            else if ((b & 0xF8) == 0xF0)
                expectedLength = 4;
            else
                return false; // Invalid start byte
            
            // Check if we have enough bytes
            if (index + expectedLength > bytes.Length)
                return false;
            
            // Check continuation bytes
            for (int i = 1; i < expectedLength; i++)
            {
                if ((bytes[index + i] & 0xC0) != 0x80)
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Finds the start of a UTF-8 character at or before the given position
        /// </summary>
        private static int FindCharacterStart(string str, int position)
        {
            if (position <= 0)
                return 0;
            
            if (position >= str.Length)
                position = str.Length - 1;
            
            // Move backward until we find a character start
            while (position > 0)
            {
                var b = (byte)str[position];
                if ((b & 0xC0) != 0x80) // Not a continuation byte
                    break;
                position--;
            }
            
            return position;
        }
        
        /// <summary>
        /// Finds the end of a UTF-8 character starting at the given position
        /// </summary>
        private static int FindCharacterEnd(string str, int position)
        {
            if (position >= str.Length)
                return str.Length;
            
            var b = (byte)str[position];
            
            // Single byte character
            if (b <= 0x7F)
                return position + 1;
            
            // Multi-byte character
            int length = 1;
            if ((b & 0xE0) == 0xC0)
                length = 2;
            else if ((b & 0xF0) == 0xE0)
                length = 3;
            else if ((b & 0xF8) == 0xF0)
                length = 4;
            
            return Math.Min(position + length, str.Length);
        }
        
        #endregion
    }
} 