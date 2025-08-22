using System;
using System.Linq;
using FLua.Ast;
using FLua.Common.Diagnostics;
using FLua.Runtime;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using Attribute = FLua.Ast.Attribute;

namespace FLua.Interpreter
{
    /// <summary>
    /// Visitor implementation for executing statements
    /// </summary>
    public class StatementExecutor : IStatementVisitor<StatementResult>
    {
        private readonly LuaEnvironment _environment;
        private readonly ExpressionEvaluator _expressionEvaluator;

        public StatementExecutor(LuaEnvironment environment)
        {
            _environment = environment;
            _expressionEvaluator = new ExpressionEvaluator(environment);
        }

        /// <summary>
        /// Main entry point for executing statements using visitor pattern
        /// </summary>
        public StatementResult Execute(Statement stmt)
        {
            return Visitor.dispatchStmt(this, stmt);
        }

        public StatementResult VisitEmpty()
        {
            return new StatementResult();
        }

        public StatementResult VisitAssignment(FSharpList<Expr> varExprs, FSharpList<Expr> valueExprs)
        {
            var varExprArray = varExprs.ToArray();
            var valueExprArray = valueExprs.ToArray();

            // Evaluate all value expressions
            var values = new System.Collections.Generic.List<LuaValue>();
            if (valueExprArray.Length > 0)
            {
                // Evaluate all expressions except the last one
                for (int i = 0; i < valueExprArray.Length - 1; i++)
                {
                    values.Add(_expressionEvaluator.Evaluate(valueExprArray[i])[0]);
                }
                
                // The last expression might return multiple values
                var lastExprValues = _expressionEvaluator.Evaluate(valueExprArray[valueExprArray.Length - 1]);
                values.AddRange(lastExprValues);
            }
            
            // Assign values to variables
            for (int i = 0; i < varExprArray.Length; i++)
            {
                var varExpr = varExprArray[i];
                var value = i < values.Count ? values[i] : LuaValue.Nil;
                
                if (varExpr.IsVar)
                {
                    var varName = ((Expr.Var)varExpr).Item;
                    _environment.SetVariable(varName, value);
                }
                else if (varExpr.IsVarPos)
                {
                    var varPos = (Expr.VarPos)varExpr;
                    try
                    {
                        _environment.SetVariable(varPos.Item1, value);
                    }
                    catch (LuaRuntimeException ex) when (ex.ErrorCode == "UNKNOWN_VAR")
                    {
                        if (varPos.Item2 != null)
                        {
                            ex.WithLocation(varPos.Item2.FileName, varPos.Item2.Line, varPos.Item2.Column);
                        }
                        throw;
                    }
                }
                else if (varExpr.IsTableAccess)
                {
                    var tableAccess = (Expr.TableAccess)varExpr;
                    var tableValue = _expressionEvaluator.Evaluate(tableAccess.Item1)[0];
                    var keyValue = _expressionEvaluator.Evaluate(tableAccess.Item2)[0];
                    
                    if (tableValue.IsTable)
                    {
                        tableValue.AsTable<LuaTable>().Set(keyValue, value);
                    }
                    else
                    {
                        throw new LuaRuntimeException("Attempt to index non-table in assignment");
                    }
                }
                else
                {
                    throw new NotImplementedException($"Assignment to expression type {varExpr.GetType().Name} not implemented");
                }
            }
            
            return new StatementResult();
        }

        public StatementResult VisitLocalAssignment(FSharpList<Tuple<string, Attribute>> variables, FSharpOption<FSharpList<Expr>> expressions)
        {
            var variableArray = variables.ToArray();
            
            // Evaluate expressions if present
            LuaValue[] values = [];
            if (FSharpOption<FSharpList<Expr>>.get_IsSome(expressions))
            {
                var exprList = expressions.Value.ToArray();
                
                if (exprList.Length > 0)
                {
                    // Evaluate all expressions except the last one
                    var tempValues = new System.Collections.Generic.List<LuaValue>();
                    for (int i = 0; i < exprList.Length - 1; i++)
                    {
                        tempValues.Add(_expressionEvaluator.Evaluate(exprList[i])[0]);
                    }
                    
                    // The last expression might return multiple values
                    var lastExprValues = _expressionEvaluator.Evaluate(exprList[exprList.Length - 1]);
                    tempValues.AddRange(lastExprValues);
                    values = tempValues.ToArray();
                }
            }
            
            // Assign values to variables with proper attribute handling
            for (int i = 0; i < variableArray.Length; i++)
            {
                var tuple = variableArray[i];
                var name = tuple.Item1;
                var attribute = tuple.Item2;
                var value = i < values.Length ? values[i] : LuaValue.Nil;
                _environment.SetLocalVariable(name, value, InterpreterOperations.ConvertAttribute(attribute));
            }
            
            return new StatementResult();
        }

        public StatementResult VisitFunctionCall(Expr expr)
        {
            // Evaluate the function call expression and discard the result
            _expressionEvaluator.Evaluate(expr);
            return new StatementResult();
        }

        public StatementResult VisitLabel(string name)
        {
            // Labels are handled at the block execution level for goto statements
            return new StatementResult();
        }

        public StatementResult VisitGoto(string name)
        {
            return new StatementResult { GotoLabel = name };
        }

        public StatementResult VisitBreak()
        {
            return new StatementResult { Break = true };
        }

        public StatementResult VisitDoBlock(FSharpList<Statement> block)
        {
            _environment.PushScope();
            try
            {
                return ExecuteBlock(block);
            }
            finally
            {
                _environment.PopScope();
            }
        }

        public StatementResult VisitWhile(Expr condition, FSharpList<Statement> block)
        {
            while (true)
            {
                var conditionValue = _expressionEvaluator.Evaluate(condition)[0];
                if (!InterpreterOperations.IsTruthy(conditionValue))
                {
                    break;
                }
                
                _environment.PushScope();
                try
                {
                    var result = ExecuteBlock(block);
                    if (result.Break)
                    {
                        break;
                    }
                    if (result.ReturnValues != null)
                    {
                        return result;
                    }
                    if (result.GotoLabel != null)
                    {
                        return result;
                    }
                }
                finally
                {
                    _environment.PopScope();
                }
            }
            
            return new StatementResult();
        }

        public StatementResult VisitRepeat(FSharpList<Statement> block, Expr condition)
        {
            do
            {
                _environment.PushScope();
                try
                {
                    var result = ExecuteBlock(block);
                    if (result.Break)
                    {
                        break;
                    }
                    if (result.ReturnValues != null)
                    {
                        return result;
                    }
                    if (result.GotoLabel != null)
                    {
                        return result;
                    }
                }
                finally
                {
                    _environment.PopScope();
                }
                
                var conditionValue = _expressionEvaluator.Evaluate(condition)[0];
                if (InterpreterOperations.IsTruthy(conditionValue))
                {
                    break;
                }
            } while (true);
            
            return new StatementResult();
        }

        public StatementResult VisitIf(FSharpList<Tuple<Expr, FSharpList<Statement>>> clauses, FSharpOption<FSharpList<Statement>> elseBlock)
        {
            var clauseArray = clauses.ToArray();
            
            // Check each if/elseif clause
            foreach (var clause in clauseArray)
            {
                var condition = clause.Item1;
                var block = clause.Item2;
                var conditionValue = _expressionEvaluator.Evaluate(condition)[0];
                if (InterpreterOperations.IsTruthy(conditionValue))
                {
                    _environment.PushScope();
                    try
                    {
                        return ExecuteBlock(block);
                    }
                    finally
                    {
                        _environment.PopScope();
                    }
                }
            }
            
            // Execute else block if present
            if (FSharpOption<FSharpList<Statement>>.get_IsSome(elseBlock))
            {
                _environment.PushScope();
                try
                {
                    return ExecuteBlock(elseBlock.Value);
                }
                finally
                {
                    _environment.PopScope();
                }
            }
            
            return new StatementResult();
        }

        public StatementResult VisitNumericFor(string variable, Expr start, Expr stop, FSharpOption<Expr> step, FSharpList<Statement> block)
        {
            var startValue = _expressionEvaluator.Evaluate(start)[0];
            var stopValue = _expressionEvaluator.Evaluate(stop)[0];
            var stepValue = FSharpOption<Expr>.get_IsSome(step) ? _expressionEvaluator.Evaluate(step.Value)[0] : LuaValue.Integer(1);
            
            if (!startValue.IsNumber || !stopValue.IsNumber || !stepValue.IsNumber)
            {
                throw new LuaRuntimeException("For loop limits must be numbers");
            }
            
            var startNum = startValue.AsNumber();
            var stopNum = stopValue.AsNumber();
            var stepNum = stepValue.AsNumber();
            
            if (stepNum == 0)
            {
                throw new LuaRuntimeException("For loop step cannot be zero");
            }
            
            _environment.PushScope();
            try
            {
                for (var i = startNum; (stepNum > 0 && i <= stopNum) || (stepNum < 0 && i >= stopNum); i += stepNum)
                {
                    _environment.SetLocalVariable(variable, LuaValue.Number(i));
                    
                    var result = ExecuteBlock(block);
                    if (result.Break)
                    {
                        break;
                    }
                    if (result.ReturnValues != null)
                    {
                        return result;
                    }
                    if (result.GotoLabel != null)
                    {
                        return result;
                    }
                }
            }
            finally
            {
                _environment.PopScope();
            }
            
            return new StatementResult();
        }

        public StatementResult VisitGenericFor(FSharpList<Tuple<string, Attribute>> variables, FSharpList<Expr> expressions, FSharpList<Statement> block)
        {
            var variableArray = variables.ToArray();
            var expressionArray = expressions.ToArray();
            
            if (expressionArray.Length == 0)
            {
                throw new LuaRuntimeException("Generic for requires at least one expression");
            }
            
            // Evaluate iterator expressions
            var iteratorValues = expressionArray.Select(expr => _expressionEvaluator.Evaluate(expr)[0]).ToArray();
            
            if (iteratorValues.Length < 3)
            {
                throw new LuaRuntimeException("Generic for requires iterator function, state, and control variable");
            }
            
            var iteratorFunc = iteratorValues[0];
            var state = iteratorValues[1];
            var controlVar = iteratorValues[2];
            
            if (!iteratorFunc.IsFunction)
            {
                throw new LuaRuntimeException("Generic for iterator must be a function");
            }
            
            _environment.PushScope();
            try
            {
                while (true)
                {
                    // Call iterator function
                    var iteratorResult = iteratorFunc.AsFunction<LuaFunction>().Call([state, controlVar]);
                    
                    if (iteratorResult.Length == 0 || iteratorResult[0].IsNil)
                    {
                        break;
                    }
                    
                    // Update control variable for next iteration
                    controlVar = iteratorResult[0];
                    
                    // Assign values to loop variables
                    for (int i = 0; i < variableArray.Length; i++)
                    {
                        var tuple = variableArray[i];
                        var name = tuple.Item1;
                        var attribute = tuple.Item2;
                        var value = i < iteratorResult.Length ? iteratorResult[i] : LuaValue.Nil;
                        _environment.SetLocalVariable(name, value, InterpreterOperations.ConvertAttribute(attribute));
                    }
                    
                    var result = ExecuteBlock(block);
                    if (result.Break)
                    {
                        break;
                    }
                    if (result.ReturnValues != null)
                    {
                        return result;
                    }
                    if (result.GotoLabel != null)
                    {
                        return result;
                    }
                }
            }
            finally
            {
                _environment.PopScope();
            }
            
            return new StatementResult();
        }

        public StatementResult VisitFunctionDef(FSharpList<string> path, FunctionDef funcDef)
        {
            var pathArray = path.ToArray();
            
            // Convert F# list to array
            var parameters = funcDef.Parameters.ToArray()
                .Select(p => p.IsNamed ? ((Parameter.Named)p).Item1 : "...")
                .ToArray();
            
            var function = new LuaUserFunction(parameters, funcDef, _environment, funcDef.IsVararg);
            var functionValue = LuaValue.Function(function);
            
            if (pathArray.Length == 1)
            {
                // Simple function definition: function name()
                _environment.SetVariable(pathArray[0], functionValue);
            }
            else
            {
                // Method definition: function table.name() or function table:name()
                var tableName = pathArray[0];
                var tableValue = _environment.GetVariable(tableName);
                
                if (!tableValue.IsTable)
                {
                    throw new LuaRuntimeException($"Attempt to index non-table '{tableName}' in function definition");
                }
                
                var table = tableValue.AsTable<LuaTable>();
                
                // Navigate through the path to set the function
                for (int i = 1; i < pathArray.Length - 1; i++)
                {
                    var nextValue = table.Get(LuaValue.String(pathArray[i]));
                    if (!nextValue.IsTable)
                    {
                        throw new LuaRuntimeException($"Attempt to index non-table in function definition path");
                    }
                    table = nextValue.AsTable<LuaTable>();
                }
                
                table.Set(LuaValue.String(pathArray[pathArray.Length - 1]), functionValue);
            }
            
            return new StatementResult();
        }

        public StatementResult VisitLocalFunctionDef(string name, FunctionDef funcDef)
        {
            // Convert F# list to array
            var parameters = funcDef.Parameters.ToArray()
                .Select(p => p.IsNamed ? ((Parameter.Named)p).Item1 : "...")
                .ToArray();
            
            var function = new LuaUserFunction(parameters, funcDef, _environment, funcDef.IsVararg);
            var functionValue = LuaValue.Function(function);
            
            _environment.SetLocalVariable(name, functionValue);
            
            return new StatementResult();
        }

        public StatementResult VisitReturn(FSharpOption<FSharpList<Expr>> expressions)
        {
            // Evaluate return expressions if present
            LuaValue[] values = [];
            if (FSharpOption<FSharpList<Expr>>.get_IsSome(expressions))
            {
                var exprList = expressions.Value.ToArray();
                if (exprList.Length > 0)
                {
                    // Evaluate all expressions except the last one
                    var tempValues = new System.Collections.Generic.List<LuaValue>();
                    for (int i = 0; i < exprList.Length - 1; i++)
                    {
                        tempValues.Add(_expressionEvaluator.Evaluate(exprList[i])[0]);
                    }
                    
                    // The last expression might return multiple values
                    var lastExprValues = _expressionEvaluator.Evaluate(exprList[exprList.Length - 1]);
                    tempValues.AddRange(lastExprValues);
                    values = tempValues.ToArray();
                }
            }
            else
            {
                values = [LuaValue.Nil];
            }
            
            return new StatementResult { ReturnValues = values };
        }

        // Helper method for executing a block of statements
        private StatementResult ExecuteBlock(FSharpList<Statement> block)
        {
            foreach (var statement in block)
            {
                var result = Execute(statement);
                if (result.ReturnValues != null || result.Break || result.GotoLabel != null)
                {
                    return result;
                }
            }
            return new StatementResult();
        }
    }
}