using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using FLua.Ast;
using FLua.Common.Diagnostics;
using FLua.Parser;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace FLua.Compiler;

/// <summary>
/// Compiles Lua code to strongly-typed delegates bound to a context object.
/// Used for configuration-driven lambdas with direct .NET interop.
/// </summary>
public class ContextBoundCompiler
{
    private readonly IDiagnosticCollector _diagnostics;
    private Dictionary<string, MemberInfo> _memberMap = new();
    private Dictionary<string, ParameterExpression> _localVariables = new();
    private ParameterExpression _contextParam = null!;
    private Type _contextType = null!;
    private Type _resultType = null!;
    
    public ContextBoundCompiler(IDiagnosticCollector? diagnostics = null)
    {
        _diagnostics = diagnostics ?? new DiagnosticCollector();
    }
    
    /// <summary>
    /// Creates a compiled delegate from Lua code bound to a context type.
    /// </summary>
    public static Func<TContext, TResult> Create<TContext, TResult>(string luaCode)
    {
        var compiler = new ContextBoundCompiler();
        return compiler.CompileToDelegate<TContext, TResult>(luaCode);
    }
    
    public Func<TContext, TResult> CompileToDelegate<TContext, TResult>(string luaCode)
    {
        _contextType = typeof(TContext);
        _resultType = typeof(TResult);
        _contextParam = Expression.Parameter(_contextType, "ctx");
        _localVariables.Clear(); // Clear any previous state
        
        // Build name mappings for context type
        BuildMemberMappings(_contextType);
        
        // For simple expressions, wrap in return statement
        var codeToParse = luaCode.Trim();
        if (!codeToParse.Contains("return") && !codeToParse.Contains("if") && !codeToParse.Contains("local") && !codeToParse.Contains("for") && !codeToParse.Contains("while"))
        {
            codeToParse = $"return {codeToParse}";
        }
        
        // Parse Lua code
        var statements = ParserHelper.ParseString(codeToParse);
        
        // Generate expression tree
        var body = GenerateBody(statements);
        
        // Ensure return type matches
        if (body.Type != _resultType)
        {
            body = Expression.Convert(body, _resultType);
        }
        
        // Local variables are now handled in GenerateBody
        
        // Create and compile lambda
        var lambda = Expression.Lambda<Func<TContext, TResult>>(body, _contextParam);
        return lambda.Compile();
    }
    
    private void BuildMemberMappings(Type type)
    {
        _memberMap.Clear();
        
        // Map properties with name translation
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // PascalCase -> snake_case
            var snakeName = ToSnakeCase(prop.Name);
            _memberMap[snakeName] = prop;
            
            // Also map camelCase
            var camelName = ToCamelCase(prop.Name);
            if (camelName != snakeName)
            {
                _memberMap[camelName] = prop;
            }
            
            // Keep original for compatibility
            _memberMap[prop.Name.ToLower()] = prop;
        }
        
        // Map methods
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                   .Where(m => !m.IsSpecialName))
        {
            var snakeName = ToSnakeCase(method.Name);
            _memberMap[snakeName] = method;
            
            var camelName = ToCamelCase(method.Name);
            if (camelName != snakeName)
            {
                _memberMap[camelName] = method;
            }
        }
    }
    
    private Expression GenerateBody(FSharpList<Statement> statements)
    {
        var expressions = new List<Expression>();
        Expression? lastExpression = null;
        var localVars = new List<ParameterExpression>();
        
        foreach (var stmt in statements)
        {
            // Track local variables before generating the statement
            var varsBeforeCount = _localVariables.Count;
            
            var expr = GenerateStatement(stmt);
            if (expr != null)
            {
                expressions.Add(expr);
                lastExpression = expr;
            }
            
            // Collect any new local variables added by this statement
            if (_localVariables.Count > varsBeforeCount)
            {
                foreach (var kvp in _localVariables)
                {
                    if (!localVars.Contains(kvp.Value))
                    {
                        localVars.Add(kvp.Value);
                    }
                }
            }
        }
        
        // Return the last expression or default
        if (lastExpression == null)
        {
            lastExpression = Expression.Default(_resultType);
        }
        
        // If we have multiple expressions or local variables, wrap in a block
        if (expressions.Count > 1 || localVars.Count > 0)
        {
            // Make sure the last expression is the return value
            if (expressions.Count > 0)
            {
                expressions[expressions.Count - 1] = lastExpression;
            }
            else
            {
                expressions.Add(lastExpression);
            }
            
            return localVars.Count > 0 
                ? Expression.Block(localVars, expressions)
                : Expression.Block(expressions);
        }
        
        return lastExpression;
    }
    
    private Expression? GenerateStatement(Statement stmt)
    {
        switch (stmt)
        {
            case Statement.Return ret:
                var returnExprs = ret.Item;
                if (OptionModule.IsSome(returnExprs))
                {
                    var exprs = ListModule.ToArray(returnExprs.Value);
                    if (exprs.Length > 0)
                    {
                        return GenerateExpression(exprs[0]);
                    }
                }
                return Expression.Default(_resultType);
                
            case Statement.If ifStmt:
                return GenerateIfStatement(ifStmt);
                
            case Statement.LocalAssignment local:
                return GenerateLocalAssignment(local);
                
            case Statement.While whileStmt:
                return GenerateWhileLoop(whileStmt);
                
            case Statement.NumericFor numFor:
                return GenerateNumericForLoop(numFor);
                
            case Statement.FunctionCall funcCall:
                return GenerateExpression(funcCall.Item);
                
            default:
                // Unsupported statement type - skip
                return null;
        }
    }
    
    private Expression GenerateIfStatement(Statement.If ifStmt)
    {
        // Build if-elseif-else chain
        Expression? result = null;
        var branches = ifStmt.Item1;
        var elseBranch = ifStmt.Item2;
        
        // Process in reverse to build nested conditionals
        if (OptionModule.IsSome(elseBranch))
        {
            var elseStatements = elseBranch.Value;
            result = GenerateBody(ListModule.OfArray(elseStatements.ToArray()));
        }
        else
        {
            result = Expression.Default(_resultType);
        }
        
        foreach (var (condition, body) in branches.Reverse())
        {
            var test = GenerateExpression(condition);
            var ifTrue = GenerateBody(ListModule.OfArray(body.ToArray()));
            result = Expression.Condition(test, ifTrue, result!);
        }
        
        return result!;
    }
    
    private Expression GenerateExpression(Expr expr)
    {
        switch (expr)
        {
            case Expr.Var varExpr:
                return ResolveVariable(varExpr.Item);
                
            case Expr.TableAccess tableAccess:
                return GenerateTableAccess(tableAccess);
                
            case Expr.FunctionCall funcCall:
                return GenerateFunctionCall(funcCall);
                
            case Expr.Binary binary:
                return GenerateBinaryOp(binary);
                
            case Expr.Literal literal:
                return GenerateLiteral(literal.Item);
                
            case Expr.Unary unary:
                return GenerateUnaryOp(unary);
                
            case Expr.TableConstructor tableConstructor:
                return GenerateTableConstructor(tableConstructor);
                
            default:
                throw new NotSupportedException($"Expression type not supported: {expr.GetType()}");
        }
    }

    
    private Expression GenerateLiteral(Literal literal)
    {
        if (literal.IsInteger)
        {
            var intLit = (Literal.Integer)literal;
            return Expression.Constant((int)intLit.Item);
        }
        
        if (literal.IsFloat)
        {
            var floatLit = (Literal.Float)literal;
            return Expression.Constant(floatLit.Item);
        }
        
        if (literal.IsString)
        {
            var strLit = (Literal.String)literal;
            return Expression.Constant(strLit.Item);
        }
        
        if (literal.IsBoolean)
        {
            var boolLit = (Literal.Boolean)literal;
            return Expression.Constant(boolLit.Item);
        }
        
        if (literal.IsNil)
        {
            return Expression.Default(_resultType);
        }
        
        return Expression.Default(_resultType);
    }
    
    private Expression ResolveVariable(string name)
    {
        // First check local variables
        if (_localVariables.TryGetValue(name, out var localVar))
        {
            return localVar;
        }
        
        // Try to resolve as context member
        var lowerName = name.ToLower();
        if (_memberMap.TryGetValue(lowerName, out var member))
        {
            if (member is PropertyInfo prop)
            {
                return Expression.Property(_contextParam, prop);
            }
        }
        
        throw new ArgumentException($"Unknown variable: {name}");
    }
    
    private Expression GenerateTableAccess(Expr.TableAccess tableAccess)
    {
        var obj = GenerateExpression(tableAccess.Item1);
        
        // Extract property name
        string propName;
        if (tableAccess.Item2 is Expr.Literal literal && literal.Item.IsString)
        {
            var strLit = (Literal.String)literal.Item;
            propName = strLit.Item;
        }
        else
        {
            throw new NotSupportedException("Only string keys supported for property access");
        }
        
        // Try name variations
        var objType = obj.Type;
        PropertyInfo? prop = null;
        
        // Try snake_case
        prop = objType.GetProperty(ToSnakeCase(propName), BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        
        // Try PascalCase
        if (prop == null)
        {
            prop = objType.GetProperty(ToPascalCase(propName), BindingFlags.Public | BindingFlags.Instance);
        }
        
        // Try as-is
        if (prop == null)
        {
            prop = objType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        }
        
        if (prop == null)
        {
            throw new ArgumentException($"Property '{propName}' not found on type {objType.Name}");
        }
        
        return Expression.Property(obj, prop);
    }
    
    private Expression GenerateFunctionCall(Expr.FunctionCall funcCall)
    {
        var target = funcCall.Item1;
        var args = ListModule.ToArray(funcCall.Item2);
        
        // Handle method calls (table.method)
        if (target is Expr.TableAccess tableAccess)
        {
            var obj = GenerateExpression(tableAccess.Item1);
            
            // Extract method name
            string methodName;
            if (tableAccess.Item2 is Expr.Literal literal && literal.Item.IsString)
            {
                var strLit2 = (Literal.String)literal.Item;
                methodName = strLit2.Item;
            }
            else
            {
                throw new NotSupportedException("Only string keys supported for method names");
            }
            
            // Find method with name translation
            var objType = obj.Type;
            MethodInfo? method = null;
            
            // Try snake_case
            method = objType.GetMethod(ToSnakeCase(methodName), BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            
            // Try PascalCase
            if (method == null)
            {
                method = objType.GetMethod(ToPascalCase(methodName), BindingFlags.Public | BindingFlags.Instance);
            }
            
            // Try as-is
            if (method == null)
            {
                method = objType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            }
            
            if (method == null)
            {
                throw new ArgumentException($"Method '{methodName}' not found on type {objType.Name}");
            }
            
            // Generate argument expressions
            var argExprs = args.Select(GenerateExpression).ToArray();
            
            return Expression.Call(obj, method, argExprs);
        }
        
        throw new NotSupportedException("Only method calls on objects are supported");
    }
    
    private Expression GenerateBinaryOp(Expr.Binary binary)
    {
        var left = GenerateExpression(binary.Item1);
        var right = GenerateExpression(binary.Item3);
        var op = binary.Item2;
        
        if (op.IsAdd)
            return Expression.Add(left, right);
        if (op.IsSubtract)
            return Expression.Subtract(left, right);
        if (op.IsMultiply)
            return Expression.Multiply(left, right);
        if (op.IsFloatDiv)
            return Expression.Divide(left, right);
        if (op.IsModulo)
            return Expression.Modulo(left, right);
        if (op.IsLess)
            return Expression.LessThan(left, right);
        if (op.IsGreater)
            return Expression.GreaterThan(left, right);
        if (op.IsLessEqual)
            return Expression.LessThanOrEqual(left, right);
        if (op.IsGreaterEqual)
            return Expression.GreaterThanOrEqual(left, right);
        if (op.IsEqual)
            return Expression.Equal(left, right);
        if (op.IsNotEqual)
            return Expression.NotEqual(left, right);
        if (op.IsAnd)
            return Expression.AndAlso(left, right);
        if (op.IsOr)
            return Expression.OrElse(left, right);
        
        // String concatenation
        if (op.IsConcat)
        {
            // Convert both to strings and concatenate
            var leftStr = left.Type == typeof(string) ? left : Expression.Call(left, "ToString", null);
            var rightStr = right.Type == typeof(string) ? right : Expression.Call(right, "ToString", null);
            return Expression.Call(typeof(string), "Concat", null, leftStr, rightStr);
        }
        
        // Power operation
        if (op.IsPower)
        {
            // Convert to double and use Math.Pow
            var leftDouble = Expression.Convert(left, typeof(double));
            var rightDouble = Expression.Convert(right, typeof(double));
            return Expression.Call(typeof(Math), "Pow", null, leftDouble, rightDouble);
        }
        
        // Floor division (Lua 5.3+)
        if (op.IsFloorDiv)
        {
            // Perform integer division
            return Expression.Divide(
                Expression.Convert(left, typeof(int)),
                Expression.Convert(right, typeof(int))
            );
        }
        
        // Bitwise operations
        if (op.IsBitAnd)
            return Expression.And(left, right);
        if (op.IsBitOr)
            return Expression.Or(left, right);
        if (op.IsBitXor)
            return Expression.ExclusiveOr(left, right);
        if (op.IsShiftLeft)
            return Expression.LeftShift(left, right);
        if (op.IsShiftRight)
            return Expression.RightShift(left, right);
            
        throw new NotSupportedException($"Operator not supported: {op}");
    }
    
    // Name translation helpers
    
    private Expression GenerateUnaryOp(Expr.Unary unary)
    {
        var operand = GenerateExpression(unary.Item2);
        var op = unary.Item1;
        
        if (op.IsNegate)
            return Expression.Negate(operand);
        if (op.IsNot)
            return Expression.Not(operand);
        if (op.IsLength)
        {
            // For strings, use Length property
            if (operand.Type == typeof(string))
            {
                return Expression.Property(operand, "Length");
            }
            // For arrays, use Length property
            else if (operand.Type.IsArray)
            {
                return Expression.ArrayLength(operand);
            }
            // For collections, try Count property
            else
            {
                var countProp = operand.Type.GetProperty("Count");
                if (countProp != null)
                {
                    return Expression.Property(operand, countProp);
                }
            }
            throw new NotSupportedException($"Length operator not supported for type {operand.Type}");
        }
        if (op.IsBitNot)
            return Expression.Not(operand);
            
        throw new NotSupportedException($"Unary operator not supported: {op}");
    }
    
    private Expression GenerateTableConstructor(Expr.TableConstructor tableConstructor)
    {
        // For sandbox safety, we'll create a dictionary instead of a dynamic object
        var dictType = typeof(Dictionary<string, object>);
        var newDict = Expression.New(dictType);
        
        var fields = tableConstructor.Item;
        if (ListModule.IsEmpty(fields))
        {
            return newDict;
        }
        
        // Create a block that builds the dictionary
        var dictVar = Expression.Variable(dictType, "dict");
        var expressions = new List<Expression> { Expression.Assign(dictVar, newDict) };
        
        var addMethod = dictType.GetMethod("Add");
        int positionalIndex = 1; // Lua arrays start at 1
        
        foreach (var field in fields)
        {
            if (field.IsExprField)
            {
                // Positional field: use numeric key
                var exprField = (TableField.ExprField)field;
                var keyExpr = Expression.Constant(positionalIndex.ToString());
                var valueExpr = GenerateExpression(exprField.Item);
                
                // Box value if needed
                if (valueExpr.Type.IsValueType)
                {
                    valueExpr = Expression.Convert(valueExpr, typeof(object));
                }
                
                expressions.Add(Expression.Call(dictVar, addMethod!, keyExpr, valueExpr));
                positionalIndex++;
            }
            else if (field.IsNamedField)
            {
                var namedField = (TableField.NamedField)field;
                var keyExpr = Expression.Constant(namedField.Item1);
                var valueExpr = GenerateExpression(namedField.Item2);
                
                // Box value if needed
                if (valueExpr.Type.IsValueType)
                {
                    valueExpr = Expression.Convert(valueExpr, typeof(object));
                }
                
                expressions.Add(Expression.Call(dictVar, addMethod!, keyExpr, valueExpr));
            }
            else if (field.IsKeyField)
            {
                var keyField = (TableField.KeyField)field;
                var keyExpr = GenerateExpression(keyField.Item1);
                var valueExpr = GenerateExpression(keyField.Item2);
                
                // Convert key to string if needed
                if (keyExpr.Type != typeof(string))
                {
                    keyExpr = Expression.Call(keyExpr, "ToString", null);
                }
                
                // Box value if needed
                if (valueExpr.Type.IsValueType)
                {
                    valueExpr = Expression.Convert(valueExpr, typeof(object));
                }
                
                expressions.Add(Expression.Call(dictVar, addMethod!, keyExpr, valueExpr));
            }
        }
        
        expressions.Add(dictVar);
        return Expression.Block(new[] { dictVar }, expressions);
    }
    
    

    
    private Expression? GenerateLocalAssignment(Statement.LocalAssignment local)
    {
        // local.Item1 is a list of tuples (name, attribute)
        var nameAttrs = local.Item1;
        var names = ListModule.ToArray(ListModule.Map(
            FSharpFunc<Tuple<string, FLua.Ast.Attribute>, string>.FromConverter(t => t.Item1), 
            nameAttrs));
        var values = OptionModule.IsSome(local.Item2) 
            ? ListModule.ToArray<Expr>(local.Item2.Value)
            : new Expr[0];
        
        var assignments = new List<Expression>();
        
        for (int i = 0; i < names.Length; i++)
        {
            var name = names[i];
            Expression value;
            
            if (i < values.Length)
            {
                value = GenerateExpression(values[i]);
            }
            else
            {
                // Lua assigns nil to uninitialized variables
                value = Expression.Default(typeof(object));
            }
            
            // Create or update local variable
            if (!_localVariables.ContainsKey(name))
            {
                var localVar = Expression.Variable(value.Type, name);
                _localVariables[name] = localVar;
                assignments.Add(Expression.Assign(localVar, value));
            }
            else
            {
                assignments.Add(Expression.Assign(_localVariables[name], value));
            }
        }
        
        return assignments.Count > 0 ? Expression.Block(assignments) : null;
    }
    
    private Expression GenerateWhileLoop(Statement.While whileStmt)
    {
        var condition = GenerateExpression(whileStmt.Item1);
        var body = GenerateBody(whileStmt.Item2);
        
        // Create a loop using labels and conditional goto
        var breakLabel = Expression.Label(_resultType);
        var continueLabel = Expression.Label();
        
        var loop = Expression.Loop(
            Expression.Block(
                Expression.IfThenElse(
                    condition,
                    body,
                    Expression.Break(breakLabel, Expression.Default(_resultType))
                ),
                Expression.Continue(continueLabel)
            ),
            breakLabel,
            continueLabel
        );
        
        return loop;
    }
    
    private Expression GenerateNumericForLoop(Statement.NumericFor numFor)
    {
        var (varName, start, end, stepOpt, body) = 
            (numFor.Item1, numFor.Item2, numFor.Item3, numFor.Item4, numFor.Item5);
        
        var startExpr = GenerateExpression(start);
        var endExpr = GenerateExpression(end);
        var stepExpr = OptionModule.IsSome(stepOpt) 
            ? GenerateExpression(stepOpt.Value) 
            : Expression.Constant(1);
        
        // Create loop variable
        var loopVar = Expression.Variable(typeof(int), varName);
        _localVariables[varName] = loopVar;
        
        var breakLabel = Expression.Label(_resultType);
        
        // Build the for loop
        var loop = Expression.Block(
            new[] { loopVar },
            Expression.Assign(loopVar, Expression.Convert(startExpr, typeof(int))),
            Expression.Loop(
                Expression.Block(
                    Expression.IfThen(
                        Expression.GreaterThan(loopVar, Expression.Convert(endExpr, typeof(int))),
                        Expression.Break(breakLabel, Expression.Default(_resultType))
                    ),
                    GenerateBody(body),
                    Expression.AddAssign(loopVar, Expression.Convert(stepExpr, typeof(int)))
                ),
                breakLabel
            )
        );
        
        // Remove loop variable from scope
        _localVariables.Remove(varName);
        
        return loop;
    }

    private static string ToSnakeCase(string pascalCase)
    {
        return Regex.Replace(pascalCase, "([a-z])([A-Z])", "$1_$2").ToLower();
    }
    
    private static string ToCamelCase(string pascalCase)
    {
        if (string.IsNullOrEmpty(pascalCase)) return pascalCase;
        return char.ToLower(pascalCase[0]) + pascalCase.Substring(1);
    }
    
    private static string ToPascalCase(string snakeCase)
    {
        return string.Join("", snakeCase.Split('_')
            .Select(s => string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s.Substring(1).ToLower()));
    }
}