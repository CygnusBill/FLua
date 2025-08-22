using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FLua.Common;

namespace FLua.Runtime
{
    /// <summary>
    /// Result-based version of Lua I/O Library implementation
    /// Converts all exception-based error handling to Result pattern
    /// </summary>
    public static class ResultLuaIOLib
    {
        private static LuaFileHandle? _defaultInput = null;
        private static LuaFileHandle? _defaultOutput = null;

        #region I/O Library Functions

        public static Result<LuaValue[]> OpenResult(LuaValue[] args)
        {
            if (args.Length == 0 || !args[0].IsString)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'open' (string expected)");

            var filename = args[0].AsString();
            var mode = args.Length > 1 ? args[1].AsString() : "r";

            try
            {
                Stream stream = mode switch
                {
                    "r" => File.OpenRead(filename),
                    "w" => File.OpenWrite(filename),
                    "a" => File.Open(filename, FileMode.Append, FileAccess.Write),
                    "r+" => File.Open(filename, FileMode.Open, FileAccess.ReadWrite),
                    "w+" => File.Open(filename, FileMode.Create, FileAccess.ReadWrite),
                    "a+" => File.Open(filename, FileMode.OpenOrCreate, FileAccess.ReadWrite),
                    _ => throw new ArgumentException($"invalid mode '{mode}' for 'open'")
                };

                if (mode == "a+")
                    stream.Seek(0, SeekOrigin.End);

                var handle = new LuaFileHandle(stream, filename, mode);
                return Result<LuaValue[]>.Success([LuaValue.UserData(handle)]);
            }
            catch (Exception ex)
            {
                return Result<LuaValue[]>.Success([LuaValue.Nil, LuaValue.String(ex.Message)]);
            }
        }

        public static Result<LuaValue[]> CloseResult(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                // Close default output
                if (_defaultOutput != null)
                {
                    try
                    {
                        _defaultOutput.Close();
                        return Result<LuaValue[]>.Success([LuaValue.Boolean(true)]);
                    }
                    catch (Exception ex)
                    {
                        return Result<LuaValue[]>.Success([LuaValue.Boolean(false), LuaValue.String(ex.Message)]);
                    }
                }
                return Result<LuaValue[]>.Success([LuaValue.Boolean(true)]);
            }

            if (!args[0].IsUserData)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'close' (file handle expected)");

            var handle = args[0].AsUserData<LuaFileHandle>();
            if (handle == null)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'close' (file handle expected)");

            try
            {
                if (handle.IsClosed)
                    return Result<LuaValue[]>.Failure("attempt to use a closed file");

                handle.Close();
                return Result<LuaValue[]>.Success([LuaValue.Boolean(true)]);
            }
            catch (Exception ex)
            {
                return Result<LuaValue[]>.Success([LuaValue.Boolean(false), LuaValue.String(ex.Message)]);
            }
        }

        public static Result<LuaValue[]> ReadResult(LuaValue[] args)
        {
            var handle = _defaultInput;
            var startIndex = 0;

            // Check if first argument is a file handle
            if (args.Length > 0 && args[0].IsUserData)
            {
                handle = args[0].AsUserData<LuaFileHandle>();
                startIndex = 1;
            }

            if (handle == null)
            {
                // Create stdin handle
                handle = new LuaFileHandle(Console.OpenStandardInput(), "<stdin>", "r", true);
            }

            if (handle.IsClosed)
                return Result<LuaValue[]>.Failure("attempt to use a closed file");

            try
            {
                using var reader = new StreamReader(handle.Stream, Encoding.UTF8, false, 1024, true);
                var results = new List<LuaValue>();

                // If no format arguments, read a line
                if (args.Length == startIndex)
                {
                    var line = reader.ReadLine();
                    results.Add(line != null ? LuaValue.String(line) : LuaValue.Nil);
                }
                else
                {
                    // Process format arguments
                    for (int i = startIndex; i < args.Length; i++)
                    {
                        var format = args[i];

                        if (format.IsString)
                        {
                            var formatStr = format.AsString();
                            switch (formatStr)
                            {
                                case "*l":
                                case "*line":
                                    var line = reader.ReadLine();
                                    results.Add(line != null ? LuaValue.String(line) : LuaValue.Nil);
                                    break;

                                case "*a":
                                case "*all":
                                    var all = reader.ReadToEnd();
                                    results.Add(LuaValue.String(all));
                                    break;

                                case "*n":
                                case "*number":
                                    // Simplified number reading
                                    var numberLine = reader.ReadLine();
                                    if (numberLine != null && double.TryParse(numberLine.Trim(), out var number))
                                        results.Add(LuaValue.Number(number));
                                    else
                                        results.Add(LuaValue.Nil);
                                    break;

                                default:
                                    results.Add(LuaValue.Nil);
                                    break;
                            }
                        }
                        else if (format.IsInteger)
                        {
                            var count = (int)format.AsInteger();
                            if (count == 0)
                            {
                                results.Add(LuaValue.String(""));
                            }
                            else
                            {
                                var buffer = new char[count];
                                var read = reader.Read(buffer, 0, count);
                                if (read > 0)
                                    results.Add(LuaValue.String(new string(buffer, 0, read)));
                                else
                                    results.Add(LuaValue.Nil);
                            }
                        }
                    }
                }

                return Result<LuaValue[]>.Success(results.ToArray());
            }
            catch (Exception ex)
            {
                return Result<LuaValue[]>.Failure($"read error: {ex.Message}");
            }
        }

        public static Result<LuaValue[]> WriteResult(LuaValue[] args)
        {
            var handle = _defaultOutput;
            var startIndex = 0;

            // Check if first argument is a file handle
            if (args.Length > 0 && args[0].IsUserData)
            {
                handle = args[0].AsUserData<LuaFileHandle>();
                startIndex = 1;
            }

            if (handle == null)
            {
                // Create stdout handle
                handle = new LuaFileHandle(Console.OpenStandardOutput(), "<stdout>", "w", true);
            }

            if (handle.IsClosed)
                return Result<LuaValue[]>.Failure("attempt to use a closed file");

            try
            {
                using var writer = new StreamWriter(handle.Stream, Encoding.UTF8, 1024, true);

                // Write all arguments
                for (int i = startIndex; i < args.Length; i++)
                {
                    var value = args[i];
                    writer.Write(value.AsString());
                }

                writer.Flush();
                return Result<LuaValue[]>.Success([LuaValue.UserData(handle)]);
            }
            catch (Exception ex)
            {
                return Result<LuaValue[]>.Success([LuaValue.Nil, LuaValue.String(ex.Message)]);
            }
        }

        public static Result<LuaValue[]> FlushResult(LuaValue[] args)
        {
            var handle = _defaultOutput;

            // Check if argument is a file handle
            if (args.Length > 0 && args[0].IsUserData)
            {
                handle = args[0].AsUserData<LuaFileHandle>();
            }

            if (handle == null)
            {
                // Flush stdout as default
                try
                {
                    Console.Out.Flush();
                    return Result<LuaValue[]>.Success([LuaValue.Boolean(true)]);
                }
                catch (Exception ex)
                {
                    return Result<LuaValue[]>.Success([LuaValue.Boolean(false), LuaValue.String(ex.Message)]);
                }
            }

            if (handle.IsClosed)
                return Result<LuaValue[]>.Failure("attempt to use a closed file");

            try
            {
                handle.Stream.Flush();
                return Result<LuaValue[]>.Success([LuaValue.Boolean(true)]);
            }
            catch (Exception ex)
            {
                return Result<LuaValue[]>.Success([LuaValue.Boolean(false), LuaValue.String(ex.Message)]);
            }
        }

        public static Result<LuaValue[]> InputResult(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                // Return current input file
                return Result<LuaValue[]>.Success([_defaultInput != null ? LuaValue.UserData(_defaultInput) : LuaValue.Nil]);
            }

            var arg = args[0];

            if (arg.IsString)
            {
                var filename = arg.AsString();
                try
                {
                    var stream = File.OpenRead(filename);
                    var fileHandle = new LuaFileHandle(stream, filename, "r");
                    _defaultInput = fileHandle;
                    return Result<LuaValue[]>.Success([LuaValue.UserData(fileHandle)]);
                }
                catch (Exception ex)
                {
                    return Result<LuaValue[]>.Failure($"cannot open '{filename}': {ex.Message}");
                }
            }
            else if (arg.IsUserData)
            {
                var fileHandle = arg.AsUserData<LuaFileHandle>();
                if (fileHandle != null)
                {
                    _defaultInput = fileHandle;
                    return Result<LuaValue[]>.Success([LuaValue.UserData(fileHandle)]);
                }
            }

            return Result<LuaValue[]>.Failure("bad argument to 'input' (string or file handle expected)");
        }

        public static Result<LuaValue[]> OutputResult(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                // Return current output file
                return Result<LuaValue[]>.Success([_defaultOutput != null ? LuaValue.UserData(_defaultOutput) : LuaValue.Nil]);
            }

            var arg = args[0];

            if (arg.IsString)
            {
                var filename = arg.AsString();
                try
                {
                    var stream = File.OpenWrite(filename);
                    var fileHandle = new LuaFileHandle(stream, filename, "w");
                    _defaultOutput = fileHandle;
                    return Result<LuaValue[]>.Success([LuaValue.UserData(fileHandle)]);
                }
                catch (Exception ex)
                {
                    return Result<LuaValue[]>.Failure($"cannot open '{filename}': {ex.Message}");
                }
            }
            else if (arg.IsUserData)
            {
                var fileHandle = arg.AsUserData<LuaFileHandle>();
                if (fileHandle != null)
                {
                    _defaultOutput = fileHandle;
                    return Result<LuaValue[]>.Success([LuaValue.UserData(fileHandle)]);
                }
            }

            return Result<LuaValue[]>.Failure("bad argument to 'output' (string or file handle expected)");
        }

        public static Result<LuaValue[]> TypeResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Success([LuaValue.Nil]);

            var arg = args[0];

            if (arg.IsUserData && arg.AsUserData<LuaFileHandle>() is LuaFileHandle handle)
            {
                if (handle.IsClosed)
                    return Result<LuaValue[]>.Success([LuaValue.String("closed file")]);
                else
                    return Result<LuaValue[]>.Success([LuaValue.String("file")]);
            }

            return Result<LuaValue[]>.Success([LuaValue.Nil]);
        }

        public static Result<LuaValue[]> LinesResult(LuaValue[] args)
        {
            var filename = args.Length > 0 && args[0].IsString ? args[0].AsString() : null;

            try
            {
                LuaFileHandle handle;
                
                if (filename != null)
                {
                    var stream = File.OpenRead(filename);
                    handle = new LuaFileHandle(stream, filename, "r");
                }
                else
                {
                    handle = _defaultInput ?? new LuaFileHandle(Console.OpenStandardInput(), "<stdin>", "r", true);
                }

                // Create iterator function
                var reader = new StreamReader(handle.Stream);
                var iterator = new BuiltinFunction(iterArgs =>
                {
                    if (handle.IsClosed)
                        return [];

                    try
                    {
                        var line = reader.ReadLine();
                        return line != null ? [LuaValue.String(line)] : [];
                    }
                    catch
                    {
                        return [];
                    }
                });

                return Result<LuaValue[]>.Success([LuaValue.Function(iterator)]);
            }
            catch (Exception ex)
            {
                return Result<LuaValue[]>.Failure($"cannot open '{filename}': {ex.Message}");
            }
        }

        #endregion
    }
}