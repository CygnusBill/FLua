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
            mathTable.Set(LuaValue.String("pi"), LuaValue.Number(Math.PI));
            mathTable.Set(LuaValue.String("huge"), LuaValue.Number(double.PositiveInfinity));
            mathTable.Set(LuaValue.String("mininteger"), LuaValue.Integer(long.MinValue));
            mathTable.Set(LuaValue.String("maxinteger"), LuaValue.Integer(long.MaxValue));
            
            // Basic arithmetic functions
            mathTable.Set(LuaValue.String("abs"), new BuiltinFunction(Abs));
            mathTable.Set(LuaValue.String("max"), new BuiltinFunction(Max));
            mathTable.Set(LuaValue.String("min"), new BuiltinFunction(Min));
            mathTable.Set(LuaValue.String("floor"), new BuiltinFunction(Floor));
            mathTable.Set(LuaValue.String("ceil"), new BuiltinFunction(Ceil));
            mathTable.Set(LuaValue.String("fmod"), new BuiltinFunction(FMod));
            mathTable.Set(LuaValue.String("modf"), new BuiltinFunction(Modf));
            
            // Trigonometric functions
            mathTable.Set(LuaValue.String("sin"), new BuiltinFunction(Sin));
            mathTable.Set(LuaValue.String("cos"), new BuiltinFunction(Cos));
            mathTable.Set(LuaValue.String("tan"), new BuiltinFunction(Tan));
            mathTable.Set(LuaValue.String("asin"), new BuiltinFunction(ASin));
            mathTable.Set(LuaValue.String("acos"), new BuiltinFunction(ACos));
            mathTable.Set(LuaValue.String("atan"), new BuiltinFunction(ATan));
            mathTable.Set(LuaValue.String("deg"), new BuiltinFunction(Deg));
            mathTable.Set(LuaValue.String("rad"), new BuiltinFunction(Rad));
            
            // Exponential and logarithmic functions
            mathTable.Set(LuaValue.String("exp"), new BuiltinFunction(Exp));
            mathTable.Set(LuaValue.String("log"), new BuiltinFunction(Log));
            mathTable.Set(LuaValue.String("sqrt"), new BuiltinFunction(Sqrt));
            mathTable.Set(LuaValue.String("pow"), new BuiltinFunction(Pow));
            
            // Random functions
            mathTable.Set(LuaValue.String("random"), new BuiltinFunction(Random));
            mathTable.Set(LuaValue.String("randomseed"), new BuiltinFunction(RandomSeed));
            
            // Type and conversion functions
            mathTable.Set(LuaValue.String("type"), new BuiltinFunction(Type));
            mathTable.Set(LuaValue.String("tointeger"), new BuiltinFunction(ToInteger));
            mathTable.Set(LuaValue.String("ult"), new BuiltinFunction(Ult));
            
            env.SetVariable("math", mathTable);
        }
        
        #region Basic Arithmetic Functions
        
        private static LuaValue[] Abs(LuaValue[] args)
        {
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
                return [LuaValue.Number(Math.Abs(value.AsNumber()))];
            }
            
            throw new LuaRuntimeException("bad argument #1 to 'abs' (number expected)");
        }
        
        private static LuaValue[] Max(LuaValue[] args)
        {
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
                
                if (current.AsNumber() > max.AsNumber())
                    max = current;
            }
            
            return [max];
        }
        
        private static LuaValue[] Min(LuaValue[] args)
        {
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
                
                if (current.AsNumber() < min.AsNumber())
                    min = current;
            }
            
            return [min];
        }
        
        private static LuaValue[] Floor(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'floor' (number expected)");
            
            var value = args[0];
            if (value.IsInteger)
                return [value]; // Integer is already floored
            
            if (value.IsNumber)
            {
                var result = Math.Floor(value.AsNumber());
                // Return integer if it fits in long range
                if (result >= long.MinValue && result <= long.MaxValue && result == Math.Truncate(result))
                    return [LuaValue.Integer((long)result)];
                return [LuaValue.Number(result)];
            }
            
            throw new LuaRuntimeException("bad argument #1 to 'floor' (number expected)");
        }
        
        private static LuaValue[] Ceil(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'ceil' (number expected)");
            
            var value = args[0];
            if (value.IsInteger)
                return [value]; // Integer is already ceiled
            
            if (value.IsNumber)
            {
                var result = Math.Ceiling(value.AsNumber());
                // Return integer if it fits in long range
                if (result >= long.MinValue && result <= long.MaxValue && result == Math.Truncate(result))
                    return [LuaValue.Integer((long)result)];
                return [LuaValue.Number(result)];
            }
            
            throw new LuaRuntimeException("bad argument #1 to 'ceil' (number expected)");
        }
        
        private static LuaValue[] FMod(LuaValue[] args)
        {
            if (args.Length < 2)
                throw new LuaRuntimeException("bad argument #2 to 'fmod' (number expected)");
            
            var x = args[0];
            var y = args[1];
            
            if (!x.IsNumber)
                throw new LuaRuntimeException("bad argument #1 to 'fmod' (number expected)");
            if (!y.IsNumber)
                throw new LuaRuntimeException("bad argument #2 to 'fmod' (number expected)");
            
            if (y.AsNumber() == 0)
                throw new LuaRuntimeException("bad argument #2 to 'fmod' (zero)");
            
            var result = x.AsNumber() % y.AsNumber();
            
            // Preserve integer type if both inputs are integers
            if (x.IsInteger && y.IsInteger)
                return [LuaValue.Integer((long)result)];
            
            return [LuaValue.Number(result)];
        }
        
        private static LuaValue[] Modf(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'modf' (number expected)");
            
            var value = args[0];
            if (!value.IsNumber)
                throw new LuaRuntimeException("bad argument #1 to 'modf' (number expected)");
            
            var num = value.AsNumber();
            var intPart = Math.Truncate(num);
            var fracPart = num - intPart;
            
            LuaValue intResult;
            if (intPart >= long.MinValue && intPart <= long.MaxValue && intPart == Math.Truncate(intPart))
                intResult = LuaValue.Integer((long)intPart);
            else
                intResult = LuaValue.Number(intPart);
            
            return [intResult, LuaValue.Number(fracPart)];
        }
        
        #endregion
        
        #region Trigonometric Functions
        
        private static LuaValue[] Sin(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'sin' (number expected)");
            
            var value = args[0];
            if (!value.IsNumber)
                throw new LuaRuntimeException("bad argument #1 to 'sin' (number expected)");
            
            return [LuaValue.Number(Math.Sin(value.AsNumber()))];
        }
        
        private static LuaValue[] Cos(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'cos' (number expected)");
            
            var value = args[0];
            if (!value.IsNumber)
                throw new LuaRuntimeException("bad argument #1 to 'cos' (number expected)");
            
            return [LuaValue.Number(Math.Cos(value.AsNumber()))];
        }
        
        private static LuaValue[] Tan(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'tan' (number expected)");
            
            var value = args[0];
            if (!value.IsNumber)
                throw new LuaRuntimeException("bad argument #1 to 'tan' (number expected)");
            
            return [LuaValue.Number(Math.Tan(value.AsNumber()))];
        }
        
        private static LuaValue[] ASin(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'asin' (number expected)");
            
            var value = args[0];
            if (!value.IsNumber)
                throw new LuaRuntimeException("bad argument #1 to 'asin' (number expected)");
            
            return [LuaValue.Number(Math.Asin(value.AsNumber()))];
        }
        
        private static LuaValue[] ACos(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'acos' (number expected)");
            
            var value = args[0];
            if (!value.IsNumber)
                throw new LuaRuntimeException("bad argument #1 to 'acos' (number expected)");
            
            return [LuaValue.Number(Math.Acos(value.AsNumber()))];
        }
        
        private static LuaValue[] ATan(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'atan' (number expected)");
            
            var value = args[0];
            if (!value.IsNumber)
                throw new LuaRuntimeException("bad argument #1 to 'atan' (number expected)");
            
            if (args.Length >= 2)
            {
                var x = args[1];
                if (!x.IsNumber)
                    throw new LuaRuntimeException("bad argument #2 to 'atan' (number expected)");
                
                return [LuaValue.Number(Math.Atan2(value.AsNumber(), x.AsNumber()))];
            }
            
            return [LuaValue.Number(Math.Atan(value.AsNumber()))];
        }
        
        private static LuaValue[] Deg(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'deg' (number expected)");
            
            var value = args[0];
            if (!value.IsNumber)
                throw new LuaRuntimeException("bad argument #1 to 'deg' (number expected)");
            
            return [LuaValue.Number(value.AsNumber() * 180.0 / Math.PI)];
        }
        
        private static LuaValue[] Rad(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'rad' (number expected)");
            
            var value = args[0];
            if (!value.IsNumber)
                throw new LuaRuntimeException("bad argument #1 to 'rad' (number expected)");
            
            return [LuaValue.Number(value.AsNumber() * Math.PI / 180.0)];
        }
        
        #endregion
        
        #region Exponential and Logarithmic Functions
        
        private static LuaValue[] Exp(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'exp' (number expected)");
            
            var value = args[0];
            if (!value.IsNumber)
                throw new LuaRuntimeException("bad argument #1 to 'exp' (number expected)");
            
            return [LuaValue.Number(Math.Exp(value.AsNumber()))];
        }
        
        private static LuaValue[] Log(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'log' (number expected)");
            
            var value = args[0];
            if (!value.IsNumber)
                throw new LuaRuntimeException("bad argument #1 to 'log' (number expected)");
            
            if (args.Length >= 2)
            {
                var baseValue = args[1];
                if (!baseValue.IsNumber)
                    throw new LuaRuntimeException("bad argument #2 to 'log' (number expected)");
                
                return [LuaValue.Number(Math.Log(value.AsNumber(), baseValue.AsNumber()))];
            }
            
            return [LuaValue.Number(Math.Log(value.AsNumber()))];
        }
        
        private static LuaValue[] Sqrt(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'sqrt' (number expected)");
            
            var value = args[0];
            if (!value.IsNumber)
                throw new LuaRuntimeException("bad argument #1 to 'sqrt' (number expected)");
            
            return [LuaValue.Number(Math.Sqrt(value.AsNumber()))];
        }
        
        private static LuaValue[] Pow(LuaValue[] args)
        {
            if (args.Length < 2)
                throw new LuaRuntimeException("bad argument #2 to 'pow' (number expected)");
            
            var x = args[0];
            var y = args[1];
            
            if (!x.IsNumber)
                throw new LuaRuntimeException("bad argument #1 to 'pow' (number expected)");
            if (!y.IsNumber)
                throw new LuaRuntimeException("bad argument #2 to 'pow' (number expected)");
            
            return [LuaValue.Number(Math.Pow(x.AsNumber(), y.AsNumber()))];
        }
        
        #endregion
        
        #region Random Functions
        
        private static LuaValue[] Random(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                // Return random float between 0 and 1
                return [LuaValue.Number(_random.NextDouble())];
            }
            else if (args.Length == 1)
            {
                // Return random integer between 1 and n
                var n = args[0];
                if (!n.IsInteger || n.AsInteger() <= 0)
                    throw new LuaRuntimeException("bad argument #1 to 'random' (interval is empty)");
                
                return [LuaValue.Integer(_random.Next(1, (int)n.AsInteger() + 1))];
            }
            else if (args.Length >= 2)
            {
                // Return random integer between m and n
                var m = args[0];
                var n = args[1];
                
                if (!m.IsInteger)
                    throw new LuaRuntimeException("bad argument #1 to 'random' (number expected)");
                if (!n.IsInteger)
                    throw new LuaRuntimeException("bad argument #2 to 'random' (number expected)");
                
                if (m.AsInteger() > n.AsInteger())
                    throw new LuaRuntimeException("bad argument #2 to 'random' (interval is empty)");
                
                return [LuaValue.Integer(_random.Next((int)m.AsInteger(), (int)n.AsInteger() + 1))];
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
                return [LuaValue.Integer(seed), LuaValue.Integer(seed)];
            }
            else
            {
                var seed = args[0];
                if (!seed.IsInteger)
                    throw new LuaRuntimeException("bad argument #1 to 'randomseed' (number expected)");
                
                var seedValue = (int)seed.AsInteger();
                var newRandom = new Random(seedValue);
                // In a full implementation, we'd replace the global random instance
                return [LuaValue.Integer(seedValue), LuaValue.Integer(seedValue)];
            }
        }
        
        #endregion
        
        #region Type and Conversion Functions
        
        private static LuaValue[] Type(LuaValue[] args)
        {
            if (args.Length == 0)
                return [LuaValue.Nil];
            
            var value = args[0];
            var type = value.Type switch
            {
                LuaType.Integer => "integer",
                LuaType.Float => "float",
                _ => "nil"
            };
            
            if (type == "nil")
                return [LuaValue.Nil];
            
            return [LuaValue.String(type)];
        }
        
        private static LuaValue[] ToInteger(LuaValue[] args)
        {
            if (args.Length == 0)
                return [LuaValue.Nil];
            
            var value = args[0];
            
            if (value.IsInteger)
                return [value];
            
            if (value.IsFloat)
            {
                var num = value.AsFloat();
                if (num == Math.Truncate(num) && 
                    num >= long.MinValue && num <= long.MaxValue)
                {
                    return [LuaValue.Integer((long)num)];
                }
            }
            
            if (value.IsString)
            {
                var str = value.AsString().Trim();
                if (long.TryParse(str, out var result))
                    return [LuaValue.Integer(result)];
                
                if (double.TryParse(str, out var floatResult) &&
                    floatResult == Math.Truncate(floatResult) &&
                    floatResult >= long.MinValue && floatResult <= long.MaxValue)
                {
                    return [LuaValue.Integer((long)floatResult)];
                }
            }
            
            return [LuaValue.Nil];
        }
        
        private static LuaValue[] Ult(LuaValue[] args)
        {
            if (args.Length < 2)
                throw new LuaRuntimeException("bad argument #2 to 'ult' (number expected)");
            
            var x = args[0];
            var y = args[1];
            
            if (!x.IsInteger)
                throw new LuaRuntimeException("bad argument #1 to 'ult' (number expected)");
            if (!y.IsInteger)
                throw new LuaRuntimeException("bad argument #2 to 'ult' (number expected)");
            
            // Unsigned comparison
            var ux = (ulong)x.AsInteger();
            var uy = (ulong)y.AsInteger();
            
            return [LuaValue.Boolean(ux < uy)];
        }
        
        #endregion
    }
} 