using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using FLua.Ast;
using FLua.Common;
using FLua.Common.Diagnostics;
using FLua.Parser;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace FLua.Compiler
{
    /// <summary>
    /// Result-based version of ContextBoundCompiler
    /// Compiles Lua code to strongly-typed delegates bound to a context object with explicit error handling
    /// </summary>
    public class ResultContextBoundCompiler
    {
        private readonly IDiagnosticCollector _diagnostics;
        private Dictionary<string, MemberInfo> _memberMap = new();
        private Dictionary<string, ParameterExpression> _localVariables = new();
        private ParameterExpression _contextParam = null!;
        private Type _contextType = null!;
        private Type _resultType = null!;
        
        public ResultContextBoundCompiler(IDiagnosticCollector? diagnostics = null)
        {
            _diagnostics = diagnostics ?? new DiagnosticCollector();
        }
        
        /// <summary>
        /// Creates a compiled delegate from Lua code bound to a context type.
        /// Returns Result with detailed error information instead of throwing exceptions.
        /// </summary>
        public static CompilationResult<Func<TContext, TResult>> Create<TContext, TResult>(string luaCode)
        {
            var compiler = new ResultContextBoundCompiler();
            return compiler.CompileToDelegate<TContext, TResult>(luaCode);
        }
        
        public CompilationResult<Func<TContext, TResult>> CompileToDelegate<TContext, TResult>(string luaCode)
        {
            _contextType = typeof(TContext);
            _resultType = typeof(TResult);
            _contextParam = Expression.Parameter(_contextType, "ctx");
            _localVariables.Clear();
            
            // Build name mappings for context type
            var memberMappingResult = BuildMemberMappingsResult(_contextType);
            if (!memberMappingResult.IsSuccess)
                return CompilationResult<Func<TContext, TResult>>.Failure(memberMappingResult.Diagnostics);
            
            // Parse Lua code
            var parseResult = ParseLuaCode(luaCode);
            if (!parseResult.IsSuccess)
                return CompilationResult<Func<TContext, TResult>>.Failure(parseResult.Diagnostics);
            
            // Compile to expression tree
            var compileResult = CompileToExpression(parseResult.Value);
            if (!compileResult.IsSuccess)
                return CompilationResult<Func<TContext, TResult>>.Failure(compileResult.Diagnostics);
            
            // Build lambda
            var lambdaResult = BuildLambda<TContext, TResult>(compileResult.Value);
            if (!lambdaResult.IsSuccess)
                return CompilationResult<Func<TContext, TResult>>.Failure(lambdaResult.Diagnostics);
            
            // Combine all diagnostics (warnings, etc.)
            var allDiagnostics = memberMappingResult.Diagnostics
                .Concat(parseResult.Diagnostics)
                .Concat(compileResult.Diagnostics)
                .Concat(lambdaResult.Diagnostics)
                .ToList();
            
            return CompilationResult<Func<TContext, TResult>>.Success(lambdaResult.Value, allDiagnostics);
        }
        
        private CompilationResult<Dictionary<string, MemberInfo>> BuildMemberMappingsResult(Type contextType)
        {
            try
            {
                _memberMap.Clear();
                var diagnostics = new List<CompilerDiagnostic>();
                
                foreach (var prop in contextType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var luaName = ConvertToLuaName(prop.Name);
                    if (_memberMap.ContainsKey(luaName))
                    {
                        diagnostics.Add(new CompilerDiagnostic(
                            DiagnosticSeverity.Warning,
                            $"Duplicate member name after conversion: '{prop.Name}' -> '{luaName}'"));
                        continue;
                    }
                    _memberMap[luaName] = prop;
                }
                
                foreach (var field in contextType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    var luaName = ConvertToLuaName(field.Name);
                    if (_memberMap.ContainsKey(luaName))
                    {
                        diagnostics.Add(new CompilerDiagnostic(
                            DiagnosticSeverity.Warning,
                            $"Duplicate member name after conversion: '{field.Name}' -> '{luaName}'"));
                        continue;
                    }
                    _memberMap[luaName] = field;
                }
                
                return CompilationResult<Dictionary<string, MemberInfo>>.Success(_memberMap, diagnostics);
            }
            catch (Exception ex)
            {
                return CompilationResult<Dictionary<string, MemberInfo>>.FromException(ex);
            }
        }
        
        private CompilationResult<Chunk> ParseLuaCode(string luaCode)
        {
            try
            {
                var parseResult = ParserHelper.ParseScript(luaCode);
                
                if (parseResult.IsError)
                {
                    var error = ((FSharpResult<Chunk, string>.Error)parseResult).Item;
                    return CompilationResult<Chunk>.Error($"Parse error: {error}");
                }
                
                var chunk = ((FSharpResult<Chunk, string>.Ok)parseResult).Item;
                return CompilationResult<Chunk>.Success(chunk);
            }
            catch (Exception ex)
            {
                return CompilationResult<Chunk>.FromException(ex);
            }
        }
        
        private CompilationResult<Expression> CompileToExpression(Chunk chunk)
        {
            try
            {
                // Find the last expression statement or return statement
                var statements = chunk.Block.Statements.ToArray();
                if (statements.Length == 0)
                {
                    return CompilationResult<Expression>.Error("No statements found in Lua code");
                }
                
                var lastStatement = statements[statements.Length - 1];
                var exprResult = CompileStatementToExpression(lastStatement);
                
                return exprResult;
            }
            catch (Exception ex)
            {
                return CompilationResult<Expression>.FromException(ex);
            }
        }
        
        private CompilationResult<Expression> CompileStatementToExpression(Statement statement)
        {
            try
            {
                return statement switch
                {
                    Statement.ExpressionStatement exprStmt => CompileExpressionResult(exprStmt.Expression),
                    Statement.ReturnStatement returnStmt => 
                        returnStmt.Values.Count > 0 
                            ? CompileExpressionResult(returnStmt.Values.Head)
                            : CompilationResult<Expression>.Success(Expression.Default(_resultType)),
                    _ => CompilationResult<Expression>.Error($"Statement type not supported: {statement.GetType()}")
                };
            }
            catch (Exception ex)
            {
                return CompilationResult<Expression>.FromException(ex);
            }
        }
        
        private CompilationResult<Expression> CompileExpressionResult(Expr expr)
        {
            try
            {
                return expr switch
                {
                    Expr.Literal literal => CompileLiteralResult(literal.Item),
                    Expr.Var varExpr => CompileVariableResult(varExpr.Item),
                    Expr.Index indexExpr => CompileIndexResult(indexExpr.Item1, indexExpr.Item2),
                    Expr.FunCall funCall => CompileFunctionCallResult(funCall.Item1, funCall.Item2),
                    Expr.BinaryOp binaryOp => CompileBinaryOpResult(binaryOp.Item1, binaryOp.Item2, binaryOp.Item3),
                    Expr.UnaryOp unaryOp => CompileUnaryOpResult(unaryOp.Item1, unaryOp.Item2),
                    _ => CompilationResult<Expression>.Error($"Expression type not supported: {expr.GetType()}")
                };
            }
            catch (Exception ex)
            {
                return CompilationResult<Expression>.FromException(ex);
            }
        }
        
        private CompilationResult<Expression> CompileLiteralResult(Literal literal)
        {
            try
            {
                var expr = literal switch
                {
                    Literal.String str => Expression.Constant(str.Item, typeof(string)),
                    Literal.Integer num => Expression.Constant(num.Item, typeof(long)),
                    Literal.Float num => Expression.Constant(num.Item, typeof(double)),
                    Literal.Boolean b => Expression.Constant(b.Item, typeof(bool)),
                    Literal.Nil => Expression.Constant(null, typeof(object)),
                    _ => throw new NotImplementedException($"Literal type {literal.GetType()} not implemented")
                };
                
                return CompilationResult<Expression>.Success(expr);
            }
            catch (Exception ex)
            {
                return CompilationResult<Expression>.FromException(ex);
            }
        }
        
        private CompilationResult<Expression> CompileVariableResult(string name)
        {
            // Check local variables first
            if (_localVariables.TryGetValue(name, out var localVar))
            {
                return CompilationResult<Expression>.Success(localVar);
            }
            
            // Check context members
            if (_memberMap.TryGetValue(name, out var member))
            {
                var memberExpr = member switch
                {
                    PropertyInfo prop => Expression.Property(_contextParam, prop),
                    FieldInfo field => Expression.Field(_contextParam, field),
                    _ => throw new ArgumentException($"Unsupported member type: {member.GetType()}")
                };
                
                return CompilationResult<Expression>.Success(memberExpr);
            }
            
            return CompilationResult<Expression>.Error($"Unknown variable: {name}");
        }
        
        private CompilationResult<Expression> CompileIndexResult(Expr obj, Expr key)
        {
            var objResult = CompileExpressionResult(obj);
            if (!objResult.IsSuccess) return objResult;
            
            var keyResult = CompileExpressionResult(key);
            if (!keyResult.IsSuccess) return keyResult;
            
            try
            {
                var objExpr = objResult.Value;
                var keyExpr = keyResult.Value;
                
                // Handle property access on objects
                if (keyExpr is ConstantExpression constExpr && constExpr.Value is string propName)
                {
                    var objType = objExpr.Type;
                    var prop = objType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                    
                    if (prop == null)
                    {
                        return CompilationResult<Expression>.Error($"Property '{propName}' not found on type {objType.Name}");
                    }
                    
                    return CompilationResult<Expression>.Success(Expression.Property(objExpr, prop));
                }
                else
                {
                    return CompilationResult<Expression>.Error("Only string keys supported for property access");
                }
            }
            catch (Exception ex)
            {
                return CompilationResult<Expression>.FromException(ex);
            }
        }
        
        private CompilationResult<Expression> CompileFunctionCallResult(Expr func, FSharpList<Expr> args)
        {
            var funcResult = CompileExpressionResult(func);
            if (!funcResult.IsSuccess) return funcResult;
            
            try
            {
                var funcExpr = funcResult.Value;
                
                // Handle method calls on objects
                if (funcExpr is MemberExpression memberExpr)
                {
                    var objExpr = memberExpr.Expression;
                    var member = memberExpr.Member;
                    
                    if (member is PropertyInfo prop && args.Length == 1)
                    {
                        // This might be a method call - look for methods on the property type
                        var argResult = CompileExpressionResult(args.Head);
                        if (!argResult.IsSuccess) return argResult;
                        
                        if (argResult.Value is ConstantExpression methodConstant && methodConstant.Value is string methodName)
                        {
                            var objType = objExpr!.Type;
                            var method = objType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
                            
                            if (method == null)
                            {
                                return CompilationResult<Expression>.Error($"Method '{methodName}' not found on type {objType.Name}");
                            }
                            
                            return CompilationResult<Expression>.Success(Expression.Call(objExpr, method));
                        }
                        else
                        {
                            return CompilationResult<Expression>.Error("Only string keys supported for method names");
                        }
                    }
                }
                
                return CompilationResult<Expression>.Error("Only method calls on objects are supported");
            }
            catch (Exception ex)
            {
                return CompilationResult<Expression>.FromException(ex);
            }
        }
        
        private CompilationResult<Expression> CompileBinaryOpResult(BinaryOp op, Expr leftExpr, Expr rightExpr)
        {
            var leftResult = CompileExpressionResult(leftExpr);
            if (!leftResult.IsSuccess) return leftResult;
            
            var rightResult = CompileExpressionResult(rightExpr);
            if (!rightResult.IsSuccess) return rightResult;
            
            try
            {
                var left = leftResult.Value;
                var right = rightResult.Value;
                
                var expr = op switch
                {
                    var o when o.IsAdd => Expression.Add(left, right),
                    var o when o.IsSubtract => Expression.Subtract(left, right),
                    var o when o.IsMultiply => Expression.Multiply(left, right),
                    var o when o.IsDivide => Expression.Divide(left, right),
                    var o when o.IsModulo => Expression.Modulo(left, right),
                    var o when o.IsPower => Expression.Power(left, right),
                    var o when o.IsEqual => Expression.Equal(left, right),
                    var o when o.IsNotEqual => Expression.NotEqual(left, right),
                    var o when o.IsLessThan => Expression.LessThan(left, right),
                    var o when o.IsLessThanOrEqual => Expression.LessThanOrEqual(left, right),
                    var o when o.IsGreaterThan => Expression.GreaterThan(left, right),
                    var o when o.IsGreaterThanOrEqual => Expression.GreaterThanOrEqual(left, right),
                    var o when o.IsAnd => Expression.AndAlso(left, right),
                    var o when o.IsOr => Expression.OrElse(left, right),
                    var o when o.IsBitAnd => Expression.And(left, right),
                    var o when o.IsBitOr => Expression.Or(left, right),
                    var o when o.IsBitXor => Expression.ExclusiveOr(left, right),
                    var o when o.IsLeftShift => Expression.LeftShift(left, right),
                    var o when o.IsRightShift => Expression.RightShift(left, right),
                    _ => throw new NotSupportedException($"Operator not supported: {op}")
                };
                
                return CompilationResult<Expression>.Success(expr);
            }
            catch (Exception ex)
            {
                return CompilationResult<Expression>.FromException(ex);
            }
        }
        
        private CompilationResult<Expression> CompileUnaryOpResult(UnaryOp op, Expr operandExpr)
        {
            var operandResult = CompileExpressionResult(operandExpr);
            if (!operandResult.IsSuccess) return operandResult;
            
            try
            {
                var operand = operandResult.Value;
                
                var expr = op switch
                {
                    var o when o.IsMinus => Expression.Negate(operand),
                    var o when o.IsNot => Expression.Not(operand),
                    var o when o.IsLength => CompileLengthOperatorResult(operand),
                    var o when o.IsBitNot => Expression.Not(operand),
                    _ => throw new NotSupportedException($"Unary operator not supported: {op}")
                };
                
                return CompilationResult<Expression>.Success(expr);
            }
            catch (Exception ex)
            {
                return CompilationResult<Expression>.FromException(ex);
            }
        }
        
        private Expression CompileLengthOperatorResult(Expression operand)
        {
            if (operand.Type == typeof(string))
            {
                return Expression.Property(operand, nameof(string.Length));
            }
            else if (operand.Type.IsArray)
            {
                return Expression.Property(operand, nameof(Array.Length));
            }
            else if (operand.Type.GetInterfaces().Any(i => i.Name.StartsWith("ICollection")))
            {
                var countProp = operand.Type.GetProperty("Count");
                if (countProp != null)
                {
                    return Expression.Property(operand, countProp);
                }
            }
            
            throw new NotSupportedException($"Length operator not supported for type {operand.Type}");
        }
        
        private CompilationResult<Func<TContext, TResult>> BuildLambda<TContext, TResult>(Expression body)
        {
            try
            {
                // Convert expression to target result type if needed
                if (!_resultType.IsAssignableFrom(body.Type))
                {
                    if (body.Type.IsValueType && !_resultType.IsValueType)
                    {
                        // Box value types when returning object
                        body = Expression.Convert(body, _resultType);
                    }
                    else if (body.Type != _resultType)
                    {
                        // Try conversion
                        body = Expression.Convert(body, _resultType);
                    }
                }
                
                var lambda = Expression.Lambda<Func<TContext, TResult>>(body, _contextParam);
                var compiled = lambda.Compile();
                
                return CompilationResult<Func<TContext, TResult>>.Success(compiled);
            }
            catch (Exception ex)
            {
                return CompilationResult<Func<TContext, TResult>>.FromException(ex);
            }
        }
        
        private static string ConvertToLuaName(string dotNetName)
        {
            // Convert PascalCase to snake_case or camelCase
            // For simplicity, using camelCase conversion here
            if (string.IsNullOrEmpty(dotNetName))
                return dotNetName;
            
            return char.ToLowerInvariant(dotNetName[0]) + dotNetName.Substring(1);
        }
    }
}