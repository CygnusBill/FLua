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
            var string_table = (LuaTable)_env.GetVariable("string");
            var lenFunc = (LuaFunction)string_table.Get(new LuaString("len"));
            var result = lenFunc.Call(new LuaValue[] { new LuaString("hello") });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(5, ((LuaInteger)result[0]).Value);
        }

        [TestMethod]
        public void StringLen_EmptyString_ShouldReturnZero()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var lenFunc = (LuaFunction)string_table.Get(new LuaString("len"));
            var result = lenFunc.Call(new LuaValue[] { new LuaString("") });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(0, ((LuaInteger)result[0]).Value);
        }

        // Boundary Value Testing: Testing with very long string
        [TestMethod]
        public void StringLen_LongString_ShouldReturnCorrectLength()
        {
            var longString = new string('a', 10000);
            var string_table = (LuaTable)_env.GetVariable("string");
            var lenFunc = (LuaFunction)string_table.Get(new LuaString("len"));
            var result = lenFunc.Call(new LuaValue[] { new LuaString(longString) });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(10000, ((LuaInteger)result[0]).Value);
        }

        // Domain Testing: Testing with number inputs
        [TestMethod]
        public void StringLen_NumberInput_ShouldReturnStringLength()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var lenFunc = (LuaFunction)string_table.Get(new LuaString("len"));
            var result = lenFunc.Call(new LuaValue[] { new LuaInteger(12345) });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(5, ((LuaInteger)result[0]).Value); // "12345" has 5 characters
        }

        #endregion

        #region Sub Function Tests

        // Equivalence Class Testing: Testing different substring scenarios
        [TestMethod]
        public void StringSub_PositiveIndices_ShouldReturnCorrectSubstring()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var subFunc = (LuaFunction)string_table.Get(new LuaString("sub"));
            var result = subFunc.Call(new LuaValue[] { 
                new LuaString("hello world"), 
                new LuaInteger(7), 
                new LuaInteger(11) 
            });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("world", ((LuaString)result[0]).Value);
        }

        [TestMethod]
        public void StringSub_NegativeIndices_ShouldReturnCorrectSubstring()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var subFunc = (LuaFunction)string_table.Get(new LuaString("sub"));
            var result = subFunc.Call(new LuaValue[] { 
                new LuaString("hello world"), 
                new LuaInteger(-5), 
                new LuaInteger(-1) 
            });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("world", ((LuaString)result[0]).Value);
        }

        // Boundary Value Testing: Testing edge cases
        [TestMethod]
        public void StringSub_StartIndexOnly_ShouldReturnRestOfString()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var subFunc = (LuaFunction)string_table.Get(new LuaString("sub"));
            var result = subFunc.Call(new LuaValue[] { 
                new LuaString("hello world"), 
                new LuaInteger(7)
            });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("world", ((LuaString)result[0]).Value);
        }

        [TestMethod]
        public void StringSub_OutOfBounds_ShouldReturnEmptyString()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var subFunc = (LuaFunction)string_table.Get(new LuaString("sub"));
            var result = subFunc.Call(new LuaValue[] { 
                new LuaString("hello"), 
                new LuaInteger(10), 
                new LuaInteger(15) 
            });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("", ((LuaString)result[0]).Value);
        }

        #endregion

        #region Upper/Lower Function Tests

        // Equivalence Class Testing: Testing case conversion
        [TestMethod]
        public void StringUpper_MixedCase_ShouldReturnUppercase()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var upperFunc = (LuaFunction)string_table.Get(new LuaString("upper"));
            var result = upperFunc.Call(new LuaValue[] { new LuaString("Hello World") });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("HELLO WORLD", ((LuaString)result[0]).Value);
        }

        [TestMethod]
        public void StringLower_MixedCase_ShouldReturnLowercase()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var lowerFunc = (LuaFunction)string_table.Get(new LuaString("lower"));
            var result = lowerFunc.Call(new LuaValue[] { new LuaString("Hello World") });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("hello world", ((LuaString)result[0]).Value);
        }

        // Domain Testing: Testing with special characters
        [TestMethod]
        public void StringUpper_WithNumbers_ShouldKeepNumbersUnchanged()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var upperFunc = (LuaFunction)string_table.Get(new LuaString("upper"));
            var result = upperFunc.Call(new LuaValue[] { new LuaString("abc123def") });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("ABC123DEF", ((LuaString)result[0]).Value);
        }

        [TestMethod]
        public void StringLower_EmptyString_ShouldReturnEmptyString()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var lowerFunc = (LuaFunction)string_table.Get(new LuaString("lower"));
            var result = lowerFunc.Call(new LuaValue[] { new LuaString("") });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("", ((LuaString)result[0]).Value);
        }

        #endregion

        #region Reverse Function Tests

        // Equivalence Class Testing: Testing string reversal
        [TestMethod]
        public void StringReverse_RegularString_ShouldReturnReversed()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var reverseFunc = (LuaFunction)string_table.Get(new LuaString("reverse"));
            var result = reverseFunc.Call(new LuaValue[] { new LuaString("hello") });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("olleh", ((LuaString)result[0]).Value);
        }

        [TestMethod]
        public void StringReverse_Palindrome_ShouldReturnSameString()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var reverseFunc = (LuaFunction)string_table.Get(new LuaString("reverse"));
            var result = reverseFunc.Call(new LuaValue[] { new LuaString("racecar") });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("racecar", ((LuaString)result[0]).Value);
        }

        // Boundary Value Testing: Testing edge cases
        [TestMethod]
        public void StringReverse_SingleCharacter_ShouldReturnSameCharacter()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var reverseFunc = (LuaFunction)string_table.Get(new LuaString("reverse"));
            var result = reverseFunc.Call(new LuaValue[] { new LuaString("a") });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("a", ((LuaString)result[0]).Value);
        }

        [TestMethod]
        public void StringReverse_EmptyString_ShouldReturnEmptyString()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var reverseFunc = (LuaFunction)string_table.Get(new LuaString("reverse"));
            var result = reverseFunc.Call(new LuaValue[] { new LuaString("") });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("", ((LuaString)result[0]).Value);
        }

        #endregion

        #region Repeat Function Tests

        // Equivalence Class Testing: Testing string repetition
        [TestMethod]
        public void StringRep_PositiveCount_ShouldRepeatString()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var repFunc = (LuaFunction)string_table.Get(new LuaString("rep"));
            var result = repFunc.Call(new LuaValue[] { new LuaString("ab"), new LuaInteger(3) });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("ababab", ((LuaString)result[0]).Value);
        }

        // Boundary Value Testing: Testing edge cases
        [TestMethod]
        public void StringRep_ZeroCount_ShouldReturnEmptyString()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var repFunc = (LuaFunction)string_table.Get(new LuaString("rep"));
            var result = repFunc.Call(new LuaValue[] { new LuaString("hello"), new LuaInteger(0) });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("", ((LuaString)result[0]).Value);
        }

        [TestMethod]
        public void StringRep_OneCount_ShouldReturnOriginalString()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var repFunc = (LuaFunction)string_table.Get(new LuaString("rep"));
            var result = repFunc.Call(new LuaValue[] { new LuaString("hello"), new LuaInteger(1) });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("hello", ((LuaString)result[0]).Value);
        }

        // Risk-Based Testing: Testing with large repeat count
        [TestMethod]
        public void StringRep_LargeCount_ShouldHandleCorrectly()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var repFunc = (LuaFunction)string_table.Get(new LuaString("rep"));
            var result = repFunc.Call(new LuaValue[] { new LuaString("x"), new LuaInteger(1000) });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(1000, ((LuaString)result[0]).Value.Length);
            Assert.IsTrue(((LuaString)result[0]).Value.All(c => c == 'x'));
        }

        #endregion

        #region Char/Byte Function Tests

        // Decision Table Testing: Testing character code conversions
        [TestMethod]
        public void StringChar_SingleCode_ShouldReturnCharacter()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var charFunc = (LuaFunction)string_table.Get(new LuaString("char"));
            var result = charFunc.Call(new LuaValue[] { new LuaInteger(65) }); // ASCII for 'A'
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("A", ((LuaString)result[0]).Value);
        }

        [TestMethod]
        public void StringChar_MultipleCodes_ShouldReturnString()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var charFunc = (LuaFunction)string_table.Get(new LuaString("char"));
            var result = charFunc.Call(new LuaValue[] { 
                new LuaInteger(72),  // 'H'
                new LuaInteger(105), // 'i'
                new LuaInteger(33)   // '!'
            });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("Hi!", ((LuaString)result[0]).Value);
        }

        [TestMethod]
        public void StringByte_SingleCharacter_ShouldReturnCode()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var byteFunc = (LuaFunction)string_table.Get(new LuaString("byte"));
            var result = byteFunc.Call(new LuaValue[] { new LuaString("A") });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(65, ((LuaInteger)result[0]).Value);
        }

        [TestMethod]
        public void StringByte_MultipleCharacters_ShouldReturnMultipleCodes()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var byteFunc = (LuaFunction)string_table.Get(new LuaString("byte"));
            var result = byteFunc.Call(new LuaValue[] { 
                new LuaString("Hi!"), 
                new LuaInteger(1), 
                new LuaInteger(3) 
            });
            
            Assert.AreEqual(3, result.Length);
            Assert.AreEqual(72, ((LuaInteger)result[0]).Value);  // 'H'
            Assert.AreEqual(105, ((LuaInteger)result[1]).Value); // 'i'
            Assert.AreEqual(33, ((LuaInteger)result[2]).Value);  // '!'
        }

        // Boundary Value Testing: Testing with boundary ASCII values
        [TestMethod]
        public void StringChar_NullCharacter_ShouldReturnNullChar()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var charFunc = (LuaFunction)string_table.Get(new LuaString("char"));
            var result = charFunc.Call(new LuaValue[] { new LuaInteger(0) });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("\0", ((LuaString)result[0]).Value);
        }

        [TestMethod]
        public void StringChar_MaxASCII_ShouldReturnCharacter()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var charFunc = (LuaFunction)string_table.Get(new LuaString("char"));
            var result = charFunc.Call(new LuaValue[] { new LuaInteger(255) });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(((char)255).ToString(), ((LuaString)result[0]).Value);
        }

        #endregion

        #region Format Function Tests

        // Decision Table Testing: Testing various format specifiers
        [TestMethod]
        public void StringFormat_IntegerSpecifier_ShouldFormatCorrectly()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var formatFunc = (LuaFunction)string_table.Get(new LuaString("format"));
            var result = formatFunc.Call(new LuaValue[] { 
                new LuaString("Number: %d"), 
                new LuaInteger(42) 
            });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("Number: 42", ((LuaString)result[0]).Value);
        }

        [TestMethod]
        public void StringFormat_FloatSpecifier_ShouldFormatCorrectly()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var formatFunc = (LuaFunction)string_table.Get(new LuaString("format"));
            var result = formatFunc.Call(new LuaValue[] { 
                new LuaString("Value: %.2f"), 
                new LuaNumber(3.14159) 
            });
            
            Assert.AreEqual(1, result.Length);
            string resultStr = ((LuaString)result[0]).Value;
            Assert.IsTrue(resultStr.StartsWith("Value: ") && resultStr.Contains("3.14"), 
                $"Expected format 'Value: 3.14*' but got '{resultStr}'");
        }

        [TestMethod]
        public void StringFormat_StringSpecifier_ShouldFormatCorrectly()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var formatFunc = (LuaFunction)string_table.Get(new LuaString("format"));
            var result = formatFunc.Call(new LuaValue[] { 
                new LuaString("Hello, %s!"), 
                new LuaString("World") 
            });
            
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("Hello, World!", ((LuaString)result[0]).Value);
        }

        // Scenario Testing: Testing complex format strings
        [TestMethod]
        public void StringFormat_MultipleSpecifiers_ShouldFormatCorrectly()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var formatFunc = (LuaFunction)string_table.Get(new LuaString("format"));
            var result = formatFunc.Call(new LuaValue[] { 
                new LuaString("%s scored %d points with %.1f%% accuracy"), 
                new LuaString("Alice"),
                new LuaInteger(95),
                new LuaNumber(87.5)
            });
            
            Assert.AreEqual(1, result.Length);
            var formatted = ((LuaString)result[0]).Value;
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
            var string_table = (LuaTable)_env.GetVariable("string");
            var findFunc = (LuaFunction)string_table.Get(new LuaString("find"));
            var result = findFunc.Call(new LuaValue[] { 
                new LuaString("hello world"), 
                new LuaString("world") 
            });
            
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(7, ((LuaInteger)result[0]).Value); // 1-indexed position
            Assert.AreEqual(11, ((LuaInteger)result[1]).Value); // end position
        }

        [TestMethod]
        public void StringFind_PatternNotFound_ShouldReturnNil()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var findFunc = (LuaFunction)string_table.Get(new LuaString("find"));
            var result = findFunc.Call(new LuaValue[] { 
                new LuaString("hello world"), 
                new LuaString("xyz") 
            });
            
            Assert.AreEqual(1, result.Length);
            Assert.IsInstanceOfType(result[0], typeof(LuaNil));
        }

        // Boundary Value Testing: Testing edge cases
        [TestMethod]
        public void StringFind_EmptyPattern_ShouldFindAtBeginning()
        {
            var string_table = (LuaTable)_env.GetVariable("string");
            var findFunc = (LuaFunction)string_table.Get(new LuaString("find"));
            var result = findFunc.Call(new LuaValue[] { 
                new LuaString("hello"), 
                new LuaString("") 
            });
            
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(1, ((LuaInteger)result[0]).Value);
            Assert.AreEqual(0, ((LuaInteger)result[1]).Value);
        }

        #endregion
    }
} 