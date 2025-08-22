using System;
using System.Collections.Generic;

namespace FLua.Runtime
{
    /// <summary>
    /// Table implementation for Lua
    /// </summary>
    public class LuaTable
    {
        private readonly Dictionary<LuaValue, LuaValue> _dictionary = new Dictionary<LuaValue, LuaValue>();
        private readonly List<LuaValue> _array = [];
        private LuaTable? _metatable;
        
        // Built-in library optimization support
        private string? _builtinLibraryName;
        private Dictionary<string, bool>? _fastPathEnabled;

        public IReadOnlyDictionary<LuaValue, LuaValue> Dictionary => _dictionary;
        public IReadOnlyList<LuaValue> Array => _array;
        public LuaTable? Metatable 
        { 
            get => _metatable; 
            set => _metatable = value; 
        }
        
        // Built-in library optimization properties
        public string? BuiltinLibraryName => _builtinLibraryName;
        public bool IsBuiltinLibrary => _builtinLibraryName != null;

        public LuaValue Get(LuaValue key)
        {
            // Try array part first for integer keys
            if (key.TryGetInteger(out long index) && index > 0 && index <= _array.Count)
            {
                return _array[(int)(index - 1)];
            }

            // Then try dictionary part
            if (_dictionary.TryGetValue(key, out var value))
            {
                return value;
            }

            // Check metatable __index
            if (_metatable != null)
            {
                var indexMeta = _metatable.Get(LuaValue.String("__index"));
                if (indexMeta.Type == LuaType.Function)
                {
                    // TODO: Call metamethod
                    return LuaValue.Nil;
                }
                else if (indexMeta.Type == LuaType.Table)
                {
                    return indexMeta.AsTable<LuaTable>().Get(key);
                }
            }

            return LuaValue.Nil;
        }

        public LuaValue RawGet(LuaValue key)
        {
            // Try array part first for integer keys
            if (key.TryGetInteger(out long index) && index > 0 && index <= _array.Count)
            {
                return _array[(int)(index - 1)];
            }

            // Then try dictionary part
            if (_dictionary.TryGetValue(key, out var value))
            {
                return value;
            }

            return LuaValue.Nil;
        }

        public void Set(LuaValue key, LuaValue value)
        {
            // Track modifications to built-in library functions
            if (_fastPathEnabled != null && key.IsString)
            {
                var keyStr = key.AsString();
                if (_fastPathEnabled.ContainsKey(keyStr))
                {
                    // Mark this function as modified (disable fast path)
                    _fastPathEnabled[keyStr] = false;
                }
            }
            
            // Handle array part for positive integer keys
            if (key.TryGetInteger(out long index) && index > 0)
            {
                if (index == _array.Count + 1 && value.Type != LuaType.Nil)
                {
                    _array.Add(value);
                    return;
                }
                else if (index <= _array.Count && index > 0)
                {
                    if (value.Type == LuaType.Nil)
                    {
                        _array[(int)(index - 1)] = value;
                        // If we're setting nil at the end of the array, truncate trailing nils
                        while (_array.Count > 0 && _array[_array.Count - 1].Type == LuaType.Nil)
                        {
                            _array.RemoveAt(_array.Count - 1);
                        }
                    }
                    else
                    {
                        _array[(int)(index - 1)] = value;
                    }
                    return;
                }
            }

            // Use dictionary part for everything else
            if (value.Type == LuaType.Nil)
            {
                _dictionary.Remove(key);
            }
            else
            {
                _dictionary[key] = value;
            }
        }

        public void RawSet(LuaValue key, LuaValue value)
        {
            Set(key, value); // For now, same as Set since we don't check metamethods here
        }

        public int Length()
        {
            // Lua's # operator returns the last integer key with a non-nil value
            int len = _array.Count;
            while (len > 0 && _array[len - 1].Type == LuaType.Nil)
            {
                len--;
            }
            return len;
        }

        /// <summary>
        /// Initialize this table as a built-in library with fast path optimization
        /// </summary>
        public void InitializeAsBuiltinLibrary(string libraryName)
        {
            _builtinLibraryName = libraryName;
            _fastPathEnabled = new Dictionary<string, bool>();
        }
        
        /// <summary>
        /// Mark a function as eligible for fast path optimization
        /// </summary>
        public void EnableFastPath(string functionName)
        {
            if (_fastPathEnabled != null)
            {
                _fastPathEnabled[functionName] = true;
            }
        }
        
        /// <summary>
        /// Check if a function can use fast path optimization
        /// </summary>
        public bool CanUseFastPath(string functionName)
        {
            return _fastPathEnabled?.GetValueOrDefault(functionName, false) ?? false;
        }
        
        /// <summary>
        /// Disable fast path for a specific function (used when function is modified)
        /// </summary>
        public void DisableFastPath(string functionName)
        {
            if (_fastPathEnabled != null && _fastPathEnabled.ContainsKey(functionName))
            {
                _fastPathEnabled[functionName] = false;
            }
        }

        public LuaValue ToLuaValue() => LuaValue.Table(this);
    }

    /// <summary>
    /// Base class for Lua functions
    /// </summary>
    public abstract class LuaFunction
    {
        public abstract LuaValue[] Call(params LuaValue[] args);
        public LuaValue ToLuaValue() => LuaValue.Function(this);
    }

    /// <summary>
    /// Built-in function implementation
    /// </summary>
    public class BuiltinFunction : LuaFunction
    {
        private readonly Func<LuaValue[], LuaValue[]> _func;
        
        public BuiltinFunction(Func<LuaValue[], LuaValue[]> func)
        {
            _func = func;
        }
        
        public override LuaValue[] Call(params LuaValue[] args)
        {
            return _func(args);
        }
        
        public static implicit operator LuaValue(BuiltinFunction func) => func.ToLuaValue();
    }

    /// <summary>
    /// User-defined Lua function
    /// </summary>
    public class LuaUserFunction : LuaFunction
    {
        public string[] Parameters { get; }
        public object Body { get; } // AST node
        public LuaEnvironment CapturedEnvironment { get; }
        public bool IsVararg { get; }

        public LuaUserFunction(string[] parameters, object body, LuaEnvironment capturedEnvironment, bool isVararg = false)
        {
            Parameters = parameters;
            Body = body;
            CapturedEnvironment = capturedEnvironment;
            IsVararg = isVararg;
        }

        public override LuaValue[] Call(params LuaValue[] args)
        {
            // This will be implemented by the interpreter
            throw new NotImplementedException("LuaUserFunction.Call must be implemented by the interpreter");
        }
    }

    /// <summary>
    /// File handle for I/O operations
    /// </summary>
    public class LuaFileHandle : IDisposable
    {
        public System.IO.Stream Stream { get; private set; }
        public System.IO.StreamReader? Reader { get; private set; }
        public System.IO.StreamWriter? Writer { get; private set; }
        public string Filename { get; }
        public string Mode { get; }
        public bool IsClosed { get; private set; }
        public bool IsStandardStream { get; }

        public LuaFileHandle(System.IO.Stream stream, string filename, string mode, bool isStandardStream = false)
        {
            Stream = stream ?? throw new ArgumentNullException(nameof(stream));
            Filename = filename;
            Mode = mode;
            IsStandardStream = isStandardStream;
            IsClosed = false;
        }

        public void Close()
        {
            if (!IsClosed && !IsStandardStream)
            {
                Reader?.Close();
                Writer?.Close();
                Stream?.Close();
                IsClosed = true;
            }
        }

        public void Dispose()
        {
            Close();
        }

        public LuaValue ToLuaValue() => LuaValue.UserData(this);
        
        public override string ToString() => $"file ({Filename})";
        
        public static implicit operator LuaValue(LuaFileHandle handle) => handle.ToLuaValue();
    }

    /// <summary>
    /// Coroutine implementation
    /// </summary>
    public class LuaCoroutine
    {
        public enum CoroutineStatus
        {
            Suspended,
            Running,
            Normal,
            Dead
        }

        public CoroutineStatus Status { get; set; }
        public LuaFunction Function { get; }
        public Stack<object> CallStack { get; }
        public Queue<LuaValue[]> YieldedValues { get; }

        public LuaCoroutine(LuaFunction function)
        {
            Function = function;
            Status = CoroutineStatus.Suspended;
            CallStack = new Stack<object>();
            YieldedValues = new Queue<LuaValue[]>();
        }

        public LuaValue ToLuaValue() => LuaValue.Thread(this);
    }
}