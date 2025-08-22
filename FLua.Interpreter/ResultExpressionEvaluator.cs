using System;
using System.Linq;
using FLua.Ast;
using FLua.Common;
using FLua.Common.Diagnostics;
using FLua.Runtime;
using Microsoft.FSharp.Collections;

namespace FLua.Interpreter
{
    /// <summary>
    /// Result-based implementation of expression evaluation using visitor pattern.
    /// This eliminates exceptions for control flow and provides explicit error handling.
    /// </summary>
    public class ResultExpressionEvaluator : IResultExpressionVisitor<LuaValue[]>
    {
        private LuaEnvironment _environment;

        public ResultExpressionEvaluator(LuaEnvironment environment)
        {
            _environment = environment;
        }

        /// <summary>
        /// Updates the current environment
        /// </summary>
        public void SetEnvironment(LuaEnvironment environment)
        {
            _environment = environment;
        }

        /// <summary>
        /// Main entry point for evaluating expressions using Result-based visitor pattern
        /// </summary>
        public Result<LuaValue[]> Evaluate(Expr expr)
        {
            return Visitor.dispatchExprResult(this, expr);
        }

        public Result<LuaValue[]> VisitLiteral(Literal literal)
        {
            try
            {
                return Result<LuaValue[]>.Success([EvaluateLiteral(literal)]);
            }
            catch (Exception ex)
            {
                return Result<LuaValue[]>.Failure($"Failed to evaluate literal: {ex.Message}");
            }
        }

        public Result<LuaValue[]> VisitVar(string name)
        {
            try
            {
                var value = _environment.GetVariable(name);
                return Result<LuaValue[]>.Success([value]);
            }
            catch (LuaRuntimeException ex) when (ex.ErrorCode == "UNKNOWN_VAR")
            {
                return Result<LuaValue[]>.Failure($"Unknown variable '{name}'");
            }
            catch (Exception ex)
            {
                return Result<LuaValue[]>.Failure($"Failed to get variable '{name}': {ex.Message}");
            }
        }

        public Result<LuaValue[]> VisitVarPos(string name, SourceLocation location)
        {
            var result = VisitVar(name);
            if (result.IsFailure && location != null)
            {
                // Enhance error message with location info
                return Result<LuaValue[]>.Failure($"{result.Error} at {location.FileName}:{location.Line}:{location.Column}");
            }
            return result;
        }

        public Result<LuaValue[]> VisitTableAccess(Expr table, Expr key)
        {
            return Evaluate(table)
                .Bind(tableValues =>
                {
                    if (tableValues.Length == 0)
                        return Result<LuaValue[]>.Failure("Table expression returned no values");

                    var tableValue = tableValues[0];
                    
                    return Evaluate(key)
                        .Bind(keyValues =>
                        {
                            if (keyValues.Length == 0)
                                return Result<LuaValue[]>.Failure("Key expression returned no values");

                            var keyValue = keyValues[0];

                            return tableValue.TryAsTable<LuaTable>()
                                .Map(luaTable => new LuaValue[] { luaTable.Get(keyValue) });
                        });
                });
        }

        public Result<LuaValue[]> VisitTableConstructor(FSharpList<TableField> fields)
        {
            var table = new LuaTable();
            int arrayIndex = 1;

            foreach (var field in fields)
            {
                var fieldResult = field switch
                {
                    TableField.ExprField exprField => 
                        Evaluate(exprField.Item)
                            .Map(values => 
                            {
                                if (values.Length > 0)
                                    table.Set(LuaValue.Integer(arrayIndex++), values[0]);
                                return true;
                            }),

                    TableField.NamedField namedField =>
                        Evaluate(namedField.Item2)
                            .Map(values =>
                            {
                                if (values.Length > 0)
                                    table.Set(LuaValue.String(namedField.Item1), values[0]);
                                return true;
                            }),

                    TableField.KeyField keyField =>
                        Evaluate(keyField.Item1)
                            .Bind(keyValues =>
                            {
                                if (keyValues.Length == 0)
                                    return Result<bool>.Failure("Key expression returned no values");

                                return Evaluate(keyField.Item2)
                                    .Map(valueValues =>
                                    {
                                        if (valueValues.Length > 0)
                                            table.Set(keyValues[0], valueValues[0]);
                                        return true;
                                    });
                            }),

                    _ => Result<bool>.Failure($"Unknown field type: {field.GetType().Name}")
                };

                if (fieldResult.IsFailure)
                    return Result<LuaValue[]>.Failure($"Failed to evaluate table field: {fieldResult.Error}");
            }

            return Result<LuaValue[]>.Success([LuaValue.Table(table)]);
        }

        public Result<LuaValue[]> VisitFunctionDef(FunctionDef funcDef)
        {
            try
            {
                // Convert F# list to array
                var parameters = funcDef.Parameters.ToArray()
                    .Select(p => p.IsNamed ? ((Parameter.Named)p).Item1 : "...")
                    .ToArray();

                var function = new LuaUserFunction(parameters, funcDef, _environment, funcDef.IsVararg);
                return Result<LuaValue[]>.Success([LuaValue.Function(function)]);
            }
            catch (Exception ex)
            {
                return Result<LuaValue[]>.Failure($"Failed to create function: {ex.Message}");
            }
        }

        public Result<LuaValue[]> VisitFunctionCall(Expr func, FSharpList<Expr> args)
        {
            return EvaluateFunctionCallInternal(func, args);
        }

        public Result<LuaValue[]> VisitFunctionCallPos(Expr func, FSharpList<Expr> args, SourceLocation location)
        {
            var result = EvaluateFunctionCallInternal(func, args);
            if (result.IsFailure && location != null)
            {
                // Enhance error message with location info
                return Result<LuaValue[]>.Failure($"{result.Error} at {location.FileName}:{location.Line}:{location.Column}");
            }
            return result;
        }

        public Result<LuaValue[]> VisitMethodCall(Expr obj, string methodName, FSharpList<Expr> args)
        {
            return Evaluate(obj)
                .Bind(objValues =>
                {
                    if (objValues.Length == 0)
                        return Result<LuaValue[]>.Failure("Object expression returned no values");

                    var objValue = objValues[0];

                    // Fast path for string method calls
                    if (objValue.IsString)
                    {
                        return objValue.TryAsString()
                            .Bind(str =>
                            {
                                // Evaluate arguments
                                var argResults = args.ToArray().Select(arg => Evaluate(arg)).ToArray();
                                var combinedArgs = Result.Combine(argResults);

                                return combinedArgs.Bind(argArrays =>
                                {
                                    var stringArgs = argArrays.SelectMany(arr => arr).ToArray();

                                    // Check if the string library allows fast path for this method
                                    var stringValue = _environment.GetVariable("string");
                                    if (stringValue.IsTable)
                                    {
                                        return stringValue.TryAsTable<LuaTable>()
                                            .Bind(stringTable =>
                                            {
                                                if (stringTable.CanUseFastPath(methodName))
                                                {
                                                    var fastResult = LuaOperations.TryFastStringMethodCall(str, methodName, stringArgs);
                                                    if (fastResult.HasValue)
                                                    {
                                                        return Result<LuaValue[]>.Success([fastResult.Value]);
                                                    }
                                                }
                                                
                                                // Fall through to normal method call
                                                return CallTableMethod(objValue, stringTable, methodName, stringArgs);
                                            })
;
                                    }

                                    return Result<LuaValue[]>.Failure("String library not available");
                                });
                            })
;
                    }

                    // Fall back to table lookup for other methods
                    var argResults = args.ToArray().Select(arg => Evaluate(arg)).ToArray();
                    var combinedArgs = Result.Combine(argResults);

                    return combinedArgs.Bind(argArrays =>
                    {
                        var argValues = argArrays.SelectMany(arr => arr).ToArray();

                        return objValue.TryAsTable<LuaTable>()
                            .Bind(table => CallTableMethod(objValue, table, methodName, argValues))
;
                    });
                });
        }

        public Result<LuaValue[]> VisitUnary(UnaryOp op, Expr expr)
        {
            return Evaluate(expr)
                .Bind(values =>
                {
                    if (values.Length == 0)
                        return Result<LuaValue[]>.Failure("Expression returned no values");

                    var value = values[0];
                    
                    try
                    {
                        var result = EvaluateUnaryOp(op, value);
                        return Result<LuaValue[]>.Success([result]);
                    }
                    catch (Exception ex)
                    {
                        return Result<LuaValue[]>.Failure($"Unary operation failed: {ex.Message}");
                    }
                });
        }

        public Result<LuaValue[]> VisitBinary(Expr left, BinaryOp op, Expr right)
        {
            return Evaluate(left)
                .Bind(leftValues =>
                {
                    if (leftValues.Length == 0)
                        return Result<LuaValue[]>.Failure("Left expression returned no values");

                    return Evaluate(right)
                        .Bind(rightValues =>
                        {
                            if (rightValues.Length == 0)
                                return Result<LuaValue[]>.Failure("Right expression returned no values");

                            var leftValue = leftValues[0];
                            var rightValue = rightValues[0];

                            try
                            {
                                var result = EvaluateBinaryOp(leftValue, op, rightValue);
                                return Result<LuaValue[]>.Success([result]);
                            }
                            catch (Exception ex)
                            {
                                return Result<LuaValue[]>.Failure($"Binary operation failed: {ex.Message}");
                            }
                        });
                });
        }

        public Result<LuaValue[]> VisitVararg()
        {
            // Return empty for now - varargs would be handled by function context
            return Result<LuaValue[]>.Success(Array.Empty<LuaValue>());
        }

        public Result<LuaValue[]> VisitParen(Expr expr)
        {
            // Parentheses just return the inner expression as single value
            return Evaluate(expr)
                .Map(values => values.Length > 0 ? new LuaValue[] { values[0] } : Array.Empty<LuaValue>());
        }

        // Private helper methods

        private Result<LuaValue[]> EvaluateFunctionCallInternal(Expr func, FSharpList<Expr> args)
        {
            // Fast path for math function calls
            if (func.IsTableAccess)
            {
                var tableAccess = func as Expr.TableAccess;
                if (tableAccess!.Item1.IsVar)
                {
                    var varExpr = tableAccess.Item1 as Expr.Var;
                    if (varExpr!.Item == "math" && tableAccess.Item2.IsLiteral)
                    {
                        var literalExpr = tableAccess.Item2 as Expr.Literal;
                        if (literalExpr!.Item.IsString)
                        {
                            var stringLiteral = literalExpr.Item as Literal.String;
                            var functionName = stringLiteral!.Item;

                            // Evaluate arguments
                            var argResults = args.ToArray().Select(arg => Evaluate(arg)).ToArray();
                            var combinedArgs = Result.Combine(argResults);

                            return combinedArgs.Bind(argArrays =>
                            {
                                var mathArgs = argArrays.SelectMany(arr => arr).ToArray();

                                // Try the fast path only if math table and function are unmodified
                                var mathValue = _environment.GetVariable("math");
                                return mathValue.TryAsTable<LuaTable>()
                                    .Bind(mathTable =>
                                    {
                                        if (mathTable.CanUseFastPath(functionName))
                                        {
                                            var fastResult = LuaOperations.TryFastMathFunctionCall(functionName, mathArgs);
                                            if (fastResult != null)
                                            {
                                                return Result<LuaValue[]>.Success(fastResult);
                                            }
                                        }

                                        // Fall back to normal function call
                                        return CallNormalFunction(mathValue, mathArgs);
                                    })
        ;
                            });
                        }
                    }
                }
            }

            // Normal function call
            return Evaluate(func)
                .Bind(funcValues =>
                {
                    if (funcValues.Length == 0)
                        return Result<LuaValue[]>.Failure("Function expression returned no values");

                    var funcValue = funcValues[0];

                    var argResults = args.ToArray().Select(arg => Evaluate(arg)).ToArray();
                    var combinedArgs = Result.Combine(argResults);

                    return combinedArgs.Bind(argArrays =>
                    {
                        var argValues = argArrays.SelectMany(arr => arr).ToArray();
                        return CallNormalFunction(funcValue, argValues);
                    });
                });
        }

        private Result<LuaValue[]> CallNormalFunction(LuaValue funcValue, LuaValue[] argValues)
        {
            if (funcValue.IsFunction)
            {
                return funcValue.TryAsFunction<LuaFunction>()
                    .Bind(function =>
                    {
                        try
                        {
                            // Check if it's a user-defined function that needs interpreter execution
                            if (function is LuaUserFunction userFunc)
                            {
                                var result = ExecuteUserFunction(userFunc, argValues);
                                return Result<LuaValue[]>.Success(result);
                            }
                            else
                            {
                                var result = function.Call(argValues);
                                return Result<LuaValue[]>.Success(result);
                            }
                        }
                        catch (Exception ex)
                        {
                            return Result<LuaValue[]>.Failure($"Function call failed: {ex.Message}");
                        }
                    })
;
            }
            else if (funcValue.IsTable)
            {
                return funcValue.TryAsTable<LuaTable>()
                    .Bind(table =>
                    {
                        if (table.Metatable != null)
                        {
                            // Check for __call metamethod
                            var callMethod = table.Metatable.RawGet(LuaValue.String("__call"));
                            if (callMethod.IsFunction)
                            {
                                return callMethod.TryAsFunction<LuaFunction>()
                                    .Bind(metamethod =>
                                    {
                                        // Add the table itself as the first argument
                                        var callArgs = new LuaValue[argValues.Length + 1];
                                        callArgs[0] = funcValue;
                                        Array.Copy(argValues, 0, callArgs, 1, argValues.Length);

                                        try
                                        {
                                            // Check if it's a user-defined function that needs interpreter execution
                                            if (metamethod is LuaUserFunction userMetamethod)
                                            {
                                                var result = ExecuteUserFunction(userMetamethod, callArgs);
                                                return Result<LuaValue[]>.Success(result);
                                            }
                                            else
                                            {
                                                var result = metamethod.Call(callArgs);
                                                return Result<LuaValue[]>.Success(result);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            return Result<LuaValue[]>.Failure($"Metamethod call failed: {ex.Message}");
                                        }
                                    })
        ;
                            }
                        }

                        return Result<LuaValue[]>.Failure("Table has no __call metamethod");
                    })
;
            }

            return Result<LuaValue[]>.Failure($"Attempt to call non-function ({funcValue.Type})");
        }

        private Result<LuaValue[]> CallTableMethod(LuaValue objValue, LuaTable table, string methodName, LuaValue[] argValues)
        {
            var methodValue = table.Get(LuaValue.String(methodName));

            if (methodValue.IsFunction)
            {
                return methodValue.TryAsFunction<LuaFunction>()
                    .Bind(method =>
                    {
                        // Add the object as the first argument (self)
                        var callArgs = new LuaValue[argValues.Length + 1];
                        callArgs[0] = objValue;
                        Array.Copy(argValues, 0, callArgs, 1, argValues.Length);

                        try
                        {
                            // Check if it's a user-defined function that needs interpreter execution
                            if (method is LuaUserFunction userMethod)
                            {
                                var result = ExecuteUserFunction(userMethod, callArgs);
                                return Result<LuaValue[]>.Success(result);
                            }
                            else
                            {
                                var result = method.Call(callArgs);
                                return Result<LuaValue[]>.Success(result);
                            }
                        }
                        catch (Exception ex)
                        {
                            return Result<LuaValue[]>.Failure($"Method call failed: {ex.Message}");
                        }
                    })
;
            }

            return Result<LuaValue[]>.Failure($"Attempt to call method '{methodName}' on {objValue.Type}");
        }

        private LuaValue EvaluateLiteral(Literal literal)
        {
            if (literal.IsNil)
                return LuaValue.Nil;
            else if (literal.IsBoolean)
                return LuaValue.Boolean(((Literal.Boolean)literal).Item);
            else if (literal.IsInteger)
                return LuaValue.Integer((long)((Literal.Integer)literal).Item);
            else if (literal.IsFloat)
                return LuaValue.Float(((Literal.Float)literal).Item);
            else if (literal.IsString)
                return LuaValue.String(((Literal.String)literal).Item);

            throw new NotImplementedException($"Literal type not implemented: {literal.GetType().Name}");
        }

        private LuaValue EvaluateBinaryOp(LuaValue left, BinaryOp op, LuaValue right)
        {
            // Use the centralized operations from FLua.Runtime
            return InterpreterOperations.EvaluateBinaryOp(left, op, right);
        }

        private LuaValue EvaluateUnaryOp(UnaryOp op, LuaValue value)
        {
            // Use the centralized operations from FLua.Runtime
            return InterpreterOperations.EvaluateUnaryOp(op, value);
        }

        private LuaValue[] ExecuteUserFunction(LuaUserFunction userFunc, LuaValue[] argValues)
        {
            // Create a new environment for the function execution
            var functionEnv = new LuaEnvironment(userFunc.CapturedEnvironment);
            
            // Bind parameters to arguments
            for (int i = 0; i < userFunc.Parameters.Length; i++)
            {
                var paramName = userFunc.Parameters[i];
                if (paramName == "...") // varargs parameter
                {
                    // Handle varargs - collect remaining arguments
                    var varargsValues = new LuaValue[Math.Max(0, argValues.Length - i)];
                    Array.Copy(argValues, i, varargsValues, 0, varargsValues.Length);
                    // TODO: Set varargs in environment when varargs support is added
                    break;
                }
                
                var argValue = i < argValues.Length ? argValues[i] : LuaValue.Nil;
                functionEnv.SetVariable(paramName, argValue);
            }
            
            // Execute the function body using a StatementExecutor
            var originalEnv = _environment;
            _environment = functionEnv;
            
            try
            {
                var statementExecutor = new StatementExecutor(functionEnv);
                var funcDef = (FunctionDef)userFunc.Body;
                
                // Execute each statement in the function body
                foreach (var statement in funcDef.Body)
                {
                    var result = statementExecutor.Execute(statement);
                    if (result.ReturnValues != null || result.Break || result.GotoLabel != null)
                    {
                        // Return the result values or nil if no return
                        return result.ReturnValues ?? new[] { LuaValue.Nil };
                    }
                }
                
                // If no return statement was executed, return nil
                return new[] { LuaValue.Nil };
            }
            finally
            {
                _environment = originalEnv;
            }
        }
    }
}