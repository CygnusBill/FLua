using System;
using FLua.Common;

namespace FLua.Runtime
{
    /// <summary>
    /// Result-based implementation of Lua math library functions.
    /// Eliminates exception-based error handling in favor of explicit Result types.
    /// </summary>
    public static class ResultLuaMathLib
    {
        private static readonly Random _random = new Random();
        
        /// <summary>
        /// Adds the Result-based math library to the Lua environment
        /// </summary>
        public static void AddResultMathLibrary(LuaEnvironment env)
        {
            var mathTable = new LuaTable();
            
            // Initialize as built-in library with fast path optimization
            mathTable.InitializeAsBuiltinLibrary("math");
            
            // Constants (no fast path needed, just stored values)
            mathTable.Set(LuaValue.String("pi"), LuaValue.Number(Math.PI));
            mathTable.Set(LuaValue.String("huge"), LuaValue.Number(double.PositiveInfinity));
            mathTable.Set(LuaValue.String("mininteger"), LuaValue.Integer(long.MinValue));
            mathTable.Set(LuaValue.String("maxinteger"), LuaValue.Integer(long.MaxValue));
            
            // Result-based wrapper functions that handle errors gracefully
            mathTable.Set(LuaValue.String("abs"), new BuiltinFunction(args => AbsResult(args).Match(
                success => success,
                failure => throw new LuaRuntimeException(failure))));
            mathTable.EnableFastPath("abs");
            
            mathTable.Set(LuaValue.String("max"), new BuiltinFunction(args => MaxResult(args).Match(
                success => success,
                failure => throw new LuaRuntimeException(failure))));
            mathTable.EnableFastPath("max");
            
            mathTable.Set(LuaValue.String("min"), new BuiltinFunction(args => MinResult(args).Match(
                success => success,
                failure => throw new LuaRuntimeException(failure))));
            mathTable.EnableFastPath("min");
            
            mathTable.Set(LuaValue.String("floor"), new BuiltinFunction(args => FloorResult(args).Match(
                success => success,
                failure => throw new LuaRuntimeException(failure))));
            mathTable.EnableFastPath("floor");
            
            mathTable.Set(LuaValue.String("ceil"), new BuiltinFunction(args => CeilResult(args).Match(
                success => success,
                failure => throw new LuaRuntimeException(failure))));
            mathTable.EnableFastPath("ceil");
            
            // Add other functions...
            mathTable.Set(LuaValue.String("sin"), new BuiltinFunction(args => SinResult(args).Match(
                success => success,
                failure => throw new LuaRuntimeException(failure))));
            mathTable.EnableFastPath("sin");
            
            mathTable.Set(LuaValue.String("cos"), new BuiltinFunction(args => CosResult(args).Match(
                success => success,
                failure => throw new LuaRuntimeException(failure))));
            mathTable.EnableFastPath("cos");
            
            mathTable.Set(LuaValue.String("sqrt"), new BuiltinFunction(args => SqrtResult(args).Match(
                success => success,
                failure => throw new LuaRuntimeException(failure))));
            mathTable.EnableFastPath("sqrt");
            
            mathTable.Set(LuaValue.String("pow"), new BuiltinFunction(args => PowResult(args).Match(
                success => success,
                failure => throw new LuaRuntimeException(failure))));
            mathTable.EnableFastPath("pow");
            
            env.SetVariable("math", mathTable);
        }
        
        #region Result-Based Math Functions
        
        /// <summary>
        /// Result-based absolute value function - no exceptions
        /// </summary>
        public static Result<LuaValue[]> AbsResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'abs' (number expected)");
            
            var value = args[0];
            
            if (value.IsInteger)
            {
                var intVal = value.AsInteger();
                if (intVal == long.MinValue)
                    return Result<LuaValue[]>.Success([LuaValue.Integer(long.MinValue)]); // MinValue abs is itself in Lua
                return Result<LuaValue[]>.Success([LuaValue.Integer(Math.Abs(intVal))]);
            }
            
            return value.TryAsDouble()
                .Map(num => new LuaValue[] { LuaValue.Number(Math.Abs(num)) })
                .Match(
                    success => Result<LuaValue[]>.Success(success),
                    failure => Result<LuaValue[]>.Failure("bad argument #1 to 'abs' (number expected)"));
        }
        
        /// <summary>
        /// Result-based maximum function - no exceptions
        /// </summary>
        public static Result<LuaValue[]> MaxResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'max' (value expected)");
            
            var max = args[0];
            return max.TryAsDouble()
                .Bind(_ =>
                {
                    // Validate all arguments are numbers
                    for (int i = 1; i < args.Length; i++)
                    {
                        var current = args[i];
                        var currentResult = current.TryAsDouble();
                        if (currentResult.IsFailure)
                            return Result<LuaValue[]>.Failure($"bad argument #{i + 1} to 'max' (number expected)");
                        
                        if (currentResult.Value > max.AsDouble())
                            max = current;
                    }
                    
                    return Result<LuaValue[]>.Success([max]);
                })
                .Match(
                    success => success,
                    failure => Result<LuaValue[]>.Failure("bad argument #1 to 'max' (number expected)"));
        }
        
        /// <summary>
        /// Result-based minimum function - no exceptions
        /// </summary>
        public static Result<LuaValue[]> MinResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'min' (value expected)");
            
            var min = args[0];
            return min.TryAsDouble()
                .Bind(_ =>
                {
                    // Validate all arguments are numbers
                    for (int i = 1; i < args.Length; i++)
                    {
                        var current = args[i];
                        var currentResult = current.TryAsDouble();
                        if (currentResult.IsFailure)
                            return Result<LuaValue[]>.Failure($"bad argument #{i + 1} to 'min' (number expected)");
                        
                        if (currentResult.Value < min.AsDouble())
                            min = current;
                    }
                    
                    return Result<LuaValue[]>.Success([min]);
                })
                .Match(
                    success => success,
                    failure => Result<LuaValue[]>.Failure("bad argument #1 to 'min' (number expected)"));
        }
        
        /// <summary>
        /// Result-based floor function - no exceptions
        /// </summary>
        public static Result<LuaValue[]> FloorResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'floor' (number expected)");
            
            var value = args[0];
            
            if (value.IsInteger)
                return Result<LuaValue[]>.Success([value]); // Integer is already floored
            
            return value.TryAsDouble()
                .Map(num =>
                {
                    var result = Math.Floor(num);
                    // Return integer if it fits in long range
                    if (result >= long.MinValue && result <= long.MaxValue && result == Math.Truncate(result))
                        return new LuaValue[] { LuaValue.Integer((long)result) };
                    return new LuaValue[] { LuaValue.Number(result) };
                });
        }
        
        /// <summary>
        /// Result-based ceiling function - no exceptions
        /// </summary>
        public static Result<LuaValue[]> CeilResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'ceil' (number expected)");
            
            var value = args[0];
            
            if (value.IsInteger)
                return Result<LuaValue[]>.Success([value]); // Integer is already ceiled
            
            return value.TryAsDouble()
                .Map(num =>
                {
                    var result = Math.Ceiling(num);
                    // Return integer if it fits in long range
                    if (result >= long.MinValue && result <= long.MaxValue && result == Math.Truncate(result))
                        return new LuaValue[] { LuaValue.Integer((long)result) };
                    return new LuaValue[] { LuaValue.Number(result) };
                });
        }
        
        /// <summary>
        /// Result-based sine function - no exceptions
        /// </summary>
        public static Result<LuaValue[]> SinResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'sin' (number expected)");
            
            return args[0].TryAsDouble()
                .Map(value => new LuaValue[] { LuaValue.Float(Math.Sin(value)) });
        }
        
        /// <summary>
        /// Result-based cosine function - no exceptions
        /// </summary>
        public static Result<LuaValue[]> CosResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'cos' (number expected)");
            
            return args[0].TryAsDouble()
                .Map(value => new LuaValue[] { LuaValue.Float(Math.Cos(value)) });
        }
        
        /// <summary>
        /// Result-based square root function - no exceptions
        /// </summary>
        public static Result<LuaValue[]> SqrtResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'sqrt' (number expected)");
            
            return args[0].TryAsDouble()
                .Map(value => new LuaValue[] { LuaValue.Float(Math.Sqrt(value)) });
        }
        
        /// <summary>
        /// Result-based power function - no exceptions
        /// </summary>
        public static Result<LuaValue[]> PowResult(LuaValue[] args)
        {
            if (args.Length < 2)
                return Result<LuaValue[]>.Failure("bad argument #2 to 'pow' (number expected)");
            
            var x = args[0];
            var y = args[1];
            
            return x.TryAsDouble()
                .Bind(xVal => y.TryAsDouble()
                    .Map(yVal => new LuaValue[] { LuaValue.Float(Math.Pow(xVal, yVal)) })
                    .Match(
                        success => Result<LuaValue[]>.Success(success),
                        failure => Result<LuaValue[]>.Failure("bad argument #2 to 'pow' (number expected)")))
                .Match(
                    success => success,
                    failure => Result<LuaValue[]>.Failure("bad argument #1 to 'pow' (number expected)"));
        }
        
        /// <summary>
        /// Result-based floating-point modulo function - no exceptions
        /// </summary>
        public static Result<LuaValue[]> FModResult(LuaValue[] args)
        {
            if (args.Length < 2)
                return Result<LuaValue[]>.Failure("bad argument #2 to 'fmod' (number expected)");
            
            var x = args[0];
            var y = args[1];
            
            return x.TryAsDouble()
                .Bind(xVal => y.TryAsDouble()
                    .Bind(yVal =>
                    {
                        if (yVal == 0)
                            return Result<LuaValue[]>.Failure("bad argument #2 to 'fmod' (zero)");
                        
                        var result = xVal % yVal;
                        
                        // Preserve integer type if both inputs are integers
                        if (x.IsInteger && y.IsInteger)
                            return Result<LuaValue[]>.Success([LuaValue.Integer((long)result)]);
                        
                        return Result<LuaValue[]>.Success([LuaValue.Float(result)]);
                    })
                    .Match(
                        success => success,
                        failure => Result<LuaValue[]>.Failure("bad argument #2 to 'fmod' (number expected)")))
                .Match(
                    success => success,
                    failure => Result<LuaValue[]>.Failure("bad argument #1 to 'fmod' (number expected)"));
        }
        
        /// <summary>
        /// Result-based modf function (returns integer and fractional parts) - no exceptions
        /// </summary>
        public static Result<LuaValue[]> ModfResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'modf' (number expected)");
            
            return args[0].TryAsDouble()
                .Map(num =>
                {
                    var intPart = Math.Truncate(num);
                    var fracPart = num - intPart;
                    
                    var intResult = LuaValue.Number(intPart);
                    return new LuaValue[] { intResult, LuaValue.Float(fracPart) };
                });
        }
        
        /// <summary>
        /// Result-based random number generator - no exceptions
        /// </summary>
        public static Result<LuaValue[]> RandomResult(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                // Return random float between 0 and 1
                return Result<LuaValue[]>.Success([LuaValue.Number(_random.NextDouble())]);
            }
            else if (args.Length == 1)
            {
                // Return random integer between 1 and n
                return args[0].TryAsIntegerValue()
                    .Bind(n =>
                    {
                        if (n <= 0)
                            return Result<LuaValue[]>.Failure("bad argument #1 to 'random' (interval is empty)");
                        
                        return Result<LuaValue[]>.Success([LuaValue.Integer(_random.Next(1, (int)n + 1))]);
                    });
            }
            else if (args.Length >= 2)
            {
                // Return random integer between m and n
                var m = args[0];
                var n = args[1];
                
                return m.TryAsIntegerValue()
                    .Bind(mVal => n.TryAsIntegerValue()
                        .Bind(nVal =>
                        {
                            if (mVal > nVal)
                                return Result<LuaValue[]>.Failure("bad argument #2 to 'random' (interval is empty)");
                            
                            return Result<LuaValue[]>.Success([LuaValue.Integer(_random.Next((int)mVal, (int)nVal + 1))]);
                        })
                        .Match(
                            success => success,
                            failure => Result<LuaValue[]>.Failure("bad argument #2 to 'random' (number expected)")))
                    .Match(
                        success => success,
                        failure => Result<LuaValue[]>.Failure("bad argument #1 to 'random' (number expected)"));
            }
            
            return Result<LuaValue[]>.Failure("bad arguments to 'random'");
        }
        
        /// <summary>
        /// Result-based random seed function - no exceptions
        /// </summary>
        public static Result<LuaValue[]> RandomSeedResult(LuaValue[] args)
        {
            if (args.Length == 0)
            {
                // Use current time as seed
                var seed = Environment.TickCount;
                var newRandom = new Random(seed);
                // In a full implementation, we'd replace the global random instance
                return Result<LuaValue[]>.Success([LuaValue.Integer(seed), LuaValue.Integer(seed)]);
            }
            else
            {
                return args[0].TryAsIntegerValue()
                    .Map(seedValue =>
                    {
                        var seedInt = (int)seedValue;
                        var newRandom = new Random(seedInt);
                        // In a full implementation, we'd replace the global random instance
                        return new LuaValue[] { LuaValue.Integer(seedInt), LuaValue.Integer(seedInt) };
                    });
            }
        }
        
        /// <summary>
        /// Result-based unsigned less-than comparison - no exceptions
        /// </summary>
        public static Result<LuaValue[]> UltResult(LuaValue[] args)
        {
            if (args.Length < 2)
                return Result<LuaValue[]>.Failure("bad argument #2 to 'ult' (number expected)");
            
            var x = args[0];
            var y = args[1];
            
            return x.TryAsIntegerValue()
                .Bind(xVal => y.TryAsIntegerValue()
                    .Map(yVal =>
                    {
                        // Unsigned comparison
                        var ux = (ulong)xVal;
                        var uy = (ulong)yVal;
                        
                        return new LuaValue[] { LuaValue.Boolean(ux < uy) };
                    })
                    .Match(
                        success => Result<LuaValue[]>.Success(success),
                        failure => Result<LuaValue[]>.Failure("bad argument #2 to 'ult' (number expected)")))
                .Match(
                    success => success,
                    failure => Result<LuaValue[]>.Failure("bad argument #1 to 'ult' (number expected)"));
        }
        
        #endregion
        
        #region Fast Path Integration
        
        /// <summary>
        /// Fast path math function calls - integrates with Result pattern
        /// </summary>
        public static Result<LuaValue[]> TryFastMathFunctionCall(string functionName, LuaValue[] args)
        {
            return functionName switch
            {
                "abs" => AbsResult(args),
                "max" => MaxResult(args),
                "min" => MinResult(args),
                "floor" => FloorResult(args),
                "ceil" => CeilResult(args),
                "sin" => SinResult(args),
                "cos" => CosResult(args),
                "sqrt" => SqrtResult(args),
                "pow" => PowResult(args),
                "fmod" => FModResult(args),
                "modf" => ModfResult(args),
                "random" => RandomResult(args),
                "randomseed" => RandomSeedResult(args),
                "ult" => UltResult(args),
                _ => Result<LuaValue[]>.Failure($"Unknown math function: {functionName}")
            };
        }
        
        #endregion
    }
}