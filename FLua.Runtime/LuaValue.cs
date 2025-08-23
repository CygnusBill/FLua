using System;
using System.Runtime.InteropServices;
using System.Text;

namespace FLua.Runtime
{
    public enum LuaType : uint
    {
        Nil = 0,
        Boolean = 1,
        Integer = 2, // int64
        Float = 3, // double
        String = 4,
        Table = 5,
        Function = 6,
        UserData = 7,
        Thread = 8,
        LightUserData = 9
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LuaValue : IEquatable<LuaValue>
    {
        public LuaType Type; // 4 bytes
        private unsafe fixed byte _data[16]; // 16 bytes inline

        public Span<byte> Data
        {
            get
            {
                unsafe
                {
                    fixed (byte* ptr = _data)
                    {
                        return new Span<byte>(ptr, 16);
                    }
                }
            }
        }

        #region Static Properties and Constructors

        /// <summary>
        /// Singleton nil value
        /// </summary>
        public static readonly LuaValue Nil = LuaValue.CreateNil();

        /// <summary>
        /// Create a nil value (for compatibility)
        /// </summary>
        private static LuaValue CreateNil() => new LuaValue() { Type = LuaType.Nil };

        public static LuaValue Boolean(bool value)
        {
            var result = new LuaValue { Type = LuaType.Boolean };
            result.Data[0] = value ? (byte)1 : (byte)0;
            return result;
        }

        public static LuaValue Integer(long value)
        {
            var result = new LuaValue { Type = LuaType.Integer };
            BitConverter.TryWriteBytes(result.Data, value);
            return result;
        }

        public static LuaValue Float(double value)
        {
            var result = new LuaValue { Type = LuaType.Float };
            BitConverter.TryWriteBytes(result.Data, value);
            return result;
        }

        public static LuaValue Number(double value)
        {
            // Auto-detect if we can represent as integer without loss
            if (value == Math.Truncate(value) &&
                value >= long.MinValue && value <= long.MaxValue)
            {
                return Integer((long)value);
            }

            return Float(value);
        }

        public static LuaValue Number(long value) => Integer(value);

        public static LuaValue String(string value)
        {
            if (value == null)
                return Nil;
                
            var result = new LuaValue { Type = LuaType.String };
            var handle = GCHandle.Alloc(value, GCHandleType.Normal);
            var ptr = GCHandle.ToIntPtr(handle);
            BitConverter.TryWriteBytes(result.Data, ptr.ToInt64());
            return result;
        }

        public static LuaValue Table(object table)
        {
            var result = new LuaValue { Type = LuaType.Table };
            var handle = GCHandle.Alloc(table, GCHandleType.Normal);
            var ptr = GCHandle.ToIntPtr(handle);
            BitConverter.TryWriteBytes(result.Data, ptr.ToInt64());
            return result;
        }

        public static LuaValue Function(object function)
        {
            var result = new LuaValue { Type = LuaType.Function };
            var handle = GCHandle.Alloc(function, GCHandleType.Normal);
            var ptr = GCHandle.ToIntPtr(handle);
            BitConverter.TryWriteBytes(result.Data, ptr.ToInt64());
            return result;
        }

        public static LuaValue UserData(object userData)
        {
            var result = new LuaValue { Type = LuaType.UserData };
            var handle = GCHandle.Alloc(userData, GCHandleType.Normal);
            var ptr = GCHandle.ToIntPtr(handle);
            BitConverter.TryWriteBytes(result.Data, ptr.ToInt64());
            return result;
        }

        public static LuaValue FromObject(object? obj)
        {
            return obj switch
            {
                int i => Integer(i),
                long l => Integer(l),
                float f => Float(f),
                double d => Float(d),
                bool b => Boolean(b),
                string s => String(s),
                LuaTable t => Table(t),
                LuaFunction f => Function(f),
                LuaValue v => v,
                null => Nil,
                _ => throw new ArgumentException("Cannot convert object to LuaValue")
            };
        }

        public static LuaValue Thread(object thread)
        {
            var result = new LuaValue { Type = LuaType.Thread };
            var handle = GCHandle.Alloc(thread, GCHandleType.Normal);
            var ptr = GCHandle.ToIntPtr(handle);
            BitConverter.TryWriteBytes(result.Data, ptr.ToInt64());
            return result;
        }

        public static LuaValue LightUserData(IntPtr ptr)
        {
            var result = new LuaValue { Type = LuaType.LightUserData };
            BitConverter.TryWriteBytes(result.Data, ptr.ToInt64());
            return result;
        }

        #endregion

        #region Type Checks

        public bool IsNil => Type == LuaType.Nil;
        public bool IsBoolean => Type == LuaType.Boolean;
        public bool IsInteger => Type == LuaType.Integer;
        public bool IsFloat => Type == LuaType.Float;
        public bool IsNumber => Type == LuaType.Integer || Type == LuaType.Float;
        public bool IsString => Type == LuaType.String;
        public bool IsTable => Type == LuaType.Table;
        public bool IsFunction => Type == LuaType.Function;
        public bool IsUserData => Type == LuaType.UserData;
        public bool IsThread => Type == LuaType.Thread;
        public bool IsLightUserData => Type == LuaType.LightUserData;

        #endregion

        #region Value Accessors

        public bool AsBoolean()
        {
            if (Type != LuaType.Boolean)
                throw new InvalidOperationException($"Value is not a boolean, it's {Type}");
            return Data[0] != 0;
        }

        public long AsInteger()
        {
            if (Type != LuaType.Integer)
                throw new InvalidOperationException($"Value is not an integer, it's {Type}");
            return BitConverter.ToInt64(Data);
        }

        public double AsFloat()
        {
            if (Type != LuaType.Float)
                throw new InvalidOperationException($"Value is not a float, it's {Type}");
            return BitConverter.ToDouble(Data);
        }

        public double AsDouble()
        {
            return Type switch
            {
                LuaType.Integer => (double)BitConverter.ToInt64(Data),
                LuaType.Float => BitConverter.ToDouble(Data),
                _ => throw new InvalidOperationException($"Value is not a number, it's {Type}")
            };
        }

        /// <summary>
        /// Gets the number as integer if it's an integer type, otherwise converts float to int
        /// </summary>
        public long AsIntegerValue()
        {
            return Type switch
            {
                LuaType.Integer => BitConverter.ToInt64(Data),
                LuaType.Float => (long)BitConverter.ToDouble(Data),
                _ => throw new InvalidOperationException($"Value is not a number, it's {Type}")
            };
        }

        public string AsString()
        {
            if (Type != LuaType.String)
                throw new InvalidOperationException($"Value is not a string, it's {Type}");

            var ptr = new IntPtr(BitConverter.ToInt64(Data));
            var handle = GCHandle.FromIntPtr(ptr);
            return (string)handle.Target!;
        }

        public T AsTable<T>() where T : LuaTable
        {
            var ptr = new IntPtr(BitConverter.ToInt64(Data));
            var handle = GCHandle.FromIntPtr(ptr);
            return (T)handle.Target!;
        }

        public T AsFunction<T>() where T : LuaFunction
        {
            var ptr = new IntPtr(BitConverter.ToInt64(Data));
            var handle = GCHandle.FromIntPtr(ptr);
            return (T)handle.Target!;
        }
        
        /// <summary>
        /// Non-generic version for IL generation compatibility
        /// </summary>
        public LuaFunction AsFunction()
        {
            if (Type != LuaType.Function)
                throw new InvalidOperationException($"Value is not a function, it's {Type}");
                
            var ptr = new IntPtr(BitConverter.ToInt64(Data));
            var handle = GCHandle.FromIntPtr(ptr);
            return (LuaFunction)handle.Target!;
        }

        public T AsUserData<T>() where T : class
        {
            if (Type != LuaType.UserData)
                throw new InvalidOperationException($"Value is not userdata, it's {Type}");

            var ptr = new IntPtr(BitConverter.ToInt64(Data));
            var handle = GCHandle.FromIntPtr(ptr);
            return (T)handle.Target!;
        }

        public T AsThread<T>() where T : class
        {
            if (Type != LuaType.Thread)
                throw new InvalidOperationException($"Value is not a thread, it's {Type}");

            var ptr = new IntPtr(BitConverter.ToInt64(Data));
            var handle = GCHandle.FromIntPtr(ptr);
            return (T)handle.Target!;
        }

        public IntPtr AsLightUserData()
        {
            if (Type != LuaType.LightUserData)
                throw new InvalidOperationException($"Value is not light userdata, it's {Type}");

            return new IntPtr(BitConverter.ToInt64(Data));
        }

        #endregion

        #region Safe Accessors (Try pattern)

        public bool TryGetBoolean(out bool value)
        {
            if (Type == LuaType.Boolean)
            {
                value = Data[0] != 0;
                return true;
            }

            value = false;
            return false;
        }

        public bool TryGetInteger(out long value)
        {
            if (Type == LuaType.Integer)
            {
                value = BitConverter.ToInt64(Data);
                return true;
            }

            value = 0;
            return false;
        }

        public bool TryGetFloat(out double value)
        {
            if (Type == LuaType.Float)
            {
                value = BitConverter.ToDouble(Data);
                return true;
            }

            if (Type == LuaType.Integer)
            {
                value = (double)BitConverter.ToInt64(Data);
                return true;
            }

            value = 0.0;
            return false;
        }

        public bool TryGetNumber(out double value)
        {
            if (Type == LuaType.Integer)
            {
                value = (double)BitConverter.ToInt64(Data);
                return true;
            }

            if (Type == LuaType.Float)
            {
                value = BitConverter.ToDouble(Data);
                return true;
            }

            value = 0.0;
            return false;
        }

        /// <summary>
        /// Try to get as integer, with conversion from float if lossless
        /// </summary>
        public bool TryGetIntegerValue(out long value)
        {
            if (Type == LuaType.Integer)
            {
                value = BitConverter.ToInt64(Data);
                return true;
            }

            if (Type == LuaType.Float)
            {
                var floatVal = BitConverter.ToDouble(Data);
                if (floatVal == Math.Truncate(floatVal) &&
                    floatVal >= long.MinValue && floatVal <= long.MaxValue)
                {
                    value = (long)floatVal;
                    return true;
                }
            }

            value = 0;
            return false;
        }

        public bool TryGetString(out string? value)
        {
            if (Type == LuaType.String)
            {
                var ptr = new IntPtr(BitConverter.ToInt64(Data));
                var handle = GCHandle.FromIntPtr(ptr);
                value = (string)handle.Target!;
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetTable<T>(out T? value) where T : class
        {
            if (Type == LuaType.Table)
            {
                var ptr = new IntPtr(BitConverter.ToInt64(Data));
                var handle = GCHandle.FromIntPtr(ptr);
                value = (T)handle.Target!;
                return true;
            }

            value = null;
            return false;
        }

        public bool TryGetLightUserData(out IntPtr value)
        {
            if (Type == LuaType.LightUserData)
            {
                value = new IntPtr(BitConverter.ToInt64(Data));
                return true;
            }

            value = IntPtr.Zero;
            return false;
        }

        #endregion

        #region Memory Management

        /// <summary>
        /// Releases managed references for GC types. Call this when the LuaValue is no longer needed.
        /// </summary>
        public void Release()
        {
            if (Type == LuaType.String || Type == LuaType.Table ||
                Type == LuaType.Function || Type == LuaType.UserData || Type == LuaType.Thread)
            {
                var ptr = new IntPtr(BitConverter.ToInt64(Data));
                if (ptr != IntPtr.Zero)
                {
                    var handle = GCHandle.FromIntPtr(ptr);
                    if (handle.IsAllocated)
                    {
                        handle.Free();
                    }
                }
            }
        }

        #endregion

        #region Arithmetic Operations

        /// <summary>
        /// Performs Lua-style arithmetic promotion and operation
        /// </summary>
        public static LuaValue Add(LuaValue left, LuaValue right)
        {
            if (!left.IsNumber || !right.IsNumber)
                throw new InvalidOperationException("Cannot perform arithmetic on non-numeric values");

            // If both are integers, keep as integer if no overflow
            if (left.IsInteger && right.IsInteger)
            {
                var leftInt = left.AsInteger();
                var rightInt = right.AsInteger();

                try
                {
                    return Integer(checked(leftInt + rightInt));
                }
                catch (OverflowException)
                {
                    // Fall through to float arithmetic
                }
            }

            // Otherwise, use float arithmetic
            return Float(left.AsDouble() + right.AsDouble());
        }

        public static LuaValue Subtract(LuaValue left, LuaValue right)
        {
            if (!left.IsNumber || !right.IsNumber)
                throw new InvalidOperationException("Cannot perform arithmetic on non-numeric values");

            if (left.IsInteger && right.IsInteger)
            {
                var leftInt = left.AsInteger();
                var rightInt = right.AsInteger();

                try
                {
                    return Integer(checked(leftInt - rightInt));
                }
                catch (OverflowException)
                {
                    // Fall through to float arithmetic
                }
            }

            return Float(left.AsDouble() - right.AsDouble());
        }

        public static LuaValue Multiply(LuaValue left, LuaValue right)
        {
            if (!left.IsNumber || !right.IsNumber)
                throw new InvalidOperationException("Cannot perform arithmetic on non-numeric values");

            if (left.IsInteger && right.IsInteger)
            {
                var leftInt = left.AsInteger();
                var rightInt = right.AsInteger();

                try
                {
                    return Integer(checked(leftInt * rightInt));
                }
                catch (OverflowException)
                {
                    // Fall through to float arithmetic
                }
            }

            return Float(left.AsDouble() * right.AsDouble());
        }

        public static LuaValue Divide(LuaValue left, LuaValue right)
        {
            if (!left.IsNumber || !right.IsNumber)
                throw new InvalidOperationException("Cannot perform arithmetic on non-numeric values");

            // Division always results in float in Lua 5.3+
            return Float(left.AsDouble() / right.AsDouble());
        }

        public static LuaValue FloorDivide(LuaValue left, LuaValue right)
        {
            if (!left.IsNumber || !right.IsNumber)
                throw new InvalidOperationException("Cannot perform arithmetic on non-numeric values");

            var result = Math.Floor(left.AsDouble() / right.AsDouble());

            // Floor division should always return integer if result fits in long range
            if (result == Math.Truncate(result) &&
                result >= long.MinValue && result <= long.MaxValue)
            {
                return Integer((long)result);
            }

            return Float(result);
        }

        public static LuaValue Modulo(LuaValue left, LuaValue right)
        {
            if (!left.IsNumber || !right.IsNumber)
                throw new InvalidOperationException("Cannot perform arithmetic on non-numeric values");

            if (left.IsInteger && right.IsInteger)
            {
                return Integer(left.AsInteger() % right.AsInteger());
            }

            return Float(left.AsDouble() % right.AsDouble());
        }

        public static LuaValue Power(LuaValue left, LuaValue right)
        {
            if (!left.IsNumber || !right.IsNumber)
                throw new InvalidOperationException("Cannot perform arithmetic on non-numeric values");

            // Power always results in float
            return Float(Math.Pow(left.AsDouble(), right.AsDouble()));
        }

        public static LuaValue UnaryMinus(LuaValue value)
        {
            if (!value.IsNumber)
                throw new InvalidOperationException("Cannot negate non-numeric value");

            if (value.IsInteger)
            {
                var intVal = value.AsInteger();
                if (intVal != long.MinValue) // Avoid overflow
                    return Integer(-intVal);
            }

            return Float(-value.AsDouble());
        }

        #endregion

        #region Lua Semantics

        /// <summary>
        /// Implements Lua's truthiness rules: only nil and false are falsy
        /// </summary>
        public bool IsTruthy()
        {
            return Type != LuaType.Nil && !(Type == LuaType.Boolean && Data[0] == 0);
        }

        /// <summary>
        /// Converts value to string using Lua's rules
        /// </summary>
        public string ToLuaString()
        {
            return Type switch
            {
                LuaType.Nil => "nil",
                LuaType.Boolean => AsBoolean() ? "true" : "false",
                LuaType.Integer => AsInteger().ToString(),
                LuaType.Float => AsFloat().ToString("G17"), // Preserve precision
                LuaType.String => AsString(),
                LuaType.Table => $"table: 0x{BitConverter.ToInt64(Data):X}",
                LuaType.Function => $"function: 0x{BitConverter.ToInt64(Data):X}",
                LuaType.UserData => $"userdata: 0x{BitConverter.ToInt64(Data):X}",
                LuaType.Thread => $"thread: 0x{BitConverter.ToInt64(Data):X}",
                LuaType.LightUserData => $"userdata: 0x{BitConverter.ToInt64(Data):X}",
                _ => $"unknown: {Type}"
            };
        }

        /// <summary>
        /// Attempts to convert to number using Lua's rules
        /// </summary>
        public bool TryToNumber(out double result)
        {
            switch (Type)
            {
                case LuaType.Integer:
                    result = (double)AsInteger();
                    return true;
                case LuaType.Float:
                    result = AsFloat();
                    return true;
                case LuaType.String:
                    var str = AsString().Trim();
                    // Try integer first
                    if (long.TryParse(str, out long intResult))
                    {
                        result = (double)intResult;
                        return true;
                    }

                    // Then try float
                    return double.TryParse(str, out result);
                default:
                    result = 0.0;
                    return false;
            }
        }

        /// <summary>
        /// Attempts to convert to integer using Lua's rules
        /// </summary>
        public bool TryToInteger(out long result)
        {
            switch (Type)
            {
                case LuaType.Integer:
                    result = AsInteger();
                    return true;
                case LuaType.Float:
                    var floatVal = AsFloat();
                    if (floatVal == Math.Truncate(floatVal) &&
                        floatVal >= long.MinValue && floatVal <= long.MaxValue)
                    {
                        result = (long)floatVal;
                        return true;
                    }

                    break;
                case LuaType.String:
                    return long.TryParse(AsString().Trim(), out result);
            }

            result = 0;
            return false;
        }

        #endregion

        #region Equality and Hashing

        public bool Equals(LuaValue other)
        {
            if (Type != other.Type)
            {
                return false;
            }

            return Type switch
            {
                LuaType.Nil => true,
                LuaType.Boolean => AsBoolean() == other.AsBoolean(),
                LuaType.Integer => AsInteger() == other.AsInteger(),
                LuaType.Float => Math.Abs(AsFloat() - other.AsFloat()) < double.Epsilon,
                LuaType.String => AsString() == other.AsString(),
                _ => BitConverter.ToInt64(Data) == BitConverter.ToInt64(other.Data)
            };
        }

        public override bool Equals(object? obj)
        {
            return obj is LuaValue other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Type switch
            {
                LuaType.Nil => 0,
                LuaType.Boolean => HashCode.Combine(Type, AsBoolean()),
                LuaType.Integer => HashCode.Combine(Type, AsInteger()),
                LuaType.Float => HashCode.Combine(Type, AsFloat()),
                LuaType.String => HashCode.Combine(Type, AsString()),
                _ => HashCode.Combine(Type, BitConverter.ToInt64(Data))
            };
        }

        public static bool operator ==(LuaValue left, LuaValue right) => left.Equals(right);
        public static bool operator !=(LuaValue left, LuaValue right) => !left.Equals(right);

        #endregion

        #region Implicit Conversions

        public static implicit operator LuaValue(bool value) => Boolean(value);
        public static implicit operator LuaValue(long value) => Integer(value);
        public static implicit operator LuaValue(int value) => Integer(value);
        public static implicit operator LuaValue(double value) => Float(value);
        public static implicit operator LuaValue(float value) => Float(value);
        public static implicit operator LuaValue(string? value) => value == null ? Nil : String(value);
        public static implicit operator LuaValue(LuaTable table) => Table(table);
        public static implicit operator LuaValue(LuaFunction func) => Function(func);

        // Note: We don't provide implicit conversions back to C# types as they can fail
        // Use the AsXxx() methods or TryGetXxx() methods instead

        #endregion

        #region Arithmetic Operators

        public static LuaValue operator +(LuaValue left, LuaValue right) => Add(left, right);
        public static LuaValue operator -(LuaValue left, LuaValue right) => Subtract(left, right);
        public static LuaValue operator *(LuaValue left, LuaValue right) => Multiply(left, right);
        public static LuaValue operator /(LuaValue left, LuaValue right) => Divide(left, right);
        public static LuaValue operator %(LuaValue left, LuaValue right) => Modulo(left, right);
        public static LuaValue operator -(LuaValue value) => UnaryMinus(value);

        #endregion

        public override string ToString() => ToLuaString();
    }
}
