using FLua.Runtime;
using FLua.Common;
using System.Text;

namespace FLua.Runtime.LibraryTests;

/// <summary>
/// Comprehensive unit tests for LuaIOLib using Lee Copeland's testing methodology.
/// Includes boundary value analysis, equivalence class partitioning, and error path testing.
/// Tests use temporary files for isolation and safety.
/// </summary>
[TestClass]
public class LuaIOLibTests
{
    private LuaEnvironment _env = null!;
    private List<string> _tempFiles = new List<string>();
    private string _tempDir = string.Empty;

    [TestInitialize]
    public void Setup()
    {
        _env = new LuaEnvironment();
        LuaIOLib.AddIOLibrary(_env);
        
        // Create a temporary directory for test files
        _tempDir = Path.Combine(Path.GetTempPath(), $"FLuaIOTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        // Clean up all temporary files and directory
        try
        {
            foreach (var file in _tempFiles)
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    #region Open Function Tests - Boundary Value Analysis

    [TestMethod]
    public void Open_ReadMode_OpensExistingFile()
    {
        var testFile = CreateTempFile("hello world");
        var result = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("r"));
        
        Assert.IsTrue(result.IsTable);
        var fileTable = result.AsTable<LuaTable>();
        Assert.IsTrue(fileTable.Get(LuaValue.String("read")).IsFunction);
    }

    [TestMethod]
    public void Open_WriteMode_CreatesNewFile()
    {
        var testFile = GetTempFilePath();
        var result = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("w"));
        
        Assert.IsTrue(result.IsTable);
        var fileTable = result.AsTable<LuaTable>();
        Assert.IsTrue(fileTable.Get(LuaValue.String("write")).IsFunction);
        
        // Close the file
        var closeFunc = fileTable.Get(LuaValue.String("close")).AsFunction();
        closeFunc.Call(new[] { result });
        
        Assert.IsTrue(File.Exists(testFile));
    }

    [TestMethod]
    public void Open_AppendMode_AppendsToFile()
    {
        var testFile = CreateTempFile("initial");
        var result = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("a"));
        
        Assert.IsTrue(result.IsTable);
        var fileTable = result.AsTable<LuaTable>();
        
        // Write to the file
        var writeFunc = fileTable.Get(LuaValue.String("write")).AsFunction();
        writeFunc.Call(new[] { result, LuaValue.String(" appended") });
        
        // Close the file
        var closeFunc = fileTable.Get(LuaValue.String("close")).AsFunction();
        closeFunc.Call(new[] { result });
        
        var content = File.ReadAllText(testFile);
        Assert.AreEqual("initial appended", content);
    }

    [TestMethod]
    public void Open_ReadWriteMode_AllowsBothOperations()
    {
        var testFile = CreateTempFile("test");
        var result = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("r+"));
        
        Assert.IsTrue(result.IsTable);
        var fileTable = result.AsTable<LuaTable>();
        Assert.IsTrue(fileTable.Get(LuaValue.String("read")).IsFunction);
        Assert.IsTrue(fileTable.Get(LuaValue.String("write")).IsFunction);
        
        // Close the file
        var closeFunc = fileTable.Get(LuaValue.String("close")).AsFunction();
        closeFunc.Call(new[] { result });
    }

    [TestMethod]
    public void Open_DefaultMode_UsesReadMode()
    {
        var testFile = CreateTempFile("content");
        var result = CallIOFunction("open", LuaValue.String(testFile));
        
        Assert.IsTrue(result.IsTable);
        var fileTable = result.AsTable<LuaTable>();
        Assert.IsTrue(fileTable.Get(LuaValue.String("read")).IsFunction);
        
        // Close the file
        var closeFunc = fileTable.Get(LuaValue.String("close")).AsFunction();
        closeFunc.Call(new[] { result });
    }

    [TestMethod]
    public void Open_NonExistentFile_ReturnsNilWithError()
    {
        var result = CallIOFunction("open", LuaValue.String("/nonexistent/path/file.txt"), LuaValue.String("r"));
        
        Assert.IsTrue(result.IsNil);
    }

    [TestMethod]
    public void Open_InvalidMode_ReturnsNilWithError()
    {
        var testFile = CreateTempFile("test");
        var result = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("invalid"));
        
        Assert.IsTrue(result.IsNil);
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Open_NoFilename_ThrowsException()
    {
        CallIOFunction("open");
    }

    [TestMethod]
    public void Open_EmptyFilename_ReturnsNilWithError()
    {
        var result = CallIOFunction("open", LuaValue.String(""), LuaValue.String("r"));
        Assert.IsTrue(result.IsNil);
    }

    #endregion

    #region File Handle Read Tests - Equivalence Class Partitioning

    [TestMethod]
    public void FileHandle_Read_DefaultReadsLine()
    {
        var testFile = CreateTempFile("line1\nline2\nline3");
        var fileHandle = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("r"));
        var readFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("read")).AsFunction();
        
        var result = readFunc.Call(new[] { fileHandle });
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual("line1", result[0].AsString());
        
        // Close the file
        var closeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("close")).AsFunction();
        closeFunc.Call(new[] { fileHandle });
    }

    [TestMethod]
    public void FileHandle_Read_LineFormat()
    {
        var testFile = CreateTempFile("first line\nsecond line");
        var fileHandle = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("r"));
        var readFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("read")).AsFunction();
        
        var result = readFunc.Call(new[] { fileHandle, LuaValue.String("*l") });
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual("first line", result[0].AsString());
        
        // Close the file
        var closeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("close")).AsFunction();
        closeFunc.Call(new[] { fileHandle });
    }

    [TestMethod]
    public void FileHandle_Read_AllFormat()
    {
        var content = "line1\nline2\nline3";
        var testFile = CreateTempFile(content);
        var fileHandle = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("r"));
        var readFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("read")).AsFunction();
        
        var result = readFunc.Call(new[] { fileHandle, LuaValue.String("*a") });
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(content, result[0].AsString());
        
        // Close the file
        var closeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("close")).AsFunction();
        closeFunc.Call(new[] { fileHandle });
    }

    [TestMethod]
    public void FileHandle_Read_NumericCount()
    {
        var testFile = CreateTempFile("hello world");
        var fileHandle = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("r"));
        var readFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("read")).AsFunction();
        
        var result = readFunc.Call(new[] { fileHandle, LuaValue.Integer(5) });
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual("hello", result[0].AsString());
        
        // Close the file
        var closeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("close")).AsFunction();
        closeFunc.Call(new[] { fileHandle });
    }

    [TestMethod]
    public void FileHandle_Read_ZeroCount_ReturnsEmpty()
    {
        var testFile = CreateTempFile("hello");
        var fileHandle = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("r"));
        var readFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("read")).AsFunction();
        
        var result = readFunc.Call(new[] { fileHandle, LuaValue.Integer(0) });
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual("", result[0].AsString());
        
        // Close the file
        var closeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("close")).AsFunction();
        closeFunc.Call(new[] { fileHandle });
    }

    [TestMethod]
    public void FileHandle_Read_BeyondEOF_ReturnsNil()
    {
        var testFile = CreateTempFile("short");
        var fileHandle = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("r"));
        var readFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("read")).AsFunction();
        
        // Read more than available
        var result = readFunc.Call(new[] { fileHandle, LuaValue.Integer(100) });
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual("short", result[0].AsString());
        
        // Try to read again
        result = readFunc.Call(new[] { fileHandle, LuaValue.Integer(1) });
        Assert.AreEqual(1, result.Length);
        Assert.IsTrue(result[0].IsNil);
        
        // Close the file
        var closeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("close")).AsFunction();
        closeFunc.Call(new[] { fileHandle });
    }

    [TestMethod]
    public void FileHandle_Read_EmptyFile_ReturnsNil()
    {
        var testFile = CreateTempFile("");
        var fileHandle = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("r"));
        var readFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("read")).AsFunction();
        
        var result = readFunc.Call(new[] { fileHandle });
        Assert.AreEqual(1, result.Length);
        Assert.IsTrue(result[0].IsNil);
        
        // Close the file
        var closeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("close")).AsFunction();
        closeFunc.Call(new[] { fileHandle });
    }

    #endregion

    #region File Handle Write Tests - Error Conditions

    [TestMethod]
    public void FileHandle_Write_SingleString()
    {
        var testFile = GetTempFilePath();
        var fileHandle = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("w"));
        var writeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("write")).AsFunction();
        
        var result = writeFunc.Call(new[] { fileHandle, LuaValue.String("hello") });
        Assert.AreEqual(1, result.Length);
        Assert.IsTrue(result[0].IsTable); // Returns file handle
        
        // Close the file
        var closeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("close")).AsFunction();
        closeFunc.Call(new[] { fileHandle });
        
        var content = File.ReadAllText(testFile);
        Assert.AreEqual("hello", content);
    }

    [TestMethod]
    public void FileHandle_Write_MultipleStrings()
    {
        var testFile = GetTempFilePath();
        var fileHandle = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("w"));
        var writeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("write")).AsFunction();
        
        var result = writeFunc.Call(new[] { 
            fileHandle, 
            LuaValue.String("hello"), 
            LuaValue.String(" "), 
            LuaValue.String("world") 
        });
        
        Assert.AreEqual(1, result.Length);
        Assert.IsTrue(result[0].IsTable);
        
        // Close the file
        var closeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("close")).AsFunction();
        closeFunc.Call(new[] { fileHandle });
        
        var content = File.ReadAllText(testFile);
        Assert.AreEqual("hello world", content);
    }

    [TestMethod]
    public void FileHandle_Write_Numbers_ConvertsToString()
    {
        var testFile = GetTempFilePath();
        var fileHandle = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("w"));
        var writeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("write")).AsFunction();
        
        writeFunc.Call(new[] { fileHandle, LuaValue.Integer(42), LuaValue.Float(3.14) });
        
        // Close the file
        var closeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("close")).AsFunction();
        closeFunc.Call(new[] { fileHandle });
        
        var content = File.ReadAllText(testFile);
        Assert.IsTrue(content.Contains("42"));
        Assert.IsTrue(content.Contains("3.14"));
    }

    [TestMethod]
    public void FileHandle_Write_EmptyString()
    {
        var testFile = GetTempFilePath();
        var fileHandle = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("w"));
        var writeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("write")).AsFunction();
        
        writeFunc.Call(new[] { fileHandle, LuaValue.String("") });
        
        // Close the file
        var closeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("close")).AsFunction();
        closeFunc.Call(new[] { fileHandle });
        
        var content = File.ReadAllText(testFile);
        Assert.AreEqual("", content);
    }

    #endregion

    #region File Handle Close Tests

    [TestMethod]
    public void FileHandle_Close_SuccessfulClose()
    {
        var testFile = CreateTempFile("test");
        var fileHandle = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("r"));
        var closeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("close")).AsFunction();
        
        var result = closeFunc.Call(new[] { fileHandle });
        Assert.AreEqual(1, result.Length);
        Assert.IsTrue(result[0].AsBoolean());
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void FileHandle_ReadAfterClose_ThrowsException()
    {
        var testFile = CreateTempFile("test");
        var fileHandle = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("r"));
        var readFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("read")).AsFunction();
        var closeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("close")).AsFunction();
        
        // Close the file
        closeFunc.Call(new[] { fileHandle });
        
        // Try to read from closed file
        readFunc.Call(new[] { fileHandle });
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void FileHandle_WriteAfterClose_ThrowsException()
    {
        var testFile = GetTempFilePath();
        var fileHandle = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("w"));
        var writeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("write")).AsFunction();
        var closeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("close")).AsFunction();
        
        // Close the file
        closeFunc.Call(new[] { fileHandle });
        
        // Try to write to closed file
        writeFunc.Call(new[] { fileHandle, LuaValue.String("test") });
    }

    #endregion

    #region Flush Function Tests

    [TestMethod]
    public void FileHandle_Flush_SuccessfulFlush()
    {
        var testFile = GetTempFilePath();
        var fileHandle = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("w"));
        var flushFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("flush")).AsFunction();
        
        var result = flushFunc.Call(new[] { fileHandle });
        Assert.AreEqual(1, result.Length);
        Assert.IsTrue(result[0].AsBoolean());
        
        // Close the file
        var closeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("close")).AsFunction();
        closeFunc.Call(new[] { fileHandle });
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void FileHandle_FlushAfterClose_ThrowsException()
    {
        var testFile = GetTempFilePath();
        var fileHandle = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("w"));
        var flushFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("flush")).AsFunction();
        var closeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("close")).AsFunction();
        
        // Close the file
        closeFunc.Call(new[] { fileHandle });
        
        // Try to flush closed file
        flushFunc.Call(new[] { fileHandle });
    }

    #endregion

    #region Type Function Tests - File Handle Classification

    [TestMethod]
    public void Type_OpenFileHandle_ReturnsFile()
    {
        var testFile = CreateTempFile("test");
        var fileHandle = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("r"));
        
        var result = CallIOFunction("type", fileHandle);
        Assert.AreEqual("file", result.AsString());
        
        // Close the file
        var closeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("close")).AsFunction();
        closeFunc.Call(new[] { fileHandle });
    }

    [TestMethod]
    public void Type_ClosedFileHandle_ReturnsClosedFile()
    {
        var testFile = CreateTempFile("test");
        var fileHandle = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("r"));
        var closeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("close")).AsFunction();
        
        // Close the file
        closeFunc.Call(new[] { fileHandle });
        
        var result = CallIOFunction("type", fileHandle);
        Assert.AreEqual("closed file", result.AsString());
    }

    [TestMethod]
    public void Type_NonFileHandle_ReturnsNil()
    {
        var result = CallIOFunction("type", LuaValue.String("not a file"));
        Assert.IsTrue(result.IsNil);
    }

    [TestMethod]
    public void Type_NoArguments_ReturnsNil()
    {
        var result = CallIOFunction("type");
        Assert.IsTrue(result.IsNil);
    }

    #endregion

    #region Lines Function Tests - Iterator Pattern

    [TestMethod]
    public void Lines_WithFilename_ReturnsIterator()
    {
        var testFile = CreateTempFile("line1\nline2\nline3");
        var result = CallIOFunction("lines", LuaValue.String(testFile));
        
        Assert.IsTrue(result.IsFunction);
        
        // Test the iterator
        var iterator = result.AsFunction();
        var line1 = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(1, line1.Length);
        Assert.AreEqual("line1", line1[0].AsString());
        
        var line2 = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(1, line2.Length);
        Assert.AreEqual("line2", line2[0].AsString());
        
        var line3 = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(1, line3.Length);
        Assert.AreEqual("line3", line3[0].AsString());
        
        // Should be exhausted
        var end = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(0, end.Length);
    }

    [TestMethod]
    public void Lines_EmptyFile_ReturnsEmptyIterator()
    {
        var testFile = CreateTempFile("");
        var result = CallIOFunction("lines", LuaValue.String(testFile));
        
        Assert.IsTrue(result.IsFunction);
        
        var iterator = result.AsFunction();
        var end = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(0, end.Length);
    }

    [TestMethod]
    public void Lines_SingleLineWithoutNewline_ReturnsOneLine()
    {
        var testFile = CreateTempFile("single line");
        var result = CallIOFunction("lines", LuaValue.String(testFile));
        
        Assert.IsTrue(result.IsFunction);
        
        var iterator = result.AsFunction();
        var line = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(1, line.Length);
        Assert.AreEqual("single line", line[0].AsString());
        
        var end = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(0, end.Length);
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Lines_NonExistentFile_ThrowsException()
    {
        CallIOFunction("lines", LuaValue.String("/nonexistent/file.txt"));
    }

    #endregion

    #region Close Function Tests - Global Close

    [TestMethod]
    public void Close_WithFileHandle_ClosesFile()
    {
        var testFile = CreateTempFile("test");
        var fileHandle = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("r"));
        
        var result = CallIOFunction("close", fileHandle);
        Assert.IsTrue(result.AsBoolean());
    }

    [TestMethod]
    public void Close_NoArguments_ClosesDefaultOutput()
    {
        var result = CallIOFunction("close");
        Assert.IsTrue(result.AsBoolean());
    }

    [TestMethod]
    [ExpectedException(typeof(LuaRuntimeException))]
    public void Close_InvalidHandle_ThrowsException()
    {
        CallIOFunction("close", LuaValue.String("not a file handle"));
    }

    #endregion

    #region Boundary Value Tests - Large Files and Edge Cases

    [TestMethod]
    public void FileHandle_Read_VeryLargeCount()
    {
        var testFile = CreateTempFile("small content");
        var fileHandle = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("r"));
        var readFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("read")).AsFunction();
        
        // Try to read much more than available
        var result = readFunc.Call(new[] { fileHandle, LuaValue.Integer(1000000) });
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual("small content", result[0].AsString());
        
        // Close the file
        var closeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("close")).AsFunction();
        closeFunc.Call(new[] { fileHandle });
    }

    [TestMethod]
    public void FileHandle_Write_VeryLongString()
    {
        var testFile = GetTempFilePath();
        var fileHandle = CallIOFunction("open", LuaValue.String(testFile), LuaValue.String("w"));
        var writeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("write")).AsFunction();
        
        var longString = new string('x', 10000);
        writeFunc.Call(new[] { fileHandle, LuaValue.String(longString) });
        
        // Close the file
        var closeFunc = fileHandle.AsTable<LuaTable>().Get(LuaValue.String("close")).AsFunction();
        closeFunc.Call(new[] { fileHandle });
        
        var content = File.ReadAllText(testFile);
        Assert.AreEqual(longString, content);
    }

    [TestMethod]
    public void Lines_FileWithManyLines_HandlesAll()
    {
        var lines = new List<string>();
        for (int i = 0; i < 1000; i++)
        {
            lines.Add($"line{i}");
        }
        
        var testFile = CreateTempFile(string.Join("\n", lines));
        var result = CallIOFunction("lines", LuaValue.String(testFile));
        
        Assert.IsTrue(result.IsFunction);
        
        var iterator = result.AsFunction();
        for (int i = 0; i < 1000; i++)
        {
            var line = iterator.Call(Array.Empty<LuaValue>());
            Assert.AreEqual(1, line.Length);
            Assert.AreEqual($"line{i}", line[0].AsString());
        }
        
        // Should be exhausted
        var end = iterator.Call(Array.Empty<LuaValue>());
        Assert.AreEqual(0, end.Length);
    }

    #endregion

    #region Helper Methods

    private string CreateTempFile(string content)
    {
        var filePath = GetTempFilePath();
        File.WriteAllText(filePath, content, Encoding.UTF8);
        return filePath;
    }

    private string GetTempFilePath()
    {
        var fileName = $"test_{Guid.NewGuid():N}.txt";
        var filePath = Path.Combine(_tempDir, fileName);
        _tempFiles.Add(filePath);
        return filePath;
    }

    private LuaValue CallIOFunction(string functionName, params LuaValue[] args)
    {
        var ioTable = _env.GetVariable("io").AsTable<LuaTable>();
        var function = ioTable.Get(LuaValue.String(functionName)).AsFunction();
        var results = function.Call(args);
        return results.Length > 0 ? results[0] : LuaValue.Nil;
    }

    private LuaValue[] CallIOFunctionMultiple(string functionName, params LuaValue[] args)
    {
        var ioTable = _env.GetVariable("io").AsTable<LuaTable>();
        var function = ioTable.Get(LuaValue.String(functionName)).AsFunction();
        return function.Call(args);
    }

    #endregion
}
