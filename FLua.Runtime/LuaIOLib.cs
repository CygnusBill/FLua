using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FLua.Runtime
{
    /// <summary>
    /// Lua IO Library implementation (basic file operations)
    /// </summary>
    public static class LuaIOLib
    {
        private static readonly Dictionary<string, Stream> _openFiles = new Dictionary<string, Stream>();
        private static Stream? _defaultInput = Console.OpenStandardInput();
        private static Stream? _defaultOutput = Console.OpenStandardOutput();
        
        /// <summary>
        /// Adds the io library to the Lua environment
        /// </summary>
        public static void AddIOLibrary(LuaEnvironment env)
        {
            var ioTable = new LuaTable();
            
            // File operations
            ioTable.Set(new LuaString("open"), new BuiltinFunction(Open));
            ioTable.Set(new LuaString("close"), new BuiltinFunction(Close));
            ioTable.Set(new LuaString("read"), new BuiltinFunction(Read));
            ioTable.Set(new LuaString("write"), new BuiltinFunction(Write));
            ioTable.Set(new LuaString("flush"), new BuiltinFunction(Flush));
            
            // Standard streams
            ioTable.Set(new LuaString("input"), new BuiltinFunction(Input));
            ioTable.Set(new LuaString("output"), new BuiltinFunction(Output));
            ioTable.Set(new LuaString("stderr"), new LuaFileHandle(Console.OpenStandardError(), "stderr", "w"));
            
            // Utility functions
            ioTable.Set(new LuaString("type"), new BuiltinFunction(Type));
            ioTable.Set(new LuaString("lines"), new BuiltinFunction(Lines));
            
            env.SetVariable("io", ioTable);
        }
        
        #region File Operations
        
        private static LuaValue[] Open(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'open' (string expected)");
            
            var filename = args[0].AsString;
            var mode = args.Length > 1 ? args[1].AsString : "r";
            
            try
            {
                Stream stream;
                switch (mode)
                {
                    case "r":
                        stream = File.OpenRead(filename);
                        break;
                    case "w":
                        stream = File.OpenWrite(filename);
                        break;
                    case "a":
                        stream = File.Open(filename, FileMode.Append, FileAccess.Write);
                        break;
                    case "r+":
                        stream = File.Open(filename, FileMode.Open, FileAccess.ReadWrite);
                        break;
                    case "w+":
                        stream = File.Open(filename, FileMode.Create, FileAccess.ReadWrite);
                        break;
                    case "a+":
                        stream = File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                        stream.Seek(0, SeekOrigin.End);
                        break;
                    default:
                        throw new LuaRuntimeException($"invalid mode '{mode}' for 'open'");
                }
                
                var fileHandle = CreateFileHandle(stream, filename, mode);
                return new LuaValue[] { fileHandle };
            }
            catch (Exception ex)
            {
                return new LuaValue[] { LuaNil.Instance, new LuaString(ex.Message) };
            }
        }
        
        /// <summary>
        /// Creates a file handle as a table with methods
        /// </summary>
        private static LuaTable CreateFileHandle(Stream stream, string filename, string mode)
        {
            var fileHandle = new LuaTable();
            var handle = new LuaFileHandle(stream, filename, mode);
            
            // Add methods to the file handle table
            fileHandle.Set(new LuaString("write"), new BuiltinFunction(args =>
            {
                if (handle.IsClosed)
                    throw new LuaRuntimeException("attempt to use a closed file");
                
                try
                {
                    using var writer = new StreamWriter(handle.Stream, Encoding.UTF8, 1024, true);
                    
                    // Skip the first argument (self)
                    for (int i = 1; i < args.Length; i++)
                    {
                        writer.Write(args[i].AsString);
                    }
                    
                    writer.Flush();
                    return new[] { fileHandle };
                }
                catch (Exception ex)
                {
                    return new LuaValue[] { LuaNil.Instance, new LuaString(ex.Message) };
                }
            }));
            
            fileHandle.Set(new LuaString("read"), new BuiltinFunction(args =>
            {
                if (handle.IsClosed)
                    throw new LuaRuntimeException("attempt to use a closed file");
                
                try
                {
                    using var reader = new StreamReader(handle.Stream, Encoding.UTF8, false, 1024, true);
                    
                    if (args.Length <= 1) // Only self argument
                    {
                        // Read a line by default
                        var line = reader.ReadLine();
                        return line != null ? new LuaValue[] { new LuaString(line) } : new LuaValue[] { LuaNil.Instance };
                    }
                    
                    var results = new List<LuaValue>();
                    
                    for (int i = 1; i < args.Length; i++) // Skip self
                    {
                        var format = args[i];
                        
                        if (format is LuaString str)
                        {
                            switch (str.Value)
                            {
                                case "*l":
                                case "*line":
                                    var line = reader.ReadLine();
                                    results.Add(line != null ? new LuaString(line) : LuaNil.Instance);
                                    break;
                                case "*a":
                                case "*all":
                                    var all = reader.ReadToEnd();
                                    results.Add(new LuaString(all));
                                    break;
                                default:
                                    results.Add(LuaNil.Instance);
                                    break;
                            }
                        }
                        else if (format.AsInteger.HasValue)
                        {
                            // Read specified number of characters
                            var count = (int)format.AsInteger.Value;
                            var buffer = new char[count];
                            var actualRead = reader.Read(buffer, 0, count);
                            if (actualRead > 0)
                                results.Add(new LuaString(new string(buffer, 0, actualRead)));
                            else
                                results.Add(LuaNil.Instance);
                        }
                        else
                        {
                            results.Add(LuaNil.Instance);
                        }
                    }
                    
                    return results.ToArray();
                }
                catch (Exception ex)
                {
                    throw new LuaRuntimeException($"read error: {ex.Message}");
                }
            }));
            
            fileHandle.Set(new LuaString("close"), new BuiltinFunction(args =>
            {
                try
                {
                    handle.Close();
                    return new LuaValue[] { new LuaBoolean(true) };
                }
                catch (Exception ex)
                {
                    return new LuaValue[] { LuaNil.Instance, new LuaString(ex.Message) };
                }
            }));
            
            fileHandle.Set(new LuaString("flush"), new BuiltinFunction(args =>
            {
                if (handle.IsClosed)
                    throw new LuaRuntimeException("attempt to use a closed file");
                
                try
                {
                    handle.Stream.Flush();
                    return new[] { new LuaBoolean(true) };
                }
                catch (Exception ex)
                {
                    return new LuaValue[] { LuaNil.Instance, new LuaString(ex.Message) };
                }
            }));
            
            // Store the actual handle for internal use
            fileHandle.Set(new LuaString("_handle"), handle);
            
            return fileHandle;
        }
        
        private static LuaValue[] Close(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                // Close default output
                _defaultOutput?.Close();
                return new LuaValue[] { new LuaBoolean(true) };
            }
            
            var fileHandle = args[0];
            if (fileHandle is LuaTable table)
            {
                var handleValue = table.Get(new LuaString("_handle"));
                if (handleValue is LuaFileHandle handle)
                {
                    try
                    {
                        handle.Close();
                        return new LuaValue[] { new LuaBoolean(true) };
                    }
                    catch (Exception ex)
                    {
                        return new LuaValue[] { LuaNil.Instance, new LuaString(ex.Message) };
                    }
                }
            }
            else if (fileHandle is LuaFileHandle handle)
            {
                try
                {
                    handle.Close();
                    return new LuaValue[] { new LuaBoolean(true) };
                }
                catch (Exception ex)
                {
                    return new LuaValue[] { LuaNil.Instance, new LuaString(ex.Message) };
                }
            }
            
            throw new LuaRuntimeException("bad argument #1 to 'close' (file handle expected)");
        }
        
        private static LuaValue[] Read(LuaValue[] args)
        {
            Stream? stream = _defaultInput;
            int startIndex = 0;
            
            // Check if first argument is a file handle
            if (args.Length > 0 && args[0] is LuaFileHandle handle)
            {
                stream = handle.Stream;
                startIndex = 1;
            }
            
            if (stream == null)
                throw new LuaRuntimeException("attempt to use a closed file");
            
            try
            {
                if (startIndex >= args.Length)
                {
                    // Read a line by default
                    using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
                    var line = reader.ReadLine();
                    return line != null ? new LuaValue[] { new LuaString(line) } : new LuaValue[] { LuaNil.Instance };
                }
                
                var results = new List<LuaValue>();
                using var reader2 = new StreamReader(stream, Encoding.UTF8, false, 1024, true);
                
                for (int i = startIndex; i < args.Length; i++)
                {
                    var format = args[i];
                    
                    if (format is LuaString str)
                    {
                        switch (str.Value)
                        {
                            case "*l":
                            case "*line":
                                var line = reader2.ReadLine();
                                results.Add(line != null ? new LuaString(line) : LuaNil.Instance);
                                break;
                            case "*a":
                            case "*all":
                                var all = reader2.ReadToEnd();
                                results.Add(new LuaString(all));
                                break;
                            case "*n":
                            case "*number":
                                // Try to read a number - simplified implementation
                                var numStr = "";
                                int ch;
                                while ((ch = reader2.Read()) != -1 && (char.IsDigit((char)ch) || ch == '.' || ch == '-' || ch == '+'))
                                {
                                    numStr += (char)ch;
                                }
                                if (double.TryParse(numStr, out var num))
                                    results.Add(new LuaNumber(num));
                                else
                                    results.Add(LuaNil.Instance);
                                break;
                            default:
                                results.Add(LuaNil.Instance);
                                break;
                        }
                    }
                    else if (format.AsInteger.HasValue)
                    {
                        // Read specified number of characters
                        var count = (int)format.AsInteger.Value;
                        var buffer = new char[count];
                        var actualRead = reader2.Read(buffer, 0, count);
                        if (actualRead > 0)
                            results.Add(new LuaString(new string(buffer, 0, actualRead)));
                        else
                            results.Add(LuaNil.Instance);
                    }
                    else
                    {
                        results.Add(LuaNil.Instance);
                    }
                }
                
                return results.ToArray();
            }
            catch (Exception ex)
            {
                throw new LuaRuntimeException($"read error: {ex.Message}");
            }
        }
        
        private static LuaValue[] Write(LuaValue[] args)
        {
            Stream? stream = _defaultOutput;
            int startIndex = 0;
            
            // Check if first argument is a file handle
            if (args.Length > 0 && args[0] is LuaFileHandle handle)
            {
                stream = handle.Stream;
                startIndex = 1;
            }
            
            if (stream == null)
                throw new LuaRuntimeException("attempt to use a closed file");
            
            try
            {
                using var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true);
                
                for (int i = startIndex; i < args.Length; i++)
                {
                    writer.Write(args[i].AsString);
                }
                
                writer.Flush();
                return new[] { new LuaBoolean(true) };
            }
            catch (Exception ex)
            {
                return new LuaValue[] { LuaNil.Instance, new LuaString(ex.Message) };
            }
        }
        
        private static LuaValue[] Flush(LuaValue[] args)
        {
            Stream? stream = _defaultOutput;
            
            if (args.Length > 0 && args[0] is LuaFileHandle handle)
            {
                stream = handle.Stream;
            }
            
            if (stream == null)
                throw new LuaRuntimeException("attempt to use a closed file");
            
            try
            {
                stream.Flush();
                return new[] { new LuaBoolean(true) };
            }
            catch (Exception ex)
            {
                return new LuaValue[] { LuaNil.Instance, new LuaString(ex.Message) };
            }
        }
        
        #endregion
        
        #region Standard Streams
        
        private static LuaValue[] Input(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                // Return current input stream
                return new LuaValue[] { new LuaFileHandle(_defaultInput ?? Stream.Null, "stdin", "r") };
            }
            
            // Set new input stream
            var newInput = args[0];
            if (newInput is LuaString filename)
            {
                try
                {
                    _defaultInput?.Close();
                    _defaultInput = File.OpenRead(filename.Value);
                    return new LuaValue[] { new LuaFileHandle(_defaultInput, filename.Value, "r") };
                }
                catch (Exception ex)
                {
                    throw new LuaRuntimeException($"cannot open '{filename.Value}': {ex.Message}");
                }
            }
            else if (newInput is LuaFileHandle handle)
            {
                _defaultInput = handle.Stream;
                return new[] { handle };
            }
            
            throw new LuaRuntimeException("bad argument to 'input' (string or file handle expected)");
        }
        
        private static LuaValue[] Output(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                // Return current output stream
                return new LuaValue[] { new LuaFileHandle(_defaultOutput ?? Stream.Null, "stdout", "w") };
            }
            
            // Set new output stream
            var newOutput = args[0];
            if (newOutput is LuaString filename)
            {
                try
                {
                    _defaultOutput?.Close();
                    _defaultOutput = File.OpenWrite(filename.Value);
                    return new LuaValue[] { new LuaFileHandle(_defaultOutput, filename.Value, "w") };
                }
                catch (Exception ex)
                {
                    throw new LuaRuntimeException($"cannot open '{filename.Value}': {ex.Message}");
                }
            }
            else if (newOutput is LuaFileHandle handle)
            {
                _defaultOutput = handle.Stream;
                return new[] { handle };
            }
            
            throw new LuaRuntimeException("bad argument to 'output' (string or file handle expected)");
        }
        
        #endregion
        
        #region Utility Functions
        
        private static LuaValue[] Type(LuaValue[] args)
        {
            if (args.Length == 0)
                return new[] { LuaNil.Instance };
            
            var value = args[0];
            if (value is LuaTable table)
            {
                var handleValue = table.Get(new LuaString("_handle"));
                if (handleValue is LuaFileHandle handle)
                {
                    if (handle.IsClosed)
                        return new LuaValue[] { new LuaString("closed file") };
                    else
                        return new LuaValue[] { new LuaString("file") };
                }
            }
            else if (value is LuaFileHandle handle)
            {
                if (handle.IsClosed)
                    return new LuaValue[] { new LuaString("closed file") };
                else
                    return new LuaValue[] { new LuaString("file") };
            }
            
            return new[] { LuaNil.Instance };
        }
        
        private static LuaValue[] Lines(LuaValue[] args)
        {
            string filename = "";
            if (args.Length > 0)
                filename = args[0].AsString;
            
            try
            {
                Stream stream;
                bool shouldClose = false;
                
                if (string.IsNullOrEmpty(filename))
                {
                    stream = _defaultInput ?? Console.OpenStandardInput();
                }
                else
                {
                    stream = File.OpenRead(filename);
                    shouldClose = true;
                }
                
                var reader = new StreamReader(stream);
                var lines = new List<string>();
                
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
                
                if (shouldClose)
                {
                    reader.Close();
                    stream.Close();
                }
                
                // Return an iterator function
                var index = 0;
                var iterator = new LuaUserFunction(iterArgs =>
                {
                    if (index < lines.Count)
                    {
                        return new[] { new LuaString(lines[index++]) };
                    }
                    return new LuaValue[0];
                });
                
                return new LuaValue[] { iterator };
            }
            catch (Exception ex)
            {
                throw new LuaRuntimeException($"cannot open '{filename}': {ex.Message}");
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Represents a Lua file handle
    /// </summary>
    public class LuaFileHandle : LuaValue
    {
        public Stream Stream { get; private set; }
        public string Filename { get; }
        public string Mode { get; }
        public bool IsClosed { get; private set; }
        
        public LuaFileHandle(Stream stream, string filename, string mode)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            Filename = filename;
            Mode = mode;
            IsClosed = false;
        }
        
        public void Close()
        {
            if (!IsClosed)
            {
                Stream.Close();
                IsClosed = true;
            }
        }
        
        public override string ToString() => $"file ({Filename})";
    }
} 