using System;
using System.Linq;
using FLua.Ast;
using FLua.Common.Diagnostics;
using FLua.Runtime;
using Microsoft.FSharp.Collections;

namespace FLua.Interpreter
{
    /// <summary>
    /// Visitor implementation for evaluating expressions
    /// </summary>
    public class ExpressionEvaluator : IExpressionVisitor<LuaValue[]>
    {
        private readonly LuaEnvironment _environment;

        public ExpressionEvaluator(LuaEnvironment environment)
        {
            _environment = environment;
        }

        /// <summary>
        /// Main entry point for evaluating expressions using visitor pattern
        /// </summary>
        public LuaValue[] Evaluate(Expr expr)
        {
            return Visitor.dispatchExpr(this, expr);
        }

        public LuaValue[] VisitLiteral(Literal literal)
        {
            return [EvaluateLiteral(literal)];
        }

        public LuaValue[] VisitVar(string name)
        {
            return [_environment.GetVariable(name)];
        }

        public LuaValue[] VisitVarPos(string name, SourceLocation location)
        {
            try
            {
                return [_environment.GetVariable(name)];
            }
            catch (LuaRuntimeException ex) when (ex.ErrorCode == "UNKNOWN_VAR")
            {
                // Add position information to the error
                if (location != null)
                {
                    ex.WithLocation(location.FileName, location.Line, location.Column);
                }
                throw;
            }
        }

        public LuaValue[] VisitTableAccess(Expr table, Expr key)
        {
            var tableValue = Evaluate(table)[0]; // Get single value
            var keyValue = Evaluate(key)[0];     // Get single value

            if (tableValue.IsTable)
            {
                return [tableValue.AsTable<LuaTable>().Get(keyValue)];
            }

            throw new LuaRuntimeException("Attempt to index non-table");
        }

        public LuaValue[] VisitTableConstructor(FSharpList<TableField> fields)
        {
            var table = new LuaTable();
            int arrayIndex = 1;

            foreach (var field in fields)
            {
                if (field.IsExprField)
                {
                    var exprField = field as TableField.ExprField;
                    var value = Evaluate(exprField!.Item)[0];
                    table.Set(LuaValue.Integer(arrayIndex++), value);
                }
                else if (field.IsNamedField)
                {
                    var namedField = field as TableField.NamedField;
                    var value = Evaluate(namedField!.Item2)[0];
                    table.Set(LuaValue.String(namedField.Item1), value);
                }
                else if (field.IsKeyField)
                {
                    var keyField = field as TableField.KeyField;
                    var key = Evaluate(keyField!.Item1)[0];
                    var value = Evaluate(keyField.Item2)[0];
                    table.Set(key, value);
                }
            }

            return [LuaValue.Table(table)];
        }

        public LuaValue[] VisitFunctionDef(FunctionDef funcDef)
        {
            // Convert F# list to array
            var parameters = funcDef.Parameters.ToArray()
                .Select(p => p.IsNamed ? ((Parameter.Named)p).Item1 : "...")
                .ToArray();

            var function = new LuaUserFunction(parameters, funcDef, _environment, funcDef.IsVararg);
            return [LuaValue.Function(function)];
        }

        public LuaValue[] VisitFunctionCall(Expr func, FSharpList<Expr> args)
        {
            return EvaluateFunctionCallInternal(func, args);
        }

        public LuaValue[] VisitFunctionCallPos(Expr func, FSharpList<Expr> args, SourceLocation location)
        {
            try
            {
                return EvaluateFunctionCallInternal(func, args);
            }
            catch (LuaRuntimeException ex)
            {
                if (location != null)
                {
                    ex.WithLocation(location.FileName, location.Line, location.Column);
                }
                throw;
            }
        }

        public LuaValue[] VisitMethodCall(Expr obj, string methodName, FSharpList<Expr> args)
        {
            var objValue = Evaluate(obj)[0];

            // Fast path for string method calls
            if (objValue.IsString)
            {
                var str = objValue.AsString();
                var stringArgs = args.ToArray().Select(arg => Evaluate(arg)[0]).ToArray();

                // Check if the string library allows fast path for this method
                var stringValue = _environment.GetVariable("string");
                if (stringValue.IsTable)
                {
                    var stringTable = stringValue.AsTable<LuaTable>();
                    if (stringTable.CanUseFastPath(methodName))
                    {
                        var fastResult = LuaOperations.TryFastStringMethodCall(str, methodName, stringArgs);
                        if (fastResult.HasValue)
                        {
                            return [fastResult.Value];
                        }
                    }
                }
            }

            // Fall back to table lookup for other methods
            var argValues = args.ToArray().Select(arg => Evaluate(arg)[0]).ToArray();

            if (objValue.IsTable)
            {
                var methodValue = objValue.AsTable<LuaTable>().Get(LuaValue.String(methodName));

                if (methodValue.IsFunction)
                {
                    // Add the object as the first argument (self)
                    var callArgs = new LuaValue[argValues.Length + 1];
                    callArgs[0] = objValue;
                    Array.Copy(argValues, 0, callArgs, 1, argValues.Length);

                    return methodValue.AsFunction<LuaFunction>().Call(callArgs);
                }
            }

            throw new LuaRuntimeException($"Attempt to call method '{methodName}' on {objValue.Type}");
        }

        public LuaValue[] VisitUnary(UnaryOp op, Expr expr)
        {
            var value = Evaluate(expr)[0];
            return [EvaluateUnaryOp(op, value)];
        }

        public LuaValue[] VisitBinary(Expr left, BinaryOp op, Expr right)
        {
            var leftValue = Evaluate(left)[0];
            var rightValue = Evaluate(right)[0];
            return [EvaluateBinaryOp(leftValue, op, rightValue)];
        }

        public LuaValue[] VisitVararg()
        {
            // Return empty for now - varargs would be handled by function context
            return Array.Empty<LuaValue>();
        }

        public LuaValue[] VisitParen(Expr expr)
        {
            // Parentheses just return the inner expression as single value
            return [Evaluate(expr)[0]];
        }

        // Private helper methods

        private LuaValue[] EvaluateFunctionCallInternal(Expr func, FSharpList<Expr> args)
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
                            var mathArgs = args.ToArray().Select(arg => Evaluate(arg)[0]).ToArray();

                            // Try the fast path only if math table and function are unmodified
                            var mathValue = _environment.GetVariable("math");
                            if (mathValue.IsTable)
                            {
                                var mathTable = mathValue.AsTable<LuaTable>();
                                if (mathTable.CanUseFastPath(functionName))
                                {
                                    var fastResult = LuaOperations.TryFastMathFunctionCall(functionName, mathArgs);
                                    if (fastResult != null)
                                    {
                                        return fastResult;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            // Normal function call
            var funcValue = Evaluate(func)[0];
            var argValues = args.ToArray().Select(arg => Evaluate(arg)[0]).ToArray();

            if (funcValue.IsFunction)
            {
                return funcValue.AsFunction<LuaFunction>().Call(argValues);
            }
            else if (funcValue.IsTable && funcValue.AsTable<LuaTable>().Metatable != null)
            {
                var table = funcValue.AsTable<LuaTable>();

                // Check for __call metamethod
                var callMethod = table.Metatable!.RawGet(LuaValue.String("__call"));
                if (callMethod.IsFunction)
                {
                    // Add the table itself as the first argument
                    var callArgs = new LuaValue[argValues.Length + 1];
                    callArgs[0] = table;
                    Array.Copy(argValues, 0, callArgs, 1, argValues.Length);

                    return callMethod.AsFunction<LuaFunction>().Call(callArgs);
                }
            }

            throw new LuaRuntimeException("Attempt to call non-function");
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
    }
}