using System;
using System.Collections.Generic;

namespace FLua.Runtime
{
    /// <summary>
    /// Represents a Lua coroutine
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
        
        private readonly LuaFunction _function;
        private readonly Stack<IEnumerator<LuaValue[]>> _callStack = new Stack<IEnumerator<LuaValue[]>>();
        private CoroutineStatus _status = CoroutineStatus.Suspended;
        private LuaValue[] _args = Array.Empty<LuaValue>();
        
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
                return new LuaValue[] { new LuaBoolean(false), new LuaString("cannot resume dead coroutine") };
            }
            
            if (_status == CoroutineStatus.Running)
            {
                return new LuaValue[] { new LuaBoolean(false), new LuaString("cannot resume running coroutine") };
            }
            
            var previousStatus = _status;
            _status = CoroutineStatus.Running;
            
            try
            {
                // First resume
                if (previousStatus == CoroutineStatus.Suspended && _callStack.Count == 0)
                {
                    var results = _function.Call(args);
                    _status = CoroutineStatus.Dead;
                    return PrependSuccess(results);
                }
                
                // Resume from yield
                if (_callStack.Count > 0)
                {
                    var currentCall = _callStack.Peek();
                    _args = args;
                    
                    if (currentCall.MoveNext())
                    {
                        _status = CoroutineStatus.Suspended;
                        return PrependSuccess(currentCall.Current);
                    }
                    else
                    {
                        _callStack.Pop();
                        if (_callStack.Count == 0)
                        {
                            _status = CoroutineStatus.Dead;
                            return new LuaValue[] { new LuaBoolean(true) };
                        }
                        else
                        {
                            return Resume(Array.Empty<LuaValue>());
                        }
                    }
                }
                
                _status = CoroutineStatus.Dead;
                return new LuaValue[] { new LuaBoolean(true) };
            }
            catch (LuaRuntimeException ex)
            {
                _status = CoroutineStatus.Dead;
                return new LuaValue[] { new LuaBoolean(false), new LuaString(ex.Message) };
            }
            catch (Exception ex)
            {
                _status = CoroutineStatus.Dead;
                return new LuaValue[] { new LuaBoolean(false), new LuaString($"Internal error: {ex.Message}") };
            }
        }
        
        /// <summary>
        /// Yields the coroutine with the given values
        /// </summary>
        public LuaValue[] Yield(LuaValue[] values)
        {
            if (_status != CoroutineStatus.Running)
            {
                throw new LuaRuntimeException("attempt to yield from outside a coroutine");
            }
            
            _status = CoroutineStatus.Suspended;
            return _args;
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
            newResults[0] = new LuaBoolean(true);
            Array.Copy(results, 0, newResults, 1, results.Length);
            return newResults;
        }
        
        public override string ToString() => $"thread: {GetHashCode():x8}";
    }
    
    /// <summary>
    /// Coroutine library functions for Lua
    /// </summary>
    public static class LuaCoroutineLib
    {
        /// <summary>
        /// Adds coroutine library functions to the environment
        /// </summary>
        public static void AddCoroutineLibrary(LuaEnvironment env)
        {
            var coroutineTable = new LuaTable();
            
            coroutineTable.Set(new LuaString("create"), new BuiltinFunction(Create));
            coroutineTable.Set(new LuaString("resume"), new BuiltinFunction(Resume));
            coroutineTable.Set(new LuaString("yield"), new BuiltinFunction(Yield));
            coroutineTable.Set(new LuaString("status"), new BuiltinFunction(Status));
            coroutineTable.Set(new LuaString("running"), new BuiltinFunction(Running));
            coroutineTable.Set(new LuaString("isyieldable"), new BuiltinFunction(IsYieldable));
            
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
            // This will be handled by the interpreter
            throw new LuaRuntimeException("attempt to yield from outside a coroutine");
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
            
            return new LuaValue[] { new LuaString(co.Status) };
        }
        
        /// <summary>
        /// Gets the running coroutine
        /// </summary>
        private static LuaValue[] Running(LuaValue[] args)
        {
            // For now, just return nil (no coroutine is running)
            return new LuaValue[] { LuaNil.Instance, new LuaBoolean(false) };
        }
        
        /// <summary>
        /// Checks if the current thread can yield
        /// </summary>
        private static LuaValue[] IsYieldable(LuaValue[] args)
        {
            // For now, always return false
            return new LuaValue[] { new LuaBoolean(false) };
        }
    }
} 