using System;
using System.Collections.Generic;
using System.Linq;

namespace FLua.Runtime
{
    /// <summary>
    /// Exception thrown when a coroutine yields
    /// </summary>
    public class CoroutineYieldException : Exception
    {
        public LuaValue[] Values { get; }
        
        public CoroutineYieldException(LuaValue[] values)
        {
            Values = values;
        }
    }
    
    // Moved to LuaTypes.cs - this file now only contains CoroutineYieldException and helper code
    /*
    /// <summary>
    /// Represents a Lua coroutine with proper state management
    /// </summary>
    public class LuaCoroutine : LuaValue
    {
        private enum CoroutineStatus
        {
            Dead,
            Suspended,
            Running,
            Normal
        }
        
        /// <summary>
        /// Thread-local storage for the currently running coroutine
        /// </summary>
        [ThreadStatic]
        public static LuaCoroutine? CurrentCoroutine;
        
        private readonly LuaFunction _function;
        private CoroutineStatus _status = CoroutineStatus.Suspended;
        private LuaValue[]? _yieldedValues = null;
        private bool _hasStarted = false;
        private bool _hasYielded = false;
        private LuaValue[]? _resumeArgs = null;
        private LuaValue[]? _finalResult = null;
        
        public LuaCoroutine(LuaFunction function)
        {
            _function = function ?? throw new ArgumentNullException(nameof(function));
        }
        
        /// <summary>
        /// Resumes the coroutine with the given arguments
        /// </summary>
        public LuaValue[] Resume(LuaValue[] args)
        {
            if (_status == CoroutineStatus.Dead)
            {
                return new LuaValue[] { LuaValue.Boolean(false), LuaValue.String("cannot resume dead coroutine") };
            }
            
            if (_status == CoroutineStatus.Running)
            {
                return new LuaValue[] { LuaValue.Boolean(false), LuaValue.String("cannot resume running coroutine") };
            }
            
            var previousCoroutine = CurrentCoroutine;
            CurrentCoroutine = this;
            _status = CoroutineStatus.Running;
            
            try
            {
                _resumeArgs = args;
                
                if (!_hasStarted)
                {
                    // First resume - start the coroutine
                    _hasStarted = true;
                    var results = _function.Call(args);
                    
                    if (_hasYielded)
                    {
                        // The function yielded during execution
                        _status = CoroutineStatus.Suspended;
                        _hasYielded = false; // Reset for next resume
                        return PrependSuccess(_yieldedValues ?? Array.Empty<LuaValue>());
                    }
                    else
                    {
                        // The function completed normally
                        _status = CoroutineStatus.Dead;
                        _finalResult = results;
                        return PrependSuccess(results);
                    }
                }
                else
                {
                    // Resume from previous yield
                    // For the basic implementation, we can't truly resume mid-function
                    // but we can simulate some behavior for simple cases
                    if (_finalResult != null)
                    {
                        _status = CoroutineStatus.Dead;
                        return PrependSuccess(_finalResult);
                    }
                    else
                    {
                        _status = CoroutineStatus.Dead;
                        return new LuaValue[] { LuaValue.Boolean(true) };
                    }
                }
            }
            catch (CoroutineYieldException yieldEx)
            {
                // The coroutine yielded
                _hasYielded = true;
                _yieldedValues = yieldEx.Values;
                _status = CoroutineStatus.Suspended;
                return PrependSuccess(yieldEx.Values);
            }
            catch (LuaRuntimeException ex)
            {
                _status = CoroutineStatus.Dead;
                return new LuaValue[] { LuaValue.Boolean(false), LuaValue.String(ex.Message) };
            }
            catch (Exception ex)
            {
                _status = CoroutineStatus.Dead;
                return new LuaValue[] { LuaValue.Boolean(false), LuaValue.String($"Internal error: {ex.Message}") };
            }
            finally
            {
                CurrentCoroutine = previousCoroutine;
                if (_status != CoroutineStatus.Suspended)
                {
                    _status = _status == CoroutineStatus.Running ? CoroutineStatus.Dead : _status;
                }
            }
        }
        
        /// <summary>
        /// Yields the coroutine with the given values
        /// </summary>
        public static LuaValue[] Yield(LuaValue[] values)
        {
            var currentCoroutine = CurrentCoroutine;
            if (currentCoroutine == null)
            {
                throw new LuaRuntimeException("attempt to yield from outside a coroutine");
            }
            
            // Throw the yield exception to unwind the stack
            throw new CoroutineYieldException(values);
        }
        
        /// <summary>
        /// Gets the status of the coroutine
        /// </summary>
        public string Status => _status switch
        {
            CoroutineStatus.Dead => "dead",
            CoroutineStatus.Suspended => "suspended",
            CoroutineStatus.Running => "running",
            CoroutineStatus.Normal => "normal",
            _ => "unknown"
        };
        
        /// <summary>
        /// Prepends a success boolean to the results
        /// </summary>
        private static LuaValue[] PrependSuccess(LuaValue[] results)
        {
            var newResults = new LuaValue[results.Length + 1];
            newResults[0] = LuaValue.Boolean(true);
            Array.Copy(results, 0, newResults, 1, results.Length);
            return newResults;
        }
        
        /// <summary>
        /// Closes a coroutine (Lua 5.4 feature)
        /// </summary>
        public void Close()
        {
            if (_status == CoroutineStatus.Dead)
                return; // Already closed
            
            // Force the coroutine to dead status
            _status = CoroutineStatus.Dead;
            
            // In a full implementation, this would also call any to-be-closed variables
            // and handle cleanup properly
        }
        
        public override string ToString() => $"thread: {GetHashCode():x8}";
    }
    
    /// <summary>
    /// Coroutine library functions for Lua with full Lua 5.4 compatibility
    /// </summary>
    public static class LuaCoroutineLib
    {
        /// <summary>
        /// Adds coroutine library functions to the environment
        /// </summary>
        public static void AddCoroutineLibrary(LuaEnvironment env)
        {
            var coroutineTable = new LuaTable();
            
            coroutineTable.Set(LuaValue.String("create"), new BuiltinFunction(Create));
            coroutineTable.Set(LuaValue.String("resume"), new BuiltinFunction(Resume));
            coroutineTable.Set(LuaValue.String("yield"), new BuiltinFunction(Yield));
            coroutineTable.Set(LuaValue.String("status"), new BuiltinFunction(Status));
            coroutineTable.Set(LuaValue.String("running"), new BuiltinFunction(Running));
            coroutineTable.Set(LuaValue.String("isyieldable"), new BuiltinFunction(IsYieldable));
            coroutineTable.Set(LuaValue.String("wrap"), new BuiltinFunction(Wrap));
            coroutineTable.Set(LuaValue.String("close"), new BuiltinFunction(Close));
            
            env.SetVariable("coroutine", coroutineTable);
        }
        
        /// <summary>
        /// Creates a new coroutine
        /// </summary>
        private static LuaValue[] Create(LuaValue[] args)
        {
            if (args.Length == 0 || !(args[0] is LuaFunction func))
            {
                throw new LuaRuntimeException("bad argument #1 to 'create' (function expected)");
            }
            
            return new LuaValue[] { new LuaCoroutine(func) };
        }
        
        /// <summary>
        /// Resumes a coroutine
        /// </summary>
        private static LuaValue[] Resume(LuaValue[] args)
        {
            if (args.Length == 0 || !(args[0] is LuaCoroutine co))
            {
                throw new LuaRuntimeException("bad argument #1 to 'resume' (coroutine expected)");
            }
            
            var resumeArgs = new LuaValue[args.Length - 1];
            if (args.Length > 1)
            {
                Array.Copy(args, 1, resumeArgs, 0, args.Length - 1);
            }
            
            return co.Resume(resumeArgs);
        }
        
        /// <summary>
        /// Yields a coroutine
        /// </summary>
        private static LuaValue[] Yield(LuaValue[] args)
        {
            return LuaCoroutine.Yield(args);
        }
        
        /// <summary>
        /// Gets the status of a coroutine
        /// </summary>
        private static LuaValue[] Status(LuaValue[] args)
        {
            if (args.Length == 0 || !(args[0] is LuaCoroutine co))
            {
                throw new LuaRuntimeException("bad argument #1 to 'status' (coroutine expected)");
            }
            
            return new LuaValue[] { LuaValue.String(co.Status) };
        }
        
        /// <summary>
        /// Gets the running coroutine
        /// </summary>
        private static LuaValue[] Running(LuaValue[] args)
        {
            var current = LuaCoroutine.CurrentCoroutine;
            if (current != null)
            {
                return new LuaValue[] { current, LuaValue.Boolean(false) };
            }
            
            return new LuaValue[] { LuaValue.Nil, LuaValue.Boolean(true) };
        }
        
        /// <summary>
        /// Checks if the current thread can yield
        /// </summary>
        private static LuaValue[] IsYieldable(LuaValue[] args)
        {
            // Can yield if we're in a coroutine
            return new LuaValue[] { LuaValue.Boolean(LuaCoroutine.CurrentCoroutine != null) };
        }
        
        /// <summary>
        /// Creates a wrapped coroutine that can be called directly
        /// </summary>
        private static LuaValue[] Wrap(LuaValue[] args)
        {
            if (args.Length == 0 || !(args[0] is LuaFunction func))
            {
                throw new LuaRuntimeException("bad argument #1 to 'wrap' (function expected)");
            }
            
            var coroutine = new LuaCoroutine(func);
            
            // Return a function that resumes the coroutine
            var wrapper = new LuaUserFunction(wrapArgs =>
            {
                var results = coroutine.Resume(wrapArgs);
                
                // Check if the resume was successful
                if (results.Length > 0 && results[0] is LuaBoolean success && !success.Value)
                {
                    // Resume failed - throw the error
                    var errorMessage = results.Length > 1 ? results[1].AsString() : "coroutine error";
                    throw new LuaRuntimeException(errorMessage);
                }
                
                // Return the results (excluding the success flag)
                if (results.Length > 1)
                {
                    var returnValues = new LuaValue[results.Length - 1];
                    Array.Copy(results, 1, returnValues, 0, results.Length - 1);
                    return returnValues;
                }
                
                return Array.Empty<LuaValue>();
            });
            
            return new LuaValue[] { wrapper };
        }
        
        /// <summary>
        /// Closes a coroutine (Lua 5.4 feature)
        /// </summary>
        private static LuaValue[] Close(LuaValue[] args)
        {
            if (args.Length == 0 || !(args[0] is LuaCoroutine co))
            {
                throw new LuaRuntimeException("bad argument #1 to 'close' (coroutine expected)");
            }
            
            // In Lua 5.4, close forces a coroutine to dead status
            // For this implementation, we'll just check if it's already closed
            if (co.Status == "dead")
            {
                return new LuaValue[] { LuaValue.Boolean(true) };
            }
            else if (co.Status == "suspended")
            {
                // Force close - in a full implementation, this would call to-be-closed variables
                return new LuaValue[] { LuaValue.Boolean(true) };
            }
            else
            {
                return new LuaValue[] { LuaValue.Boolean(false), LuaValue.String("cannot close running coroutine") };
            }
        }
    }
    
    /// <summary>
    /// Producer-style coroutine that yields values
    /// </summary>
    public class CoroutineProducer : LuaUserFunction
    {
        private readonly Func<LuaValue[]> _producer;
        private bool _isFinished = false;
        
        public CoroutineProducer(Func<LuaValue[]> producer) : base(args => Array.Empty<LuaValue>())
        {
            _producer = producer;
        }
        
        public override LuaValue[] Call(LuaValue[] arguments)
        {
            if (_isFinished)
            {
                return Array.Empty<LuaValue>();
            }
            
            try
            {
                var values = _producer();
                if (values.Length == 0)
                {
                    _isFinished = true;
                    return Array.Empty<LuaValue>();
                }
                
                // Yield the produced values
                return LuaCoroutine.Yield(values);
            }
            catch (Exception)
            {
                _isFinished = true;
                throw;
            }
        }
    }
    
    /// <summary>
    /// Helper methods for creating common coroutine patterns
    /// </summary>
    public static class CoroutineHelpers
    {
        /// <summary>
        /// Creates a producer coroutine that yields a sequence of values
        /// </summary>
        public static LuaCoroutine CreateProducer(IEnumerable<LuaValue> values)
        {
            var enumerator = values.GetEnumerator();
            
            var producer = new LuaUserFunction(args =>
            {
                var results = new List<LuaValue>();
                
                while (enumerator.MoveNext())
                {
                    LuaCoroutine.Yield(new[] { enumerator.Current });
                }
                
                return Array.Empty<LuaValue>();
            });
            
            return new LuaCoroutine(producer);
        }
        
        /// <summary>
        /// Creates a range coroutine that yields numbers from start to end
        /// </summary>
        public static LuaCoroutine CreateRange(int start, int end, int step = 1)
        {
            var rangeFunc = new LuaUserFunction(args =>
            {
                for (int i = start; step > 0 ? i <= end : i >= end; i += step)
                {
                    LuaCoroutine.Yield(new[] { LuaValue.Integer(i) });
                }
                return Array.Empty<LuaValue>();
            });
            
            return new LuaCoroutine(rangeFunc);
        }
        
        /// <summary>
        /// Creates a Fibonacci sequence coroutine
        /// </summary>
        public static LuaCoroutine CreateFibonacci(int count = int.MaxValue)
        {
            var fibFunc = new LuaUserFunction(args =>
            {
                long a = 0, b = 1;
                int generated = 0;
                
                while (generated < count)
                {
                    LuaCoroutine.Yield(new[] { LuaValue.Integer(a) });
                    (a, b) = (b, a + b);
                    generated++;
                }
                
                return Array.Empty<LuaValue>();
            });
            
            return new LuaCoroutine(fibFunc);
        }
    }
    */
}
