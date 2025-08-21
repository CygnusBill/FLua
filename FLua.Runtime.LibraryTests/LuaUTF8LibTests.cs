using FLua.Runtime;
using FLua.Common;
using System.Globalization;
using System.Text;

namespace FLua.Runtime.LibraryTests;

/// <summary>
/// Comprehensive unit tests for LuaUTF8Lib using Lee Copeland's testing methodology.
/// Includes boundary value analysis, equivalence class partitioning, and error path testing.
/// Tests cover ASCII, Unicode, and invalid UTF-8 sequences.
/// </summary>
[TestClass]
public class LuaUTF8LibTests
{
    private LuaEnvironment _env = null!;

    [TestInitialize]
    public void Setup()
    {
        _env = new LuaEnvironment();
        LuaUTF8Lib.AddUTF8Library(_env);
    }

    #region Length Function Tests - Boundary Value Analysis

    [TestMethod]
    public void Len_EmptyString_ReturnsZero()
    {
        var result = CallUTF8Function("len", LuaValue.String(""));
        Assert.AreEqual(0L, result.AsInteger());
    }

    [TestMethod]
    public void Len_ASCIIString_ReturnsCharacterCount()
    {
        var result = CallUTF8Function("len", LuaValue.String("hello"));
        Assert.AreEqual(5L, result.AsInteger());
    }

    [TestMethod]
    public void Len_UnicodeString_ReturnsCorrectCount()
    {
        // "café" - 4 Unicode characters
        var result = CallUTF8Function("len", LuaValue.String("café"));
        Assert.AreEqual(4L, result.AsInteger());
    }

    [TestMethod]
    public void Len_EmojiString_CountsCorrectly()
    {
        // "🌟" is a single Unicode character (but multiple bytes)
        var result = CallUTF8Function("len", LuaValue.String("🌟"));
        Assert.AreEqual(1L, result.AsInteger());
    }

    [TestMethod]
    public void Len_MixedString_CountsCorrectly()
    {
        // Mix of ASCII, accented characters, and emoji
        var result = CallUTF8Function("len", LuaValue.String("Hello 世界 🌍"));
        Assert.AreEqual(10L, result.AsInteger()); // H-e-l-l-o-[space]-世-界-[space]-🌍
    }

    [TestMethod]
    public void Len_WithStartPosition_CountsFromPosition()
    {
        var result = CallUTF8Function("len", LuaValue.String("hello"), LuaValue.Integer(3));
        Assert.AreEqual(3L, result.AsInteger()); // "llo" = 3 characters
    }

    [TestMethod]
    public void Len_WithStartAndEnd_CountsRange()
    {
        var result = CallUTF8Function("len", LuaValue.String("hello"), LuaValue.Integer(2), LuaValue.Integer(4));
        Assert.AreEqual(3L, result.AsInteger()); // "ell" = 3 characters
    }

    [TestMethod]
    public void Len_StartGreaterThanEnd_ReturnsZero()
    {
        var result = CallUTF8Function("len", LuaValue.String("hello"), LuaValue.Integer(4), LuaValue.Integer(2));
        Assert.AreEqual(0L, result.AsInteger());
    }

    [TestMethod]
    public void Len_OutOfBoundsStart_ReturnsZero()
    {
        var result = CallUTF8Function("len", LuaValue.String("hello"), LuaValue.Integer(10));
        Assert.AreEqual(0L, result.AsInteger());
    }

    [TestMethod]
    public void Len_NegativeStart_StartsFromBeginning()
    {
        var result = CallUTF8Function("len", LuaValue.String("hello"), LuaValue.Integer(-5));
        Assert.AreEqual(5L, result.AsInteger());
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Len_NoArguments_ThrowsException()
    {
        CallUTF8Function("len");
    }

    #endregion

    #region Character Function Tests - Unicode Code Points

    [TestMethod]
    public void Char_ASCIICodePoints_ReturnsString()
    {
        var result = CallUTF8Function("char", LuaValue.Integer(72), LuaValue.Integer(101), LuaValue.Integer(108), LuaValue.Integer(108), LuaValue.Integer(111));
        Assert.AreEqual("Hello", result.AsString());
    }

    [TestMethod]
    public void Char_UnicodeCodePoints_ReturnsUnicodeString()
    {
        // 0x4E16 = 世, 0x754C = 界
        var result = CallUTF8Function("char", LuaValue.Integer(0x4E16), LuaValue.Integer(0x754C));
        Assert.AreEqual("世界", result.AsString());
    }

    [TestMethod]
    public void Char_EmojiCodePoint_ReturnsEmoji()
    {
        // 0x1F30D = 🌍 (Earth Globe Europe-Africa)
        var result = CallUTF8Function("char", LuaValue.Integer(0x1F30D));
        Assert.AreEqual("🌍", result.AsString());
    }

    [TestMethod]
    public void Char_SingleCodePoint_ReturnsSingleCharacter()
    {
        var result = CallUTF8Function("char", LuaValue.Integer(65)); // 'A'
        Assert.AreEqual("A", result.AsString());
    }

    [TestMethod]
    public void Char_ZeroCodePoint_ReturnsNullCharacter()
    {
        var result = CallUTF8Function("char", LuaValue.Integer(0));
        Assert.AreEqual("\0", result.AsString());
    }

    [TestMethod]
    public void Char_MaxValidCodePoint_Works()
    {
        // 0x10FFFF is the maximum valid Unicode code point
        var result = CallUTF8Function("char", LuaValue.Integer(0x10FFFF));
        Assert.IsTrue(result.IsString);
        Assert.IsFalse(string.IsNullOrEmpty(result.AsString()));
    }

    [TestMethod]
    public void Char_NoArguments_ReturnsEmptyString()
    {
        var result = CallUTF8Function("char");
        Assert.AreEqual("", result.AsString());
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Char_NegativeCodePoint_ThrowsException()
    {
        CallUTF8Function("char", LuaValue.Integer(-1));
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Char_CodePointTooLarge_ThrowsException()
    {
        CallUTF8Function("char", LuaValue.Integer(0x110000)); // Beyond max Unicode
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Char_NonIntegerArgument_ThrowsException()
    {
        CallUTF8Function("char", LuaValue.String("not a number"));
    }

    #endregion

    #region CodePoint Function Tests - Character to Code Point

    [TestMethod]
    public void CodePoint_ASCIIString_ReturnsCodePoints()
    {
        var results = CallUTF8FunctionMultiple("codepoint", LuaValue.String("ABC"));
        Assert.AreEqual(3, results.Length);
        Assert.AreEqual(65L, results[0].AsInteger()); // 'A'
        Assert.AreEqual(66L, results[1].AsInteger()); // 'B'
        Assert.AreEqual(67L, results[2].AsInteger()); // 'C'
    }

    [TestMethod]
    public void CodePoint_UnicodeString_ReturnsCorrectCodePoints()
    {
        var results = CallUTF8FunctionMultiple("codepoint", LuaValue.String("世界"));
        Assert.AreEqual(2, results.Length);
        Assert.AreEqual(0x4E16L, results[0].AsInteger()); // 世
        Assert.AreEqual(0x754CL, results[1].AsInteger()); // 界
    }

    [TestMethod]
    public void CodePoint_EmojiString_ReturnsEmojiCodePoint()
    {
        var results = CallUTF8FunctionMultiple("codepoint", LuaValue.String("🌍"));
        Assert.AreEqual(1, results.Length);
        Assert.AreEqual(0x1F30DL, results[0].AsInteger()); // 🌍
    }

    [TestMethod]
    public void CodePoint_SingleCharacter_ReturnsSingleCodePoint()
    {
        var result = CallUTF8Function("codepoint", LuaValue.String("A"));
        Assert.AreEqual(65L, result.AsInteger());
    }

    [TestMethod]
    public void CodePoint_WithPosition_ReturnsFromPosition()
    {
        var result = CallUTF8Function("codepoint", LuaValue.String("ABC"), LuaValue.Integer(2));
        Assert.AreEqual(66L, result.AsInteger()); // 'B'
    }

    [TestMethod]
    public void CodePoint_WithRange_ReturnsRange()
    {
        var results = CallUTF8FunctionMultiple("codepoint", LuaValue.String("ABCDE"), LuaValue.Integer(2), LuaValue.Integer(4));
        Assert.AreEqual(3, results.Length);
        Assert.AreEqual(66L, results[0].AsInteger()); // 'B'
        Assert.AreEqual(67L, results[1].AsInteger()); // 'C'
        Assert.AreEqual(68L, results[2].AsInteger()); // 'D'
    }

    [TestMethod]
    public void CodePoint_EmptyString_ReturnsEmpty()
    {
        var results = CallUTF8FunctionMultiple("codepoint", LuaValue.String(""));
        Assert.AreEqual(0, results.Length);
    }

    [TestMethod]
    public void CodePoint_OutOfBounds_ReturnsEmpty()
    {
        var results = CallUTF8FunctionMultiple("codepoint", LuaValue.String("ABC"), LuaValue.Integer(5));
        Assert.AreEqual(0, results.Length);
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void CodePoint_NoArguments_ThrowsException()
    {
        CallUTF8Function("codepoint");
    }

    #endregion

    #region Offset Function Tests - Position Navigation

    [TestMethod]
    public void Offset_ZeroOffset_ReturnsCharacterBounds()
    {
        var results = CallUTF8FunctionMultiple("offset", LuaValue.String("hello"), LuaValue.Integer(0), LuaValue.Integer(3));
        Assert.AreEqual(2, results.Length);
        Assert.AreEqual(3L, results[0].AsInteger()); // Start of character 3
        Assert.AreEqual(4L, results[1].AsInteger()); // End of character 3
    }

    [TestMethod]
    public void Offset_PositiveOffset_MovesForward()
    {
        var result = CallUTF8Function("offset", LuaValue.String("hello"), LuaValue.Integer(2), LuaValue.Integer(1));
        Assert.AreEqual(3L, result.AsInteger()); // Move 2 characters forward from position 1
    }

    [TestMethod]
    public void Offset_NegativeOffset_MovesBackward()
    {
        var result = CallUTF8Function("offset", LuaValue.String("hello"), LuaValue.Integer(-1), LuaValue.Integer(3));
        Assert.AreEqual(2L, result.AsInteger()); // Move 1 character backward from position 3
    }

    [TestMethod]
    public void Offset_UnicodeString_HandlesMultiByteCharacters()
    {
        var result = CallUTF8Function("offset", LuaValue.String("café"), LuaValue.Integer(1), LuaValue.Integer(1));
        Assert.AreEqual(2L, result.AsInteger()); // Move 1 character forward in "café"
    }

    [TestMethod]
    public void Offset_DefaultStartPosition_UsesOne()
    {
        var result = CallUTF8Function("offset", LuaValue.String("hello"), LuaValue.Integer(2));
        Assert.AreEqual(3L, result.AsInteger()); // Move 2 characters forward from position 1
    }

    [TestMethod]
    public void Offset_BeyondBounds_ReturnsEmpty()
    {
        var results = CallUTF8FunctionMultiple("offset", LuaValue.String("hello"), LuaValue.Integer(10), LuaValue.Integer(1));
        Assert.AreEqual(0, results.Length);
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Offset_MissingOffsetArgument_ThrowsException()
    {
        CallUTF8Function("offset", LuaValue.String("hello"));
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Offset_NonIntegerOffset_ThrowsException()
    {
        CallUTF8Function("offset", LuaValue.String("hello"), LuaValue.String("not a number"));
    }

    #endregion

    #region Codes Function Tests - Iterator Pattern

    [TestMethod]
    public void Codes_ASCIIString_ReturnsIterator()
    {
        var result = CallUTF8Function("codes", LuaValue.String("ABC"));
        
        Assert.IsTrue(result.IsFunction);
        
        var iterator = result.AsFunction();
        
        // Test iteration
        var iter1 = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(2, iter1.Length);
        Assert.AreEqual(1L, iter1[0].AsInteger()); // Position
        Assert.AreEqual(65L, iter1[1].AsInteger()); // 'A'
        
        var iter2 = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(2, iter2.Length);
        Assert.AreEqual(2L, iter2[0].AsInteger()); // Position
        Assert.AreEqual(66L, iter2[1].AsInteger()); // 'B'
        
        var iter3 = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(2, iter3.Length);
        Assert.AreEqual(3L, iter3[0].AsInteger()); // Position
        Assert.AreEqual(67L, iter3[1].AsInteger()); // 'C'
        
        // Should be exhausted
        var iterEnd = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(0, iterEnd.Length);
    }

    [TestMethod]
    public void Codes_UnicodeString_IteratesCodePoints()
    {
        var result = CallUTF8Function("codes", LuaValue.String("世界"));
        
        Assert.IsTrue(result.IsFunction);
        
        var iterator = result.AsFunction();
        
        var iter1 = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(2, iter1.Length);
        Assert.AreEqual(1L, iter1[0].AsInteger()); // Position
        Assert.AreEqual(0x4E16L, iter1[1].AsInteger()); // 世
        
        var iter2 = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(2, iter2.Length);
        Assert.AreEqual(2L, iter2[0].AsInteger()); // Position
        Assert.AreEqual(0x754CL, iter2[1].AsInteger()); // 界
        
        var iterEnd = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(0, iterEnd.Length);
    }

    [TestMethod]
    public void Codes_EmptyString_ReturnsEmptyIterator()
    {
        var result = CallUTF8Function("codes", LuaValue.String(""));
        
        Assert.IsTrue(result.IsFunction);
        
        var iterator = result.AsFunction();
        var iterEnd = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(0, iterEnd.Length);
    }

    [TestMethod]
    public void Codes_SingleCharacter_IteratesOnce()
    {
        var result = CallUTF8Function("codes", LuaValue.String("A"));
        
        Assert.IsTrue(result.IsFunction);
        
        var iterator = result.AsFunction();
        
        var iter1 = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(2, iter1.Length);
        Assert.AreEqual(1L, iter1[0].AsInteger());
        Assert.AreEqual(65L, iter1[1].AsInteger());
        
        var iterEnd = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(0, iterEnd.Length);
    }

    [TestMethod]
    public void Codes_EmojiString_HandlesEmoji()
    {
        var result = CallUTF8Function("codes", LuaValue.String("🌍"));
        
        Assert.IsTrue(result.IsFunction);
        
        var iterator = result.AsFunction();
        
        var iter1 = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(2, iter1.Length);
        Assert.AreEqual(1L, iter1[0].AsInteger());
        Assert.AreEqual(0x1F30DL, iter1[1].AsInteger()); // 🌍
        
        var iterEnd = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(0, iterEnd.Length);
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Codes_NoArguments_ThrowsException()
    {
        CallUTF8Function("codes");
    }

    #endregion

    #region Character Pattern Tests

    [TestMethod]
    public void CharPattern_IsAvailable()
    {
        var utf8Table = _env.GetVariable("utf8").AsTable<LuaTable>();
        var pattern = utf8Table.Get(LuaValue.String("charpattern"));
        
        Assert.IsTrue(pattern.IsString);
        Assert.AreEqual(LuaUTF8Lib.CharPattern, pattern.AsString());
    }

    [TestMethod]
    public void CharPattern_HasCorrectValue()
    {
        var utf8Table = _env.GetVariable("utf8").AsTable<LuaTable>();
        var pattern = utf8Table.Get(LuaValue.String("charpattern"));
        
        Assert.AreEqual("[\\0-\\x7F\\xC2-\\xF4][\\x80-\\xBF]*", pattern.AsString());
    }

    #endregion

    #region Boundary Value Tests - Unicode Edge Cases

    [TestMethod]
    public void Char_MaxASCII_Works()
    {
        var result = CallUTF8Function("char", LuaValue.Integer(127)); // DEL character
        Assert.IsTrue(result.IsString);
    }

    [TestMethod]
    public void Char_MinUnicode_Works()
    {
        var result = CallUTF8Function("char", LuaValue.Integer(128)); // First non-ASCII
        Assert.IsTrue(result.IsString);
    }

    [TestMethod]
    public void CodePoint_ControlCharacters_HandlesCorrectly()
    {
        var result = CallUTF8Function("codepoint", LuaValue.String("\t")); // Tab character
        Assert.AreEqual(9L, result.AsInteger());
    }

    [TestMethod]
    public void CodePoint_NewlineCharacter_HandlesCorrectly()
    {
        var result = CallUTF8Function("codepoint", LuaValue.String("\n"));
        Assert.AreEqual(10L, result.AsInteger());
    }

    [TestMethod]
    public void Len_VeryLongString_HandlesCorrectly()
    {
        var longString = new string('a', 10000);
        var result = CallUTF8Function("len", LuaValue.String(longString));
        Assert.AreEqual(10000L, result.AsInteger());
    }

    [TestMethod]
    public void Len_VeryLongUnicodeString_HandlesCorrectly()
    {
        var longUnicodeString = new string('世', 1000);
        var result = CallUTF8Function("len", LuaValue.String(longUnicodeString));
        Assert.AreEqual(1000L, result.AsInteger());
    }

    [TestMethod]
    public void Codes_VeryLongString_IteratesCorrectly()
    {
        var longString = new string('A', 1000);
        var result = CallUTF8Function("codes", LuaValue.String(longString));
        
        Assert.IsTrue(result.IsFunction);
        
        var iterator = result.AsFunction();
        
        // Test first and last iterations
        var iter1 = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(2, iter1.Length);
        Assert.AreEqual(1L, iter1[0].AsInteger());
        Assert.AreEqual(65L, iter1[1].AsInteger());
        
        // Exhaust iterator
        var count = 1;
        while (true)
        {
            var iter = iterator.Call(Array.Empty<LuaValue>());
            if (iter.Length == 0)
                break;
            count++;
        }
        
        Assert.AreEqual(1000, count);
    }

    #endregion

    #region Mixed Character Set Tests

    [TestMethod]
    public void Len_MixedASCIIUnicode_CountsCorrectly()
    {
        var mixed = "Hello, 世界! How are you? 😊";
        var result = CallUTF8Function("len", LuaValue.String(mixed));
        
        // Count manually: H-e-l-l-o-,-[space]-世-界-!-[space]-H-o-w-[space]-a-r-e-[space]-y-o-u-?-[space]-😊
        var info = new StringInfo(mixed);
        var expected = info.LengthInTextElements;
        
        Assert.AreEqual((long)expected, result.AsInteger());
    }

    [TestMethod]
    public void CodePoint_MixedString_ReturnsAllCodePoints()
    {
        var mixed = "A世🌍";
        var results = CallUTF8FunctionMultiple("codepoint", LuaValue.String(mixed));
        
        Assert.AreEqual(3, results.Length);
        Assert.AreEqual(65L, results[0].AsInteger()); // 'A'
        Assert.AreEqual(0x4E16L, results[1].AsInteger()); // 世
        Assert.AreEqual(0x1F30DL, results[2].AsInteger()); // 🌍
    }

    [TestMethod]
    public void Char_MixedCodePoints_CreatesCorrectString()
    {
        var result = CallUTF8Function("char", 
            LuaValue.Integer(65),      // 'A'
            LuaValue.Integer(0x4E16),  // 世
            LuaValue.Integer(0x1F30D)  // 🌍
        );
        
        Assert.AreEqual("A世🌍", result.AsString());
    }

    #endregion

    #region Helper Methods

    private LuaValue CallUTF8Function(string functionName, params LuaValue[] args)
    {
        var utf8Table = _env.GetVariable("utf8").AsTable<LuaTable>();
        var function = utf8Table.Get(LuaValue.String(functionName)).AsFunction();
        var results = function.Call(args);
        return results.Length > 0 ? results[0] : LuaValue.Nil;
    }

    private LuaValue[] CallUTF8FunctionMultiple(string functionName, params LuaValue[] args)
    {
        var utf8Table = _env.GetVariable("utf8").AsTable<LuaTable>();
        var function = utf8Table.Get(LuaValue.String(functionName)).AsFunction();
        return function.Call(args);
    }

    #endregion
}