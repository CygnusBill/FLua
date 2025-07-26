using FLua.Ast;
using System.Text;
using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace FLua.Compiler;

/// <summary>
/// Generates C# code from Lua AST
/// </summary>
public class CSharpCodeGenerator
{
    private readonly StringBuilder _code;
    private int _indentLevel;
    private int _labelCounter;
    private int _tempVarCounter;
    
    // Scope tracking for variable name mangling
    private class Scope
    {
        public Dictionary<string, string> Variables { get; } = new Dictionary<string, string>();
        public Scope? Parent { get; set; }
    }
    
    private Scope _currentScope = new Scope();
    private int _scopeDepth = 0;
    private int _variableCounter = 0;

    public CSharpCodeGenerator()
    {
        _code = new StringBuilder();
        _indentLevel = 0;
        _labelCounter = 0;
        _tempVarCounter = 0;
    }

    public string Generate(IList<Statement> block, CompilerOptions options)
    {
        _code.Clear();
        _indentLevel = 0;
        _labelCounter = 0;
        _tempVarCounter = 0;

        // Generate using statements
        GenerateUsings();
        
        // Generate namespace and class wrapper
        GenerateNamespaceStart(options);
        GenerateClassStart(options);
        
        // Generate main method
        if (options.Target == CompilationTarget.ConsoleApp)
        {
            GenerateConsoleMainMethod(block);
        }
        else
        {
            GenerateLibraryEntryPoint(block);
        }
        
        // Close class and namespace
        GenerateClassEnd();
        GenerateNamespaceEnd();

        return _code.ToString();
    }

    private void GenerateUsings()
    {
        WriteLine("using System;");
        WriteLine("using System.Collections.Generic;");
        WriteLine("using System.Numerics;");
        WriteLine("using FLua.Runtime;");
        WriteLine();
    }

    private void GenerateNamespaceStart(CompilerOptions options)
    {
        var namespaceName = options.AssemblyName ?? "CompiledLuaScript";
        WriteLine($"namespace {namespaceName};");
        WriteLine();
    }

    private void GenerateNamespaceEnd()
    {
        // Nothing needed for file-scoped namespaces
    }

    private void GenerateClassStart(CompilerOptions options)
    {
        WriteLine("public static class LuaScript");
        WriteLine("{");
        IncreaseIndent();
    }

    private void GenerateClassEnd()
    {
        DecreaseIndent();
        WriteLine("}");
    }

    private void GenerateConsoleMainMethod(IList<Statement> block)
    {
        WriteLine("public static void Main(string[] args)");
        WriteLine("{");
        IncreaseIndent();
        
        WriteLine("var env = new LuaEnvironment();");
        WriteLine("try");
        WriteLine("{");
        IncreaseIndent();
        
        GenerateBlock(block);
        
        DecreaseIndent();
        WriteLine("}");
        WriteLine("catch (Exception ex)");
        WriteLine("{");
        IncreaseIndent();
        WriteLine("Console.WriteLine($\"Error: {ex.Message}\");");
        WriteLine("Environment.Exit(1);");
        DecreaseIndent();
        WriteLine("}");
        
        DecreaseIndent();
        WriteLine("}");
    }

    private void GenerateLibraryEntryPoint(IList<Statement> block)
    {
        WriteLine("public static LuaValue[] Execute(LuaEnvironment env)");
        WriteLine("{");
        IncreaseIndent();
        
        GenerateBlock(block);
        WriteLine("return new LuaValue[0];");
        
        DecreaseIndent();
        WriteLine("}");
    }

    private void GenerateBlock(IList<Statement> block)
    {
        foreach (var statement in block)
        {
            GenerateStatement(statement);
        }
    }

    private void GenerateStatement(Statement statement)
    {
        if (statement.IsEmpty)
        {
            WriteLine("// Empty statement");
        }
        else if (statement.IsAssignment)
        {
            var vars = FSharpListToList(((Statement.Assignment)statement).Item1);
            var exprs = FSharpListToList(((Statement.Assignment)statement).Item2);
            GenerateAssignment(vars, exprs);
        }
        else if (statement.IsLocalAssignment)
        {
            var vars = FSharpListOfStringAttrToList(((Statement.LocalAssignment)statement).Item1);
            var exprs = FSharpOptionToList(((Statement.LocalAssignment)statement).Item2);
            GenerateLocalAssignment(vars, exprs);
        }
        else if (statement.IsFunctionCall)
        {
            var expr = ((Statement.FunctionCall)statement).Item;
            Write("");
            if (expr.IsFunctionCall)
            {
                var funcCallExpr = (Expr.FunctionCall)expr;
                GenerateFunctionCall(funcCallExpr.Item1, FSharpListToList(funcCallExpr.Item2), asStatement: true);
            }
            else
            {
                GenerateExpression(expr);
            }
            WriteLine(";");
        }
        else if (statement.IsReturn)
        {
            var exprs = FSharpOptionToList(((Statement.Return)statement).Item);
            GenerateReturn(exprs);
        }
        else if (statement.IsIf)
        {
            var ifStmt = (Statement.If)statement;
            var clauses = FSharpListOfTupleToList<Expr, Statement>(ifStmt.Item1);
            var elseBlock = FSharpOptionToList(ifStmt.Item2);
            GenerateIf(clauses, elseBlock);
        }
        else if (statement.IsWhile)
        {
            var whileStmt = (Statement.While)statement;
            var body = FSharpListToList(whileStmt.Item2);
            GenerateWhile(whileStmt.Item1, body);
        }
        else if (statement.IsDoBlock)
        {
            var body = FSharpListToList(((Statement.DoBlock)statement).Item);
            GenerateDoBlock(body);
        }
        else if (statement.IsLocalFunctionDef)
        {
            var localFunc = (Statement.LocalFunctionDef)statement;
            var name = localFunc.Item1;
            var funcDef = localFunc.Item2;
            GenerateLocalFunctionDef(name, funcDef);
        }
        else
        {
            WriteLine($"// TODO: Implement {statement.GetType().Name}");
        }
    }

    private void GenerateAssignment(IList<Expr> vars, IList<Expr> exprs)
    {
        for (int i = 0; i < vars.Count; i++)
        {
            var variable = vars[i];
            var value = i < exprs.Count ? exprs[i] : Expr.CreateLiteral(Literal.CreateNil());
            
            Write("");
            GenerateExpression(variable);
            Write(" = ");
            GenerateExpression(value);
            WriteLine(";");
        }
    }

    private void GenerateLocalAssignment(IList<(string, Attribute)> vars, IList<Expr>? exprs)
    {
        for (int i = 0; i < vars.Count; i++)
        {
            var (varName, attr) = vars[i];
            var value = exprs != null && i < exprs.Count ? exprs[i] : Expr.CreateLiteral(Literal.CreateNil());
            
            // Get mangled name for the variable
            var mangledName = GetOrCreateMangledName(varName);
            
            Write($"var {SanitizeIdentifier(mangledName)} = ");
            GenerateExpression(value);
            WriteLine(";");
            
            // Also store in environment for global access
            WriteLine($"env.SetVariable(\"{varName}\", {SanitizeIdentifier(mangledName)});");
        }
    }

    private void GenerateReturn(IList<Expr>? exprs)
    {
        if (exprs == null || exprs.Count == 0)
        {
            WriteLine("return new LuaValue[0];");
        }
        else
        {
            Write("return new LuaValue[] { ");
            for (int i = 0; i < exprs.Count; i++)
            {
                if (i > 0) Write(", ");
                GenerateExpression(exprs[i]);
            }
            WriteLine(" };");
        }
    }

    private void GenerateIf(IList<(Expr, IList<Statement>)> clauses, IList<Statement>? elseBlock)
    {
        bool first = true;
        foreach (var (condition, body) in clauses)
        {
            if (first)
            {
                Write("if (");
                first = false;
            }
            else
            {
                Write("else if (");
            }
            
            GenerateExpression(condition);
            WriteLine(".IsTruthy)");
            WriteLine("{");
            IncreaseIndent();
            EnterScope();
            GenerateBlock(body);
            ExitScope();
            DecreaseIndent();
            WriteLine("}");
        }
        
        if (elseBlock != null)
        {
            WriteLine("else");
            WriteLine("{");
            IncreaseIndent();
            EnterScope();
            GenerateBlock(elseBlock);
            ExitScope();
            DecreaseIndent();
            WriteLine("}");
        }
    }

    private void GenerateWhile(Expr condition, IList<Statement> body)
    {
        Write("while (");
        GenerateExpression(condition);
        WriteLine(".IsTruthy)");
        WriteLine("{");
        IncreaseIndent();
        EnterScope();
        GenerateBlock(body);
        ExitScope();
        DecreaseIndent();
        WriteLine("}");
    }

    private void GenerateDoBlock(IList<Statement> body)
    {
        WriteLine("{");
        IncreaseIndent();
        EnterScope();
        GenerateBlock(body);
        ExitScope();
        DecreaseIndent();
        WriteLine("}");
    }

    private void GenerateLocalFunctionDef(string name, FunctionDef funcDef)
    {
        // Get mangled name for the function
        var mangledName = GetOrCreateMangledName(name);
        
        // Generate a C# local function
        Write($"LuaValue[] {SanitizeIdentifier(mangledName)}(params LuaValue[] args) ");
        WriteLine("{");
        IncreaseIndent();
        
        // Enter new scope for function parameters
        EnterScope();
        
        // Extract parameters
        var parameters = FSharpListToList(funcDef.Parameters);
        var paramNames = new List<string>();
        int paramIndex = 0;
        
        foreach (var param in parameters)
        {
            if (param.IsNamed)
            {
                var namedParam = (Parameter.Named)param;
                var paramName = namedParam.Item1;
                paramNames.Add(paramName);
                
                // Get mangled name for parameter
                var paramMangledName = GetOrCreateMangledName(paramName);
                
                // Assign parameter value from args array
                WriteLine($"var {SanitizeIdentifier(paramMangledName)} = args.Length > {paramIndex} ? args[{paramIndex}] : LuaValue.Nil;");
                WriteLine($"env.SetVariable(\"{paramName}\", {SanitizeIdentifier(paramMangledName)});");
                paramIndex++;
            }
            else if (param.IsVararg)
            {
                // Handle varargs - store remaining arguments
                WriteLine($"var varargs = new LuaValue[Math.Max(0, args.Length - {paramIndex})];");
                WriteLine($"if (args.Length > {paramIndex}) Array.Copy(args, {paramIndex}, varargs, 0, args.Length - {paramIndex});");
                // TODO: Set up varargs access
            }
        }
        
        // Generate function body
        var body = FSharpListToList(funcDef.Body);
        GenerateBlock(body);
        
        // Default return if no explicit return - check if last statement is return
        bool hasReturn = body.Count > 0 && body[body.Count - 1].IsReturn;
        if (!hasReturn)
        {
            WriteLine("return new LuaValue[0];");
        }
        
        // Exit function scope
        ExitScope();
        
        DecreaseIndent();
        WriteLine("}");
        
        // Create a LuaUserFunction wrapper and store it in the environment
        WriteLine($"var {SanitizeIdentifier(mangledName)}_func = new LuaUserFunction({SanitizeIdentifier(mangledName)});");
        WriteLine($"env.SetVariable(\"{name}\", {SanitizeIdentifier(mangledName)}_func);");
    }

    private void GenerateExpression(Expr expr)
    {
        if (expr.IsLiteral)
        {
            var literal = ((Expr.Literal)expr).Item;
            GenerateLiteral(literal);
        }
        else if (expr.IsVar)
        {
            var name = ((Expr.Var)expr).Item;
            var mangledName = ResolveVariableName(name);
            
            // Check if it's a local variable in any scope
            bool isLocal = false;
            var scope = _currentScope;
            while (scope != null)
            {
                if (scope.Variables.ContainsKey(name))
                {
                    isLocal = true;
                    break;
                }
                scope = scope.Parent;
            }
            
            if (isLocal)
            {
                // Use the local C# variable directly
                Write(SanitizeIdentifier(mangledName));
            }
            else
            {
                // Global variable - get from environment
                Write($"env.GetVariable(\"{name}\")");
            }
        }
        else if (expr.IsBinary)
        {
            var binaryExpr = (Expr.Binary)expr;
            GenerateBinaryExpression(binaryExpr.Item1, binaryExpr.Item2, binaryExpr.Item3);
        }
        else if (expr.IsUnary)
        {
            var unaryExpr = (Expr.Unary)expr;
            GenerateUnaryExpression(unaryExpr.Item1, unaryExpr.Item2);
        }
        else if (expr.IsFunctionCall)
        {
            var funcCallExpr = (Expr.FunctionCall)expr;
            GenerateFunctionCall(funcCallExpr.Item1, FSharpListToList(funcCallExpr.Item2));
        }
        else if (expr.IsTableAccess)
        {
            var tableAccessExpr = (Expr.TableAccess)expr;
            GenerateTableAccess(tableAccessExpr.Item1, tableAccessExpr.Item2);
        }
        else if (expr.IsParen)
        {
            var inner = ((Expr.Paren)expr).Item;
            Write("(");
            GenerateExpression(inner);
            Write(")");
        }
        else
        {
            Write($"new LuaValue() /* TODO: {expr.GetType().Name} */");
        }
    }

    private string GenerateExpressionToString(Expr expr)
    {
        var originalCode = _code.ToString();
        var originalLength = _code.Length;
        
        GenerateExpression(expr);
        
        var result = _code.ToString()[originalLength..];
        _code.Length = originalLength;
        
        return result;
    }

    private void GenerateLiteral(Literal literal)
    {
        if (literal.IsNil)
        {
            Write("LuaValue.Nil");
        }
        else if (literal.IsBoolean)
        {
            var value = ((Literal.Boolean)literal).Item;
            Write($"new LuaBoolean({value.ToString().ToLower()})");
        }
        else if (literal.IsInteger)
        {
            var value = ((Literal.Integer)literal).Item;
            Write($"new LuaInteger({value})");
        }
        else if (literal.IsFloat)
        {
            var value = ((Literal.Float)literal).Item;
            Write($"new LuaNumber({value}d)");
        }
        else if (literal.IsString)
        {
            var value = ((Literal.String)literal).Item;
            Write($"new LuaString({EscapeString(value)})");
        }
    }

    private void GenerateBinaryExpression(Expr left, BinaryOp op, Expr right)
    {
        string operatorMethod;
        if (op.IsAdd) operatorMethod = "Add";
        else if (op.IsSubtract) operatorMethod = "Subtract";
        else if (op.IsMultiply) operatorMethod = "Multiply";
        else if (op.IsFloatDiv) operatorMethod = "FloatDivide";
        else if (op.IsModulo) operatorMethod = "Modulo";
        else if (op.IsPower) operatorMethod = "Power";
        else if (op.IsConcat) operatorMethod = "Concat";
        else if (op.IsEqual) operatorMethod = "Equal";
        else if (op.IsNotEqual) operatorMethod = "NotEqual";
        else if (op.IsLess) operatorMethod = "Less";
        else if (op.IsLessEqual) operatorMethod = "LessEqual";
        else if (op.IsGreater) operatorMethod = "Greater";
        else if (op.IsGreaterEqual) operatorMethod = "GreaterEqual";
        else if (op.IsAnd) operatorMethod = "And";
        else if (op.IsOr) operatorMethod = "Or";
        else operatorMethod = "Unknown";

        Write($"LuaOperations.{operatorMethod}(");
        GenerateExpression(left);
        Write(", ");
        GenerateExpression(right);
        Write(")");
    }

    private void GenerateUnaryExpression(UnaryOp op, Expr operand)
    {
        string operatorMethod;
        if (op.IsNegate) operatorMethod = "Negate";
        else if (op.IsNot) operatorMethod = "Not";
        else if (op.IsLength) operatorMethod = "Length";
        else if (op.IsBitNot) operatorMethod = "BitNot";
        else operatorMethod = "Unknown";

        Write($"LuaOperations.{operatorMethod}(");
        GenerateExpression(operand);
        Write(")");
    }

    private void GenerateFunctionCall(Expr func, IList<Expr> args, bool asStatement = false)
    {
        Write("((LuaFunction)");
        GenerateExpression(func);
        Write(").Call(new LuaValue[] { ");
        for (int i = 0; i < args.Count; i++)
        {
            if (i > 0) Write(", ");
            GenerateExpression(args[i]);
        }
        Write(" })");
        if (!asStatement)
        {
            Write("[0]");
        }
    }

    private void GenerateTableAccess(Expr table, Expr key)
    {
        GenerateExpression(table);
        Write(".GetIndex(");
        GenerateExpression(key);
        Write(")");
    }

    #region Scope Management
    
    private void EnterScope()
    {
        var newScope = new Scope { Parent = _currentScope };
        _currentScope = newScope;
        _scopeDepth++;
    }
    
    private void ExitScope()
    {
        if (_currentScope.Parent != null)
        {
            _currentScope = _currentScope.Parent;
            _scopeDepth--;
        }
    }
    
    private string GetOrCreateMangledName(string varName)
    {
        // Check if variable exists in current scope
        if (_currentScope.Variables.TryGetValue(varName, out var mangledName))
        {
            return mangledName;
        }
        
        // Check if variable exists in any parent scope
        bool needsMangling = false;
        var scope = _currentScope.Parent;
        while (scope != null)
        {
            if (scope.Variables.ContainsKey(varName))
            {
                needsMangling = true;
                break;
            }
            scope = scope.Parent;
        }
        
        // Create mangled name if needed
        if (needsMangling)
        {
            mangledName = $"{varName}_{++_variableCounter}";
        }
        else
        {
            mangledName = varName;
        }
        
        _currentScope.Variables[varName] = mangledName;
        return mangledName;
    }
    
    private string ResolveVariableName(string varName)
    {
        // Look up variable in current scope and parent scopes
        var scope = _currentScope;
        while (scope != null)
        {
            if (scope.Variables.TryGetValue(varName, out var mangledName))
            {
                return mangledName;
            }
            scope = scope.Parent;
        }
        
        // If not found in any scope, return original name (for globals)
        return varName;
    }
    
    #endregion

    private static string SanitizeIdentifier(string name)
    {
        // Replace invalid C# identifier characters
        var sanitized = name.Replace("-", "_").Replace(".", "_");
        
        // Ensure it doesn't start with a number
        if (char.IsDigit(sanitized[0]))
        {
            sanitized = "_" + sanitized;
        }
        
        return sanitized;
    }

    private static string EscapeString(string value)
    {
        return $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
    }

    private void Write(string text)
    {
        _code.Append(text);
    }

    private void WriteLine(string text = "")
    {
        if (!string.IsNullOrEmpty(text))
        {
            _code.Append(new string(' ', _indentLevel * 4));
            _code.Append(text);
        }
        _code.AppendLine();
    }

    private void IncreaseIndent()
    {
        _indentLevel++;
    }

    private void DecreaseIndent()
    {
        _indentLevel = System.Math.Max(0, _indentLevel - 1);
    }

    // Helper methods to convert F# types to C# types
    private static IList<T> FSharpListToList<T>(FSharpList<T> fsList)
    {
        return ListModule.ToArray(fsList);
    }

    private static IList<T>? FSharpOptionToList<T>(FSharpOption<FSharpList<T>> fsOption)
    {
        return FSharpOption<FSharpList<T>>.get_IsSome(fsOption) ? FSharpListToList(fsOption.Value) : null;
    }

    private static IList<(T1, IList<T2>)> FSharpListOfTupleToList<T1, T2>(FSharpList<System.Tuple<T1, FSharpList<T2>>> fsList)
    {
        return ListModule.ToArray(fsList).Select(t => (t.Item1, FSharpListToList(t.Item2))).ToList();
    }

    private static IList<(string, Attribute)> FSharpListOfStringAttrToList(FSharpList<System.Tuple<string, Attribute>> fsList)
    {
        return ListModule.ToArray(fsList).Select(t => (t.Item1, t.Item2)).ToList();
    }
}