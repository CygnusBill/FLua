using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Runtime;
using System;
using System.Linq;

namespace FLua.Runtime.Tests
{
    [TestClass]
    public class LuaStringLibTests
    {
        private LuaEnvironment _env = null!;

        [TestInitialize]
        public void Setup()
        {
            _env = new LuaEnvironment();
            LuaStringLib.AddStringLibrary(_env);
        }

        #region Length Function Tests

        // Equivalence Class Testing: Testing different string types
        [TestMethod]
        public void StringLen_RegularString_ShouldReturnCorrectLength()
        {
            var stringTable = _env.GetVariable("string");
            var lenFunc = stringTable.AsTable<LuaTable>().Get("len");
            var result = lenFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { "hello" });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(5, result[0]);
        }

        [TestMethod]
        public void StringLen_EmptyString_ShouldReturnZero()
        {
            var stringTable = _env.GetVariable("string");
            var lenFunc = stringTable.AsTable<LuaTable>().Get("len");
            var result = lenFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { "" });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(0, result[0]);
        }

        // Boundary Value Testing: Testing with very long string
        [TestMethod]
        public void StringLen_LongString_ShouldReturnCorrectLength()
        {
            var longString = new string('a', 10000);
            var stringTable = _env.GetVariable("string");
            var lenFunc = stringTable.AsTable<LuaTable>().Get("len");
            var result = lenFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { longString });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(10000, result[0]);
        }

        // Domain Testing: Testing with number inputs
        [TestMethod]
        public void StringLen_NumberInput_ShouldReturnStringLength()
        {
            var stringTable = _env.GetVariable("string");
            var lenFunc = stringTable.AsTable<LuaTable>().Get("len");
            var result = lenFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { 12345 });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(5, result[0]); // "12345" has 5 characters
        }

        #endregion

        #region Sub Function Tests

        // Equivalence Class Testing: Testing different substring scenarios
        [TestMethod]
        public void StringSub_PositiveIndices_ShouldReturnCorrectSubstring()
        {
            var stringTable = _env.GetVariable("string");
            var subFunc = stringTable.AsTable<LuaTable>().Get("sub");
            var result = subFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { 
                "hello world", 
                7, 
                11 
            });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("world", result[0]);
        }

        [TestMethod]
        public void StringSub_NegativeIndices_ShouldReturnCorrectSubstring()
        {
            var stringTable = _env.GetVariable("string");
            var subFunc = stringTable.AsTable<LuaTable>().Get("sub");
            var result = subFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { 
                "hello world", 
                -5, 
                -1 
            });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("world", result[0]);
        }

        // Boundary Value Testing: Testing edge cases
        [TestMethod]
        public void StringSub_StartIndexOnly_ShouldReturnRestOfString()
        {
            var stringTable = _env.GetVariable("string");
            var subFunc = stringTable.AsTable<LuaTable>().Get("sub");
            var result = subFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { 
                "hello world", 
                7
            });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("world", result[0]);
        }

        [TestMethod]
        public void StringSub_OutOfBounds_ShouldReturnEmptyString()
        {
            var stringTable = _env.GetVariable("string");
            var subFunc = stringTable.AsTable<LuaTable>().Get("sub");
            var result = subFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { 
                "hello", 
                10, 
                15 
            });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("", result[0]);
        }

        #endregion

        #region Upper/Lower Function Tests

        // Equivalence Class Testing: Testing case conversion
        [TestMethod]
        public void StringUpper_MixedCase_ShouldReturnUppercase()
        {
            var stringTable = _env.GetVariable("string");
            var upperFunc = stringTable.AsTable<LuaTable>().Get("upper");
            var result = upperFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { "Hello World" });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("HELLO WORLD", result[0]);
        }

        [TestMethod]
        public void StringLower_MixedCase_ShouldReturnLowercase()
        {
            var stringTable = _env.GetVariable("string");
            var lowerFunc = stringTable.AsTable<LuaTable>().Get("lower");
            var result = lowerFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { "Hello World" });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("hello world", result[0]);
        }

        // Domain Testing: Testing with special characters
        [TestMethod]
        public void StringUpper_WithNumbers_ShouldKeepNumbersUnchanged()
        {
            var stringTable = _env.GetVariable("string");
            var upperFunc = stringTable.AsTable<LuaTable>().Get("upper");
            var result = upperFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { "abc123def" });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("ABC123DEF", result[0]);
        }

        [TestMethod]
        public void StringLower_EmptyString_ShouldReturnEmptyString()
        {
            var stringTable = _env.GetVariable("string");
            var lowerFunc = stringTable.AsTable<LuaTable>().Get("lower");
            var result = lowerFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { "" });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("", result[0]);
        }

        #endregion

        #region Reverse Function Tests

        // Equivalence Class Testing: Testing string reversal
        [TestMethod]
        public void StringReverse_RegularString_ShouldReturnReversed()
        {
            var stringTable = _env.GetVariable("string");
            var reverseFunc = stringTable.AsTable<LuaTable>().Get("reverse");
            var result = reverseFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { "hello" });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("olleh", result[0]);
        }

        [TestMethod]
        public void StringReverse_Palindrome_ShouldReturnSameString()
        {
            var stringTable = _env.GetVariable("string");
            var reverseFunc = stringTable.AsTable<LuaTable>().Get("reverse");
            var result = reverseFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { "racecar" });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("racecar", result[0]);
        }

        // Boundary Value Testing: Testing edge cases
        [TestMethod]
        public void StringReverse_SingleCharacter_ShouldReturnSameCharacter()
        {
            var stringTable = _env.GetVariable("string");
            var reverseFunc = stringTable.AsTable<LuaTable>().Get("reverse");
            var result = reverseFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { "a" });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("a", result[0]);
        }

        [TestMethod]
        public void StringReverse_EmptyString_ShouldReturnEmptyString()
        {
            var stringTable = _env.GetVariable("string");
            var reverseFunc = stringTable.AsTable<LuaTable>().Get("reverse");
            var result = reverseFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { "" });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("", result[0]);
        }

        #endregion

        #region Repeat Function Tests

        // Equivalence Class Testing: Testing string repetition
        [TestMethod]
        public void StringRep_PositiveCount_ShouldRepeatString()
        {
            var stringTable = _env.GetVariable("string");
            var repFunc = stringTable.AsTable<LuaTable>().Get("rep");
            var result = repFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { "ab", 3 });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("ababab", result[0]);
        }

        // Boundary Value Testing: Testing edge cases
        [TestMethod]
        public void StringRep_ZeroCount_ShouldReturnEmptyString()
        {
            var stringTable = _env.GetVariable("string");
            var repFunc = stringTable.AsTable<LuaTable>().Get("rep");
            var result = repFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { "hello", 0 });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("", result[0]);
        }

        [TestMethod]
        public void StringRep_OneCount_ShouldReturnOriginalString()
        {
            var stringTable = _env.GetVariable("string");
            var repFunc = stringTable.AsTable<LuaTable>().Get("rep");
            var result = repFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { "hello", 1 });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("hello", result[0]);
        }

        // Risk-Based Testing: Testing with large repeat count
        [TestMethod]
        public void StringRep_LargeCount_ShouldHandleCorrectly()
        {
            var stringTable = _env.GetVariable("string");
            var repFunc = stringTable.AsTable<LuaTable>().Get("rep");
            var result = repFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { "x", 1000 });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(1000, result[0].AsString().Length);
            Assert.IsTrue(result[0].AsString().All(c => c == 'x'));
        }

        #endregion

        #region Char/Byte Function Tests

        // Decision Table Testing: Testing character code conversions
        [TestMethod]
        public void StringChar_SingleCode_ShouldReturnCharacter()
        {
            var stringTable = _env.GetVariable("string");
            var charFunc = stringTable.AsTable<LuaTable>().Get("char");
            var result = charFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { 65 }); // ASCII for 'A'
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("A", result[0]);
        }

        [TestMethod]
        public void StringChar_MultipleCodes_ShouldReturnString()
        {
            var stringTable = _env.GetVariable("string");
            var charFunc = stringTable.AsTable<LuaTable>().Get("char");
            var result = charFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { 
                72,  // 'H'
                105, // 'i'
                33   // '!'
            });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("Hi!", result[0]);
        }

        [TestMethod]
        public void StringByte_SingleCharacter_ShouldReturnCode()
        {
            var stringTable = _env.GetVariable("string");
            var byteFunc = stringTable.AsTable<LuaTable>().Get("byte");
            var result = byteFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { "A" });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(65, result[0]);
        }

        [TestMethod]
        public void StringByte_MultipleCharacters_ShouldReturnMultipleCodes()
        {
            var stringTable = _env.GetVariable("string");
            var byteFunc = stringTable.AsTable<LuaTable>().Get("byte");
            var result = byteFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { 
                "Hi!", 
                1, 
                3 
            });
            
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual(72, result[0]);  // 'H'
            Assert.AreEqual(105, result[1]); // 'i'
            Assert.AreEqual(33, result[2]);  // '!'
        }

        // Boundary Value Testing: Testing with boundary ASCII values
        [TestMethod]
        public void StringChar_NullCharacter_ShouldReturnNullChar()
        {
            var stringTable = _env.GetVariable("string");
            var charFunc = stringTable.AsTable<LuaTable>().Get("char");
            var result = charFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { 0 });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("\0", result[0]);
        }

        [TestMethod]
        public void StringChar_MaxASCII_ShouldReturnCharacter()
        {
            var stringTable = _env.GetVariable("string");
            var charFunc = stringTable.AsTable<LuaTable>().Get("char");
            var result = charFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { 255 });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(((char)255).ToString(), result[0]);
        }

        #endregion

        #region Format Function Tests

        // Decision Table Testing: Testing various format specifiers
        [TestMethod]
        public void StringFormat_IntegerSpecifier_ShouldFormatCorrectly()
        {
            var stringTable = _env.GetVariable("string");
            var formatFunc = stringTable.AsTable<LuaTable>().Get("format");
            var result = formatFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { 
                "Number: %d", 
                42 
            });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("Number: 42", result[0]);
        }

        [TestMethod]
        public void StringFormat_FloatSpecifier_ShouldFormatCorrectly()
        {
            var stringTable = _env.GetVariable("string");
            var formatFunc = stringTable.AsTable<LuaTable>().Get("format");
            var result = formatFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { 
                "Value: %.2f", 
                3.14159 
            });
            
            Assert.AreEqual(1, result.Length);
            string resultStr = result[0].AsString();
            Assert.IsTrue(resultStr.StartsWith("Value: ") && resultStr.Contains("3.14"), 
                $"Expected format 'Value: 3.14*' but got '{resultStr}'");
        }

        [TestMethod]
        public void StringFormat_StringSpecifier_ShouldFormatCorrectly()
        {
            var stringTable = _env.GetVariable("string");
            var formatFunc = stringTable.AsTable<LuaTable>().Get("format");
            var result = formatFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { 
                "Hello, %s!", 
                "World" 
            });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("Hello, World!", result[0]);
        }

        // Scenario Testing: Testing complex format strings
        [TestMethod]
        public void StringFormat_MultipleSpecifiers_ShouldFormatCorrectly()
        {
            var stringTable = _env.GetVariable("string");
            var formatFunc = stringTable.AsTable<LuaTable>().Get("format");
            var result = formatFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { 
                "%s scored %d points with %.1f%% accuracy", 
                "Alice",
                95,
                87.5
            });
            
            Assert.AreEqual(1, result.Length);
            var formatted = result[0].AsString();
            Assert.IsTrue(formatted.Contains("Alice"), $"Result should contain 'Alice': {formatted}");
            Assert.IsTrue(formatted.Contains("95"), $"Result should contain '95': {formatted}");
            Assert.IsTrue(formatted.Contains("87.5") || formatted.Contains("87,5"), $"Result should contain '87.5' or '87,5': {formatted}");
            Assert.IsTrue(formatted.Contains("points"), $"Result should contain 'points': {formatted}");
        }

        #endregion

        #region Find Function Tests

        // Equivalence Class Testing: Testing string search
        [TestMethod]
        public void StringFind_PatternExists_ShouldReturnPosition()
        {
            var stringTable = _env.GetVariable("string");
            var findFunc = stringTable.AsTable<LuaTable>().Get("find");
            var result = findFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { 
                "hello world", 
                "world" 
            });
            
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(7, result[0]); // 1-indexed position
            Assert.AreEqual(11, result[1]); // end position
        }

        [TestMethod]
        public void StringFind_PatternNotFound_ShouldReturnNil()
        {
            var stringTable = _env.GetVariable("string");
            var findFunc = stringTable.AsTable<LuaTable>().Get("find");
            var result = findFunc.AsFunction<LuaFunction>().Call(["hello world", "xyz"]);
            
            Assert.AreEqual(1, result.Length);
            Assert.IsTrue(result[0].IsNil);
        }

        // Boundary Value Testing: Testing edge cases
        [TestMethod]
        public void StringFind_EmptyPattern_ShouldFindAtBeginning()
        {
            var stringTable = _env.GetVariable("string");
            var findFunc = stringTable.AsTable<LuaTable>().Get("find");
            var result = findFunc.AsFunction<LuaFunction>().Call(new LuaValue[] { 
                "hello", 
                "" 
            });
            
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(1, result[0]);
            Assert.AreEqual(0, result[1]);
        }

        #endregion
    }
} 