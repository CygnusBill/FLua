using System;

namespace FLua.Runtime
{
    /// <summary>
    /// Lua Math Library implementation
    /// </summary>
    public static class LuaMathLib
    {
        private static readonly Random _random = new Random();
        
        /// <summary>
        /// Adds the math library to the Lua environment
        /// </summary>
        public static void AddMathLibrary(LuaEnvironment env)
        {
            var mathTable = new LuaTable();
            
            // Constants
            mathTable.Set(new LuaString("pi"), new LuaNumber(Math.PI));
            mathTable.Set(new LuaString("huge"), new LuaNumber(double.PositiveInfinity));
            mathTable.Set(new LuaString("mininteger"), new LuaInteger(long.MinValue));
            mathTable.Set(new LuaString("maxinteger"), new LuaInteger(long.MaxValue));
            
            // Basic arithmetic functions
            mathTable.Set(new LuaString("abs"), new BuiltinFunction(Abs));
            mathTable.Set(new LuaString("max"), new BuiltinFunction(Max));
            mathTable.Set(new LuaString("min"), new BuiltinFunction(Min));
            mathTable.Set(new LuaString("floor"), new BuiltinFunction(Floor));
            mathTable.Set(new LuaString("ceil"), new BuiltinFunction(Ceil));
            mathTable.Set(new LuaString("fmod"), new BuiltinFunction(FMod));
            mathTable.Set(new LuaString("modf"), new BuiltinFunction(Modf));
            
            // Trigonometric functions
            mathTable.Set(new LuaString("sin"), new BuiltinFunction(Sin));
            mathTable.Set(new LuaString("cos"), new BuiltinFunction(Cos));
            mathTable.Set(new LuaString("tan"), new BuiltinFunction(Tan));
            mathTable.Set(new LuaString("asin"), new BuiltinFunction(ASin));
            mathTable.Set(new LuaString("acos"), new BuiltinFunction(ACos));
            mathTable.Set(new LuaString("atan"), new BuiltinFunction(ATan));
            mathTable.Set(new LuaString("deg"), new BuiltinFunction(Deg));
            mathTable.Set(new LuaString("rad"), new BuiltinFunction(Rad));
            
            // Exponential and logarithmic functions
            mathTable.Set(new LuaString("exp"), new BuiltinFunction(Exp));
            mathTable.Set(new LuaString("log"), new BuiltinFunction(Log));
            mathTable.Set(new LuaString("sqrt"), new BuiltinFunction(Sqrt));
            mathTable.Set(new LuaString("pow"), new BuiltinFunction(Pow));
            
            // Random functions
            mathTable.Set(new LuaString("random"), new BuiltinFunction(Random));
            mathTable.Set(new LuaString("randomseed"), new BuiltinFunction(RandomSeed));
            
            // Type and conversion functions
            mathTable.Set(new LuaString("type"), new BuiltinFunction(Type));
            mathTable.Set(new LuaString("tointeger"), new BuiltinFunction(ToInteger));
            mathTable.Set(new LuaString("ult"), new BuiltinFunction(Ult));
            
            env.SetVariable("math", mathTable);
        }
        
        #region Basic Arithmetic Functions
        
        private static LuaValue[] Abs(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'abs' (number expected)");
            
            var value = args[0];
            if (value is LuaInteger intVal)
            {
                if (intVal.Value == long.MinValue)
                    return new[] { new LuaInteger(long.MinValue) }; // MinValue abs is itself in Lua
                return new[] { new LuaInteger(Math.Abs(intVal.Value)) };
            }
            
            if (value.AsNumber.HasValue)
            {
                return new[] { new LuaNumber(Math.Abs(value.AsNumber.Value)) };
            }
            
            throw new LuaRuntimeException("bad argument #1 to 'abs' (number expected)");
        }
        
        private static LuaValue[] Max(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'max' (value expected)");
            
            var max = args[0];
            if (!max.AsNumber.HasValue)
                throw new LuaRuntimeException("bad argument #1 to 'max' (number expected)");
            
            for (int i = 1; i < args.Length; i++)
            {
                var current = args[i];
                if (!current.AsNumber.HasValue)
                    throw new LuaRuntimeException($"bad argument #{i + 1} to 'max' (number expected)");
                
                if (current.AsNumber.Value > max.AsNumber.Value)
                    max = current;
            }
            
            return new[] { max };
        }
        
        private static LuaValue[] Min(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'min' (value expected)");
            
            var min = args[0];
            if (!min.AsNumber.HasValue)
                throw new LuaRuntimeException("bad argument #1 to 'min' (number expected)");
            
            for (int i = 1; i < args.Length; i++)
            {
                var current = args[i];
                if (!current.AsNumber.HasValue)
                    throw new LuaRuntimeException($"bad argument #{i + 1} to 'min' (number expected)");
                
                if (current.AsNumber.Value < min.AsNumber.Value)
                    min = current;
            }
            
            return new[] { min };
        }
        
        private static LuaValue[] Floor(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'floor' (number expected)");
            
            var value = args[0];
            if (value is LuaInteger)
                return new[] { value }; // Integer is already floored
            
            if (value.AsNumber.HasValue)
            {
                var result = Math.Floor(value.AsNumber.Value);
                // Return integer if it fits in long range
                if (result >= long.MinValue && result <= long.MaxValue && result == Math.Truncate(result))
                    return new[] { new LuaInteger((long)result) };
                return new[] { new LuaNumber(result) };
            }
            
            throw new LuaRuntimeException("bad argument #1 to 'floor' (number expected)");
        }
        
        private static LuaValue[] Ceil(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'ceil' (number expected)");
            
            var value = args[0];
            if (value is LuaInteger)
                return new[] { value }; // Integer is already ceiled
            
            if (value.AsNumber.HasValue)
            {
                var result = Math.Ceiling(value.AsNumber.Value);
                // Return integer if it fits in long range
                if (result >= long.MinValue && result <= long.MaxValue && result == Math.Truncate(result))
                    return new[] { new LuaInteger((long)result) };
                return new[] { new LuaNumber(result) };
            }
            
            throw new LuaRuntimeException("bad argument #1 to 'ceil' (number expected)");
        }
        
        private static LuaValue[] FMod(LuaValue[] args)
        {
            if (args.Length < 2)
                throw new LuaRuntimeException("bad argument #2 to 'fmod' (number expected)");
            
            var x = args[0];
            var y = args[1];
            
            if (!x.AsNumber.HasValue)
                throw new LuaRuntimeException("bad argument #1 to 'fmod' (number expected)");
            if (!y.AsNumber.HasValue)
                throw new LuaRuntimeException("bad argument #2 to 'fmod' (number expected)");
            
            if (y.AsNumber.Value == 0)
                throw new LuaRuntimeException("bad argument #2 to 'fmod' (zero)");
            
            var result = x.AsNumber.Value % y.AsNumber.Value;
            
            // Preserve integer type if both inputs are integers
            if (x is LuaInteger && y is LuaInteger)
                return new[] { new LuaInteger((long)result) };
            
            return new[] { new LuaNumber(result) };
        }
        
        private static LuaValue[] Modf(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'modf' (number expected)");
            
            var value = args[0];
            if (!value.AsNumber.HasValue)
                throw new LuaRuntimeException("bad argument #1 to 'modf' (number expected)");
            
            var num = value.AsNumber.Value;
            var intPart = Math.Truncate(num);
            var fracPart = num - intPart;
            
            LuaValue intResult;
            if (intPart >= long.MinValue && intPart <= long.MaxValue && intPart == Math.Truncate(intPart))
                intResult = new LuaInteger((long)intPart);
            else
                intResult = new LuaNumber(intPart);
            
            return new[] { intResult, new LuaNumber(fracPart) };
        }
        
        #endregion
        
        #region Trigonometric Functions
        
        private static LuaValue[] Sin(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'sin' (number expected)");
            
            var value = args[0];
            if (!value.AsNumber.HasValue)
                throw new LuaRuntimeException("bad argument #1 to 'sin' (number expected)");
            
            return new[] { new LuaNumber(Math.Sin(value.AsNumber.Value)) };
        }
        
        private static LuaValue[] Cos(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'cos' (number expected)");
            
            var value = args[0];
            if (!value.AsNumber.HasValue)
                throw new LuaRuntimeException("bad argument #1 to 'cos' (number expected)");
            
            return new[] { new LuaNumber(Math.Cos(value.AsNumber.Value)) };
        }
        
        private static LuaValue[] Tan(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'tan' (number expected)");
            
            var value = args[0];
            if (!value.AsNumber.HasValue)
                throw new LuaRuntimeException("bad argument #1 to 'tan' (number expected)");
            
            return new[] { new LuaNumber(Math.Tan(value.AsNumber.Value)) };
        }
        
        private static LuaValue[] ASin(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'asin' (number expected)");
            
            var value = args[0];
            if (!value.AsNumber.HasValue)
                throw new LuaRuntimeException("bad argument #1 to 'asin' (number expected)");
            
            return new[] { new LuaNumber(Math.Asin(value.AsNumber.Value)) };
        }
        
        private static LuaValue[] ACos(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'acos' (number expected)");
            
            var value = args[0];
            if (!value.AsNumber.HasValue)
                throw new LuaRuntimeException("bad argument #1 to 'acos' (number expected)");
            
            return new[] { new LuaNumber(Math.Acos(value.AsNumber.Value)) };
        }
        
        private static LuaValue[] ATan(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'atan' (number expected)");
            
            var value = args[0];
            if (!value.AsNumber.HasValue)
                throw new LuaRuntimeException("bad argument #1 to 'atan' (number expected)");
            
            if (args.Length >= 2)
            {
                var x = args[1];
                if (!x.AsNumber.HasValue)
                    throw new LuaRuntimeException("bad argument #2 to 'atan' (number expected)");
                
                return new[] { new LuaNumber(Math.Atan2(value.AsNumber.Value, x.AsNumber.Value)) };
            }
            
            return new[] { new LuaNumber(Math.Atan(value.AsNumber.Value)) };
        }
        
        private static LuaValue[] Deg(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'deg' (number expected)");
            
            var value = args[0];
            if (!value.AsNumber.HasValue)
                throw new LuaRuntimeException("bad argument #1 to 'deg' (number expected)");
            
            return new[] { new LuaNumber(value.AsNumber.Value * 180.0 / Math.PI) };
        }
        
        private static LuaValue[] Rad(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'rad' (number expected)");
            
            var value = args[0];
            if (!value.AsNumber.HasValue)
                throw new LuaRuntimeException("bad argument #1 to 'rad' (number expected)");
            
            return new[] { new LuaNumber(value.AsNumber.Value * Math.PI / 180.0) };
        }
        
        #endregion
        
        #region Exponential and Logarithmic Functions
        
        private static LuaValue[] Exp(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'exp' (number expected)");
            
            var value = args[0];
            if (!value.AsNumber.HasValue)
                throw new LuaRuntimeException("bad argument #1 to 'exp' (number expected)");
            
            return new[] { new LuaNumber(Math.Exp(value.AsNumber.Value)) };
        }
        
        private static LuaValue[] Log(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'log' (number expected)");
            
            var value = args[0];
            if (!value.AsNumber.HasValue)
                throw new LuaRuntimeException("bad argument #1 to 'log' (number expected)");
            
            if (args.Length >= 2)
            {
                var baseValue = args[1];
                if (!baseValue.AsNumber.HasValue)
                    throw new LuaRuntimeException("bad argument #2 to 'log' (number expected)");
                
                return new[] { new LuaNumber(Math.Log(value.AsNumber.Value, baseValue.AsNumber.Value)) };
            }
            
            return new[] { new LuaNumber(Math.Log(value.AsNumber.Value)) };
        }
        
        private static LuaValue[] Sqrt(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'sqrt' (number expected)");
            
            var value = args[0];
            if (!value.AsNumber.HasValue)
                throw new LuaRuntimeException("bad argument #1 to 'sqrt' (number expected)");
            
            return new[] { new LuaNumber(Math.Sqrt(value.AsNumber.Value)) };
        }
        
        private static LuaValue[] Pow(LuaValue[] args)
        {
            if (args.Length < 2)
                throw new LuaRuntimeException("bad argument #2 to 'pow' (number expected)");
            
            var x = args[0];
            var y = args[1];
            
            if (!x.AsNumber.HasValue)
                throw new LuaRuntimeException("bad argument #1 to 'pow' (number expected)");
            if (!y.AsNumber.HasValue)
                throw new LuaRuntimeException("bad argument #2 to 'pow' (number expected)");
            
            return new[] { new LuaNumber(Math.Pow(x.AsNumber.Value, y.AsNumber.Value)) };
        }
        
        #endregion
        
        #region Random Functions
        
        private static LuaValue[] Random(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                // Return random float between 0 and 1
                return new[] { new LuaNumber(_random.NextDouble()) };
            }
            else if (args.Length == 1)
            {
                // Return random integer between 1 and n
                var n = args[0];
                if (!n.AsInteger.HasValue || n.AsInteger.Value <= 0)
                    throw new LuaRuntimeException("bad argument #1 to 'random' (interval is empty)");
                
                return new[] { new LuaInteger(_random.Next(1, (int)n.AsInteger.Value + 1)) };
            }
            else if (args.Length >= 2)
            {
                // Return random integer between m and n
                var m = args[0];
                var n = args[1];
                
                if (!m.AsInteger.HasValue)
                    throw new LuaRuntimeException("bad argument #1 to 'random' (number expected)");
                if (!n.AsInteger.HasValue)
                    throw new LuaRuntimeException("bad argument #2 to 'random' (number expected)");
                
                if (m.AsInteger.Value > n.AsInteger.Value)
                    throw new LuaRuntimeException("bad argument #2 to 'random' (interval is empty)");
                
                return new[] { new LuaInteger(_random.Next((int)m.AsInteger.Value, (int)n.AsInteger.Value + 1)) };
            }
            
            throw new LuaRuntimeException("bad arguments to 'random'");
        }
        
        private static LuaValue[] RandomSeed(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                // Use current time as seed
                var seed = Environment.TickCount;
                var newRandom = new Random(seed);
                // In a full implementation, we'd replace the global random instance
                return new[] { new LuaInteger(seed), new LuaInteger(seed) };
            }
            else
            {
                var seed = args[0];
                if (!seed.AsInteger.HasValue)
                    throw new LuaRuntimeException("bad argument #1 to 'randomseed' (number expected)");
                
                var seedValue = (int)seed.AsInteger.Value;
                var newRandom = new Random(seedValue);
                // In a full implementation, we'd replace the global random instance
                return new[] { new LuaInteger(seedValue), new LuaInteger(seedValue) };
            }
        }
        
        #endregion
        
        #region Type and Conversion Functions
        
        private static LuaValue[] Type(LuaValue[] args)
        {
            if (args.Length == 0)
                return new[] { LuaNil.Instance };
            
            var value = args[0];
            string type = value switch
            {
                LuaInteger => "integer",
                LuaNumber => "float",
                _ => LuaNil.Instance.ToString()
            };
            
            if (type == "nil")
                return new[] { LuaNil.Instance };
            
            return new[] { new LuaString(type) };
        }
        
        private static LuaValue[] ToInteger(LuaValue[] args)
        {
            if (args.Length == 0)
                return new[] { LuaNil.Instance };
            
            var value = args[0];
            
            if (value is LuaInteger)
                return new[] { value };
            
            if (value is LuaNumber num)
            {
                if (num.Value == Math.Truncate(num.Value) && 
                    num.Value >= long.MinValue && num.Value <= long.MaxValue)
                {
                    return new[] { new LuaInteger((long)num.Value) };
                }
            }
            
            if (value is LuaString str)
            {
                if (long.TryParse(str.Value.Trim(), out var result))
                    return new[] { new LuaInteger(result) };
                
                if (double.TryParse(str.Value.Trim(), out var floatResult) &&
                    floatResult == Math.Truncate(floatResult) &&
                    floatResult >= long.MinValue && floatResult <= long.MaxValue)
                {
                    return new[] { new LuaInteger((long)floatResult) };
                }
            }
            
            return new[] { LuaNil.Instance };
        }
        
        private static LuaValue[] Ult(LuaValue[] args)
        {
            if (args.Length < 2)
                throw new LuaRuntimeException("bad argument #2 to 'ult' (number expected)");
            
            var x = args[0];
            var y = args[1];
            
            if (!x.AsInteger.HasValue)
                throw new LuaRuntimeException("bad argument #1 to 'ult' (number expected)");
            if (!y.AsInteger.HasValue)
                throw new LuaRuntimeException("bad argument #2 to 'ult' (number expected)");
            
            // Unsigned comparison
            var ux = (ulong)x.AsInteger.Value;
            var uy = (ulong)y.AsInteger.Value;
            
            return new[] { new LuaBoolean(ux < uy) };
        }
        
        #endregion
    }
} 