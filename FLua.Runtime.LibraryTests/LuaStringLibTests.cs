using FLua.Runtime;
using FLua.Common;
using System.Text;

namespace FLua.Runtime.LibraryTests;

/// <summary>
/// Comprehensive unit tests for LuaStringLib using Lee Copeland's testing methodology.
/// Includes boundary value analysis, equivalence class partitioning, and error path testing.
/// </summary>
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

    #region Length Function Tests - Boundary Value Analysis

    [TestMethod]
    public void Len_EmptyString_ReturnsZero()
    {
        var result = CallStringFunction("len", LuaValue.String(""));
        Assert.AreEqual(0L, result.AsInteger());
    }

    [TestMethod]
    public void Len_SingleCharacter_ReturnsOne()
    {
        var result = CallStringFunction("len", LuaValue.String("a"));
        Assert.AreEqual(1L, result.AsInteger());
    }

    [TestMethod]
    public void Len_LongString_ReturnsCorrectLength()
    {
        var longString = new string('x', 1000);
        var result = CallStringFunction("len", LuaValue.String(longString));
        Assert.AreEqual(1000L, result.AsInteger());
    }

    [TestMethod]
    public void Len_UnicodeString_ReturnsCharacterCount()
    {
        var result = CallStringFunction("len", LuaValue.String("héllo"));
        Assert.AreEqual(5L, result.AsInteger());
    }

    [TestMethod]
    public void Len_NumberAsString_ReturnsStringLength()
    {
        var result = CallStringFunction("len", LuaValue.Integer(12345));
        Assert.AreEqual(5L, result.AsInteger());
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Len_NoArguments_ThrowsException()
    {
        CallStringFunction("len");
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Len_NilArgument_ThrowsException()
    {
        CallStringFunction("len", LuaValue.Nil);
    }

    #endregion

    #region Substring Function Tests - Boundary Value Analysis

    [TestMethod]
    public void Sub_ValidRange_ReturnsSubstring()
    {
        var result = CallStringFunction("sub", LuaValue.String("hello"), LuaValue.Integer(2), LuaValue.Integer(4));
        Assert.AreEqual("ell", result.AsString());
    }

    [TestMethod]
    public void Sub_StartOnly_ReturnsFromStartToEnd()
    {
        var result = CallStringFunction("sub", LuaValue.String("hello"), LuaValue.Integer(3));
        Assert.AreEqual("llo", result.AsString());
    }

    [TestMethod]
    public void Sub_NegativeIndices_WorksFromEnd()
    {
        var result = CallStringFunction("sub", LuaValue.String("hello"), LuaValue.Integer(-3), LuaValue.Integer(-1));
        Assert.AreEqual("llo", result.AsString());
    }

    [TestMethod]
    public void Sub_StartEqualsEnd_ReturnsSingleCharacter()
    {
        var result = CallStringFunction("sub", LuaValue.String("hello"), LuaValue.Integer(2), LuaValue.Integer(2));
        Assert.AreEqual("e", result.AsString());
    }

    [TestMethod]
    public void Sub_StartGreaterThanEnd_ReturnsEmptyString()
    {
        var result = CallStringFunction("sub", LuaValue.String("hello"), LuaValue.Integer(4), LuaValue.Integer(2));
        Assert.AreEqual("", result.AsString());
    }

    [TestMethod]
    public void Sub_OutOfBounds_ReturnsPartialOrEmpty()
    {
        var result = CallStringFunction("sub", LuaValue.String("hello"), LuaValue.Integer(10), LuaValue.Integer(15));
        Assert.AreEqual("", result.AsString());
    }

    [TestMethod]
    public void Sub_ZeroIndex_TreatedAsOne()
    {
        var result = CallStringFunction("sub", LuaValue.String("hello"), LuaValue.Integer(0), LuaValue.Integer(3));
        Assert.AreEqual("hel", result.AsString());
    }

    [TestMethod]
    public void Sub_EmptyString_ReturnsEmpty()
    {
        var result = CallStringFunction("sub", LuaValue.String(""), LuaValue.Integer(1), LuaValue.Integer(5));
        Assert.AreEqual("", result.AsString());
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Sub_MissingStartIndex_ThrowsException()
    {
        CallStringFunction("sub", LuaValue.String("hello"));
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Sub_NonIntegerStartIndex_ThrowsException()
    {
        CallStringFunction("sub", LuaValue.String("hello"), LuaValue.String("not a number"));
    }

    #endregion

    #region Case Conversion Tests

    [TestMethod]
    public void Upper_LowercaseString_ReturnsUppercase()
    {
        var result = CallStringFunction("upper", LuaValue.String("hello world"));
        Assert.AreEqual("HELLO WORLD", result.AsString());
    }

    [TestMethod]
    public void Upper_MixedCaseString_ReturnsUppercase()
    {
        var result = CallStringFunction("upper", LuaValue.String("HeLLo WoRLD"));
        Assert.AreEqual("HELLO WORLD", result.AsString());
    }

    [TestMethod]
    public void Upper_EmptyString_ReturnsEmpty()
    {
        var result = CallStringFunction("upper", LuaValue.String(""));
        Assert.AreEqual("", result.AsString());
    }

    [TestMethod]
    public void Upper_NumbersAndSymbols_UnchangedExceptLetters()
    {
        var result = CallStringFunction("upper", LuaValue.String("abc123!@#"));
        Assert.AreEqual("ABC123!@#", result.AsString());
    }

    [TestMethod]
    public void Lower_UppercaseString_ReturnsLowercase()
    {
        var result = CallStringFunction("lower", LuaValue.String("HELLO WORLD"));
        Assert.AreEqual("hello world", result.AsString());
    }

    [TestMethod]
    public void Lower_MixedCaseString_ReturnsLowercase()
    {
        var result = CallStringFunction("lower", LuaValue.String("HeLLo WoRLD"));
        Assert.AreEqual("hello world", result.AsString());
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Upper_NoArguments_ThrowsException()
    {
        CallStringFunction("upper");
    }

    #endregion

    #region Reverse Function Tests

    [TestMethod]
    public void Reverse_NormalString_ReturnsReversed()
    {
        var result = CallStringFunction("reverse", LuaValue.String("hello"));
        Assert.AreEqual("olleh", result.AsString());
    }

    [TestMethod]
    public void Reverse_SingleCharacter_ReturnsSameCharacter()
    {
        var result = CallStringFunction("reverse", LuaValue.String("a"));
        Assert.AreEqual("a", result.AsString());
    }

    [TestMethod]
    public void Reverse_EmptyString_ReturnsEmpty()
    {
        var result = CallStringFunction("reverse", LuaValue.String(""));
        Assert.AreEqual("", result.AsString());
    }

    [TestMethod]
    public void Reverse_Palindrome_ReturnsSameString()
    {
        var result = CallStringFunction("reverse", LuaValue.String("racecar"));
        Assert.AreEqual("racecar", result.AsString());
    }

    #endregion

    #region Character Functions Tests - Boundary Value Analysis

    [TestMethod]
    public void Char_ValidASCIIValues_ReturnsString()
    {
        var result = CallStringFunction("char", LuaValue.Integer(65), LuaValue.Integer(66), LuaValue.Integer(67));
        Assert.AreEqual("ABC", result.AsString());
    }

    [TestMethod]
    public void Char_SingleValue_ReturnsSingleCharacter()
    {
        var result = CallStringFunction("char", LuaValue.Integer(65));
        Assert.AreEqual("A", result.AsString());
    }

    [TestMethod]
    public void Char_Zero_ReturnsNullCharacter()
    {
        var result = CallStringFunction("char", LuaValue.Integer(0));
        Assert.AreEqual("\0", result.AsString());
    }

    [TestMethod]
    public void Char_MaxValue255_ReturnsHighASCII()
    {
        var result = CallStringFunction("char", LuaValue.Integer(255));
        Assert.AreEqual(((char)255).ToString(), result.AsString());
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Char_NegativeValue_ThrowsException()
    {
        CallStringFunction("char", LuaValue.Integer(-1));
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Char_ValueOver255_ThrowsException()
    {
        CallStringFunction("char", LuaValue.Integer(256));
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Char_NonIntegerArgument_ThrowsException()
    {
        CallStringFunction("char", LuaValue.String("not a number"));
    }

    [TestMethod]
    public void Byte_SingleCharacter_ReturnsByteValue()
    {
        var result = CallStringFunction("byte", LuaValue.String("A"));
        Assert.AreEqual(65L, result.AsInteger());
    }

    [TestMethod]
    public void Byte_MultipleCharacters_ReturnsMultipleValues()
    {
        var results = CallStringFunctionMultiple("byte", LuaValue.String("ABC"), LuaValue.Integer(1), LuaValue.Integer(3));
        Assert.AreEqual(3, results.Length);
        Assert.AreEqual(65L, results[0].AsInteger());
        Assert.AreEqual(66L, results[1].AsInteger());
        Assert.AreEqual(67L, results[2].AsInteger());
    }

    [TestMethod]
    public void Byte_WithStartIndex_ReturnsFromPosition()
    {
        var result = CallStringFunction("byte", LuaValue.String("ABC"), LuaValue.Integer(2));
        Assert.AreEqual(66L, result.AsInteger());
    }

    [TestMethod]
    public void Byte_NegativeIndex_WorksFromEnd()
    {
        var result = CallStringFunction("byte", LuaValue.String("ABC"), LuaValue.Integer(-1));
        Assert.AreEqual(67L, result.AsInteger());
    }

    [TestMethod]
    public void Byte_OutOfRange_ReturnsNoValues()
    {
        var results = CallStringFunctionMultiple("byte", LuaValue.String("ABC"), LuaValue.Integer(5));
        Assert.AreEqual(0, results.Length);
    }

    #endregion

    #region Repetition Function Tests - Error Conditions

    [TestMethod]
    public void Rep_ValidCount_RepeatsString()
    {
        var result = CallStringFunction("rep", LuaValue.String("ha"), LuaValue.Integer(3));
        Assert.AreEqual("hahaha", result.AsString());
    }

    [TestMethod]
    public void Rep_CountOne_ReturnsSameString()
    {
        var result = CallStringFunction("rep", LuaValue.String("hello"), LuaValue.Integer(1));
        Assert.AreEqual("hello", result.AsString());
    }

    [TestMethod]
    public void Rep_CountZero_ReturnsEmptyString()
    {
        var result = CallStringFunction("rep", LuaValue.String("hello"), LuaValue.Integer(0));
        Assert.AreEqual("", result.AsString());
    }

    [TestMethod]
    public void Rep_NegativeCount_ReturnsEmptyString()
    {
        var result = CallStringFunction("rep", LuaValue.String("hello"), LuaValue.Integer(-5));
        Assert.AreEqual("", result.AsString());
    }

    [TestMethod]
    public void Rep_WithSeparator_InterleavesSeparator()
    {
        var result = CallStringFunction("rep", LuaValue.String("abc"), LuaValue.Integer(3), LuaValue.String("-"));
        Assert.AreEqual("abc-abc-abc", result.AsString());
    }

    [TestMethod]
    public void Rep_EmptyString_ReturnsEmpty()
    {
        var result = CallStringFunction("rep", LuaValue.String(""), LuaValue.Integer(5));
        Assert.AreEqual("", result.AsString());
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Rep_MissingCount_ThrowsException()
    {
        CallStringFunction("rep", LuaValue.String("hello"));
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Rep_NonIntegerCount_ThrowsException()
    {
        CallStringFunction("rep", LuaValue.String("hello"), LuaValue.String("not a number"));
    }

    #endregion

    #region Pattern Matching Tests - Find Function

    [TestMethod]
    public void Find_SimplePattern_ReturnsPosition()
    {
        var results = CallStringFunctionMultiple("find", LuaValue.String("hello world"), LuaValue.String("wor"));
        Assert.AreEqual(2, results.Length);
        Assert.AreEqual(7L, results[0].AsInteger()); // 1-based index
        Assert.AreEqual(9L, results[1].AsInteger());
    }

    [TestMethod]
    public void Find_PatternNotFound_ReturnsNil()
    {
        var result = CallStringFunction("find", LuaValue.String("hello world"), LuaValue.String("xyz"));
        Assert.IsTrue(result.IsNil);
    }

    [TestMethod]
    public void Find_WithStartPosition_SearchesFromPosition()
    {
        var results = CallStringFunctionMultiple("find", LuaValue.String("hello hello"), LuaValue.String("hello"), LuaValue.Integer(2));
        Assert.AreEqual(2, results.Length);
        Assert.AreEqual(7L, results[0].AsInteger()); // Second occurrence
        Assert.AreEqual(11L, results[1].AsInteger());
    }

    [TestMethod]
    public void Find_NegativeStartPosition_WorksFromEnd()
    {
        var results = CallStringFunctionMultiple("find", LuaValue.String("hello world"), LuaValue.String("l"), LuaValue.Integer(-5));
        Assert.AreEqual(2, results.Length);
        Assert.AreEqual(10L, results[0].AsInteger()); // 'l' in "world"
        Assert.AreEqual(10L, results[1].AsInteger());
    }

    [TestMethod]
    public void Find_PlainSearch_TreatsPatternLiterally()
    {
        var results = CallStringFunctionMultiple("find", LuaValue.String("a.b.c"), LuaValue.String("."), LuaValue.Integer(1), LuaValue.Boolean(true));
        Assert.AreEqual(2, results.Length);
        Assert.AreEqual(2L, results[0].AsInteger()); // Literal dot, not regex
        Assert.AreEqual(2L, results[1].AsInteger());
    }

    [TestMethod]
    public void Find_StartBeyondStringLength_ReturnsNil()
    {
        var result = CallStringFunction("find", LuaValue.String("hello"), LuaValue.String("l"), LuaValue.Integer(10));
        Assert.IsTrue(result.IsNil);
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Find_MissingPattern_ThrowsException()
    {
        CallStringFunction("find", LuaValue.String("hello"));
    }

    #endregion

    #region Format Function Tests - Comprehensive Coverage

    [TestMethod]
    public void Format_SimpleStringReplacement_WorksCorrectly()
    {
        var result = CallStringFunction("format", LuaValue.String("Hello %s!"), LuaValue.String("World"));
        Assert.AreEqual("Hello World!", result.AsString());
    }

    [TestMethod]
    public void Format_IntegerFormatting_WorksCorrectly()
    {
        var result = CallStringFunction("format", LuaValue.String("Value: %d"), LuaValue.Integer(42));
        Assert.AreEqual("Value: 42", result.AsString());
    }

    [TestMethod]
    public void Format_FloatFormatting_WorksCorrectly()
    {
        var result = CallStringFunction("format", LuaValue.String("Pi: %.2f"), LuaValue.Float(3.14159));
        Assert.AreEqual("Pi: 3.14", result.AsString());
    }

    [TestMethod]
    public void Format_HexFormatting_WorksCorrectly()
    {
        var result = CallStringFunction("format", LuaValue.String("Hex: %x"), LuaValue.Integer(255));
        Assert.AreEqual("Hex: ff", result.AsString());
    }

    [TestMethod]
    public void Format_UppercaseHexFormatting_WorksCorrectly()
    {
        var result = CallStringFunction("format", LuaValue.String("Hex: %X"), LuaValue.Integer(255));
        Assert.AreEqual("Hex: FF", result.AsString());
    }

    [TestMethod]
    public void Format_OctalFormatting_WorksCorrectly()
    {
        var result = CallStringFunction("format", LuaValue.String("Octal: %o"), LuaValue.Integer(64));
        Assert.AreEqual("Octal: 100", result.AsString());
    }

    [TestMethod]
    public void Format_CharacterFormatting_WorksCorrectly()
    {
        var result = CallStringFunction("format", LuaValue.String("Char: %c"), LuaValue.Integer(65));
        Assert.AreEqual("Char: A", result.AsString());
    }

    [TestMethod]
    public void Format_PercentEscape_WorksCorrectly()
    {
        var result = CallStringFunction("format", LuaValue.String("100%% complete"));
        Assert.AreEqual("100% complete", result.AsString());
    }

    [TestMethod]
    public void Format_WidthSpecifier_PadsCorrectly()
    {
        var result = CallStringFunction("format", LuaValue.String("%5d"), LuaValue.Integer(42));
        Assert.AreEqual("   42", result.AsString());
    }

    [TestMethod]
    public void Format_LeftAlignFlag_AlignCorrectly()
    {
        var result = CallStringFunction("format", LuaValue.String("%-5d"), LuaValue.Integer(42));
        Assert.AreEqual("42   ", result.AsString());
    }

    [TestMethod]
    public void Format_ZeroPadFlag_PadsWithZeros()
    {
        var result = CallStringFunction("format", LuaValue.String("%05d"), LuaValue.Integer(42));
        Assert.AreEqual("00042", result.AsString());
    }

    [TestMethod]
    public void Format_PlusSignFlag_ShowsSign()
    {
        var result = CallStringFunction("format", LuaValue.String("%+d"), LuaValue.Integer(42));
        Assert.AreEqual("+42", result.AsString());
    }

    [TestMethod]
    public void Format_MultiplePlaceholders_WorksCorrectly()
    {
        var result = CallStringFunction("format", 
            LuaValue.String("Name: %s, Age: %d, Score: %.1f"), 
            LuaValue.String("John"), 
            LuaValue.Integer(25), 
            LuaValue.Float(95.5));
        Assert.AreEqual("Name: John, Age: 25, Score: 95.5", result.AsString());
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Format_MissingArgument_ThrowsException()
    {
        CallStringFunction("format", LuaValue.String("Hello %s!"));
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Format_CharacterOutOfRange_ThrowsException()
    {
        CallStringFunction("format", LuaValue.String("%c"), LuaValue.Integer(256));
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Format_NoArguments_ThrowsException()
    {
        CallStringFunction("format");
    }

    #endregion

    #region Binary Pack/Unpack Tests - Basic Coverage

    [TestMethod]
    public void Pack_UnsignedByte_WorksCorrectly()
    {
        var result = CallStringFunction("pack", LuaValue.String("B"), LuaValue.Integer(65));
        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(result.AsString());
        Assert.AreEqual(1, bytes.Length);
        Assert.AreEqual(65, bytes[0]);
    }

    [TestMethod]
    public void Pack_SignedByte_WorksCorrectly()
    {
        var result = CallStringFunction("pack", LuaValue.String("b"), LuaValue.Integer(-1));
        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(result.AsString());
        Assert.AreEqual(1, bytes.Length);
        Assert.AreEqual(255, bytes[0]); // -1 as unsigned byte
    }

    [TestMethod]
    public void Pack_SignedShort_WorksCorrectly()
    {
        var result = CallStringFunction("pack", LuaValue.String("h"), LuaValue.Integer(0x1234));
        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(result.AsString());
        Assert.AreEqual(2, bytes.Length);
    }

    [TestMethod]
    public void Pack_Float_WorksCorrectly()
    {
        var result = CallStringFunction("pack", LuaValue.String("f"), LuaValue.Float(3.14f));
        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(result.AsString());
        Assert.AreEqual(4, bytes.Length);
    }

    [TestMethod]
    public void Pack_Double_WorksCorrectly()
    {
        var result = CallStringFunction("pack", LuaValue.String("d"), LuaValue.Float(3.14159));
        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(result.AsString());
        Assert.AreEqual(8, bytes.Length);
    }

    [TestMethod]
    public void Pack_FixedString_WorksCorrectly()
    {
        var result = CallStringFunction("pack", LuaValue.String("c3"), LuaValue.String("hi"));
        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(result.AsString());
        Assert.AreEqual(3, bytes.Length);
        Assert.AreEqual((byte)'h', bytes[0]);
        Assert.AreEqual((byte)'i', bytes[1]);
        Assert.AreEqual(0, bytes[2]); // padded with zero
    }

    [TestMethod]
    public void Pack_ZeroTerminatedString_WorksCorrectly()
    {
        var result = CallStringFunction("pack", LuaValue.String("z"), LuaValue.String("hello"));
        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(result.AsString());
        Assert.AreEqual(6, bytes.Length); // 5 chars + null terminator
        Assert.AreEqual(0, bytes[5]);
    }

    [TestMethod]
    public void PackSize_ValidFormat_ReturnsSize()
    {
        var result = CallStringFunction("packsize", LuaValue.String("BHd"));
        Assert.AreEqual(11L, result.AsInteger()); // 1 + 2 + 8 bytes
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void PackSize_VariableLengthFormat_ThrowsException()
    {
        CallStringFunction("packsize", LuaValue.String("z")); // zero-terminated string is variable length
    }

    [TestMethod]
    public void Unpack_UnsignedByte_WorksCorrectly()
    {
        var data = LuaValue.String(Encoding.GetEncoding("ISO-8859-1").GetString(new byte[] { 65 }));
        var results = CallStringFunctionMultiple("unpack", LuaValue.String("B"), data);
        Assert.AreEqual(2, results.Length); // value + position
        Assert.AreEqual(65L, results[0].AsInteger());
        Assert.AreEqual(2L, results[1].AsInteger()); // position after read
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Unpack_DataTooShort_ThrowsException()
    {
        var data = LuaValue.String(""); // empty data
        CallStringFunction("unpack", LuaValue.String("B"), data);
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Pack_NoArguments_ThrowsException()
    {
        CallStringFunction("pack");
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Unpack_MissingDataArgument_ThrowsException()
    {
        CallStringFunction("unpack", LuaValue.String("B"));
    }

    #endregion

    #region GSub Function Tests - Basic Coverage

    [TestMethod]
    public void GSub_SimpleReplacement_WorksCorrectly()
    {
        var results = CallStringFunctionMultiple("gsub", 
            LuaValue.String("hello world"), 
            LuaValue.String("l"), 
            LuaValue.String("L"));
        Assert.AreEqual(2, results.Length);
        Assert.AreEqual("heLLo worLd", results[0].AsString());
        Assert.AreEqual(3L, results[1].AsInteger()); // number of replacements
    }

    [TestMethod]
    public void GSub_WithLimit_LimitsReplacements()
    {
        var results = CallStringFunctionMultiple("gsub", 
            LuaValue.String("hello world"), 
            LuaValue.String("l"), 
            LuaValue.String("L"),
            LuaValue.Integer(2));
        Assert.AreEqual(2, results.Length);
        Assert.AreEqual("heLLo world", results[0].AsString());
        Assert.AreEqual(2L, results[1].AsInteger());
    }

    [TestMethod]
    public void GSub_NoMatches_ReturnsOriginal()
    {
        var results = CallStringFunctionMultiple("gsub", 
            LuaValue.String("hello world"), 
            LuaValue.String("xyz"), 
            LuaValue.String("ABC"));
        Assert.AreEqual(2, results.Length);
        Assert.AreEqual("hello world", results[0].AsString());
        Assert.AreEqual(0L, results[1].AsInteger());
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void GSub_MissingReplacementArgument_ThrowsException()
    {
        CallStringFunction("gsub", LuaValue.String("hello"), LuaValue.String("l"));
    }

    #endregion

    #region GMatch Function Tests - Iterator

    [TestMethod]
    public void GMatch_SimplePattern_ReturnsIterator()
    {
        var result = CallStringFunction("gmatch", LuaValue.String("hello world"), LuaValue.String("l"));
        Assert.IsTrue(result.IsFunction);
        
        // Test the iterator
        var iterator = result.AsFunction();
        var match1 = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(1, match1.Length);
        Assert.AreEqual("l", match1[0].AsString());
        
        var match2 = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(1, match2.Length);
        Assert.AreEqual("l", match2[0].AsString());
        
        var match3 = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(1, match3.Length);
        Assert.AreEqual("l", match3[0].AsString());
        
        // Should be exhausted now
        var match4 = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(0, match4.Length);
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void GMatch_MissingPattern_ThrowsException()
    {
        CallStringFunction("gmatch", LuaValue.String("hello"));
    }

    #endregion

    #region Helper Methods

    private LuaValue CallStringFunction(string functionName, params LuaValue[] args)
    {
        var stringTable = _env.GetVariable("string").AsTable<LuaTable>();
        var function = stringTable.Get(LuaValue.String(functionName)).AsFunction();
        var results = function.Call(args);
        return results.Length > 0 ? results[0] : LuaValue.Nil;
    }

    private LuaValue[] CallStringFunctionMultiple(string functionName, params LuaValue[] args)
    {
        var stringTable = _env.GetVariable("string").AsTable<LuaTable>();
        var function = stringTable.Get(LuaValue.String(functionName)).AsFunction();
        return function.Call(args);
    }

    #endregion
}