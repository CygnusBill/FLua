using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using FLua.Ast;
using FLua.Common.Diagnostics;
using FLua.Runtime;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Collections;

namespace FLua.Compiler;

/// <summary>
/// Minimal expression tree generator for simple Lua expressions.
/// Only supports basic arithmetic and return statements.
/// </summary>
public class MinimalExpressionTreeGenerator
{
    private readonly IDiagnosticCollector _diagnostics;
    private readonly ParameterExpression _envParameter;
    private readonly Dictionary<string, ParameterExpression> _locals;
    
    public MinimalExpressionTreeGenerator(IDiagnosticCollector diagnostics)
    {
        _diagnostics = diagnostics;
        _envParameter = Expression.Parameter(typeof(LuaEnvironment), "env");
        _locals = new Dictionary<string, ParameterExpression>();
    }
    
    /// <summary>
    /// Generates an expression tree that returns LuaValue[].
    /// </summary>
    public Expression<Func<LuaEnvironment, LuaValue[]>> Generate(IList<Statement> statements)
    {
        var expressions = new List<Expression>();
        Expression returnValue = Expression.NewArrayInit(typeof(LuaValue));
        
        // Process all statements
        foreach (var stmt in statements)
        {
            if (stmt.IsLocalAssignment)
            {
                var localStmt = (Statement.LocalAssignment)stmt;
                var vars = localStmt.Item1;
                var initExprs = localStmt.Item2;
                
                // Create local variables
                for (int i = 0; i < vars.Length; i++)
                {
                    var (varName, _) = vars[i];
                    var localVar = Expression.Variable(typeof(LuaValue), varName);
                    _locals[varName] = localVar;
                    
                    // Initialize with value or nil
                    Expression initValue = Expression.Field(null, typeof(LuaValue), nameof(LuaValue.Nil));
                    if (OptionModule.IsSome(initExprs))
                    {
                        var exprArray = ListModule.ToArray(initExprs.Value);
                        if (i < exprArray.Length)
                        {
                            initValue = GenerateExpression(exprArray[i]);
                        }
                    }
                    
                    expressions.Add(Expression.Assign(localVar, initValue));
                }
            }
            else if (stmt.IsLocalFunctionDef)
            {
                // Local function definitions are not supported in minimal expression trees
                _diagnostics.Report(new FLuaDiagnostic
                {
                    Code = "EXPR-001",
                    Severity = ErrorSeverity.Error,
                    Message = "Local function definitions are not supported in expression tree compilation. Use a different compilation target for complex Lua programs."
                });
                // Create a local variable for the function name but set it to nil
                var localFuncStmt = (Statement.LocalFunctionDef)stmt;
                var funcName = localFuncStmt.Item1;
                var localVar = Expression.Variable(typeof(LuaValue), funcName);
                _locals[funcName] = localVar;
                expressions.Add(Expression.Assign(localVar, Expression.Field(null, typeof(LuaValue), nameof(LuaValue.Nil))));
            }
            else if (stmt.IsFunctionDef)
            {
                // Global function definitions are not supported in minimal expression trees
                _diagnostics.Report(new FLuaDiagnostic
                {
                    Code = "EXPR-002",
                    Severity = ErrorSeverity.Error,
                    Message = "Function definitions are not supported in expression tree compilation. Use a different compilation target for complex Lua programs."
                });
                // Skip this statement - global functions would be set in the environment
            }
            else if (stmt.IsReturn)
            {
                var returnStmt = (Statement.Return)stmt;
                var exprOption = returnStmt.Item;
                
                if (OptionModule.IsSome(exprOption))
                {
                    var exprs = ListModule.ToArray(exprOption.Value);
                    if (exprs.Length > 0)
                    {
                        // For now, only handle single return value
                        var expr = GenerateExpression(exprs[0]);
                        returnValue = Expression.NewArrayInit(typeof(LuaValue), expr);
                    }
                }
                break;
            }
        }
        
        // Add the return expression
        expressions.Add(returnValue);
        
        // Create block with local variables
        var body = _locals.Count > 0 
            ? Expression.Block(_locals.Values, expressions)
            : Expression.Block(expressions);
        
        return Expression.Lambda<Func<LuaEnvironment, LuaValue[]>>(body, _envParameter);
    }
    
    private Expression GenerateExpression(Expr expr)
    {
        // Use pattern matching with switch expression
        switch (expr)
        {
            case Expr.Literal literal:
                return GenerateLiteral(literal.Item);
                
            case Expr.Var variable:
                // Check if it's a local variable first
                if (_locals.TryGetValue(variable.Item, out var localVar))
                {
                    return localVar;
                }
                // Otherwise get from environment
                var getMethod = typeof(LuaEnvironment).GetMethod(nameof(LuaEnvironment.GetVariable))!;
                return Expression.Call(_envParameter, getMethod, Expression.Constant(variable.Item));
                
            case Expr.Binary binary:
                var left = GenerateExpression(binary.Item1);
                var right = GenerateExpression(binary.Item3);
                return GenerateBinaryOp(left, binary.Item2, right);
                
            case Expr.TableAccess tableAccess:
                // Handle table access like math.floor
                var table = GenerateExpression(tableAccess.Item1);
                var key = GenerateExpression(tableAccess.Item2);
                var tableGetMethod = typeof(LuaTable).GetMethod(nameof(LuaTable.Get))!;
                // Use the generic AsTable<T> method with LuaTable as the type parameter
                var asTableMethod = typeof(LuaValue).GetMethod(nameof(LuaValue.AsTable), 1, Type.EmptyTypes)!
                    .MakeGenericMethod(typeof(LuaTable));
                var tableExpr = Expression.Call(table, asTableMethod);
                return Expression.Call(tableExpr, tableGetMethod, key);
                
            case Expr.FunctionCall funcCall:
                // Handle function calls with proper Lua-like error handling
                var func = GenerateExpression(funcCall.Item1);
                var args = ListModule.ToArray(funcCall.Item2).Select(GenerateExpression).ToArray();
                var argsArray = Expression.NewArrayInit(typeof(LuaValue), args);
                
                // Check if the value is nil and throw proper Lua error
                var isNilCheck = Expression.Equal(
                    Expression.Field(func, nameof(LuaValue.Type)),
                    Expression.Constant(LuaType.Nil));
                    
                var nilErrorMessage = Expression.Constant("attempt to call a nil value");
                var throwNilError = Expression.Throw(
                    Expression.New(typeof(LuaRuntimeException).GetConstructor(new[] { typeof(string) })!, nilErrorMessage));
                
                // Check if the value is a function
                var isFunctionCheck = Expression.Equal(
                    Expression.Field(func, nameof(LuaValue.Type)),
                    Expression.Constant(LuaType.Function));
                    
                var throwNotFunctionError = Expression.Throw(
                    Expression.New(typeof(LuaRuntimeException).GetConstructor(new[] { typeof(string) })!, 
                        Expression.Constant("attempt to call a non-function value")));
                
                // Use the non-generic AsFunction() method
                var asFuncMethod = typeof(LuaValue).GetMethods()
                    .Where(m => m.Name == nameof(LuaValue.AsFunction) && !m.IsGenericMethodDefinition && m.GetParameters().Length == 0)
                    .FirstOrDefault();
                
                if (asFuncMethod == null)
                {
                    throw new InvalidOperationException("Could not find non-generic AsFunction() method on LuaValue");
                }
                
                var funcExpr = Expression.Call(func, asFuncMethod);
                var callMethod = typeof(LuaFunction).GetMethod(nameof(LuaFunction.Call))!;
                var callExpr = Expression.Call(funcExpr, callMethod, argsArray);
                
                // Create conditional expression: if nil -> throw, else if not function -> throw, else call
                var conditionalCall = Expression.Condition(
                    isNilCheck,
                    Expression.Block(typeof(LuaValue), throwNilError, Expression.Field(null, typeof(LuaValue), nameof(LuaValue.Nil))),
                    Expression.Condition(
                        isFunctionCheck,
                        Expression.ArrayIndex(callExpr, Expression.Constant(0)), // Function calls return arrays, get first element
                        Expression.Block(typeof(LuaValue), throwNotFunctionError, Expression.Field(null, typeof(LuaValue), nameof(LuaValue.Nil)))
                    )
                );
                
                return conditionalCall;
                
            case Expr.TableConstructor tableConstructor:
                // Handle table constructors like {a = 10, b = 20}
                var tableVar = Expression.Variable(typeof(LuaTable), "tableVar");
                var newTable = Expression.New(typeof(LuaTable));
                var setMethod = typeof(LuaTable).GetMethod(nameof(LuaTable.Set))!;
                var blockExpressions = new List<Expression> { Expression.Assign(tableVar, newTable) };
                
                var fields = ListModule.ToArray(tableConstructor.Item);
                foreach (var field in fields)
                {
                    if (field.IsNamedField)
                    {
                        // Handle {a = 10} style fields
                        var namedField = (TableField.NamedField)field;
                        var fieldKey = Expression.Call(typeof(LuaValue), nameof(LuaValue.String), null, Expression.Constant(namedField.Item1));
                        var fieldValue = GenerateExpression(namedField.Item2);
                        blockExpressions.Add(Expression.Call(tableVar, setMethod, fieldKey, fieldValue));
                    }
                    else if (field.IsKeyField)
                    {
                        // Handle {[key] = value} style fields
                        var keyField = (TableField.KeyField)field;
                        var fieldKey = GenerateExpression(keyField.Item1);
                        var fieldValue = GenerateExpression(keyField.Item2);
                        blockExpressions.Add(Expression.Call(tableVar, setMethod, fieldKey, fieldValue));
                    }
                    else if (field.IsExprField)
                    {
                        // Handle {value} style fields (array-like)
                        var exprField = (TableField.ExprField)field;
                        // For simple expressions, add as indexed values (1, 2, 3...)
                        var indexKey = Expression.Call(typeof(LuaValue), nameof(LuaValue.Number), null, Expression.Constant((double)(blockExpressions.Count - 1)));
                        var fieldValue = GenerateExpression(exprField.Item);
                        blockExpressions.Add(Expression.Call(tableVar, setMethod, indexKey, fieldValue));
                    }
                }
                
                // Return the table wrapped in LuaValue
                blockExpressions.Add(Expression.Call(typeof(LuaValue), nameof(LuaValue.Table), null, tableVar));
                
                return Expression.Block(new[] { tableVar }, blockExpressions);
                
            case Expr.FunctionDef functionDef:
                // Function definitions are not supported in minimal expression trees
                _diagnostics.Report(new FLuaDiagnostic
                {
                    Code = "EXPR-003",
                    Severity = ErrorSeverity.Error,
                    Message = "Function definitions are not supported in expression tree compilation. Use a different compilation target for complex Lua programs."
                });
                return Expression.Field(null, typeof(LuaValue), nameof(LuaValue.Nil));
                
            default:
                // Log what expression type is not supported
                var exprType = expr.GetType().Name;
                _diagnostics.Report(new FLuaDiagnostic
                {
                    Code = "EXPR-004",
                    Severity = ErrorSeverity.Error,
                    Message = $"Expression type '{exprType}' is not supported in minimal expression tree compilation."
                });
                return Expression.Field(null, typeof(LuaValue), nameof(LuaValue.Nil));
        }
    }
    
    private Expression GenerateLiteral(Literal literal)
    {
        if (literal.IsFloat)
        {
            var floatLit = (Literal.Float)literal;
            return Expression.Call(typeof(LuaValue), nameof(LuaValue.Number), null, 
                Expression.Constant(floatLit.Item));
        }
        
        if (literal.IsInteger)
        {
            var intLit = (Literal.Integer)literal;
            return Expression.Call(typeof(LuaValue), nameof(LuaValue.Number), null, 
                Expression.Constant((double)intLit.Item));
        }
        
        if (literal.IsString)
        {
            var strLit = (Literal.String)literal;
            return Expression.Call(typeof(LuaValue), nameof(LuaValue.String), null, 
                Expression.Constant(strLit.Item));
        }
        
        if (literal.IsBoolean)
        {
            var boolLit = (Literal.Boolean)literal;
            return Expression.Call(typeof(LuaValue), nameof(LuaValue.Boolean), null, 
                Expression.Constant(boolLit.Item));
        }
        
        if (literal.IsNil)
        {
            return Expression.Field(null, typeof(LuaValue), nameof(LuaValue.Nil));
        }
        
        return Expression.Field(null, typeof(LuaValue), nameof(LuaValue.Nil));
    }
    
    private Expression GenerateBinaryOp(Expression left, BinaryOp op, Expression right)
    {
        if (op.IsAdd)
            return Expression.Call(typeof(LuaOperations), nameof(LuaOperations.Add), null, left, right);
            
        if (op.IsSubtract)
            return Expression.Call(typeof(LuaOperations), nameof(LuaOperations.Subtract), null, left, right);
            
        if (op.IsMultiply)
            return Expression.Call(typeof(LuaOperations), nameof(LuaOperations.Multiply), null, left, right);
            
        if (op.IsFloatDiv)
            return Expression.Call(typeof(LuaOperations), nameof(LuaOperations.FloatDivide), null, left, right);
            
        if (op.IsConcat)
            // String concatenation uses LuaOperations.Concat
            return Expression.Call(typeof(LuaOperations), nameof(LuaOperations.Concat), null, left, right);
            
        if (op.IsEqual)
            return Expression.Call(typeof(LuaValue), nameof(LuaValue.Boolean), null,
                Expression.Call(left, typeof(LuaValue).GetMethod("Equals", new[] { typeof(LuaValue) })!, right));
                
        if (op.IsNotEqual)
            return Expression.Call(typeof(LuaValue), nameof(LuaValue.Boolean), null,
                Expression.Not(Expression.Call(left, typeof(LuaValue).GetMethod("Equals", new[] { typeof(LuaValue) })!, right)));
                
        if (op.IsLess)
            return Expression.Call(typeof(LuaValue), nameof(LuaValue.Boolean), null,
                Expression.LessThan(
                    Expression.Call(left, typeof(LuaValue).GetMethod(nameof(LuaValue.AsDouble))!),
                    Expression.Call(right, typeof(LuaValue).GetMethod(nameof(LuaValue.AsDouble))!)));
                    
        if (op.IsGreater)
            return Expression.Call(typeof(LuaValue), nameof(LuaValue.Boolean), null,
                Expression.GreaterThan(
                    Expression.Call(left, typeof(LuaValue).GetMethod(nameof(LuaValue.AsDouble))!),
                    Expression.Call(right, typeof(LuaValue).GetMethod(nameof(LuaValue.AsDouble))!)));
                    
        // Return nil for unsupported operators
        return Expression.Field(null, typeof(LuaValue), nameof(LuaValue.Nil));
    }
}