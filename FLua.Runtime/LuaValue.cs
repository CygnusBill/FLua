using System;
using System.Collections.Generic;
using System.Numerics;

namespace FLua.Runtime
{
    /// <summary>
    /// Represents a runtime Lua value
    /// </summary>
    public abstract class LuaValue
    {
        public static LuaValue Nil => LuaNil.Instance;

        public virtual double? AsNumber => LuaTypeConversion.ToNumber(this);
        public virtual long? AsInteger => LuaTypeConversion.ToInteger(this);
        public virtual string AsString => LuaTypeConversion.ToString(this);
        public virtual LuaTable? AsTable => null;
        public virtual LuaFunction? AsFunction => null;

        public virtual bool IsTruthy => true;
        
        public static bool IsValueTruthy(LuaValue value)
        {
            if (value == null || value is LuaNil)
                return false;
            
            if (value is LuaBoolean boolVal)
                return boolVal.Value;
            
            return true;
        }

        public static LuaValue FromObject(object value)
        {
            return value switch
            {
                null => LuaNil.Instance,
                bool b => new LuaBoolean(b),
                int i => new LuaInteger(i),
                long l => new LuaInteger(l),
                double d => new LuaNumber(d),
                float f => new LuaNumber(f),
                string s => new LuaString(s),
                _ => throw new ArgumentException($"Cannot convert {value.GetType()} to LuaValue")
            };
        }
    }

    /// <summary>
    /// Represents Lua's nil value
    /// </summary>
    public class LuaNil : LuaValue
    {
        private LuaNil() { }
        public static readonly LuaNil Instance = new LuaNil();
        public override bool IsTruthy => false;
        public override string ToString() => "nil";
    }

    /// <summary>
    /// Represents a Lua boolean value
    /// </summary>
    public class LuaBoolean : LuaValue
    {
        public bool Value { get; }

        public LuaBoolean(bool value)
        {
            Value = value;
        }

        public override bool IsTruthy => Value;
        public override string ToString() => Value ? "true" : "false";
    }

    /// <summary>
    /// Represents a Lua integer value
    /// </summary>
    public class LuaInteger : LuaValue
    {
        public long Value { get; }

        public LuaInteger(long value)
        {
            Value = value;
        }

        // These overrides provide direct access without conversion overhead
        public override double? AsNumber => Value;
        public override long? AsInteger => Value;
        public override string ToString() => Value.ToString();
    }

    /// <summary>
    /// Represents a Lua floating-point value
    /// </summary>
    public class LuaNumber : LuaValue
    {
        public double Value { get; }

        public LuaNumber(double value)
        {
            Value = value;
        }

        // These overrides provide direct access without conversion overhead
        public override double? AsNumber => Value;
        public override long? AsInteger => Math.Floor(Value) == Value && Value >= long.MinValue && Value <= long.MaxValue ? (long)Value : null;
        public override string ToString() => Value.ToString();
    }

    /// <summary>
    /// Represents a Lua string value
    /// </summary>
    public class LuaString : LuaValue
    {
        public string Value { get; }

        public LuaString(string value)
        {
            Value = value ?? string.Empty;
        }

        // String conversions are handled by LuaTypeConversion for consistency
        public override double? AsNumber => LuaTypeConversion.ToNumber(this);
        public override long? AsInteger => LuaTypeConversion.ToInteger(this);
        public override string AsString => Value;
        public override string ToString() => Value;
    }

    /// <summary>
    /// Represents a Lua table
    /// </summary>
    public class LuaTable : LuaValue
    {
        private readonly Dictionary<LuaValue, LuaValue> _dictionary = new Dictionary<LuaValue, LuaValue>(new LuaValueComparer());
        private readonly List<LuaValue> _array = new List<LuaValue>();
        private LuaTable? _metatable;

        public IReadOnlyDictionary<LuaValue, LuaValue> Dictionary => _dictionary;
        public IReadOnlyList<LuaValue> Array => _array;
        public LuaTable? Metatable 
        { 
            get => _metatable; 
            set => _metatable = value; 
        }

        public override LuaTable? AsTable => this;

        public LuaValue Get(LuaValue key)
        {
            if (key is LuaInteger intKey && intKey.Value > 0 && intKey.Value <= _array.Count)
            {
                return _array[(int)intKey.Value - 1]; // Lua arrays are 1-indexed
            }

            if (_dictionary.TryGetValue(key, out var value))
            {
                return value;
            }

            // If value not found and we have a metatable with __index, use it
            if (_metatable != null)
            {
                var indexFunc = _metatable.RawGet(new LuaString("__index"));
                if (indexFunc != LuaNil.Instance)
                {
                    if (indexFunc is LuaTable indexTable)
                    {
                        return indexTable.Get(key);
                    }
                    else if (indexFunc is LuaFunction indexFunction)
                    {
                        var result = indexFunction.Call(new[] { this, key });
                        return result.Length > 0 ? result[0] : LuaNil.Instance;
                    }
                }
            }

            return LuaNil.Instance;
        }

        /// <summary>
        /// Sets a value in the table
        /// </summary>
        public void Set(LuaValue key, LuaValue value)
        {
            // Check for __newindex metamethod
            if (_metatable != null && !_dictionary.ContainsKey(key) && 
                (!(key is LuaInteger keyInteger) || keyInteger.Value <= 0 || keyInteger.Value > _array.Count))
            {
                var newIndexFunc = _metatable.RawGet(new LuaString("__newindex"));
                if (newIndexFunc != LuaNil.Instance)
                {
                    if (newIndexFunc is LuaTable newIndexTable)
                    {
                        newIndexTable.Set(key, value);
                        return;
                    }
                    else if (newIndexFunc is LuaFunction newIndexFunction)
                    {
                        newIndexFunction.Call(new[] { this, key, value });
                        return;
                    }
                }
            }

            // Normal table setting behavior
            if (key is LuaInteger keyInt && keyInt.Value > 0)
            {
                var index = (int)keyInt.Value - 1; // Lua arrays are 1-indexed
                
                // If setting at the next available array index
                if (index == _array.Count)
                {
                    _array.Add(value);
                    return;
                }
                // If setting within existing array bounds
                else if (index < _array.Count)
                {
                    _array[index] = value;
                    return;
                }
            }

            // For non-sequential indices or non-integer keys, use the dictionary
            if (value is LuaNil)
            {
                _dictionary.Remove(key);
            }
            else
            {
                _dictionary[key] = value;
            }
        }

        /// <summary>
        /// Gets a value from the table without invoking metamethods
        /// </summary>
        public LuaValue RawGet(LuaValue key)
        {
            if (key is LuaInteger intKey && intKey.Value > 0 && intKey.Value <= _array.Count)
            {
                return _array[(int)intKey.Value - 1]; // Lua arrays are 1-indexed
            }

            if (_dictionary.TryGetValue(key, out var value))
            {
                return value;
            }

            return LuaNil.Instance;
        }

        /// <summary>
        /// Sets a value in the table without invoking metamethods
        /// </summary>
        public void RawSet(LuaValue key, LuaValue value)
        {
            if (key is LuaInteger keyInt && keyInt.Value > 0)
            {
                var index = (int)keyInt.Value - 1; // Lua arrays are 1-indexed
                
                // If setting at the next available array index
                if (index == _array.Count)
                {
                    _array.Add(value);
                    return;
                }
                // If setting within existing array bounds
                else if (index < _array.Count)
                {
                    _array[index] = value;
                    return;
                }
            }

            // For non-sequential indices or non-integer keys, use the dictionary
            if (value is LuaNil)
            {
                _dictionary.Remove(key);
            }
            else
            {
                _dictionary[key] = value;
            }
        }

        public override string ToString() => "table";

        // Comparer for LuaValue keys in dictionaries
        private class LuaValueComparer : IEqualityComparer<LuaValue>
        {
            public bool Equals(LuaValue? x, LuaValue? y)
            {
                if (x is null || y is null)
                    return x is null && y is null;

                if (x is LuaString xStr && y is LuaString yStr)
                    return xStr.Value == yStr.Value;

                if (x is LuaNumber xNum && y is LuaNumber yNum)
                    return xNum.Value == yNum.Value;

                if (x is LuaInteger xInt && y is LuaInteger yInt)
                    return xInt.Value == yInt.Value;

                if (x is LuaBoolean xBool && y is LuaBoolean yBool)
                    return xBool.Value == yBool.Value;

                // For tables and functions, compare by reference
                return ReferenceEquals(x, y);
            }

            public int GetHashCode(LuaValue? obj)
            {
                if (obj == null) return 0;
                
                return obj switch
                {
                    LuaString s => s.Value.GetHashCode(),
                    LuaNumber n => n.Value.GetHashCode(),
                    LuaInteger i => i.Value.GetHashCode(),
                    LuaBoolean b => b.Value.GetHashCode(),
                    _ => obj.GetHashCode()
                };
            }
        }
    }

    /// <summary>
    /// Represents a Lua function
    /// </summary>
    public abstract class LuaFunction : LuaValue
    {
        public override LuaFunction? AsFunction => this;
        public abstract LuaValue[] Call(LuaValue[] arguments);
        public override string ToString() => "function";
    }
    
    /// <summary>
    /// Represents a user-defined Lua function
    /// </summary>
    public class LuaUserFunction : LuaFunction
    {
        private readonly Func<LuaValue[], LuaValue[]> _function;
        
        public LuaUserFunction(Func<LuaValue[], LuaValue[]> function)
        {
            _function = function ?? throw new ArgumentNullException(nameof(function));
        }
        
        public override LuaValue[] Call(LuaValue[] arguments)
        {
            return _function(arguments);
        }
    }
} 