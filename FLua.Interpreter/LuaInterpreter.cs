using System;
using System.Collections.Generic;
using System.Linq;
using FLua.Ast;
using FLua.Parser;
using FLua.Runtime;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace FLua.Interpreter
{
    /// <summary>
    /// C# implementation of a Lua interpreter that walks the F# AST
    /// </summary>
    public class LuaInterpreter
    {
        private LuaEnvironment _environment;

        public LuaInterpreter()
        {
            _environment = LuaEnvironment.CreateStandardEnvironment();
        }

        /// <summary>
        /// Evaluates a Lua expression and returns the result
        /// </summary>
        public LuaValue EvaluateExpression(string expressionText)
        {
            try
            {
                var expr = ParserHelper.ParseExpression(expressionText);
                return EvaluateExpr(expr);
            }
            catch (Exception ex)
            {
                throw new LuaRuntimeException($"Error evaluating expression: {ex.Message}");
            }
        }

        /// <summary>
        /// Executes Lua code and returns the result
        /// </summary>
        public LuaValue[] ExecuteCode(string code)
        {
            try
            {
                var ast = ParserHelper.ParseString(code);
                return ExecuteStatements(ast);
            }
            catch (Exception ex)
            {
                throw new LuaRuntimeException($"Error executing code: {ex.Message}");
            }
        }

        /// <summary>
        /// Executes a list of Lua statements
        /// </summary>
        private LuaValue[] ExecuteStatements(FSharpList<Statement> statements)
        {
            LuaValue[]? returnValues = null;
            bool breakFlag = false;
            string? gotoLabel = null;

            foreach (var stmt in statements)
            {
                // Check for control flow interruptions
                if (returnValues != null || breakFlag || gotoLabel != null)
                    break;

                // Execute the statement
                var result = ExecuteStatement(stmt);
                
                // Update control flow flags
                returnValues = result.ReturnValues;
                breakFlag = result.BreakFlag;
                gotoLabel = result.GotoLabel;
            }

            return returnValues ?? Array.Empty<LuaValue>();
        }

        /// <summary>
        /// Executes a list of Lua statements and returns a StatementResult
        /// </summary>
        private StatementResult ExecuteStatementsWithResult(FSharpList<Statement> statements)
        {
            StatementResult result = new StatementResult();

            foreach (var stmt in statements)
            {
                // Check for control flow interruptions
                if (result.ReturnValues != null || result.BreakFlag || result.GotoLabel != null)
                    break;

                // Execute the statement
                result = ExecuteStatement(stmt);
            }

            return result;
        }

        /// <summary>
        /// Executes a single Lua statement
        /// </summary>
        private StatementResult ExecuteStatement(Statement stmt)
        {
            if (stmt.IsEmpty)
            {
                return new StatementResult();
            }
            else if (stmt.IsLocalAssignment)
            {
                var localAssign = (Statement.LocalAssignment)stmt;
                var variables = localAssign.Item1;
                var expressions = localAssign.Item2;
                
                // Evaluate expressions if present
                LuaValue[] values = Array.Empty<LuaValue>();
                if (FSharpOption<FSharpList<Expr>>.get_IsSome(expressions))
                {
                    var exprList = expressions.Value.ToArray();
                    
                    if (exprList.Length > 0)
                    {
                        // Evaluate all expressions except the last one
                        var tempValues = new List<LuaValue>();
                        for (int i = 0; i < exprList.Length - 1; i++)
                        {
                            tempValues.Add(EvaluateExpr(exprList[i]));
                        }
                        
                        // The last expression might return multiple values
                        var lastExprValues = EvaluateExprWithMultipleReturns(exprList[exprList.Length - 1]);
                        tempValues.AddRange(lastExprValues);
                        values = tempValues.ToArray();
                    }
                }
                
                // Assign values to variables
                for (int i = 0; i < variables.Length; i++)
                {
                    var (name, _) = variables[i];
                    var value = i < values.Length ? values[i] : LuaNil.Instance;
                    _environment.SetLocalVariable(name, value);
                }
                
                return new StatementResult();
            }
            else if (stmt.IsFunctionCall)
            {
                var funcCall = (Statement.FunctionCall)stmt;
                var expr = funcCall.Item;
                
                // Evaluate the function call expression and discard the result
                EvaluateExpr(expr);
                
                return new StatementResult();
            }
            else if (stmt.IsReturn)
            {
                var returnStmt = (Statement.Return)stmt;
                var expressions = returnStmt.Item;
                
                // Evaluate return expressions if present
                LuaValue[] values = Array.Empty<LuaValue>();
                if (FSharpOption<FSharpList<Expr>>.get_IsSome(expressions))
                {
                    var exprList = expressions.Value;
                    values = exprList.ToArray().Select(EvaluateExpr).ToArray();
                }
                else
                {
                    values = new[] { LuaNil.Instance };
                }
                
                return new StatementResult { ReturnValues = values };
            }
            else if (stmt.IsAssignment)
            {
                var assignStmt = (Statement.Assignment)stmt;
                var varExprs = assignStmt.Item1;
                var valueExprs = assignStmt.Item2;
                
                // Evaluate all value expressions
                var values = new List<LuaValue>();
                if (valueExprs.Length > 0)
                {
                    // Evaluate all expressions except the last one
                    for (int i = 0; i < valueExprs.Length - 1; i++)
                    {
                        values.Add(EvaluateExpr(valueExprs[i]));
                    }
                    
                    // The last expression might return multiple values
                    var lastExprValues = EvaluateExprWithMultipleReturns(valueExprs[valueExprs.Length - 1]);
                    values.AddRange(lastExprValues);
                }
                
                // Assign values to variables
                for (int i = 0; i < varExprs.Length; i++)
                {
                    var varExpr = varExprs[i];
                    var value = i < values.Count ? values[i] : LuaNil.Instance;
                    
                    if (varExpr.IsVar)
                    {
                        var varName = ((Expr.Var)varExpr).Item;
                        _environment.SetVariable(varName, value);
                    }
                    else if (varExpr.IsTableAccess)
                    {
                        var tableAccess = (Expr.TableAccess)varExpr;
                        var tableValue = EvaluateExpr(tableAccess.Item1);
                        var keyValue = EvaluateExpr(tableAccess.Item2);
                        
                        if (tableValue is LuaTable table)
                        {
                            table.Set(keyValue, value);
                        }
                        else
                        {
                            throw new LuaRuntimeException("Attempt to index non-table");
                        }
                    }
                    else
                    {
                        throw new LuaRuntimeException("Invalid assignment target");
                    }
                }
                
                return new StatementResult();
            }
            else if (stmt.IsIf)
            {
                var ifStmt = (Statement.If)stmt;
                var conditionBlocks = ifStmt.Item1;
                var elseBlock = ifStmt.Item2;
                
                // Try each condition in order
                foreach (var (condition, block) in conditionBlocks)
                {
                    var conditionValue = EvaluateExpr(condition);
                    if (LuaValue.IsValueTruthy(conditionValue))
                    {
                        // Execute this block
                        return ExecuteStatementsWithResult(block);
                    }
                }
                
                // If no condition matched, execute else block if present
                if (FSharpOption<FSharpList<Statement>>.get_IsSome(elseBlock))
                {
                    return ExecuteStatementsWithResult(elseBlock.Value);
                }
                
                return new StatementResult();
            }
            else if (stmt.IsWhile)
            {
                var whileStmt = (Statement.While)stmt;
                var condition = whileStmt.Item1;
                var body = whileStmt.Item2;
                
                StatementResult result = new StatementResult();
                
                // Execute the loop until condition becomes false or control flow is interrupted
                while (true)
                {
                    // Check the condition
                    var conditionValue = EvaluateExpr(condition);
                    if (!LuaValue.IsValueTruthy(conditionValue))
                    {
                        break;
                    }
                    
                    // Execute the body
                    result = ExecuteStatementsWithResult(body);
                    
                    // Check for control flow interruptions
                    if (result.ReturnValues != null || result.GotoLabel != null)
                    {
                        return result;
                    }
                    
                    // Handle break
                    if (result.BreakFlag)
                    {
                        return new StatementResult();  // Clear the break flag
                    }
                }
                
                return result;
            }
            else if (stmt.IsRepeat)
            {
                var repeatStmt = (Statement.Repeat)stmt;
                var body = repeatStmt.Item1;
                var condition = repeatStmt.Item2;
                
                StatementResult result;
                
                // Execute the loop at least once, then until condition becomes true or control flow is interrupted
                do
                {
                    // Execute the body
                    result = ExecuteStatementsWithResult(body);
                    
                    // Check for control flow interruptions
                    if (result.ReturnValues != null || result.GotoLabel != null)
                    {
                        return result;
                    }
                    
                    // Handle break
                    if (result.BreakFlag)
                    {
                        return new StatementResult();  // Clear the break flag
                    }
                    
                    // Check the condition (exit if true)
                    var conditionValue = EvaluateExpr(condition);
                    if (LuaValue.IsValueTruthy(conditionValue))
                    {
                        break;
                    }
                } while (true);
                
                return result;
            }
            else if (stmt.IsDoBlock)
            {
                var doBlockStmt = (Statement.DoBlock)stmt;
                var body = doBlockStmt.Item;
                
                // Create a new environment for the do block
                var blockEnv = new LuaEnvironment(_environment);
                var originalEnv = _environment;
                
                try
                {
                    // Set the new environment
                    _environment = blockEnv;
                    
                    // Execute the block
                    var result = ExecuteStatementsWithResult(body);
                    
                    return result;
                }
                finally
                {
                    // Restore the original environment
                    _environment = originalEnv;
                }
            }
            else if (stmt.IsBreak)
            {
                return new StatementResult { BreakFlag = true };
            }
            else if (stmt.IsLocalFunctionDef)
            {
                var localFuncDef = (Statement.LocalFunctionDef)stmt;
                var name = localFuncDef.Item1;
                var funcDef = localFuncDef.Item2;
                
                // Create a closure for the local function
                var closure = new LuaUserFunction(args =>
                {
                    // Create a new environment for the function execution
                    var funcEnv = new LuaEnvironment(_environment);
                    var originalEnv = _environment;
                    
                    try
                    {
                        // Set the new environment
                        _environment = funcEnv;
                        
                        // Bind parameters
                        int paramIndex = 0;
                        bool hasVararg = false;
                        
                        foreach (var param in funcDef.Parameters)
                        {
                            if (param.IsNamed)
                            {
                                var namedParam = (Parameter.Named)param;
                                var paramName = namedParam.Item1;
                                
                                // Set parameter value or nil if not enough arguments
                                var value = paramIndex < args.Length ? args[paramIndex] : LuaNil.Instance;
                                _environment.SetLocalVariable(paramName, value);
                                paramIndex++;
                            }
                            else if (param.IsVararg)
                            {
                                hasVararg = true;
                            }
                        }
                        
                        // Handle varargs
                        if (hasVararg && paramIndex < args.Length)
                        {
                            var varargTable = new LuaTable();
                            long varargIndex = 1;
                            
                            for (int i = paramIndex; i < args.Length; i++)
                            {
                                varargTable.Set(new LuaInteger(varargIndex++), args[i]);
                            }
                            
                            _environment.SetLocalVariable("...", varargTable);
                        }
                        
                        // Execute function body
                        var result = ExecuteStatements(funcDef.Body);
                        
                        return result;
                    }
                    finally
                    {
                        // Restore original environment
                        _environment = originalEnv;
                    }
                });
                
                // Bind the function as a local variable
                _environment.SetLocalVariable(name, closure);
                
                return new StatementResult();
            }
            else if (stmt.IsFunctionDef)
            {
                var funcDef = (Statement.FunctionDef)stmt;
                var path = funcDef.Item1;
                var functionDef = funcDef.Item2;
                
                // Create a closure for the function
                var closure = new LuaUserFunction(args =>
                {
                    // Create a new environment for the function execution
                    var funcEnv = new LuaEnvironment(_environment);
                    var originalEnv = _environment;
                    
                    try
                    {
                        // Set the new environment
                        _environment = funcEnv;
                        
                        // Bind parameters
                        int paramIndex = 0;
                        bool hasVararg = false;
                        
                        foreach (var param in functionDef.Parameters)
                        {
                            if (param.IsNamed)
                            {
                                var namedParam = (Parameter.Named)param;
                                var paramName = namedParam.Item1;
                                
                                // Set parameter value or nil if not enough arguments
                                var value = paramIndex < args.Length ? args[paramIndex] : LuaNil.Instance;
                                _environment.SetLocalVariable(paramName, value);
                                paramIndex++;
                            }
                            else if (param.IsVararg)
                            {
                                hasVararg = true;
                            }
                        }
                        
                        // Handle varargs
                        if (hasVararg && paramIndex < args.Length)
                        {
                            var varargTable = new LuaTable();
                            long varargIndex = 1;
                            
                            for (int i = paramIndex; i < args.Length; i++)
                            {
                                varargTable.Set(new LuaInteger(varargIndex++), args[i]);
                            }
                            
                            _environment.SetLocalVariable("...", varargTable);
                        }
                        
                        // Execute function body
                        var result = ExecuteStatements(functionDef.Body);
                        
                        return result;
                    }
                    finally
                    {
                        // Restore original environment
                        _environment = originalEnv;
                    }
                });
                
                if (path.Length == 0)
                {
                    throw new LuaRuntimeException("Function definition requires a name");
                }
                else if (path.Length == 1)
                {
                    // Simple global function: function name() end
                    _environment.SetVariable(path[0], closure);
                }
                else
                {
                    // Table method or field: function t.a.b.c() or function t.a.b:c()
                    string baseName = path[0];
                    var baseValue = _environment.GetVariable(baseName);
                    
                    // Create table if it doesn't exist
                    LuaTable baseTable;
                    if (baseValue is LuaTable table)
                    {
                        baseTable = table;
                    }
                    else if (baseValue == LuaNil.Instance)
                    {
                        baseTable = new LuaTable();
                        _environment.SetVariable(baseName, baseTable);
                    }
                    else
                    {
                        throw new LuaRuntimeException($"Attempt to index non-table '{baseName}'");
                    }
                    
                    // Navigate the path
                    LuaTable currentTable = baseTable;
                    for (int i = 1; i < path.Length - 1; i++)
                    {
                        string fieldName = path[i];
                        var fieldValue = currentTable.Get(new LuaString(fieldName));
                        
                        if (fieldValue is LuaTable nextTable)
                        {
                            currentTable = nextTable;
                        }
                        else if (fieldValue == LuaNil.Instance)
                        {
                            // Create nested table
                            var newTable = new LuaTable();
                            currentTable.Set(new LuaString(fieldName), newTable);
                            currentTable = newTable;
                        }
                        else
                        {
                            throw new LuaRuntimeException($"Attempt to index non-table '{fieldName}'");
                        }
                    }
                    
                    // Set the function at the final path element
                    currentTable.Set(new LuaString(path[path.Length - 1]), closure);
                }
                
                return new StatementResult();
            }
            else if (stmt.IsGoto)
            {
                var gotoStmt = (Statement.Goto)stmt;
                var labelName = gotoStmt.Item;
                
                return new StatementResult { GotoLabel = labelName };
            }
            else if (stmt.IsLabel)
            {
                // Labels are just markers - no action needed during execution
                return new StatementResult();
            }
            else if (stmt.IsNumericFor)
            {
                var forStmt = (Statement.NumericFor)stmt;
                var varName = forStmt.Item1;
                var startExpr = forStmt.Item2;
                var endExpr = forStmt.Item3;
                var stepExpr = forStmt.Item4;
                var body = forStmt.Item5;
                
                // Evaluate start, end, and step expressions
                var startVal = EvaluateExpr(startExpr);
                var endVal = EvaluateExpr(endExpr);
                var stepVal = FSharpOption<Expr>.get_IsSome(stepExpr) 
                    ? EvaluateExpr(stepExpr.Value) 
                    : new LuaNumber(1.0);
                
                // Ensure all values are numbers
                if (!startVal.AsNumber.HasValue || !endVal.AsNumber.HasValue || !stepVal.AsNumber.HasValue)
                {
                    throw new LuaRuntimeException("For loop limits and step must be numbers");
                }
                
                double start = startVal.AsNumber.Value;
                double end = endVal.AsNumber.Value;
                double step = stepVal.AsNumber.Value;
                
                if (step == 0)
                {
                    throw new LuaRuntimeException("For loop step cannot be zero");
                }
                
                // Create a new environment for the loop
                var loopEnv = new LuaEnvironment(_environment);
                var originalEnv = _environment;
                
                try
                {
                    // Set the new environment
                    _environment = loopEnv;
                    
                    // Execute the loop
                    StatementResult result = new StatementResult();
                    double current = start;
                    
                    while ((step > 0 && current <= end) || (step < 0 && current >= end))
                    {
                        // Set the loop variable
                        _environment.SetLocalVariable(varName, new LuaNumber(current));
                        
                        // Execute the body
                        result = ExecuteStatementsWithResult(body);
                        
                        // Check for control flow interruptions
                        if (result.ReturnValues != null || result.GotoLabel != null)
                        {
                            return result;
                        }
                        
                        // Handle break
                        if (result.BreakFlag)
                        {
                            return new StatementResult();  // Clear the break flag
                        }
                        
                        // Increment the counter
                        current += step;
                    }
                    
                    return result;
                }
                finally
                {
                    // Restore the original environment
                    _environment = originalEnv;
                }
            }
            else if (stmt.IsGenericFor)
            {
                var forStmt = (Statement.GenericFor)stmt;
                var variables = forStmt.Item1;
                var iterExprs = forStmt.Item2;
                var body = forStmt.Item3;
                
                // Evaluate iterator expressions
                var iterValues = iterExprs.ToArray().Select(EvaluateExpr).ToArray();
                
                if (iterValues.Length < 3)
                {
                    throw new LuaRuntimeException("Generic for requires iterator function, state, and initial value");
                }
                
                // Get iterator function, state, and initial value
                var iteratorFunc = iterValues[0];
                var stateVal = iterValues[1];
                var initialVal = iterValues[2];
                
                if (!(iteratorFunc is LuaFunction))
                {
                    throw new LuaRuntimeException("Iterator must be a function");
                }
                
                // Create a new environment for the loop
                var loopEnv = new LuaEnvironment(_environment);
                var originalEnv = _environment;
                
                try
                {
                    // Set the new environment
                    _environment = loopEnv;
                    
                    // Execute the loop
                    StatementResult result = new StatementResult();
                    LuaValue currentKey = initialVal;
                    
                    while (true)
                    {
                        // Call iterator function: iterator(state, key)
                        var iterResults = ((LuaFunction)iteratorFunc).Call(new[] { stateVal, currentKey });
                        
                        // Check if iteration is complete
                        if (iterResults.Length == 0 || iterResults[0] == LuaNil.Instance)
                        {
                            break;
                        }
                        
                        // Get the next key
                        currentKey = iterResults[0];
                        
                        // Bind loop variables
                        for (int i = 0; i < variables.Length; i++)
                        {
                            var (varName, _) = variables[i];
                            var value = i < iterResults.Length ? iterResults[i] : LuaNil.Instance;
                            _environment.SetLocalVariable(varName, value);
                        }
                        
                        // Execute the body
                        result = ExecuteStatementsWithResult(body);
                        
                        // Check for control flow interruptions
                        if (result.ReturnValues != null || result.GotoLabel != null)
                        {
                            return result;
                        }
                        
                        // Handle break
                        if (result.BreakFlag)
                        {
                            return new StatementResult();  // Clear the break flag
                        }
                    }
                    
                    return result;
                }
                finally
                {
                    // Restore the original environment
                    _environment = originalEnv;
                }
            }
            else
            {
                // For now, we'll just handle a few basic statement types
                throw new NotImplementedException($"Statement type not implemented: {stmt.GetType().Name}");
            }
        }

        /// <summary>
        /// Evaluates a Lua expression
        /// </summary>
        private LuaValue EvaluateExpr(Expr expr)
        {
            return EvaluateExprWithMultipleReturns(expr).FirstOrDefault() ?? LuaNil.Instance;
        }
        
        /// <summary>
        /// Evaluates a Lua expression and returns all values it produces
        /// </summary>
        private LuaValue[] EvaluateExprWithMultipleReturns(Expr expr)
        {
            if (expr.IsLiteral)
            {
                var literal = (Expr.Literal)expr;
                return new[] { EvaluateLiteral(literal.Item) };
            }
            else if (expr.IsVar)
            {
                var variable = (Expr.Var)expr;
                return new[] { _environment.GetVariable(variable.Item) };
            }
            else if (expr.IsBinary)
            {
                var binary = (Expr.Binary)expr;
                var left = EvaluateExpr(binary.Item1);
                var op = binary.Item2;
                var right = EvaluateExpr(binary.Item3);
                
                return new[] { EvaluateBinaryOp(left, op, right) };
            }
            else if (expr.IsUnary)
            {
                var unary = (Expr.Unary)expr;
                var value = EvaluateExpr(unary.Item2);
                
                return new[] { EvaluateUnaryOp(unary.Item1, value) };
            }
            else if (expr.IsTableAccess)
            {
                var tableAccess = (Expr.TableAccess)expr;
                var tableValue = EvaluateExpr(tableAccess.Item1);
                var keyValue = EvaluateExpr(tableAccess.Item2);
                
                if (tableValue is LuaTable table)
                {
                    return new[] { table.Get(keyValue) };
                }
                
                throw new LuaRuntimeException("Attempt to index non-table");
            }
            else if (expr.IsFunctionCall)
            {
                var funcCall = (Expr.FunctionCall)expr;
                var func = EvaluateExpr(funcCall.Item1);
                var args = funcCall.Item2.ToArray().Select(EvaluateExpr).ToArray();
                
                if (func is LuaFunction function)
                {
                    return function.Call(args);
                }
                else if (func is LuaTable table && table.Metatable != null)
                {
                    // Check for __call metamethod
                    var callMethod = table.Metatable.RawGet(new LuaString("__call"));
                    if (callMethod is LuaFunction callFunction)
                    {
                        // Add the table itself as the first argument
                        var callArgs = new LuaValue[args.Length + 1];
                        callArgs[0] = table;
                        Array.Copy(args, 0, callArgs, 1, args.Length);
                        
                        return callFunction.Call(callArgs);
                    }
                }
                
                throw new LuaRuntimeException("Attempt to call non-function");
            }
            else if (expr.IsMethodCall)
            {
                var methodCall = (Expr.MethodCall)expr;
                var objExpr = methodCall.Item1;
                var methodName = methodCall.Item2;
                var argExprs = methodCall.Item3;
                
                // Evaluate the object
                var objValue = EvaluateExpr(objExpr);
                
                // Evaluate arguments
                var args = argExprs.ToArray().Select(EvaluateExpr).ToArray();
                
                // Get the method from the object (which should be a table)
                if (objValue is LuaTable table)
                {
                    var methodValue = table.Get(new LuaString(methodName));
                    
                    if (methodValue is LuaFunction method)
                    {
                        // Add the object as the first argument (self)
                        var callArgs = new LuaValue[args.Length + 1];
                        callArgs[0] = objValue;
                        Array.Copy(args, 0, callArgs, 1, args.Length);
                        
                        // Call the method
                        return method.Call(callArgs);
                    }
                    
                    throw new LuaRuntimeException($"Attempt to call method '{methodName}' on table (not a function)");
                }
                
                throw new LuaRuntimeException("Attempt to call method on non-table");
            }
            else if (expr.IsTableConstructor)
            {
                var tableConstructor = (Expr.TableConstructor)expr;
                var table = new LuaTable();
                long arrayIndex = 1;
                
                foreach (var field in tableConstructor.Item)
                {
                    if (field.IsExprField)
                    {
                        var exprField = (TableField.ExprField)field;
                        var value = EvaluateExpr(exprField.Item);
                        table.Set(new LuaInteger(arrayIndex++), value);
                    }
                    else if (field.IsNamedField)
                    {
                        var namedField = (TableField.NamedField)field;
                        var name = namedField.Item1;
                        var value = EvaluateExpr(namedField.Item2);
                        table.Set(new LuaString(name), value);
                    }
                    else if (field.IsKeyField)
                    {
                        var keyField = (TableField.KeyField)field;
                        var key = EvaluateExpr(keyField.Item1);
                        var value = EvaluateExpr(keyField.Item2);
                        table.Set(key, value);
                    }
                }
                
                return new[] { table };
            }
            else if (expr.IsFunctionDef)
            {
                var funcDef = (Expr.FunctionDef)expr;
                var functionDef = funcDef.Item;
                
                // Create a closure for the function
                return new[] {
                    new LuaUserFunction(args =>
                    {
                        // Create a new environment for the function execution
                        var funcEnv = new LuaEnvironment(_environment);
                        var originalEnv = _environment;
                        
                        try
                        {
                            // Set the new environment
                            _environment = funcEnv;
                            
                            // Bind parameters
                            int paramIndex = 0;
                            bool hasVararg = false;
                            
                            foreach (var param in functionDef.Parameters)
                            {
                                if (param.IsNamed)
                                {
                                    var namedParam = (Parameter.Named)param;
                                    var paramName = namedParam.Item1;
                                    
                                    // Set parameter value or nil if not enough arguments
                                    var value = paramIndex < args.Length ? args[paramIndex] : LuaNil.Instance;
                                    _environment.SetLocalVariable(paramName, value);
                                    paramIndex++;
                                }
                                else if (param.IsVararg)
                                {
                                    hasVararg = true;
                                }
                            }
                            
                            // Handle varargs
                            if (hasVararg && paramIndex < args.Length)
                            {
                                var varargTable = new LuaTable();
                                long varargIndex = 1;
                                
                                for (int i = paramIndex; i < args.Length; i++)
                                {
                                    varargTable.Set(new LuaInteger(varargIndex++), args[i]);
                                }
                                
                                _environment.SetLocalVariable("...", varargTable);
                            }
                            
                            // Execute function body
                            var result = ExecuteStatements(functionDef.Body);
                            
                            return result;
                        }
                        finally
                        {
                            // Restore original environment
                            _environment = originalEnv;
                        }
                    })
                };
            }
            else if (expr.IsVararg)
            {
                // Access varargs stored as "..." in the environment
                var varargValue = _environment.GetVariable("...");
                
                if (varargValue is LuaTable varargTable)
                {
                    // Extract all values from the vararg table
                    var values = new List<LuaValue>();
                    for (long i = 1; ; i++)
                    {
                        var value = varargTable.Get(new LuaInteger(i));
                        if (value == LuaNil.Instance)
                            break;
                        values.Add(value);
                    }
                    
                    return values.ToArray();
                }
                
                throw new LuaRuntimeException("Attempt to use '...' outside a function with varargs");
            }
            else if (expr.IsParen)
            {
                var parenExpr = (Expr.Paren)expr;
                return EvaluateExprWithMultipleReturns(parenExpr.Item);
            }
            else
            {
                // For now, we'll just handle a few basic expression types
                throw new NotImplementedException($"Expression type not implemented: {expr.GetType().Name}");
            }
        }

        /// <summary>
        /// Evaluates a Lua literal
        /// </summary>
        private LuaValue EvaluateLiteral(Literal literal)
        {
            if (literal.IsNil)
            {
                return LuaNil.Instance;
            }
            else if (literal.IsBoolean)
            {
                var boolLiteral = (Literal.Boolean)literal;
                return new LuaBoolean(boolLiteral.Item);
            }
            else if (literal.IsInteger)
            {
                var intLiteral = (Literal.Integer)literal;
                return new LuaInteger((long)intLiteral.Item);
            }
            else if (literal.IsFloat)
            {
                var floatLiteral = (Literal.Float)literal;
                return new LuaNumber(floatLiteral.Item);
            }
            else if (literal.IsString)
            {
                var stringLiteral = (Literal.String)literal;
                return new LuaString(stringLiteral.Item);
            }
            else
            {
                throw new NotImplementedException($"Literal type not implemented: {literal.GetType().Name}");
            }
        }

        /// <summary>
        /// Evaluates a binary operation
        /// </summary>
        private LuaValue EvaluateBinaryOp(LuaValue left, BinaryOp op, LuaValue right)
        {
            // Check for metamethods first
            string? metamethod = GetMetamethodForBinaryOp(op);
            if (metamethod != null)
            {
                LuaValue? result = TryInvokeMetamethod(left, right, metamethod);
                if (result != null)
                {
                    return result;
                }
            }
            
            // Standard operations if no metamethod was found or applied
            if (op == BinaryOp.Add)
            {
                if (left.AsNumber.HasValue && right.AsNumber.HasValue)
                {
                    return new LuaNumber(left.AsNumber.Value + right.AsNumber.Value);
                }
                
                throw new LuaRuntimeException("Attempt to add non-numbers");
            }
            else if (op == BinaryOp.Subtract)
            {
                if (left.AsNumber.HasValue && right.AsNumber.HasValue)
                {
                    return new LuaNumber(left.AsNumber.Value - right.AsNumber.Value);
                }
                
                throw new LuaRuntimeException("Attempt to subtract non-numbers");
            }
            else if (op == BinaryOp.Multiply)
            {
                if (left.AsNumber.HasValue && right.AsNumber.HasValue)
                {
                    return new LuaNumber(left.AsNumber.Value * right.AsNumber.Value);
                }
                
                throw new LuaRuntimeException("Attempt to multiply non-numbers");
            }
            else if (op == BinaryOp.FloatDiv)
            {
                if (left.AsNumber.HasValue && right.AsNumber.HasValue)
                {
                    if (right.AsNumber.Value == 0)
                    {
                        throw new LuaRuntimeException("Division by zero");
                    }
                    
                    return new LuaNumber(left.AsNumber.Value / right.AsNumber.Value);
                }
                
                throw new LuaRuntimeException("Attempt to divide non-numbers");
            }
            else if (op == BinaryOp.Concat)
            {
                return new LuaString(left.AsString + right.AsString);
            }
            else if (op == BinaryOp.Equal)
            {
                // Simple equality check for now
                return new LuaBoolean(left.ToString() == right.ToString());
            }
            else if (op == BinaryOp.NotEqual)
            {
                // Simple inequality check for now
                return new LuaBoolean(left.ToString() != right.ToString());
            }
            else if (op == BinaryOp.And)
            {
                // In Lua, 'and' returns the first value if it's falsy, otherwise the second value
                return LuaValue.IsValueTruthy(left) ? right : left;
            }
            else if (op == BinaryOp.Or)
            {
                // In Lua, 'or' returns the first value if it's truthy, otherwise the second value
                return LuaValue.IsValueTruthy(left) ? left : right;
            }
            else if (op == BinaryOp.Modulo)
            {
                if (left.AsNumber.HasValue && right.AsNumber.HasValue)
                {
                    if (right.AsNumber.Value == 0)
                    {
                        throw new LuaRuntimeException("Modulo by zero");
                    }
                    
                    return new LuaNumber(left.AsNumber.Value % right.AsNumber.Value);
                }
                
                throw new LuaRuntimeException("Attempt to perform modulo on non-numbers");
            }
            else if (op == BinaryOp.Power)
            {
                if (left.AsNumber.HasValue && right.AsNumber.HasValue)
                {
                    return new LuaNumber(Math.Pow(left.AsNumber.Value, right.AsNumber.Value));
                }
                
                throw new LuaRuntimeException("Attempt to perform power operation on non-numbers");
            }
            else if (op == BinaryOp.FloorDiv)
            {
                if (left.AsNumber.HasValue && right.AsNumber.HasValue)
                {
                    if (right.AsNumber.Value == 0)
                    {
                        throw new LuaRuntimeException("Division by zero");
                    }
                    
                    return new LuaNumber(Math.Floor(left.AsNumber.Value / right.AsNumber.Value));
                }
                
                throw new LuaRuntimeException("Attempt to perform floor division on non-numbers");
            }
            else if (op == BinaryOp.Less)
            {
                if (left.AsNumber.HasValue && right.AsNumber.HasValue)
                {
                    return new LuaBoolean(left.AsNumber.Value < right.AsNumber.Value);
                }
                else if (left is LuaString leftStr && right is LuaString rightStr)
                {
                    return new LuaBoolean(leftStr.Value.CompareTo(rightStr.Value) < 0);
                }
                
                throw new LuaRuntimeException("Attempt to compare incompatible types");
            }
            else if (op == BinaryOp.LessEqual)
            {
                if (left.AsNumber.HasValue && right.AsNumber.HasValue)
                {
                    return new LuaBoolean(left.AsNumber.Value <= right.AsNumber.Value);
                }
                else if (left is LuaString leftStr && right is LuaString rightStr)
                {
                    return new LuaBoolean(leftStr.Value.CompareTo(rightStr.Value) <= 0);
                }
                
                throw new LuaRuntimeException("Attempt to compare incompatible types");
            }
            else if (op == BinaryOp.Greater)
            {
                if (left.AsNumber.HasValue && right.AsNumber.HasValue)
                {
                    return new LuaBoolean(left.AsNumber.Value > right.AsNumber.Value);
                }
                else if (left is LuaString leftStr && right is LuaString rightStr)
                {
                    return new LuaBoolean(leftStr.Value.CompareTo(rightStr.Value) > 0);
                }
                
                throw new LuaRuntimeException("Attempt to compare incompatible types");
            }
            else if (op == BinaryOp.GreaterEqual)
            {
                if (left.AsNumber.HasValue && right.AsNumber.HasValue)
                {
                    return new LuaBoolean(left.AsNumber.Value >= right.AsNumber.Value);
                }
                else if (left is LuaString leftStr && right is LuaString rightStr)
                {
                    return new LuaBoolean(leftStr.Value.CompareTo(rightStr.Value) >= 0);
                }
                
                throw new LuaRuntimeException("Attempt to compare incompatible types");
            }
            else if (op == BinaryOp.BitAnd)
            {
                if (left.AsInteger.HasValue && right.AsInteger.HasValue)
                {
                    return new LuaInteger(left.AsInteger.Value & right.AsInteger.Value);
                }
                
                throw new LuaRuntimeException("Attempt to perform bitwise AND on non-integers");
            }
            else if (op == BinaryOp.BitOr)
            {
                if (left.AsInteger.HasValue && right.AsInteger.HasValue)
                {
                    return new LuaInteger(left.AsInteger.Value | right.AsInteger.Value);
                }
                
                throw new LuaRuntimeException("Attempt to perform bitwise OR on non-integers");
            }
            else if (op == BinaryOp.BitXor)
            {
                if (left.AsInteger.HasValue && right.AsInteger.HasValue)
                {
                    return new LuaInteger(left.AsInteger.Value ^ right.AsInteger.Value);
                }
                
                throw new LuaRuntimeException("Attempt to perform bitwise XOR on non-integers");
            }
            else if (op == BinaryOp.ShiftLeft)
            {
                if (left.AsInteger.HasValue && right.AsInteger.HasValue)
                {
                    var shift = right.AsInteger.Value;
                    if (shift < 0)
                    {
                        throw new LuaRuntimeException("Negative shift count");
                    }
                    if (shift > 63)
                    {
                        throw new LuaRuntimeException("Shift count too large");
                    }
                    
                    return new LuaInteger(left.AsInteger.Value << (int)shift);
                }
                
                throw new LuaRuntimeException("Attempt to perform left shift on non-integers");
            }
            else if (op == BinaryOp.ShiftRight)
            {
                if (left.AsInteger.HasValue && right.AsInteger.HasValue)
                {
                    var shift = right.AsInteger.Value;
                    if (shift < 0)
                    {
                        throw new LuaRuntimeException("Negative shift count");
                    }
                    if (shift > 63)
                    {
                        throw new LuaRuntimeException("Shift count too large");
                    }
                    
                    return new LuaInteger(left.AsInteger.Value >> (int)shift);
                }
                
                throw new LuaRuntimeException("Attempt to perform right shift on non-integers");
            }
            else
            {
                // For now, we'll just handle a few basic operators
                throw new NotImplementedException($"Binary operator not implemented: {op}");
            }
        }
        
        /// <summary>
        /// Gets the metamethod name for a binary operation
        /// </summary>
        private string? GetMetamethodForBinaryOp(BinaryOp op)
        {
            // F# discriminated unions have a Tag property that indicates the case
            switch (op.Tag)
            {
                case 0: return "__add";       // Add
                case 1: return "__sub";       // Subtract
                case 2: return "__mul";       // Multiply
                case 3: return "__div";       // FloatDiv
                case 4: return "__idiv";      // FloorDiv
                case 5: return "__mod";       // Modulo
                case 6: return "__pow";       // Power
                case 7: return "__concat";    // Concat
                case 8: return "__band";      // BitAnd
                case 9: return "__bor";       // BitOr
                case 10: return "__bxor";     // BitXor
                case 11: return "__shl";      // ShiftLeft
                case 12: return "__shr";      // ShiftRight
                case 13: return "__eq";       // Equal
                case 14: return null;         // NotEqual (no metamethod)
                case 15: return "__lt";       // Less
                case 16: return "__le";       // LessEqual
                case 17: return null;         // Greater (use __lt with reversed args)
                case 18: return null;         // GreaterEqual (use __le with reversed args)
                case 19: return null;         // And (no metamethod)
                case 20: return null;         // Or (no metamethod)
                default: return null;
            }
        }
        
        /// <summary>
        /// Tries to invoke a metamethod for a binary operation
        /// </summary>
        private LuaValue? TryInvokeMetamethod(LuaValue left, LuaValue right, string metamethod)
        {
            // Special case for equality
            if (metamethod == "__eq")
            {
                // If both objects are tables and have the same metatable with __eq
                if (left is LuaTable leftTable && right is LuaTable rightTable)
                {
                    if (leftTable.Metatable == rightTable.Metatable && leftTable.Metatable != null)
                    {
                        var eqFunc = leftTable.Metatable.RawGet(new LuaString(metamethod));
                        if (eqFunc is LuaFunction eqFunction)
                        {
                            var result = eqFunction.Call(new LuaValue[] { left, right });
                            return result.Length > 0 ? result[0] : LuaNil.Instance;
                        }
                    }
                }
                return null;
            }
            
            // Try left operand's metamethod
            if (left is LuaTable leftT && leftT.Metatable != null)
            {
                var meta = leftT.Metatable.RawGet(new LuaString(metamethod));
                if (meta is LuaFunction leftFunc)
                {
                    var result = leftFunc.Call(new LuaValue[] { left, right });
                    return result.Length > 0 ? result[0] : LuaNil.Instance;
                }
            }
            
            // Try right operand's metamethod
            if (right is LuaTable rightT && rightT.Metatable != null)
            {
                var meta = rightT.Metatable.RawGet(new LuaString(metamethod));
                if (meta is LuaFunction rightFunc)
                {
                    var result = rightFunc.Call(new LuaValue[] { left, right });
                    return result.Length > 0 ? result[0] : LuaNil.Instance;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Evaluates a unary operation
        /// </summary>
        private LuaValue EvaluateUnaryOp(UnaryOp op, LuaValue value)
        {
            // Check for metamethods first
            string? metamethod = GetMetamethodForUnaryOp(op);
            if (metamethod != null && value is LuaTable metaTable && metaTable.Metatable != null)
            {
                var meta = metaTable.Metatable.RawGet(new LuaString(metamethod));
                if (meta is LuaFunction func)
                {
                    var result = func.Call(new[] { value });
                    if (result.Length > 0)
                    {
                        return result[0];
                    }
                }
            }
            
            // Standard operations if no metamethod was found or applied
            if (op == UnaryOp.Not)
            {
                return new LuaBoolean(!LuaValue.IsValueTruthy(value));
            }
            else if (op == UnaryOp.Negate)
            {
                if (value.AsNumber.HasValue)
                {
                    return new LuaNumber(-value.AsNumber.Value);
                }
                
                throw new LuaRuntimeException("Attempt to negate non-number");
            }
            else if (op == UnaryOp.Length)
            {
                if (value is LuaString str)
                {
                    return new LuaInteger(str.Value.Length);
                }
                else if (value is LuaTable lengthTable)
                {
                    return new LuaInteger(lengthTable.Array.Count);
                }
                
                throw new LuaRuntimeException("Attempt to get length of non-string/table");
            }
            else if (op == UnaryOp.BitNot)
            {
                if (value.AsInteger.HasValue)
                {
                    return new LuaInteger(~value.AsInteger.Value);
                }
                
                throw new LuaRuntimeException("Attempt to perform bitwise NOT on non-integer");
            }
            else
            {
                // For now, we'll just handle a few basic operators
                throw new NotImplementedException($"Unary operator not implemented: {op}");
            }
        }
        
        /// <summary>
        /// Gets the metamethod name for a unary operation
        /// </summary>
        private string? GetMetamethodForUnaryOp(UnaryOp op)
        {
            // F# discriminated unions have a Tag property that indicates the case
            switch (op.Tag)
            {
                case 0: return "__unm";      // Negate
                case 1: return null;         // Not (no metamethod)
                case 2: return "__len";      // Length
                case 3: return "__bnot";     // BitNot
                default: return null;
            }
        }
    }

    /// <summary>
    /// Represents the result of executing a statement
    /// </summary>
    public class StatementResult
    {
        public LuaValue[]? ReturnValues { get; set; }
        public bool BreakFlag { get; set; }
        public string? GotoLabel { get; set; }
    }
} 