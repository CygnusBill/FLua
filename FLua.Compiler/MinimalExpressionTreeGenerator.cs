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
    
    public MinimalExpressionTreeGenerator(IDiagnosticCollector diagnostics)
    {
        _diagnostics = diagnostics;
        _envParameter = Expression.Parameter(typeof(LuaEnvironment), "env");
    }
    
    /// <summary>
    /// Generates an expression tree that returns LuaValue[].
    /// </summary>
    public Expression<Func<LuaEnvironment, LuaValue[]>> Generate(IList<Statement> statements)
    {
        Expression returnValue = Expression.NewArrayInit(typeof(LuaValue));
        
        // Only handle the first return statement found
        foreach (var stmt in statements)
        {
            if (stmt.IsReturn)
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
        
        return Expression.Lambda<Func<LuaEnvironment, LuaValue[]>>(returnValue, _envParameter);
    }
    
    private Expression GenerateExpression(Expr expr)
    {
        // Use pattern matching with switch expression
        switch (expr)
        {
            case Expr.Literal literal:
                return GenerateLiteral(literal.Item);
                
            case Expr.Var variable:
                // Get variable from environment
                var getMethod = typeof(LuaEnvironment).GetMethod(nameof(LuaEnvironment.GetVariable))!;
                return Expression.Call(_envParameter, getMethod, Expression.Constant(variable.Item));
                
            case Expr.Binary binary:
                var left = GenerateExpression(binary.Item1);
                var right = GenerateExpression(binary.Item3);
                return GenerateBinaryOp(left, binary.Item2, right);
                
            default:
                // Return nil for unsupported expressions
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
            return Expression.Call(null, typeof(LuaValue).GetMethod("op_Addition", new[] { typeof(LuaValue), typeof(LuaValue) })!, left, right);
            
        if (op.IsSubtract)
            return Expression.Call(null, typeof(LuaValue).GetMethod("op_Subtraction", new[] { typeof(LuaValue), typeof(LuaValue) })!, left, right);
            
        if (op.IsMultiply)
            return Expression.Call(null, typeof(LuaValue).GetMethod("op_Multiply", new[] { typeof(LuaValue), typeof(LuaValue) })!, left, right);
            
        if (op.IsFloatDiv)
            return Expression.Call(null, typeof(LuaValue).GetMethod("op_Division", new[] { typeof(LuaValue), typeof(LuaValue) })!, left, right);
            
        if (op.IsConcat)
            // String concatenation uses + operator in LuaValue
            return Expression.Call(null, typeof(LuaValue).GetMethod("op_Addition", new[] { typeof(LuaValue), typeof(LuaValue) })!, left, right);
            
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