using System;

namespace FLua.Runtime
{
    /// <summary>
    /// Provides all runtime operations for Lua values.
    /// This ensures consistent behavior between interpreter and future compiler.
    /// </summary>
    public static class LuaOperations
    {
        #region Binary Operations

        /// <summary>
        /// Performs addition on two Lua values
        /// </summary>
        public static LuaValue Add(LuaValue left, LuaValue right)
        {
            // Check for metamethods first
            var metamethodResult = TryInvokeMetamethod(left, right, "__add");
            if (metamethodResult.HasValue)
                return metamethodResult.Value;

            // Use the built-in Add method from LuaValue struct
            try
            {
                return LuaValue.Add(left, right);
            }
            catch (InvalidOperationException)
            {
                throw new LuaRuntimeException("Attempt to add non-numbers");
            }
        }

        /// <summary>
        /// Performs subtraction on two Lua values
        /// </summary>
        public static LuaValue Subtract(LuaValue left, LuaValue right)
        {
            // Check for metamethods first
            var metamethodResult = TryInvokeMetamethod(left, right, "__sub");
            if (metamethodResult.HasValue)
                return metamethodResult.Value;

            // Use the built-in Subtract method from LuaValue struct
            try
            {
                return LuaValue.Subtract(left, right);
            }
            catch (InvalidOperationException)
            {
                throw new LuaRuntimeException("Attempt to subtract non-numbers");
            }
        }

        /// <summary>
        /// Performs multiplication on two Lua values
        /// </summary>
        public static LuaValue Multiply(LuaValue left, LuaValue right)
        {
            // Check for metamethods first
            var metamethodResult = TryInvokeMetamethod(left, right, "__mul");
            if (metamethodResult.HasValue)
                return metamethodResult.Value;

            // Use the built-in Multiply method from LuaValue struct
            try
            {
                return LuaValue.Multiply(left, right);
            }
            catch (InvalidOperationException)
            {
                throw new LuaRuntimeException("Attempt to multiply non-numbers");
            }
        }

        /// <summary>
        /// Performs floating-point division on two Lua values
        /// </summary>
        public static LuaValue FloatDivide(LuaValue left, LuaValue right)
        {
            // Check for metamethods first
            var metamethodResult = TryInvokeMetamethod(left, right, "__div");
            if (metamethodResult.HasValue)
                return metamethodResult.Value;

            if (left.IsNumber && right.IsNumber)
            {
                if (right.AsDouble() == 0)
                {
                    throw new LuaRuntimeException("Division by zero");
                }

                return LuaValue.Float(left.AsDouble() / right.AsDouble());
            }

            throw new LuaRuntimeException("Attempt to divide non-numbers");
        }

        /// <summary>
        /// Performs floor division on two Lua values
        /// </summary>
        public static LuaValue FloorDivide(LuaValue left, LuaValue right)
        {
            // Check for metamethods first
            var metamethodResult = TryInvokeMetamethod(left, right, "__idiv");
            if (metamethodResult.HasValue)
                return metamethodResult.Value;

            if (left.IsNumber && right.IsNumber)
            {
                if (right.AsDouble() == 0)
                {
                    throw new LuaRuntimeException("Division by zero");
                }

                return LuaValue.Number(Math.Floor(left.AsDouble() / right.AsDouble()));
            }

            throw new LuaRuntimeException("Attempt to perform floor division on non-numbers");
        }

        /// <summary>
        /// Performs modulo operation on two Lua values
        /// </summary>
        public static LuaValue Modulo(LuaValue left, LuaValue right)
        {
            // Check for metamethods first
            var metamethodResult = TryInvokeMetamethod(left, right, "__mod");
            if (metamethodResult.HasValue)
                return metamethodResult.Value;

            if (left.IsNumber && right.IsNumber)
            {
                if (right.AsDouble() == 0)
                {
                    throw new LuaRuntimeException("Modulo by zero");
                }

                return LuaValue.Number(left.AsDouble() % right.AsDouble());
            }

            throw new LuaRuntimeException("Attempt to perform modulo on non-numbers");
        }

        /// <summary>
        /// Performs power operation on two Lua values
        /// </summary>
        public static LuaValue Power(LuaValue left, LuaValue right)
        {
            // Check for metamethods first
            var metamethodResult = TryInvokeMetamethod(left, right, "__pow");
            if (metamethodResult.HasValue)
                return metamethodResult.Value;

            if (left.IsNumber && right.IsNumber)
            {
                return LuaValue.Float(Math.Pow(left.AsDouble(), right.AsDouble()));
            }

            throw new LuaRuntimeException("Attempt to perform power operation on non-numbers");
        }

        /// <summary>
        /// Performs string concatenation on two Lua values
        /// </summary>
        public static LuaValue Concat(LuaValue left, LuaValue right)
        {
            // Check for metamethods first
            var metamethodResult = TryInvokeMetamethod(left, right, "__concat");
            if (metamethodResult.HasValue)
                return metamethodResult.Value;

            var leftStr = LuaTypeConversion.ToConcatString(left);
            var rightStr = LuaTypeConversion.ToConcatString(right);
            
            if (leftStr == null || rightStr == null)
            {
                throw new LuaRuntimeException($"attempt to concatenate a {LuaTypeConversion.GetTypeName(leftStr == null ? left : right)} value");
            }

            return LuaValue.String(leftStr + rightStr);
        }

        #region Comparison Operations

        /// <summary>
        /// Performs equality comparison on two Lua values
        /// </summary>
        public static LuaValue Equal(LuaValue left, LuaValue right)
        {
            // Check for metamethods first
            var metamethodResult = TryInvokeMetamethod(left, right, "__eq");
            if (metamethodResult.HasValue)
                return metamethodResult.Value;

            // Use the built-in Equals method which handles all the type-specific comparisons
            return LuaValue.Boolean(left.Equals(right));
        }

        /// <summary>
        /// Performs inequality comparison on two Lua values
        /// </summary>
        public static LuaValue NotEqual(LuaValue left, LuaValue right)
        {
            // Lua doesn't have a __ne metamethod, so we just negate __eq
            var equalResult = Equal(left, right);
            if (equalResult.IsBoolean)
            {
                return LuaValue.Boolean(!equalResult.AsBoolean());
            }
            return LuaValue.Boolean(false);
        }

        /// <summary>
        /// Performs less-than comparison on two Lua values
        /// </summary>
        public static LuaValue Less(LuaValue left, LuaValue right)
        {
            // Check for metamethods first
            var metamethodResult = TryInvokeMetamethod(left, right, "__lt");
            if (metamethodResult.HasValue)
                return metamethodResult.Value;

            if (left.IsNumber && right.IsNumber)
            {
                return LuaValue.Boolean(left.AsDouble() < right.AsDouble());
            }
            else if (left.IsString && right.IsString)
            {
                return LuaValue.Boolean(left.AsString().CompareTo(right.AsString()) < 0);
            }

            throw new LuaRuntimeException("Attempt to compare incompatible types");
        }

        /// <summary>
        /// Performs less-than-or-equal comparison on two Lua values
        /// </summary>
        public static LuaValue LessEqual(LuaValue left, LuaValue right)
        {
            // Check for metamethods first
            var metamethodResult = TryInvokeMetamethod(left, right, "__le");
            if (metamethodResult.HasValue)
                return metamethodResult.Value;

            if (left.IsNumber && right.IsNumber)
            {
                return LuaValue.Boolean(left.AsDouble() <= right.AsDouble());
            }
            else if (left.IsString && right.IsString)
            {
                return LuaValue.Boolean(left.AsString().CompareTo(right.AsString()) <= 0);
            }

            throw new LuaRuntimeException("Attempt to compare incompatible types");
        }

        /// <summary>
        /// Performs greater-than comparison on two Lua values
        /// </summary>
        public static LuaValue Greater(LuaValue left, LuaValue right)
        {
            // Lua implements a > b as b < a
            return Less(right, left);
        }

        /// <summary>
        /// Performs greater-than-or-equal comparison on two Lua values
        /// </summary>
        public static LuaValue GreaterEqual(LuaValue left, LuaValue right)
        {
            // Lua implements a >= b as b <= a
            return LessEqual(right, left);
        }

        #endregion

        #region Logical Operations

        /// <summary>
        /// Performs logical AND operation on two Lua values
        /// </summary>
        public static LuaValue And(LuaValue left, LuaValue right)
        {
            // In Lua, 'and' returns the first value if it's falsy, otherwise the second value
            return left.IsTruthy() ? right : left;
        }

        /// <summary>
        /// Performs logical OR operation on two Lua values
        /// </summary>
        public static LuaValue Or(LuaValue left, LuaValue right)
        {
            // In Lua, 'or' returns the first value if it's truthy, otherwise the second value
            return left.IsTruthy() ? left : right;
        }

        #endregion

        #region Bitwise Operations

        /// <summary>
        /// Performs bitwise AND operation on two Lua values
        /// </summary>
        public static LuaValue BitAnd(LuaValue left, LuaValue right)
        {
            // Check for metamethods first
            var metamethodResult = TryInvokeMetamethod(left, right, "__band");
            if (metamethodResult.HasValue)
                return metamethodResult.Value;

            if (left.IsInteger && right.IsInteger)
            {
                return LuaValue.Integer(left.AsInteger() & right.AsInteger());
            }

            throw new LuaRuntimeException("Attempt to perform bitwise AND on non-integers");
        }

        /// <summary>
        /// Performs bitwise OR operation on two Lua values
        /// </summary>
        public static LuaValue BitOr(LuaValue left, LuaValue right)
        {
            // Check for metamethods first
            var metamethodResult = TryInvokeMetamethod(left, right, "__bor");
            if (metamethodResult.HasValue)
                return metamethodResult.Value;

            if (left.IsInteger && right.IsInteger)
            {
                return LuaValue.Integer(left.AsInteger() | right.AsInteger());
            }

            throw new LuaRuntimeException("Attempt to perform bitwise OR on non-integers");
        }

        /// <summary>
        /// Performs bitwise XOR operation on two Lua values
        /// </summary>
        public static LuaValue BitXor(LuaValue left, LuaValue right)
        {
            // Check for metamethods first
            var metamethodResult = TryInvokeMetamethod(left, right, "__bxor");
            if (metamethodResult.HasValue)
                return metamethodResult.Value;

            if (left.IsInteger && right.IsInteger)
            {
                return LuaValue.Integer(left.AsInteger() ^ right.AsInteger());
            }

            throw new LuaRuntimeException("Attempt to perform bitwise XOR on non-integers");
        }

        /// <summary>
        /// Performs left shift operation on two Lua values
        /// </summary>
        public static LuaValue ShiftLeft(LuaValue left, LuaValue right)
        {
            // Check for metamethods first
            var metamethodResult = TryInvokeMetamethod(left, right, "__shl");
            if (metamethodResult.HasValue)
                return metamethodResult.Value;

            if (left.IsInteger && right.IsInteger)
            {
                var shift = right.AsInteger();
                if (shift < 0)
                {
                    throw new LuaRuntimeException("Negative shift count");
                }
                
                // Lua behavior: shifts >= 64 bits result in 0
                if (shift >= 64)
                {
                    return LuaValue.Integer(0);
                }

                return LuaValue.Integer(left.AsInteger() << (int)shift);
            }

            throw new LuaRuntimeException("Attempt to perform left shift on non-integers");
        }

        /// <summary>
        /// Performs right shift operation on two Lua values
        /// </summary>
        public static LuaValue ShiftRight(LuaValue left, LuaValue right)
        {
            // Check for metamethods first
            var metamethodResult = TryInvokeMetamethod(left, right, "__shr");
            if (metamethodResult.HasValue)
                return metamethodResult.Value;

            if (left.IsInteger && right.IsInteger)
            {
                var shift = right.AsInteger();
                if (shift < 0)
                {
                    throw new LuaRuntimeException("Negative shift count");
                }
                
                // Lua behavior: shifts >= 64 bits result in 0
                if (shift >= 64)
                {
                    return LuaValue.Integer(0);
                }

                return LuaValue.Integer(left.AsInteger() >> (int)shift);
            }

            throw new LuaRuntimeException("Attempt to perform right shift on non-integers");
        }

        #endregion

        #endregion

        #region Unary Operations

        /// <summary>
        /// Performs logical NOT operation on a Lua value
        /// </summary>
        public static LuaValue Not(LuaValue value)
        {
            // 'not' has no metamethod in Lua
            return LuaValue.Boolean(!value.IsTruthy());
        }

        /// <summary>
        /// Performs negation on a Lua value
        /// </summary>
        public static LuaValue Negate(LuaValue value)
        {
            // Check for metamethods first
            var metamethodResult = TryInvokeUnaryMetamethod(value, "__unm");
            if (metamethodResult.HasValue)
                return metamethodResult.Value;

            if (value.IsNumber)
            {
                return LuaValue.Number(-value.AsDouble());
            }

            throw new LuaRuntimeException("Attempt to negate non-number");
        }

        /// <summary>
        /// Gets the length of a Lua value
        /// </summary>
        public static LuaValue Length(LuaValue value)
        {
            // Check for metamethods first
            var metamethodResult = TryInvokeUnaryMetamethod(value, "__len");
            if (metamethodResult.HasValue)
                return metamethodResult.Value;

            if (value.IsString)
            {
                return LuaValue.Integer(value.AsString().Length);
            }
            else if (value.IsTable)
            {
                var table = value.AsTable<LuaTable>();
                return LuaValue.Integer(table.Array.Count);
            }

            throw new LuaRuntimeException("Attempt to get length of non-string/table");
        }

        /// <summary>
        /// Performs bitwise NOT operation on a Lua value
        /// </summary>
        public static LuaValue BitNot(LuaValue value)
        {
            // Check for metamethods first
            var metamethodResult = TryInvokeUnaryMetamethod(value, "__bnot");
            if (metamethodResult.HasValue)
                return metamethodResult.Value;

            if (value.IsInteger)
            {
                return LuaValue.Integer(~value.AsInteger());
            }

            throw new LuaRuntimeException("Attempt to perform bitwise NOT on non-integer");
        }

        #endregion

        #region Metamethod Support

        /// <summary>
        /// Attempts to invoke a binary metamethod on two values
        /// </summary>
        private static LuaValue? TryInvokeMetamethod(LuaValue left, LuaValue right, string metamethod)
        {
            return LuaMetamethods.InvokeBinaryMetamethod(left, right, metamethod);
        }

        /// <summary>
        /// Attempts to invoke a unary metamethod on a value
        /// </summary>
        private static LuaValue? TryInvokeUnaryMetamethod(LuaValue value, string metamethod)
        {
            return LuaMetamethods.InvokeUnaryMetamethod(value, metamethod);
        }

        #endregion

        #region Operation Dispatch


        #endregion

        #region Table Operations

        /// <summary>
        /// Creates a table from an array of key-value pairs
        /// </summary>
        public static LuaTable CreateTable(LuaValue[] keyValuePairs)
        {
            if (keyValuePairs.Length % 2 != 0)
                throw new ArgumentException("Key-value pairs array must have even length");

            var table = new LuaTable();
            for (int i = 0; i < keyValuePairs.Length; i += 2)
            {
                table.Set(keyValuePairs[i], keyValuePairs[i + 1]);
            }
            return table;
        }

        /// <summary>
        /// Optimized string method call that bypasses table lookup for common operations
        /// </summary>
        public static LuaValue? TryFastStringMethodCall(string str, string methodName, LuaValue[] args)
        {
            switch (methodName)
            {
                case "upper":
                    return LuaValue.String(str.ToUpper());
                    
                case "lower":
                    return LuaValue.String(str.ToLower());
                    
                case "len":
                    return LuaValue.Integer(str.Length);
                    
                case "reverse":
                    return LuaValue.String(new string(str.Reverse().ToArray()));
                    
                case "sub":
                    if (args.Length >= 1 && args[0].TryGetInteger(out long start))
                    {
                        var startIdx = start > 0 ? (int)(start - 1) : (int)(str.Length + start);
                        startIdx = Math.Max(0, Math.Min(startIdx, str.Length));
                        
                        if (args.Length >= 2 && args[1].TryGetInteger(out long end))
                        {
                            var endIdx = end > 0 ? (int)end - 1 : (int)(str.Length + end);
                            endIdx = Math.Max(-1, Math.Min(endIdx, str.Length - 1));
                            
                            if (startIdx <= endIdx)
                            {
                                return LuaValue.String(str.Substring(startIdx, endIdx - startIdx + 1));
                            }
                            return LuaValue.String("");
                        }
                        else
                        {
                            return LuaValue.String(str.Substring(startIdx));
                        }
                    }
                    throw new LuaRuntimeException("bad argument #1 to 'sub' (number expected)");
                    
                case "rep":
                    if (args.Length >= 1 && args[0].TryGetInteger(out long count))
                    {
                        if (count <= 0) return LuaValue.String("");
                        var separator = args.Length >= 2 ? args[1].AsString() : "";
                        if (separator == "")
                        {
                            return LuaValue.String(string.Concat(Enumerable.Repeat(str, (int)count)));
                        }
                        else
                        {
                            return LuaValue.String(string.Join(separator, Enumerable.Repeat(str, (int)count)));
                        }
                    }
                    throw new LuaRuntimeException("bad argument #1 to 'rep' (number expected)");
                    
                default:
                    // Return null to indicate no fast path available
                    return null;
            }
        }

        /// <summary>
        /// Optimized math function call that bypasses table lookup for common operations
        /// </summary>
        public static LuaValue[]? TryFastMathFunctionCall(string functionName, LuaValue[] args)
        {
            switch (functionName)
            {
                case "abs":
                    if (args.Length == 0)
                        throw new LuaRuntimeException("bad argument #1 to 'abs' (number expected)");
                    
                    var value = args[0];
                    if (value.IsInteger)
                    {
                        var intVal = value.AsInteger();
                        if (intVal == long.MinValue)
                            return [LuaValue.Integer(long.MinValue)]; // MinValue abs is itself in Lua
                        return [LuaValue.Integer(Math.Abs(intVal))];
                    }
                    
                    if (value.IsNumber)
                    {
                        return [LuaValue.Number(Math.Abs(value.AsDouble()))];
                    }
                    
                    throw new LuaRuntimeException("bad argument #1 to 'abs' (number expected)");
                    
                case "sin":
                    if (args.Length == 0)
                        throw new LuaRuntimeException("bad argument #1 to 'sin' (number expected)");
                    
                    if (!args[0].IsNumber)
                        throw new LuaRuntimeException("bad argument #1 to 'sin' (number expected)");
                    
                    return [LuaValue.Float(Math.Sin(args[0].AsDouble()))];
                    
                case "cos":
                    if (args.Length == 0)
                        throw new LuaRuntimeException("bad argument #1 to 'cos' (number expected)");
                    
                    if (!args[0].IsNumber)
                        throw new LuaRuntimeException("bad argument #1 to 'cos' (number expected)");
                    
                    return [LuaValue.Float(Math.Cos(args[0].AsDouble()))];
                    
                case "tan":
                    if (args.Length == 0)
                        throw new LuaRuntimeException("bad argument #1 to 'tan' (number expected)");
                    
                    if (!args[0].IsNumber)
                        throw new LuaRuntimeException("bad argument #1 to 'tan' (number expected)");
                    
                    return [LuaValue.Float(Math.Tan(args[0].AsDouble()))];
                    
                case "sqrt":
                    if (args.Length == 0)
                        throw new LuaRuntimeException("bad argument #1 to 'sqrt' (number expected)");
                    
                    if (!args[0].IsNumber)
                        throw new LuaRuntimeException("bad argument #1 to 'sqrt' (number expected)");
                    
                    return [LuaValue.Float(Math.Sqrt(args[0].AsDouble()))];
                    
                case "exp":
                    if (args.Length == 0)
                        throw new LuaRuntimeException("bad argument #1 to 'exp' (number expected)");
                    
                    if (!args[0].IsNumber)
                        throw new LuaRuntimeException("bad argument #1 to 'exp' (number expected)");
                    
                    return [LuaValue.Float(Math.Exp(args[0].AsDouble()))];
                    
                case "floor":
                    if (args.Length == 0)
                        throw new LuaRuntimeException("bad argument #1 to 'floor' (number expected)");
                    
                    var floorValue = args[0];
                    if (!floorValue.IsNumber)
                        throw new LuaRuntimeException("bad argument #1 to 'floor' (number expected)");
                    
                    var floorResult = Math.Floor(floorValue.AsDouble());
                    if (floorResult >= long.MinValue && floorResult <= long.MaxValue && floorResult == Math.Truncate(floorResult))
                        return [LuaValue.Integer((long)floorResult)];
                    else
                        return [LuaValue.Float(floorResult)];
                        
                case "ceil":
                    if (args.Length == 0)
                        throw new LuaRuntimeException("bad argument #1 to 'ceil' (number expected)");
                    
                    var ceilValue = args[0];
                    if (!ceilValue.IsNumber)
                        throw new LuaRuntimeException("bad argument #1 to 'ceil' (number expected)");
                    
                    var ceilResult = Math.Ceiling(ceilValue.AsDouble());
                    if (ceilResult >= long.MinValue && ceilResult <= long.MaxValue && ceilResult == Math.Truncate(ceilResult))
                        return [LuaValue.Integer((long)ceilResult)];
                    else
                        return [LuaValue.Float(ceilResult)];
                        
                case "max":
                    if (args.Length == 0)
                        throw new LuaRuntimeException("bad argument #1 to 'max' (value expected)");
                    
                    var max = args[0];
                    if (!max.IsNumber)
                        throw new LuaRuntimeException("bad argument #1 to 'max' (number expected)");
                    
                    for (int i = 1; i < args.Length; i++)
                    {
                        var current = args[i];
                        if (!current.IsNumber)
                            throw new LuaRuntimeException($"bad argument #{i + 1} to 'max' (number expected)");
                        
                        if (current.AsDouble() > max.AsDouble())
                            max = current;
                    }
                    
                    return [max];
                    
                case "min":
                    if (args.Length == 0)
                        throw new LuaRuntimeException("bad argument #1 to 'min' (value expected)");
                    
                    var min = args[0];
                    if (!min.IsNumber)
                        throw new LuaRuntimeException("bad argument #1 to 'min' (number expected)");
                    
                    for (int i = 1; i < args.Length; i++)
                    {
                        var current = args[i];
                        if (!current.IsNumber)
                            throw new LuaRuntimeException($"bad argument #{i + 1} to 'min' (number expected)");
                        
                        if (current.AsDouble() < min.AsDouble())
                            min = current;
                    }
                    
                    return [min];
                    
                default:
                    // Return null to indicate no fast path available
                    return null;
            }
        }

        /// <summary>
        /// Gets a method from a value (handles both tables and values with metatables)
        /// </summary>
        public static LuaValue GetMethod(LuaEnvironment env, LuaValue obj, LuaValue methodName)
        {
            // For tables, use Get directly
            if (obj.IsTable)
            {
                var table = obj.AsTable<LuaTable>();
                return table.Get(methodName);
            }
            
            // For strings, get from string metatable
            if (obj.IsString)
            {
                // Get the string library method
                var stringLibValue = env.GetVariable("string");
                if (stringLibValue.IsTable)
                {
                    var stringLib = stringLibValue.AsTable<LuaTable>();
                    return stringLib.Get(methodName);
                }
            }
            
            // TODO: Handle other types with metatables
            return LuaValue.Nil;
        }

        #endregion
    }
}