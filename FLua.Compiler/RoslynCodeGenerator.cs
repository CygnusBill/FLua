using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using FLua.Ast;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using LuaAttribute = FLua.Ast.Attribute;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Collections;

namespace FLua.Compiler
{
    /// <summary>
    /// Generates C# code from Lua AST using Roslyn syntax factory
    /// </summary>
    public class RoslynCodeGenerator
    {
        private CompilerOptions _options = null!;
        
        // Scope management for variable name resolution
        private class Scope
        {
            public Dictionary<string, string> Variables { get; } = new Dictionary<string, string>();
            public Scope? Parent { get; set; }
        }
        
        private Scope _currentScope = new Scope();
        private int _variableCounter = 0;
        
        public RoslynCodeGenerator()
        {
        }
        
        public CompilationUnitSyntax Generate(IList<Statement> block, CompilerOptions options)
        {
            _options = options;
            
            // Create the Execute method
            var executeMethod = CreateExecuteMethod(block);
            
            // Create the LuaScript class
            var luaScriptClass = ClassDeclaration("LuaScript")
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                .AddMembers(executeMethod);
            
            // Create the namespace
            var namespaceDecl = NamespaceDeclaration(ParseName(options.AssemblyName ?? "CompiledLuaScript"))
                .AddMembers(luaScriptClass);
            
            // Create the compilation unit with usings
            var compilationUnit = CompilationUnit()
                .AddUsings(
                    UsingDirective(ParseName("System")),
                    UsingDirective(ParseName("System.Collections.Generic")),
                    UsingDirective(ParseName("System.Numerics")),
                    UsingDirective(ParseName("FLua.Runtime")))
                .AddMembers(namespaceDecl)
                .NormalizeWhitespace();
            
            return compilationUnit;
        }
        
        private MethodDeclarationSyntax CreateExecuteMethod(IList<Statement> block)
        {
            // Create the method signature: public static LuaValue[] Execute(LuaEnvironment env)
            var method = MethodDeclaration(
                    ArrayType(IdentifierName("LuaValue"))
                        .WithRankSpecifiers(SingletonList(ArrayRankSpecifier())),
                    "Execute")
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                .AddParameterListParameters(
                    Parameter(Identifier("env"))
                        .WithType(IdentifierName("LuaEnvironment")));
            
            // Generate method body
            var statements = new List<StatementSyntax>();
            
            // Add all Lua statements
            foreach (var stmt in block)
            {
                var generatedStmts = GenerateStatement(stmt);
                statements.AddRange(generatedStmts);
            }
            
            // Add default return if needed
            if (block.Count == 0 || !block[block.Count - 1].IsReturn)
            {
                statements.Add(ReturnStatement(
                    ArrayCreationExpression(
                        ArrayType(IdentifierName("LuaValue"))
                            .WithRankSpecifiers(SingletonList(ArrayRankSpecifier())))
                    .WithInitializer(InitializerExpression(
                        SyntaxKind.ArrayInitializerExpression))));
            }
            
            // Create method body
            method = method.WithBody(Block(statements));
            
            return method;
        }
        
        private IEnumerable<StatementSyntax> GenerateStatement(Statement statement)
        {
            if (statement.IsLocalAssignment)
            {
                var localAssign = (Statement.LocalAssignment)statement;
                var vars = FSharpListToList(localAssign.Item1);
                var exprs = OptionModule.IsSome(localAssign.Item2) ? FSharpListToList(localAssign.Item2.Value) : new List<Expr>();
                
                return GenerateLocalAssignment(vars, exprs);
            }
            else if (statement.IsFunctionCall)
            {
                var funcCall = (Statement.FunctionCall)statement;
                var expr = GenerateFunctionCallStatement(funcCall.Item);
                return new[] { ExpressionStatement(expr) };
            }
            else if (statement.IsReturn)
            {
                var ret = (Statement.Return)statement;
                var exprs = OptionModule.IsSome(ret.Item) ? FSharpListToList(ret.Item.Value) : new List<Expr>();
                return new[] { GenerateReturn(exprs) };
            }
            else if (statement.IsIf)
            {
                var ifStmt = (Statement.If)statement;
                var clauses = FSharpListToList(ifStmt.Item1);
                var elseBlock = OptionModule.IsSome(ifStmt.Item2) ? FSharpListToList(ifStmt.Item2.Value) : null;
                return new[] { GenerateIf(clauses, elseBlock) };
            }
            else if (statement.IsDoBlock)
            {
                var doBlock = (Statement.DoBlock)statement;
                var body = FSharpListToList(doBlock.Item);
                return new[] { GenerateDoBlock(body) };
            }
            else if (statement.IsLocalFunctionDef)
            {
                var funcDef = (Statement.LocalFunctionDef)statement;
                return GenerateLocalFunctionDef(funcDef.Item1, funcDef.Item2);
            }
            else
            {
                // Return a comment for unimplemented statement types
                return new[] { 
                    EmptyStatement().WithLeadingTrivia(
                        Comment($"// TODO: Implement {statement.GetType().Name}")) 
                };
            }
        }
        
        private IEnumerable<StatementSyntax> GenerateLocalAssignment(IList<(string, LuaAttribute)> vars, IList<Expr> exprs)
        {
            var statements = new List<StatementSyntax>();
            
            for (int i = 0; i < vars.Count; i++)
            {
                var (varName, attr) = vars[i];
                var value = i < exprs.Count ? exprs[i] : Expr.CreateLiteral(FLua.Ast.Literal.CreateNil());
                
                // Get mangled name for the variable
                var mangledName = GetOrCreateMangledName(varName);
                
                // Create local variable declaration: var mangledName = expression;
                var varDecl = LocalDeclarationStatement(
                    VariableDeclaration(IdentifierName("var"))
                        .AddVariables(
                            VariableDeclarator(Identifier(SanitizeIdentifier(mangledName)))
                                .WithInitializer(EqualsValueClause(GenerateExpression(value)))));
                
                statements.Add(varDecl);
                
                // Also store in environment: env.SetVariable("varName", mangledName);
                var setVarCall = ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("env"),
                            IdentifierName("SetVariable")))
                    .AddArgumentListArguments(
                        Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(varName))),
                        Argument(IdentifierName(SanitizeIdentifier(mangledName)))));
                
                statements.Add(setVarCall);
            }
            
            return statements;
        }
        
        private ExpressionSyntax GenerateExpression(Expr expr)
        {
            if (expr.IsLiteral)
            {
                var literal = ((Expr.Literal)expr).Item;
                return GenerateLiteral(literal);
            }
            else if (expr.IsVar)
            {
                var name = ((Expr.Var)expr).Item;
                return GenerateVariable(name);
            }
            else if (expr.IsBinary)
            {
                var binary = (Expr.Binary)expr;
                return GenerateBinaryExpression(binary.Item1, binary.Item2, binary.Item3);
            }
            else if (expr.IsFunctionCall)
            {
                var funcCall = (Expr.FunctionCall)expr;
                return GenerateFunctionCall(funcCall.Item1, FSharpListToList(funcCall.Item2));
            }
            else
            {
                // Return a placeholder for unimplemented expressions
                return LiteralExpression(SyntaxKind.NullLiteralExpression);
            }
        }
        
        private ExpressionSyntax GenerateLiteral(Literal literal)
        {
            if (literal.IsNil)
            {
                return MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("LuaValue"),
                    IdentifierName("Nil"));
            }
            else if (literal.IsBoolean)
            {
                var value = ((Literal.Boolean)literal).Item;
                return ObjectCreationExpression(IdentifierName("LuaBoolean"))
                    .AddArgumentListArguments(
                        Argument(LiteralExpression(
                            value ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression)));
            }
            else if (literal.IsInteger)
            {
                var value = ((Literal.Integer)literal).Item;
                return ObjectCreationExpression(IdentifierName("LuaInteger"))
                    .AddArgumentListArguments(
                        Argument(LiteralExpression(
                            SyntaxKind.NumericLiteralExpression, 
                            Literal(long.Parse(value.ToString())))));
            }
            else if (literal.IsString)
            {
                var value = ((Literal.String)literal).Item;
                return ObjectCreationExpression(IdentifierName("LuaString"))
                    .AddArgumentListArguments(
                        Argument(LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            Literal(value))));
            }
            else if (literal.IsFloat)
            {
                var value = ((Literal.Float)literal).Item;
                return ObjectCreationExpression(IdentifierName("LuaNumber"))
                    .AddArgumentListArguments(
                        Argument(LiteralExpression(
                            SyntaxKind.NumericLiteralExpression,
                            Literal(value))));
            }
            else
            {
                throw new NotImplementedException($"Literal type {literal.GetType()} not implemented");
            }
        }
        
        private ExpressionSyntax GenerateVariable(string name)
        {
            var mangledName = ResolveVariableName(name);
            
            // Check if it's a local variable
            if (IsLocalVariable(name))
            {
                return IdentifierName(SanitizeIdentifier(mangledName));
            }
            else
            {
                // Global variable - get from environment
                return InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("env"),
                        IdentifierName("GetVariable")))
                    .AddArgumentListArguments(
                        Argument(LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            Literal(name))));
            }
        }
        
        private ExpressionSyntax GenerateBinaryExpression(Expr left, BinaryOp op, Expr right)
        {
            string methodName;
            if (op.IsAdd) methodName = "Add";
            else if (op.IsSubtract) methodName = "Subtract";
            else if (op.IsMultiply) methodName = "Multiply";
            else if (op.IsFloatDiv) methodName = "FloatDivide";
            else if (op.IsModulo) methodName = "Modulo";
            else if (op.IsPower) methodName = "Power";
            else if (op.IsConcat) methodName = "Concat";
            else if (op.IsEqual) methodName = "Equal";
            else if (op.IsNotEqual) methodName = "NotEqual";
            else if (op.IsLess) methodName = "Less";
            else if (op.IsLessEqual) methodName = "LessEqual";
            else if (op.IsGreater) methodName = "Greater";
            else if (op.IsGreaterEqual) methodName = "GreaterEqual";
            else if (op.IsAnd) methodName = "And";
            else if (op.IsOr) methodName = "Or";
            else methodName = "Unknown";
            
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("LuaOperations"),
                    IdentifierName(methodName)))
                .AddArgumentListArguments(
                    Argument(GenerateExpression(left)),
                    Argument(GenerateExpression(right)));
        }
        
        private ExpressionSyntax GenerateFunctionCall(Expr func, IList<Expr> args)
        {
            // Generate: ((LuaFunction)func).Call(new LuaValue[] { args })
            var funcExpr = ParenthesizedExpression(
                CastExpression(
                    IdentifierName("LuaFunction"),
                    GenerateExpression(func)));
            
            var argExpressions = args.Select(arg => GenerateExpression(arg)).ToArray();
            
            var arrayCreation = ArrayCreationExpression(
                ArrayType(IdentifierName("LuaValue"))
                    .WithRankSpecifiers(SingletonList(ArrayRankSpecifier())))
                .WithInitializer(InitializerExpression(
                    SyntaxKind.ArrayInitializerExpression,
                    SeparatedList(argExpressions)));
            
            var callExpr = InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    funcExpr,
                    IdentifierName("Call")))
                .AddArgumentListArguments(Argument(arrayCreation));
            
            // For expressions, we need to index [0]
            return ElementAccessExpression(callExpr)
                .AddArgumentListArguments(
                    Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))));
        }
        
        private ExpressionSyntax GenerateFunctionCallStatement(Expr expr)
        {
            if (expr.IsFunctionCall)
            {
                var funcCall = (Expr.FunctionCall)expr;
                var func = funcCall.Item1;
                var args = FSharpListToList(funcCall.Item2);
                
                // Generate: ((LuaFunction)func).Call(new LuaValue[] { args })
                var funcExpr = ParenthesizedExpression(
                    CastExpression(
                        IdentifierName("LuaFunction"),
                        GenerateExpression(func)));
                
                var argExpressions = args.Select(arg => GenerateExpression(arg)).ToArray();
                
                var arrayCreation = ArrayCreationExpression(
                    ArrayType(IdentifierName("LuaValue"))
                        .WithRankSpecifiers(SingletonList(ArrayRankSpecifier())))
                    .WithInitializer(InitializerExpression(
                        SyntaxKind.ArrayInitializerExpression,
                        SeparatedList(argExpressions)));
                
                return InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        funcExpr,
                        IdentifierName("Call")))
                    .AddArgumentListArguments(Argument(arrayCreation));
            }
            
            // Fallback for non-function call expressions
            return GenerateExpression(expr);
        }
        
        private StatementSyntax GenerateReturn(IList<Expr> exprs)
        {
            var exprArray = exprs.Select(e => GenerateExpression(e)).ToArray();
            
            var arrayCreation = ArrayCreationExpression(
                ArrayType(IdentifierName("LuaValue"))
                    .WithRankSpecifiers(SingletonList(ArrayRankSpecifier())))
                .WithInitializer(InitializerExpression(
                    SyntaxKind.ArrayInitializerExpression,
                    SeparatedList(exprArray)));
            
            return ReturnStatement(arrayCreation);
        }
        
        private StatementSyntax GenerateIf(IList<(Expr, IList<Statement>)> clauses, IList<Statement>? elseBlock)
        {
            IfStatementSyntax? result = null;
            ElseClauseSyntax? currentElse = null;
            
            // Build if/else if chain from the end
            for (int i = clauses.Count - 1; i >= 0; i--)
            {
                var (condition, body) = clauses[i];
                
                // Generate condition.IsTruthy
                var conditionExpr = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    GenerateExpression(condition),
                    IdentifierName("IsTruthy"));
                
                // Generate body block
                EnterScope();
                var bodyStatements = body.SelectMany(s => GenerateStatement(s)).ToList();
                ExitScope();
                var bodyBlock = Block(bodyStatements);
                
                if (i == clauses.Count - 1)
                {
                    // Last clause - might have else block
                    if (elseBlock != null)
                    {
                        EnterScope();
                        var elseStatements = elseBlock.SelectMany(s => GenerateStatement(s)).ToList();
                        ExitScope();
                        currentElse = ElseClause(Block(elseStatements));
                    }
                    
                    result = IfStatement(conditionExpr, bodyBlock, currentElse);
                }
                else
                {
                    // Create else if
                    currentElse = ElseClause(result!);
                    result = IfStatement(conditionExpr, bodyBlock, currentElse);
                }
            }
            
            return result!;
        }
        
        private StatementSyntax GenerateDoBlock(IList<Statement> body)
        {
            EnterScope();
            var statements = body.SelectMany(s => GenerateStatement(s)).ToList();
            ExitScope();
            return Block(statements);
        }
        
        private IEnumerable<StatementSyntax> GenerateLocalFunctionDef(string name, FunctionDef funcDef)
        {
            var statements = new List<StatementSyntax>();
            var mangledName = GetOrCreateMangledName(name);
            
            // Create the local function
            var funcDecl = LocalFunctionStatement(
                returnType: ArrayType(IdentifierName("LuaValue"))
                    .WithRankSpecifiers(SingletonList(ArrayRankSpecifier())),
                identifier: Identifier(SanitizeIdentifier(mangledName)))
                .AddParameterListParameters(
                    Parameter(Identifier("args"))
                        .WithType(ArrayType(IdentifierName("LuaValue"))
                            .WithRankSpecifiers(SingletonList(ArrayRankSpecifier())))
                        .AddModifiers(Token(SyntaxKind.ParamsKeyword)));
            
            // Generate function body
            EnterScope();
            var bodyStatements = new List<StatementSyntax>();
            
            // Handle parameters
            var parameters = FSharpListToList(funcDef.Parameters);
            int paramIndex = 0;
            
            foreach (var param in parameters)
            {
                if (param.IsNamed)
                {
                    var namedParam = (Parameter.Named)param;
                    var paramName = namedParam.Item1;
                    var paramMangledName = GetOrCreateMangledName(paramName);
                    
                    // var paramMangledName = args.Length > paramIndex ? args[paramIndex] : LuaValue.Nil;
                    var paramDecl = LocalDeclarationStatement(
                        VariableDeclaration(IdentifierName("var"))
                            .AddVariables(
                                VariableDeclarator(Identifier(SanitizeIdentifier(paramMangledName)))
                                    .WithInitializer(EqualsValueClause(
                                        ConditionalExpression(
                                            BinaryExpression(
                                                SyntaxKind.GreaterThanExpression,
                                                MemberAccessExpression(
                                                    SyntaxKind.SimpleMemberAccessExpression,
                                                    IdentifierName("args"),
                                                    IdentifierName("Length")),
                                                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(paramIndex))),
                                            ElementAccessExpression(IdentifierName("args"))
                                                .AddArgumentListArguments(
                                                    Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(paramIndex)))),
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName("LuaValue"),
                                                IdentifierName("Nil")))))));
                    
                    bodyStatements.Add(paramDecl);
                    
                    // env.SetVariable("paramName", paramMangledName);
                    var setVarCall = ExpressionStatement(
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName("env"),
                                IdentifierName("SetVariable")))
                        .AddArgumentListArguments(
                            Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(paramName))),
                            Argument(IdentifierName(SanitizeIdentifier(paramMangledName)))));
                    
                    bodyStatements.Add(setVarCall);
                    paramIndex++;
                }
            }
            
            // Add function body
            var funcBody = FSharpListToList(funcDef.Body);
            bodyStatements.AddRange(funcBody.SelectMany(s => GenerateStatement(s)));
            
            // Add default return if needed
            bool hasReturn = funcBody.Count > 0 && funcBody[funcBody.Count - 1].IsReturn;
            if (!hasReturn)
            {
                bodyStatements.Add(ReturnStatement(
                    ArrayCreationExpression(
                        ArrayType(IdentifierName("LuaValue"))
                            .WithRankSpecifiers(SingletonList(ArrayRankSpecifier())))
                    .WithInitializer(InitializerExpression(
                        SyntaxKind.ArrayInitializerExpression))));
            }
            
            ExitScope();
            
            funcDecl = funcDecl.WithBody(Block(bodyStatements));
            statements.Add(funcDecl);
            
            // Create LuaUserFunction wrapper
            var wrapperVarName = $"{SanitizeIdentifier(mangledName)}_func";
            var wrapperDecl = LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("var"))
                    .AddVariables(
                        VariableDeclarator(Identifier(wrapperVarName))
                            .WithInitializer(EqualsValueClause(
                                ObjectCreationExpression(IdentifierName("LuaUserFunction"))
                                    .AddArgumentListArguments(
                                        Argument(IdentifierName(SanitizeIdentifier(mangledName))))))));
            
            statements.Add(wrapperDecl);
            
            // Store the wrapped function variable in the scope
            _currentScope.Variables[name] = wrapperVarName;
            
            // env.SetVariable("name", mangledName_func);
            var setFuncCall = ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("env"),
                        IdentifierName("SetVariable")))
                .AddArgumentListArguments(
                    Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(name))),
                    Argument(IdentifierName(wrapperVarName))));
            
            statements.Add(setFuncCall);
            
            return statements;
        }
        
        #region Scope Management
        
        private void EnterScope()
        {
            var newScope = new Scope { Parent = _currentScope };
            _currentScope = newScope;
        }
        
        private void ExitScope()
        {
            if (_currentScope.Parent != null)
            {
                _currentScope = _currentScope.Parent;
            }
        }
        
        private string GetOrCreateMangledName(string varName)
        {
            if (_currentScope.Variables.TryGetValue(varName, out var mangledName))
            {
                return mangledName;
            }
            
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
            var scope = _currentScope;
            while (scope != null)
            {
                if (scope.Variables.TryGetValue(varName, out var mangledName))
                {
                    return mangledName;
                }
                scope = scope.Parent;
            }
            
            return varName;
        }
        
        private bool IsLocalVariable(string varName)
        {
            var scope = _currentScope;
            while (scope != null)
            {
                if (scope.Variables.ContainsKey(varName))
                {
                    return true;
                }
                scope = scope.Parent;
            }
            return false;
        }
        
        #endregion
        
        #region Utility Methods
        
        private static string SanitizeIdentifier(string name)
        {
            var sanitized = name.Replace("-", "_").Replace(".", "_");
            
            if (sanitized.Length > 0 && char.IsDigit(sanitized[0]))
            {
                sanitized = "_" + sanitized;
            }
            
            return sanitized;
        }
        
        private static IList<T> FSharpListToList<T>(Microsoft.FSharp.Collections.FSharpList<T> fsList)
        {
            return Microsoft.FSharp.Collections.ListModule.ToArray(fsList);
        }
        
        private static IList<(string, LuaAttribute)> FSharpListToList(
            Microsoft.FSharp.Collections.FSharpList<System.Tuple<string, LuaAttribute>> fsList)
        {
            return Microsoft.FSharp.Collections.ListModule.ToArray(fsList)
                .Select(t => (t.Item1, t.Item2))
                .ToList();
        }
        
        private static IList<(Expr, IList<Statement>)> FSharpListToList(
            Microsoft.FSharp.Collections.FSharpList<System.Tuple<Expr, Microsoft.FSharp.Collections.FSharpList<Statement>>> fsList)
        {
            return Microsoft.FSharp.Collections.ListModule.ToArray(fsList)
                .Select(t => (t.Item1, (IList<Statement>)FSharpListToList(t.Item2)))
                .ToList();
        }
        
        #endregion
    }
}