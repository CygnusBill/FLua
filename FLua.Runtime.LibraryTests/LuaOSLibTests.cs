using FLua.Runtime;
using FLua.Common;
using System.Globalization;

namespace FLua.Runtime.LibraryTests;

/// <summary>
/// Comprehensive unit tests for LuaOSLib using Lee Copeland's testing methodology.
/// Includes boundary value analysis, equivalence class partitioning, and error path testing.
/// Tests handle system dependencies carefully to avoid side effects.
/// </summary>
[TestClass]
public class LuaOSLibTests
{
    private LuaEnvironment _env = null!;
    private readonly List<string> _tempFiles = new List<string>();
    private CultureInfo? _originalCulture;

    [TestInitialize]
    public void Setup()
    {
        _env = new LuaEnvironment();
        LuaOSLib.AddOSLibrary(_env);
        _originalCulture = CultureInfo.CurrentCulture;
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Clean up temp files
        foreach (var file in _tempFiles)
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // Restore original culture
        if (_originalCulture != null)
        {
            try
            {
                CultureInfo.CurrentCulture = _originalCulture;
                CultureInfo.CurrentUICulture = _originalCulture;
            }
            catch
            {
                // Ignore culture restoration errors
            }
        }
    }

    #region Clock Function Tests - Monotonic Time

    [TestMethod]
    public void Clock_ReturnsPositiveNumber()
    {
        var result = CallOSFunction("clock");
        
        Assert.IsTrue(result.IsNumber);
        var clockValue = result.AsDouble();
        Assert.IsTrue(clockValue >= 0, $"Clock should return non-negative value, got {clockValue}");
    }

    [TestMethod]
    public void Clock_SuccessiveCallsIncreaseMonotonically()
    {
        var result1 = CallOSFunction("clock");
        
        // Small delay to ensure time passes
        Thread.Sleep(10);
        
        var result2 = CallOSFunction("clock");
        
        Assert.IsTrue(result1.IsNumber);
        Assert.IsTrue(result2.IsNumber);
        Assert.IsTrue(result2.AsDouble() >= result1.AsDouble(),
            $"Second clock call ({result2.AsDouble()}) should be >= first call ({result1.AsDouble()})");
    }

    [TestMethod]
    public void Clock_NoArguments_Works()
    {
        var result = CallOSFunction("clock");
        Assert.IsTrue(result.IsNumber);
    }

    [TestMethod]
    public void Clock_WithArguments_IgnoresThem()
    {
        var result = CallOSFunction("clock", LuaValue.String("ignored"), LuaValue.Integer(42));
        Assert.IsTrue(result.IsNumber);
    }

    #endregion

    #region Time Function Tests - Unix Timestamp

    [TestMethod]
    public void Time_NoArguments_ReturnsCurrentTime()
    {
        var beforeTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var result = CallOSFunction("time");
        var afterTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        
        Assert.IsTrue(result.IsInteger);
        var luaTime = result.AsInteger();
        
        Assert.IsTrue(luaTime >= beforeTime && luaTime <= afterTime,
            $"Lua time {luaTime} should be between {beforeTime} and {afterTime}");
    }

    [TestMethod]
    public void Time_WithDateTable_ReturnsCorrectTimestamp()
    {
        var dateTable = new LuaTable();
        dateTable.Set(LuaValue.String("year"), LuaValue.Integer(2023));
        dateTable.Set(LuaValue.String("month"), LuaValue.Integer(6));
        dateTable.Set(LuaValue.String("day"), LuaValue.Integer(15));
        dateTable.Set(LuaValue.String("hour"), LuaValue.Integer(12));
        dateTable.Set(LuaValue.String("min"), LuaValue.Integer(30));
        dateTable.Set(LuaValue.String("sec"), LuaValue.Integer(45));
        
        var result = CallOSFunction("time", LuaValue.Table(dateTable));
        
        Assert.IsTrue(result.IsInteger);
        
        // Verify by converting back
        var expectedDate = new DateTime(2023, 6, 15, 12, 30, 45, DateTimeKind.Utc);
        var expectedTimestamp = ((DateTimeOffset)expectedDate).ToUnixTimeSeconds();
        
        Assert.AreEqual(expectedTimestamp, result.AsInteger());
    }

    [TestMethod]
    public void Time_PartialDateTable_FillsDefaults()
    {
        var dateTable = new LuaTable();
        dateTable.Set(LuaValue.String("year"), LuaValue.Integer(2023));
        dateTable.Set(LuaValue.String("month"), LuaValue.Integer(6));
        dateTable.Set(LuaValue.String("day"), LuaValue.Integer(15));
        // hour, min, sec should default to 0
        
        var result = CallOSFunction("time", LuaValue.Table(dateTable));
        
        Assert.IsTrue(result.IsInteger);
        
        var expectedDate = new DateTime(2023, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var expectedTimestamp = ((DateTimeOffset)expectedDate).ToUnixTimeSeconds();
        
        Assert.AreEqual(expectedTimestamp, result.AsInteger());
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Time_InvalidDateTable_ThrowsException()
    {
        var dateTable = new LuaTable();
        dateTable.Set(LuaValue.String("year"), LuaValue.Integer(2023));
        dateTable.Set(LuaValue.String("month"), LuaValue.Integer(13)); // Invalid month
        dateTable.Set(LuaValue.String("day"), LuaValue.Integer(1));
        
        CallOSFunction("time", LuaValue.Table(dateTable));
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Time_NonTableArgument_ThrowsException()
    {
        CallOSFunction("time", LuaValue.String("not a table"));
    }

    #endregion

    #region Date Function Tests - Formatting

    [TestMethod]
    public void Date_DefaultFormat_ReturnsFormattedString()
    {
        var result = CallOSFunction("date");
        
        Assert.IsTrue(result.IsString);
        var dateString = result.AsString();
        Assert.IsFalse(string.IsNullOrEmpty(dateString));
    }

    [TestMethod]
    public void Date_TableFormat_ReturnsDateTable()
    {
        var result = CallOSFunction("date", LuaValue.String("*t"));
        
        Assert.IsTrue(result.IsTable);
        var dateTable = result.AsTable<LuaTable>();
        
        Assert.IsTrue(dateTable.Get(LuaValue.String("year")).IsInteger);
        Assert.IsTrue(dateTable.Get(LuaValue.String("month")).IsInteger);
        Assert.IsTrue(dateTable.Get(LuaValue.String("day")).IsInteger);
        Assert.IsTrue(dateTable.Get(LuaValue.String("hour")).IsInteger);
        Assert.IsTrue(dateTable.Get(LuaValue.String("min")).IsInteger);
        Assert.IsTrue(dateTable.Get(LuaValue.String("sec")).IsInteger);
        Assert.IsTrue(dateTable.Get(LuaValue.String("wday")).IsInteger);
        Assert.IsTrue(dateTable.Get(LuaValue.String("yday")).IsInteger);
        Assert.IsTrue(dateTable.Get(LuaValue.String("isdst")).IsBoolean);
        
        // Validate ranges
        var year = dateTable.Get(LuaValue.String("year")).AsInteger();
        var month = dateTable.Get(LuaValue.String("month")).AsInteger();
        var day = dateTable.Get(LuaValue.String("day")).AsInteger();
        var wday = dateTable.Get(LuaValue.String("wday")).AsInteger();
        var yday = dateTable.Get(LuaValue.String("yday")).AsInteger();
        
        Assert.IsTrue(year >= 1900 && year <= 3000);
        Assert.IsTrue(month >= 1 && month <= 12);
        Assert.IsTrue(day >= 1 && day <= 31);
        Assert.IsTrue(wday >= 1 && wday <= 7);
        Assert.IsTrue(yday >= 1 && yday <= 366);
    }

    [TestMethod]
    public void Date_WithSpecificTime_ReturnsCorrectDate()
    {
        // June 15, 2023, 12:30:45 UTC
        var timestamp = new DateTimeOffset(2023, 6, 15, 12, 30, 45, TimeSpan.Zero).ToUnixTimeSeconds();
        var result = CallOSFunction("date", LuaValue.String("*t"), LuaValue.Integer(timestamp));
        
        Assert.IsTrue(result.IsTable);
        var dateTable = result.AsTable<LuaTable>();
        
        Assert.AreEqual(2023L, dateTable.Get(LuaValue.String("year")).AsInteger());
        Assert.AreEqual(6L, dateTable.Get(LuaValue.String("month")).AsInteger());
        Assert.AreEqual(15L, dateTable.Get(LuaValue.String("day")).AsInteger());
        Assert.AreEqual(12L, dateTable.Get(LuaValue.String("hour")).AsInteger());
        Assert.AreEqual(30L, dateTable.Get(LuaValue.String("min")).AsInteger());
        Assert.AreEqual(45L, dateTable.Get(LuaValue.String("sec")).AsInteger());
    }

    [TestMethod]
    public void Date_UTCFormat_ReturnsUTCTime()
    {
        var result = CallOSFunction("date", LuaValue.String("!*t"));
        
        Assert.IsTrue(result.IsTable);
        var dateTable = result.AsTable<LuaTable>();
        
        // Verify we get reasonable UTC values
        Assert.IsTrue(dateTable.Get(LuaValue.String("year")).IsInteger);
        Assert.IsTrue(dateTable.Get(LuaValue.String("month")).IsInteger);
    }

    [TestMethod]
    public void Date_YearFormat_ReturnsYear()
    {
        var result = CallOSFunction("date", LuaValue.String("%Y"));
        
        Assert.IsTrue(result.IsString);
        var yearString = result.AsString();
        
        Assert.IsTrue(int.TryParse(yearString, out var year));
        Assert.IsTrue(year >= 2020 && year <= 3000);
    }

    [TestMethod]
    public void Date_MonthFormat_ReturnsMonth()
    {
        var result = CallOSFunction("date", LuaValue.String("%m"));
        
        Assert.IsTrue(result.IsString);
        var monthString = result.AsString();
        
        Assert.IsTrue(int.TryParse(monthString, out var month));
        Assert.IsTrue(month >= 1 && month <= 12);
    }

    #endregion

    #region DiffTime Function Tests - Time Arithmetic

    [TestMethod]
    public void DiffTime_PositiveDifference_ReturnsCorrectValue()
    {
        var result = CallOSFunction("difftime", LuaValue.Integer(1000), LuaValue.Integer(500));
        
        Assert.IsTrue(result.IsNumber);
        Assert.AreEqual(500.0, result.AsDouble());
    }

    [TestMethod]
    public void DiffTime_NegativeDifference_ReturnsNegativeValue()
    {
        var result = CallOSFunction("difftime", LuaValue.Integer(500), LuaValue.Integer(1000));
        
        Assert.IsTrue(result.IsNumber);
        Assert.AreEqual(-500.0, result.AsDouble());
    }

    [TestMethod]
    public void DiffTime_SameTimes_ReturnsZero()
    {
        var result = CallOSFunction("difftime", LuaValue.Integer(1234), LuaValue.Integer(1234));
        
        Assert.IsTrue(result.IsNumber);
        Assert.AreEqual(0.0, result.AsDouble());
    }

    [TestMethod]
    public void DiffTime_FloatingPointTimes_HandlesDecimals()
    {
        var result = CallOSFunction("difftime", LuaValue.Float(123.5), LuaValue.Float(123.2));
        
        Assert.IsTrue(result.IsNumber);
        Assert.AreEqual(0.3, result.AsDouble(), 1e-10);
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void DiffTime_MissingSecondArgument_ThrowsException()
    {
        CallOSFunction("difftime", LuaValue.Integer(1000));
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void DiffTime_NonNumericFirstArgument_ThrowsException()
    {
        CallOSFunction("difftime", LuaValue.String("not a number"), LuaValue.Integer(1000));
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void DiffTime_NonNumericSecondArgument_ThrowsException()
    {
        CallOSFunction("difftime", LuaValue.Integer(1000), LuaValue.String("not a number"));
    }

    #endregion

    #region GetEnv Function Tests - Environment Variables

    [TestMethod]
    public void GetEnv_ExistingVariable_ReturnsValue()
    {
        // Use PATH which should exist on all systems
        var result = CallOSFunction("getenv", LuaValue.String("PATH"));
        
        Assert.IsTrue(result.IsString);
        var pathValue = result.AsString();
        Assert.IsFalse(string.IsNullOrEmpty(pathValue));
    }

    [TestMethod]
    public void GetEnv_NonExistentVariable_ReturnsNil()
    {
        var result = CallOSFunction("getenv", LuaValue.String("FLUA_NONEXISTENT_VAR_12345"));
        
        Assert.IsTrue(result.IsNil);
    }

    [TestMethod]
    public void GetEnv_EmptyVariableName_ReturnsNil()
    {
        var result = CallOSFunction("getenv", LuaValue.String(""));
        
        Assert.IsTrue(result.IsNil);
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void GetEnv_NoArguments_ThrowsException()
    {
        CallOSFunction("getenv");
    }

    [TestMethod]
    public void GetEnv_NonStringArgument_ConvertsToString()
    {
        // Should convert number to string for lookup
        var result = CallOSFunction("getenv", LuaValue.Integer(123));
        
        Assert.IsTrue(result.IsNil); // Unlikely to have env var named "123"
    }

    #endregion

    #region SetLocale Function Tests - Locale Management

    [TestMethod]
    public void SetLocale_NoArguments_ReturnsCurrentLocale()
    {
        var result = CallOSFunction("setlocale");
        
        Assert.IsTrue(result.IsString);
        var locale = result.AsString();
        Assert.IsFalse(string.IsNullOrEmpty(locale));
    }

    [TestMethod]
    public void SetLocale_EmptyString_ReturnsCurrentLocale()
    {
        var result = CallOSFunction("setlocale", LuaValue.String(""));
        
        Assert.IsTrue(result.IsString);
    }

    [TestMethod]
    public void SetLocale_CLocale_ReturnsC()
    {
        var result = CallOSFunction("setlocale", LuaValue.String("C"));
        
        Assert.IsTrue(result.IsString);
        var locale = result.AsString();
        Assert.IsTrue(locale == "C" || locale == "" || locale.Contains("Invariant"));
    }

    [TestMethod]
    public void SetLocale_POSIXLocale_ReturnsC()
    {
        var result = CallOSFunction("setlocale", LuaValue.String("POSIX"));
        
        Assert.IsTrue(result.IsString);
        var locale = result.AsString();
        Assert.IsTrue(locale == "C" || locale == "" || locale.Contains("Invariant"));
    }

    [TestMethod]
    public void SetLocale_InvalidLocale_ReturnsNil()
    {
        var result = CallOSFunction("setlocale", LuaValue.String("invalid-locale-xyz"));
        
        Assert.IsTrue(result.IsNil);
    }

    [TestMethod]
    public void SetLocale_AllCategory_SetsLocale()
    {
        var result = CallOSFunction("setlocale", LuaValue.String("C"), LuaValue.String("all"));
        
        Assert.IsTrue(result.IsString);
    }

    [TestMethod]
    public void SetLocale_NumericCategory_SetsLocale()
    {
        var result = CallOSFunction("setlocale", LuaValue.String("C"), LuaValue.String("numeric"));
        
        Assert.IsTrue(result.IsString);
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void SetLocale_InvalidCategory_ThrowsException()
    {
        CallOSFunction("setlocale", LuaValue.String("C"), LuaValue.String("invalid"));
    }

    #endregion

    #region Remove Function Tests - File System Operations

    [TestMethod]
    public void Remove_ExistingFile_ReturnsTrue()
    {
        var tempFile = CreateTempFile("test content");
        
        var result = CallOSFunction("remove", LuaValue.String(tempFile));
        
        Assert.IsTrue(result.AsBoolean());
        Assert.IsFalse(File.Exists(tempFile));
    }

    [TestMethod]
    public void Remove_NonExistentFile_ReturnsNilWithMessage()
    {
        var results = CallOSFunctionMultiple("remove", LuaValue.String("/nonexistent/file.txt"));
        
        Assert.AreEqual(2, results.Length);
        Assert.IsTrue(results[0].IsNil);
        Assert.IsTrue(results[1].IsString);
        Assert.IsTrue(results[1].AsString().Contains("No such file"));
    }

    [TestMethod]
    public void Remove_EmptyDirectory_ReturnsTrue()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"FLuaTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var result = CallOSFunction("remove", LuaValue.String(tempDir));
            
            Assert.IsTrue(result.AsBoolean());
            Assert.IsFalse(Directory.Exists(tempDir));
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir);
        }
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Remove_NoArguments_ThrowsException()
    {
        CallOSFunction("remove");
    }

    #endregion

    #region TmpName Function Tests - Temporary File Generation

    [TestMethod]
    public void TmpName_ReturnsValidTempFileName()
    {
        var result = CallOSFunction("tmpname");
        
        Assert.IsTrue(result.IsString);
        var tempFileName = result.AsString();
        
        Assert.IsFalse(string.IsNullOrEmpty(tempFileName));
        Assert.IsTrue(Path.IsPathFullyQualified(tempFileName));
        
        // Clean up the temp file if it was created
        if (File.Exists(tempFileName))
        {
            File.Delete(tempFileName);
        }
    }

    [TestMethod]
    public void TmpName_MultipleCallsReturnDifferentNames()
    {
        var result1 = CallOSFunction("tmpname");
        var result2 = CallOSFunction("tmpname");
        
        Assert.IsTrue(result1.IsString);
        Assert.IsTrue(result2.IsString);
        
        var tempFile1 = result1.AsString();
        var tempFile2 = result2.AsString();
        
        Assert.AreNotEqual(tempFile1, tempFile2);
        
        // Clean up
        foreach (var file in new[] { tempFile1, tempFile2 })
        {
            if (File.Exists(file))
                File.Delete(file);
        }
    }

    [TestMethod]
    public void TmpName_NoArguments_Works()
    {
        var result = CallOSFunction("tmpname");
        Assert.IsTrue(result.IsString);
    }

    [TestMethod]
    public void TmpName_WithArguments_IgnoresThem()
    {
        var result = CallOSFunction("tmpname", LuaValue.String("ignored"));
        Assert.IsTrue(result.IsString);
    }

    #endregion

    #region Boundary Value Tests - Edge Cases

    [TestMethod]
    public void Time_UnixEpoch_ReturnsZero()
    {
        var epochTable = new LuaTable();
        epochTable.Set(LuaValue.String("year"), LuaValue.Integer(1970));
        epochTable.Set(LuaValue.String("month"), LuaValue.Integer(1));
        epochTable.Set(LuaValue.String("day"), LuaValue.Integer(1));
        epochTable.Set(LuaValue.String("hour"), LuaValue.Integer(0));
        epochTable.Set(LuaValue.String("min"), LuaValue.Integer(0));
        epochTable.Set(LuaValue.String("sec"), LuaValue.Integer(0));
        
        var result = CallOSFunction("time", LuaValue.Table(epochTable));
        
        Assert.IsTrue(result.IsInteger);
        Assert.AreEqual(0L, result.AsInteger());
    }

    [TestMethod]
    public void Date_UnixEpoch_ReturnsCorrectDate()
    {
        var result = CallOSFunction("date", LuaValue.String("!*t"), LuaValue.Integer(0));
        
        Assert.IsTrue(result.IsTable);
        var dateTable = result.AsTable<LuaTable>();
        
        Assert.AreEqual(1970L, dateTable.Get(LuaValue.String("year")).AsInteger());
        Assert.AreEqual(1L, dateTable.Get(LuaValue.String("month")).AsInteger());
        Assert.AreEqual(1L, dateTable.Get(LuaValue.String("day")).AsInteger());
        Assert.AreEqual(0L, dateTable.Get(LuaValue.String("hour")).AsInteger());
        Assert.AreEqual(0L, dateTable.Get(LuaValue.String("min")).AsInteger());
        Assert.AreEqual(0L, dateTable.Get(LuaValue.String("sec")).AsInteger());
    }

    [TestMethod]
    public void DiffTime_MaxValues_HandlesLargeNumbers()
    {
        var result = CallOSFunction("difftime", LuaValue.Integer(long.MaxValue), LuaValue.Integer(0));
        
        Assert.IsTrue(result.IsNumber);
        Assert.AreEqual((double)long.MaxValue, result.AsDouble());
    }

    [TestMethod]
    public void DiffTime_MinValues_HandlesNegativeNumbers()
    {
        var result = CallOSFunction("difftime", LuaValue.Integer(long.MinValue), LuaValue.Integer(0));
        
        Assert.IsTrue(result.IsNumber);
        Assert.AreEqual((double)long.MinValue, result.AsDouble());
    }

    #endregion

    #region Helper Methods

    private string CreateTempFile(string content)
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, content);
        _tempFiles.Add(tempFile);
        return tempFile;
    }

    private LuaValue CallOSFunction(string functionName, params LuaValue[] args)
    {
        var osTable = _env.GetVariable("os").AsTable<LuaTable>();
        var function = osTable.Get(LuaValue.String(functionName)).AsFunction();
        var results = function.Call(args);
        return results.Length > 0 ? results[0] : LuaValue.Nil;
    }

    private LuaValue[] CallOSFunctionMultiple(string functionName, params LuaValue[] args)
    {
        var osTable = _env.GetVariable("os").AsTable<LuaTable>();
        var function = osTable.Get(LuaValue.String(functionName)).AsFunction();
        return function.Call(args);
    }

    #endregion
}