using System;
using System.Collections.Generic;

namespace FLua.Runtime
{
    /// <summary>
    /// Coroutine library implementation for Lua 5.4
    /// </summary>
    public static class LuaCoroutineLib
    {
        /// <summary>
        /// Thread-local storage for the currently running coroutine
        /// </summary>
        [ThreadStatic]
        private static LuaCoroutine? CurrentCoroutine;

        /// <summary>
        /// Adds coroutine library functions to the environment
        /// </summary>
        public static void AddCoroutineLibrary(LuaEnvironment env)
        {
            var coroutineTable = new LuaTable();
            
            coroutineTable.Set(LuaValue.String("create"), LuaValue.Function(new BuiltinFunction(Create)));
            coroutineTable.Set(LuaValue.String("resume"), LuaValue.Function(new BuiltinFunction(Resume)));
            coroutineTable.Set(LuaValue.String("yield"), LuaValue.Function(new BuiltinFunction(Yield)));
            coroutineTable.Set(LuaValue.String("status"), LuaValue.Function(new BuiltinFunction(Status)));
            coroutineTable.Set(LuaValue.String("running"), LuaValue.Function(new BuiltinFunction(Running)));
            coroutineTable.Set(LuaValue.String("isyieldable"), LuaValue.Function(new BuiltinFunction(IsYieldable)));
            coroutineTable.Set(LuaValue.String("wrap"), LuaValue.Function(new BuiltinFunction(Wrap)));
            coroutineTable.Set(LuaValue.String("close"), LuaValue.Function(new BuiltinFunction(Close)));
            
            env.SetVariable("coroutine", LuaValue.Table(coroutineTable));
        }
        
        /// <summary>
        /// Creates a new coroutine
        /// </summary>
        private static LuaValue[] Create(LuaValue[] args)
        {
            if (args.Length == 0 || !args[0].IsFunction)
            {
                throw new LuaRuntimeException("bad argument #1 to 'create' (function expected)");
            }
            
            var function = args[0].AsFunction<LuaFunction>();
            var coroutine = new LuaCoroutine(function);
            
            return new LuaValue[] { LuaValue.Thread(coroutine) };
        }
        
        /// <summary>
        /// Resumes a coroutine
        /// </summary>
        private static LuaValue[] Resume(LuaValue[] args)
        {
            if (args.Length == 0 || !args[0].IsThread)
            {
                throw new LuaRuntimeException("bad argument #1 to 'resume' (thread expected)");
            }
            
            var coroutine = args[0].AsThread<LuaCoroutine>();
            var resumeArgs = new LuaValue[args.Length - 1];
            if (args.Length > 1)
            {
                Array.Copy(args, 1, resumeArgs, 0, args.Length - 1);
            }
            
            // Check coroutine status
            if (coroutine.Status == LuaCoroutine.CoroutineStatus.Dead)
            {
                return new LuaValue[] { LuaValue.Boolean(false), LuaValue.String("cannot resume dead coroutine") };
            }
            
            if (coroutine.Status == LuaCoroutine.CoroutineStatus.Running)
            {
                return new LuaValue[] { LuaValue.Boolean(false), LuaValue.String("cannot resume running coroutine") };
            }
            
            // Save current coroutine and set this one as running
            var previousCoroutine = CurrentCoroutine;
            CurrentCoroutine = coroutine;
            coroutine.Status = LuaCoroutine.CoroutineStatus.Running;
            
            try
            {
                // For now, we'll just run the function to completion
                // A real implementation would need interpreter support for yield points
                var results = coroutine.Function.Call(resumeArgs);
                
                // Mark as dead after completion
                coroutine.Status = LuaCoroutine.CoroutineStatus.Dead;
                
                // Prepend success flag
                var fullResults = new LuaValue[results.Length + 1];
                fullResults[0] = LuaValue.Boolean(true);
                Array.Copy(results, 0, fullResults, 1, results.Length);
                
                return fullResults;
            }
            catch (CoroutineYieldException yieldEx)
            {
                // Coroutine yielded
                coroutine.Status = LuaCoroutine.CoroutineStatus.Suspended;
                coroutine.YieldedValues.Enqueue(yieldEx.Values);
                
                // Return success + yielded values
                var fullResults = new LuaValue[yieldEx.Values.Length + 1];
                fullResults[0] = LuaValue.Boolean(true);
                Array.Copy(yieldEx.Values, 0, fullResults, 1, yieldEx.Values.Length);
                
                return fullResults;
            }
            catch (LuaRuntimeException ex)
            {
                coroutine.Status = LuaCoroutine.CoroutineStatus.Dead;
                return new LuaValue[] { LuaValue.Boolean(false), LuaValue.String(ex.Message) };
            }
            catch (Exception ex)
            {
                coroutine.Status = LuaCoroutine.CoroutineStatus.Dead;
                return new LuaValue[] { LuaValue.Boolean(false), LuaValue.String($"Internal error: {ex.Message}") };
            }
            finally
            {
                CurrentCoroutine = previousCoroutine;
            }
        }
        
        /// <summary>
        /// Yields from a coroutine
        /// </summary>
        private static LuaValue[] Yield(LuaValue[] args)
        {
            if (CurrentCoroutine == null)
            {
                throw new LuaRuntimeException("attempt to yield from outside a coroutine");
            }
            
            throw new CoroutineYieldException(args);
        }
        
        /// <summary>
        /// Gets the status of a coroutine
        /// </summary>
        private static LuaValue[] Status(LuaValue[] args)
        {
            if (args.Length == 0 || !args[0].IsThread)
            {
                throw new LuaRuntimeException("bad argument #1 to 'status' (thread expected)");
            }
            
            var coroutine = args[0].AsThread<LuaCoroutine>();
            var status = coroutine.Status switch
            {
                LuaCoroutine.CoroutineStatus.Suspended => "suspended",
                LuaCoroutine.CoroutineStatus.Running => "running",
                LuaCoroutine.CoroutineStatus.Normal => "normal",
                LuaCoroutine.CoroutineStatus.Dead => "dead",
                _ => "unknown"
            };
            
            return new LuaValue[] { LuaValue.String(status) };
        }
        
        /// <summary>
        /// Gets the running coroutine
        /// </summary>
        private static LuaValue[] Running(LuaValue[] args)
        {
            if (CurrentCoroutine != null)
            {
                return new LuaValue[] { LuaValue.Thread(CurrentCoroutine), LuaValue.Boolean(false) };
            }
            
            // Not in a coroutine - return nil and true (main thread)
            return new LuaValue[] { LuaValue.Nil, LuaValue.Boolean(true) };
        }
        
        /// <summary>
        /// Checks if current thread can yield
        /// </summary>
        private static LuaValue[] IsYieldable(LuaValue[] args)
        {
            return new LuaValue[] { LuaValue.Boolean(CurrentCoroutine != null) };
        }
        
        /// <summary>
        /// Creates a wrapped coroutine
        /// </summary>
        private static LuaValue[] Wrap(LuaValue[] args)
        {
            if (args.Length == 0 || !args[0].IsFunction)
            {
                throw new LuaRuntimeException("bad argument #1 to 'wrap' (function expected)");
            }
            
            var function = args[0].AsFunction<LuaFunction>();
            var coroutine = new LuaCoroutine(function);
            
            // Return a function that resumes the coroutine
            var wrapper = new BuiltinFunction(wrapArgs =>
            {
                // Create full args array with coroutine as first argument
                var resumeArgs = new LuaValue[wrapArgs.Length + 1];
                resumeArgs[0] = LuaValue.Thread(coroutine);
                Array.Copy(wrapArgs, 0, resumeArgs, 1, wrapArgs.Length);
                
                var results = Resume(resumeArgs);
                
                // Check if resume was successful
                if (results.Length > 0 && results[0].IsBoolean && !results[0].AsBoolean())
                {
                    // Resume failed - throw the error
                    var errorMessage = results.Length > 1 ? results[1].AsString() : "coroutine error";
                    throw new LuaRuntimeException(errorMessage);
                }
                
                // Return results without the success flag
                if (results.Length > 1)
                {
                    var returnValues = new LuaValue[results.Length - 1];
                    Array.Copy(results, 1, returnValues, 0, results.Length - 1);
                    return returnValues;
                }
                
                return Array.Empty<LuaValue>();
            });
            
            return new LuaValue[] { LuaValue.Function(wrapper) };
        }
        
        /// <summary>
        /// Closes a coroutine (Lua 5.4 feature)
        /// </summary>
        private static LuaValue[] Close(LuaValue[] args)
        {
            if (args.Length == 0 || !args[0].IsThread)
            {
                throw new LuaRuntimeException("bad argument #1 to 'close' (thread expected)");
            }
            
            var coroutine = args[0].AsThread<LuaCoroutine>();
            
            if (coroutine.Status == LuaCoroutine.CoroutineStatus.Dead)
            {
                return new LuaValue[] { LuaValue.Boolean(true) };
            }
            else if (coroutine.Status == LuaCoroutine.CoroutineStatus.Suspended)
            {
                coroutine.Status = LuaCoroutine.CoroutineStatus.Dead;
                return new LuaValue[] { LuaValue.Boolean(true) };
            }
            else
            {
                return new LuaValue[] { LuaValue.Boolean(false), LuaValue.String("cannot close running coroutine") };
            }
        }
    }
}