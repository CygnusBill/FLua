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
        private int _tempVarCount = 0;
        private int tempVarCounter = 0;
        private int _anonymousFunctionCounter = 0;
        private List<MethodDeclarationSyntax> _pendingMethods = new List<MethodDeclarationSyntax>();
        
        public RoslynCodeGenerator()
        {
        }
        
        public CompilationUnitSyntax Generate(IList<Statement> block, CompilerOptions options)
        {
            _options = options;
            
            // Create the Execute method
            var executeMethod = CreateExecuteMethod(block);
            
            // Create class members
            var classMembers = new List<MemberDeclarationSyntax> { executeMethod };
            
            // Add any pending methods (from anonymous functions)
            classMembers.AddRange(_pendingMethods);
            
            // Add Main method for console applications
            if (options.Target == CompilationTarget.ConsoleApp)
            {
                var mainMethod = CreateMainMethod();
                classMembers.Add(mainMethod);
            }
            
            // Create the LuaScript class
            var luaScriptClass = ClassDeclaration("LuaScript")
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                .AddMembers(classMembers.ToArray());
            
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
        
        private MethodDeclarationSyntax CreateMainMethod()
        {
            // Create: public static int Main(string[] args)
            var method = MethodDeclaration(
                    PredefinedType(Token(SyntaxKind.IntKeyword)),
                    "Main")
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                .AddParameterListParameters(
                    Parameter(Identifier("args"))
                        .WithType(ArrayType(PredefinedType(Token(SyntaxKind.StringKeyword)))
                            .WithRankSpecifiers(SingletonList(ArrayRankSpecifier()))));
            
            // Create: return LuaConsoleRunner.Run(Execute, args);
            var runCall = ReturnStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("LuaConsoleRunner"),
                        IdentifierName("Run")))
                    .AddArgumentListArguments(
                        Argument(IdentifierName("Execute")),
                        Argument(IdentifierName("args"))));
            
            // Create method body
            method = method.WithBody(Block(runCall));
            
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
            else if (statement.IsAssignment)
            {
                var assign = (Statement.Assignment)statement;
                var vars = FSharpListToList(assign.Item1);
                var exprs = FSharpListToList(assign.Item2);
                
                return GenerateAssignment(vars, exprs);
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
            else if (statement.IsWhile)
            {
                var whileStmt = (Statement.While)statement;
                var condition = whileStmt.Item1;
                var body = FSharpListToList(whileStmt.Item2);
                return new[] { GenerateWhile(condition, body) };
            }
            else if (statement.IsRepeat)
            {
                var repeatStmt = (Statement.Repeat)statement;
                var body = FSharpListToList(repeatStmt.Item1);
                var condition = repeatStmt.Item2;
                return new[] { GenerateRepeat(body, condition) };
            }
            else if (statement.IsNumericFor)
            {
                var forStmt = (Statement.NumericFor)statement;
                var varName = forStmt.Item1;
                var start = forStmt.Item2;
                var stop = forStmt.Item3;
                var step = forStmt.Item4;
                var body = FSharpListToList(forStmt.Item5);
                return new[] { GenerateNumericFor(varName, start, stop, step, body) };
            }
            else if (statement.IsGenericFor)
            {
                var forStmt = (Statement.GenericFor)statement;
                var vars = FSharpListToList(forStmt.Item1);
                var exprs = FSharpListToList(forStmt.Item2);
                var body = FSharpListToList(forStmt.Item3);
                return new[] { GenerateGenericFor(vars, exprs, body) };
            }
            else if (statement.IsBreak)
            {
                return new[] { BreakStatement() };
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
            
            // Special case: Single function call with multiple assignment targets
            if (exprs.Count == 1 && vars.Count > 1 && exprs[0].IsFunctionCall)
            {
                // Generate temporary variable to hold all return values
                var resultsVarName = "_results_" + tempVarCounter++;
                
                // LuaValue[] _results = functionCall();
                var resultsDecl = LocalDeclarationStatement(
                    VariableDeclaration(
                        ArrayType(IdentifierName("LuaValue"))
                            .WithRankSpecifiers(SingletonList(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(OmittedArraySizeExpression())))))
                        .AddVariables(
                            VariableDeclarator(Identifier(resultsVarName))
                                .WithInitializer(EqualsValueClause(GenerateFunctionCall(exprs[0])))));
                
                statements.Add(resultsDecl);
                
                // Assign each variable from the results array
                for (int i = 0; i < vars.Count; i++)
                {
                    var (varName, attr) = vars[i];
                    var mangledName = GetOrCreateMangledName(varName);
                    
                    // LuaValue mangledName = (_results.Length > i) ? _results[i] : LuaNil.Instance;
                    var valueExpr = ConditionalExpression(
                        BinaryExpression(
                            SyntaxKind.GreaterThanExpression,
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(resultsVarName),
                                IdentifierName("Length")),
                            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(i))),
                        ElementAccessExpression(IdentifierName(resultsVarName))
                            .AddArgumentListArguments(Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(i)))),
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("LuaNil"),
                            IdentifierName("Instance")));
                    
                    var varDecl = LocalDeclarationStatement(
                        VariableDeclaration(IdentifierName("LuaValue"))
                            .AddVariables(
                                VariableDeclarator(Identifier(SanitizeIdentifier(mangledName)))
                                    .WithInitializer(EqualsValueClause(valueExpr))));
                    
                    statements.Add(varDecl);
                    
                    // Also store in environment
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
            }
            else
            {
                // Normal case: process each variable individually
                for (int i = 0; i < vars.Count; i++)
                {
                    var (varName, attr) = vars[i];
                    var value = i < exprs.Count ? exprs[i] : Expr.CreateLiteral(FLua.Ast.Literal.CreateNil());
                    
                    // Get mangled name for the variable
                    var mangledName = GetOrCreateMangledName(varName);
                    
                    // Create local variable declaration: LuaValue mangledName = expression;
                    var varDecl = LocalDeclarationStatement(
                        VariableDeclaration(IdentifierName("LuaValue"))
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
            }
            
            return statements;
        }
        
        private IEnumerable<StatementSyntax> GenerateAssignment(IList<Expr> vars, IList<Expr> exprs)
        {
            var statements = new List<StatementSyntax>();
            
            // Special case: Single function call with multiple assignment targets
            if (exprs.Count == 1 && vars.Count > 1 && exprs[0].IsFunctionCall)
            {
                // Generate temporary variable to hold all return values
                var resultsVarName = "_results_" + tempVarCounter++;
                
                // LuaValue[] _results = functionCall();
                var resultsDecl = LocalDeclarationStatement(
                    VariableDeclaration(
                        ArrayType(IdentifierName("LuaValue"))
                            .WithRankSpecifiers(SingletonList(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(OmittedArraySizeExpression())))))
                        .AddVariables(
                            VariableDeclarator(Identifier(resultsVarName))
                                .WithInitializer(EqualsValueClause(GenerateFunctionCall(exprs[0])))));
                
                statements.Add(resultsDecl);
                
                // Assign each variable from the results array
                for (int i = 0; i < vars.Count; i++)
                {
                    var varExpr = vars[i];
                    
                    // LuaValue value = (_results.Length > i) ? _results[i] : LuaNil.Instance;
                    var valueExpr = ConditionalExpression(
                        BinaryExpression(
                            SyntaxKind.GreaterThanExpression,
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                IdentifierName(resultsVarName),
                                IdentifierName("Length")),
                            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(i))),
                        ElementAccessExpression(IdentifierName(resultsVarName))
                            .AddArgumentListArguments(Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(i)))),
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("LuaNil"),
                            IdentifierName("Instance")));
                    
                    statements.AddRange(GenerateSingleAssignment(varExpr, valueExpr));
                }
            }
            else
            {
                // Normal case: process each variable individually
                for (int i = 0; i < vars.Count; i++)
                {
                    var varExpr = vars[i];
                    var valueExpr = i < exprs.Count ? GenerateExpression(exprs[i]) : 
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("LuaNil"),
                            IdentifierName("Instance"));
                    
                    statements.AddRange(GenerateSingleAssignment(varExpr, valueExpr));
                }
            }
            
            return statements;
        }
        
        private IEnumerable<StatementSyntax> GenerateSingleAssignment(Expr varExpr, ExpressionSyntax valueExpr)
        {
            var statements = new List<StatementSyntax>();
            
            if (varExpr.IsVar)
            {
                // Simple variable assignment
                var varName = ((Expr.Var)varExpr).Item;
                    
                    // Check if it's a local variable
                    if (IsLocalVariable(varName))
                    {
                        var mangledName = ResolveVariableName(varName);
                        
                        // Direct assignment to local variable
                        var assignment = ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(SanitizeIdentifier(mangledName)),
                                valueExpr));
                        
                        statements.Add(assignment);
                        
                        // Also update in environment
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
                    else
                    {
                        // Global variable assignment
                        var setVarCall = ExpressionStatement(
                            InvocationExpression(
                                MemberAccessExpression(
                                    SyntaxKind.SimpleMemberAccessExpression,
                                    IdentifierName("env"),
                                    IdentifierName("SetVariable")))
                            .AddArgumentListArguments(
                                Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(varName))),
                                Argument(valueExpr)));
                        
                        statements.Add(setVarCall);
                    }
                }
                else if (varExpr.IsTableAccess)
                {
                    // Table indexing assignment: table[key] = value
                    var tableAccess = (Expr.TableAccess)varExpr;
                    
                    // Cast table to LuaTable
                    var tableExpr = ParenthesizedExpression(
                        CastExpression(
                            IdentifierName("LuaTable"),
                            GenerateExpression(tableAccess.Item1)));
                    
                    var setCall = ExpressionStatement(
                        InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                tableExpr,
                                IdentifierName("Set")))
                        .AddArgumentListArguments(
                            Argument(GenerateExpression(tableAccess.Item2)),
                            Argument(valueExpr)));
                    
                    statements.Add(setCall);
                }
                else
                {
                    // TODO: Handle other complex assignments
                    statements.Add(EmptyStatement().WithLeadingTrivia(
                        Comment($"// TODO: Implement complex assignment for {varExpr.GetType().Name}")));
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
            else if (expr.IsUnary)
            {
                var unary = (Expr.Unary)expr;
                return GenerateUnaryExpression(unary.Item1, unary.Item2);
            }
            else if (expr.IsTableConstructor)
            {
                var tableConstructor = (Expr.TableConstructor)expr;
                return GenerateTableConstructor(FSharpListToList(tableConstructor.Item));
            }
            else if (expr.IsTableAccess)
            {
                var tableAccess = (Expr.TableAccess)expr;
                return GenerateTableAccess(tableAccess.Item1, tableAccess.Item2);
            }
            else if (expr.IsMethodCall)
            {
                var methodCall = (Expr.MethodCall)expr;
                var allValues = GenerateMethodCall(methodCall.Item1, methodCall.Item2, FSharpListToList(methodCall.Item3));
                // For expressions, we need just the first value
                return ElementAccessExpression(allValues)
                    .AddArgumentListArguments(
                        Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))));
            }
            else if (expr.IsFunctionDef)
            {
                var funcDef = (Expr.FunctionDef)expr;
                return GenerateFunctionExpression(funcDef.Item);
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
        
        private ExpressionSyntax GenerateUnaryExpression(UnaryOp op, Expr operand)
        {
            string methodName;
            if (op.IsNegate) methodName = "Negate";
            else if (op.IsNot) methodName = "Not";
            else if (op.IsLength) methodName = "Length";
            else if (op.IsBitNot) methodName = "BitNot";
            else methodName = "Unknown";
            
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("LuaOperations"),
                    IdentifierName(methodName)))
                .AddArgumentListArguments(
                    Argument(GenerateExpression(operand)));
        }
        
        private ExpressionSyntax GenerateTableConstructor(IList<TableField> fields)
        {
            // Create new LuaTable
            var tableCreation = ObjectCreationExpression(IdentifierName("LuaTable"))
                .AddArgumentListArguments();
            
            if (fields.Count == 0)
            {
                return tableCreation;
            }
            
            // For non-empty tables, we'll generate a helper method call
            // LuaOperations.CreateTable(new object[] { key1, value1, key2, value2, ... })
            var keyValuePairs = new List<ExpressionSyntax>();
            
            int arrayIndex = 1;
            foreach (var field in fields)
            {
                if (field.IsExprField)
                {
                    // Array-style field: use integer key
                    var exprField = (TableField.ExprField)field;
                    keyValuePairs.Add(ObjectCreationExpression(IdentifierName("LuaInteger"))
                        .AddArgumentListArguments(
                            Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal((long)arrayIndex)))));
                    keyValuePairs.Add(GenerateExpression(exprField.Item));
                    arrayIndex++;
                }
                else if (field.IsNamedField)
                {
                    // Named field: use string key
                    var namedField = (TableField.NamedField)field;
                    keyValuePairs.Add(ObjectCreationExpression(IdentifierName("LuaString"))
                        .AddArgumentListArguments(
                            Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(namedField.Item1)))));
                    keyValuePairs.Add(GenerateExpression(namedField.Item2));
                }
                else if (field.IsKeyField)
                {
                    // Key field: use expression as key
                    var keyField = (TableField.KeyField)field;
                    keyValuePairs.Add(GenerateExpression(keyField.Item1));
                    keyValuePairs.Add(GenerateExpression(keyField.Item2));
                }
            }
            
            // Create the array of key-value pairs
            var arrayCreation = ArrayCreationExpression(
                ArrayType(IdentifierName("LuaValue"))
                    .WithRankSpecifiers(SingletonList(ArrayRankSpecifier())))
                .WithInitializer(InitializerExpression(
                    SyntaxKind.ArrayInitializerExpression,
                    SeparatedList(keyValuePairs)));
            
            // Call LuaOperations.CreateTable
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("LuaOperations"),
                    IdentifierName("CreateTable")))
                .AddArgumentListArguments(
                    Argument(arrayCreation));
        }
        
        private ExpressionSyntax GenerateTableAccess(Expr table, Expr key)
        {
            // Generate: ((LuaTable)table).Get(key)
            var tableExpr = ParenthesizedExpression(
                CastExpression(
                    IdentifierName("LuaTable"),
                    GenerateExpression(table)));
            
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    tableExpr,
                    IdentifierName("Get")))
                .AddArgumentListArguments(
                    Argument(GenerateExpression(key)));
        }
        
        private ExpressionSyntax GenerateFunctionExpression(FunctionDef funcDef)
        {
            // Generate an anonymous function as a LuaUserFunction
            var parameters = FSharpListToList(funcDef.Parameters);
            var body = FSharpListToList(funcDef.Body);
            var isVarArgs = funcDef.IsVararg;
            
            // Generate unique name for the anonymous function
            var funcName = $"__anon_{_anonymousFunctionCounter++}";
            
            // Create the function method
            var funcMethod = GenerateFunctionMethod(funcName, parameters, body, isVarArgs);
            _pendingMethods.Add(funcMethod);
            
            // Create LuaUserFunction instance
            return ObjectCreationExpression(IdentifierName("LuaUserFunction"))
                .AddArgumentListArguments(
                    Argument(IdentifierName(funcName)));
        }
        
        private ExpressionSyntax GenerateMethodCall(Expr obj, string methodName, IList<Expr> args)
        {
            // Method call obj:method(args) is equivalent to obj.method(obj, args)
            // We need to use LuaOperations.GetMethod to handle both tables and strings
            var objExpr = GenerateExpression(obj);
            var methodKey = ObjectCreationExpression(IdentifierName("LuaString"))
                .AddArgumentListArguments(
                    Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(methodName))));
            
            // Call LuaOperations.GetMethod(env, obj, methodName)
            var methodAccess = InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("LuaOperations"),
                    IdentifierName("GetMethod")))
                .AddArgumentListArguments(
                    Argument(IdentifierName("env")),
                    Argument(objExpr),
                    Argument(methodKey));
            
            // Cast to LuaFunction
            var funcExpr = ParenthesizedExpression(
                CastExpression(
                    IdentifierName("LuaFunction"),
                    methodAccess));
            
            // Prepare arguments with self as first argument
            var allArgs = new List<ExpressionSyntax> { GenerateExpression(obj) };
            allArgs.AddRange(args.Select(arg => GenerateExpression(arg)));
            
            var arrayCreation = ArrayCreationExpression(
                ArrayType(IdentifierName("LuaValue"))
                    .WithRankSpecifiers(SingletonList(ArrayRankSpecifier())))
                .WithInitializer(InitializerExpression(
                    SyntaxKind.ArrayInitializerExpression,
                    SeparatedList(allArgs)));
            
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    funcExpr,
                    IdentifierName("Call")))
                .AddArgumentListArguments(
                    Argument(arrayCreation));
        }
        
        private ExpressionSyntax GenerateFunctionCall(Expr expr)
        {
            // Generate function call that returns all values (used for multiple assignment)
            if (expr.IsFunctionCall)
            {
                var funcCall = (Expr.FunctionCall)expr;
                var func = funcCall.Item1;
                var args = FSharpListToList(funcCall.Item2);
                return GenerateFunctionCallRaw(func, args);
            }
            else if (expr.IsMethodCall)
            {
                var methodCall = (Expr.MethodCall)expr;
                var obj = methodCall.Item1;
                var method = methodCall.Item2;
                var args = FSharpListToList(methodCall.Item3);
                
                // Return the full method call (array of values)
                return GenerateMethodCall(obj, method, args);
            }
            else
            {
                throw new InvalidOperationException("Expression is not a function call");
            }
        }
        
        private ExpressionSyntax GenerateFunctionCallRaw(Expr func, IList<Expr> args, ExpressionSyntax funcOverride = null)
        {
            // Generate: ((LuaFunction)func).Call(new LuaValue[] { args })
            ExpressionSyntax funcExpr;
            if (funcOverride != null)
            {
                funcExpr = ParenthesizedExpression(
                    CastExpression(
                        IdentifierName("LuaFunction"),
                        funcOverride));
            }
            else
            {
                funcExpr = ParenthesizedExpression(
                    CastExpression(
                        IdentifierName("LuaFunction"),
                        GenerateExpression(func)));
            }
            
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
        
        private ExpressionSyntax GenerateFunctionCall(Expr func, IList<Expr> args)
        {
            // For expressions, we need to index [0]
            return ElementAccessExpression(GenerateFunctionCallRaw(func, args))
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
        
        private MethodDeclarationSyntax GenerateFunctionMethod(string name, IList<Parameter> parameters, IList<Statement> body, bool isVarArgs)
        {
            // Create method that matches LuaFunction delegate signature
            var method = MethodDeclaration(
                ArrayType(IdentifierName("LuaValue"))
                    .WithRankSpecifiers(SingletonList(ArrayRankSpecifier())),
                Identifier(name))
                .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.StaticKeyword))
                .AddParameterListParameters(
                    Parameter(Identifier("args"))
                        .WithType(ArrayType(IdentifierName("LuaValue"))
                            .WithRankSpecifiers(SingletonList(ArrayRankSpecifier())))
                        .AddModifiers(Token(SyntaxKind.ParamsKeyword)));
            
            // Generate method body
            EnterScope();
            var bodyStatements = new List<StatementSyntax>();
            
            // Handle parameters
            int paramIndex = 0;
            foreach (var param in parameters)
            {
                if (param.IsNamed)
                {
                    var namedParam = (Parameter.Named)param;
                    var paramName = namedParam.Item1;
                    var paramMangledName = GetOrCreateMangledName(paramName);
                    
                    // var paramMangledName = args.Length > paramIndex ? args[paramIndex] : LuaNil.Instance;
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
                                                IdentifierName("LuaNil"),
                                                IdentifierName("Instance")))))));
                    
                    bodyStatements.Add(paramDecl);
                    paramIndex++;
                }
                else if (param.IsVararg)
                {
                    // Handle varargs - create local array using range syntax
                    var varArgsDecl = LocalDeclarationStatement(
                        VariableDeclaration(IdentifierName("var"))
                            .AddVariables(
                                VariableDeclarator(Identifier("varargs__"))
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
                                                    Argument(
                                                        RangeExpression()
                                                            .WithLeftOperand(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(paramIndex))))),
                                            ArrayCreationExpression(
                                                ArrayType(IdentifierName("LuaValue"))
                                                    .WithRankSpecifiers(SingletonList(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(OmittedArraySizeExpression()))))))))));
                    
                    bodyStatements.Add(varArgsDecl);
                }
            }
            
            // Generate the function body statements
            foreach (var stmt in body)
            {
                bodyStatements.AddRange(GenerateStatement(stmt));
            }
            
            // Add default return if needed
            if (bodyStatements.Count == 0 || !IsReturnStatement(bodyStatements.Last()))
            {
                bodyStatements.Add(ReturnStatement(
                    ArrayCreationExpression(
                        ArrayType(IdentifierName("LuaValue"))
                            .WithRankSpecifiers(SingletonList(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(OmittedArraySizeExpression())))))));
            }
            
            ExitScope();
            
            method = method.WithBody(Block(bodyStatements));
            return method;
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
        
        private StatementSyntax GenerateWhile(Expr condition, IList<Statement> body)
        {
            // Generate condition.IsTruthy
            var conditionExpr = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                GenerateExpression(condition),
                IdentifierName("IsTruthy"));
            
            // Generate body block
            EnterScope();
            var bodyStatements = body.SelectMany(s => GenerateStatement(s)).ToList();
            ExitScope();
            
            return WhileStatement(conditionExpr, Block(bodyStatements));
        }
        
        private StatementSyntax GenerateRepeat(IList<Statement> body, Expr condition)
        {
            // Generate body block
            EnterScope();
            var bodyStatements = body.SelectMany(s => GenerateStatement(s)).ToList();
            
            // In Lua, repeat...until stops when condition is true
            // In C#, do...while continues when condition is true
            // So we need to negate the condition
            var notCondition = PrefixUnaryExpression(
                SyntaxKind.LogicalNotExpression,
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    GenerateExpression(condition),
                    IdentifierName("IsTruthy")));
            
            ExitScope();
            
            return DoStatement(Block(bodyStatements), notCondition);
        }
        
        private StatementSyntax GenerateNumericFor(string varName, Expr start, Expr stop, Microsoft.FSharp.Core.FSharpOption<Expr> step, IList<Statement> body)
        {
            EnterScope();
            var statements = new List<StatementSyntax>();
            
            // Get mangled name for the loop variable
            var mangledName = GetOrCreateMangledName(varName);
            
            // Generate step value (default to 1 if not provided)
            var stepExpr = OptionModule.IsSome(step) 
                ? GenerateExpression(step.Value)
                : ObjectCreationExpression(IdentifierName("LuaNumber"))
                    .AddArgumentListArguments(Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1.0))));
            
            // var start_val = start;
            var startVarName = $"{mangledName}_start";
            statements.Add(LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("var"))
                    .AddVariables(
                        VariableDeclarator(Identifier(startVarName))
                            .WithInitializer(EqualsValueClause(GenerateExpression(start))))));
            
            // var stop_val = stop;
            var stopVarName = $"{mangledName}_stop";
            statements.Add(LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("var"))
                    .AddVariables(
                        VariableDeclarator(Identifier(stopVarName))
                            .WithInitializer(EqualsValueClause(GenerateExpression(stop))))));
            
            // var step_val = step;
            var stepVarName = $"{mangledName}_step";
            statements.Add(LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("var"))
                    .AddVariables(
                        VariableDeclarator(Identifier(stepVarName))
                            .WithInitializer(EqualsValueClause(stepExpr)))));
            
            // Convert to numbers and store in non-nullable variables
            var startNumVarName = $"{mangledName}_start_num";
            statements.Add(LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("double"))
                    .AddVariables(
                        VariableDeclarator(Identifier(startNumVarName))
                            .WithInitializer(EqualsValueClause(
                                BinaryExpression(
                                    SyntaxKind.CoalesceExpression,
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("LuaTypeConversion"),
                                            IdentifierName("ToNumber")))
                                        .AddArgumentListArguments(Argument(IdentifierName(startVarName))),
                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0.0))))))));
            
            var stopNumVarName = $"{mangledName}_stop_num";
            statements.Add(LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("double"))
                    .AddVariables(
                        VariableDeclarator(Identifier(stopNumVarName))
                            .WithInitializer(EqualsValueClause(
                                BinaryExpression(
                                    SyntaxKind.CoalesceExpression,
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("LuaTypeConversion"),
                                            IdentifierName("ToNumber")))
                                        .AddArgumentListArguments(Argument(IdentifierName(stopVarName))),
                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0.0))))))));
            
            var stepNumVarName = $"{mangledName}_step_num";
            statements.Add(LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("double"))
                    .AddVariables(
                        VariableDeclarator(Identifier(stepNumVarName))
                            .WithInitializer(EqualsValueClause(
                                BinaryExpression(
                                    SyntaxKind.CoalesceExpression,
                                    InvocationExpression(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("LuaTypeConversion"),
                                            IdentifierName("ToNumber")))
                                        .AddArgumentListArguments(Argument(IdentifierName(stepVarName))),
                                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1.0))))))));
            
            // Create loop variable as LuaValue
            var loopVar = SanitizeIdentifier(mangledName);
            statements.Add(LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("LuaValue"))
                    .AddVariables(
                        VariableDeclarator(Identifier(loopVar))
                            .WithInitializer(EqualsValueClause(LiteralExpression(SyntaxKind.NullLiteralExpression))))));
            
            // for (double i_num = start_num; (step_num > 0 && i_num <= stop_num) || (step_num < 0 && i_num >= stop_num); i_num += step_num)
            var loopNumVar = $"{loopVar}_num";
            
            // step_num > 0
            var stepPositive = BinaryExpression(
                SyntaxKind.GreaterThanExpression,
                IdentifierName(stepNumVarName),
                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0.0)));
            
            // i_num <= stop_num
            var ascendingCond = BinaryExpression(
                SyntaxKind.LessThanOrEqualExpression,
                IdentifierName(loopNumVar),
                IdentifierName(stopNumVarName));
            
            // step_num < 0
            var stepNegative = BinaryExpression(
                SyntaxKind.LessThanExpression,
                IdentifierName(stepNumVarName),
                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0.0)));
            
            // i_num >= stop_num
            var descendingCond = BinaryExpression(
                SyntaxKind.GreaterThanOrEqualExpression,
                IdentifierName(loopNumVar),
                IdentifierName(stopNumVarName));
            
            // (step > 0 && i <= stop) || (step < 0 && i >= stop)
            var condition = BinaryExpression(
                SyntaxKind.LogicalOrExpression,
                ParenthesizedExpression(
                    BinaryExpression(
                        SyntaxKind.LogicalAndExpression,
                        stepPositive,
                        ascendingCond)),
                ParenthesizedExpression(
                    BinaryExpression(
                        SyntaxKind.LogicalAndExpression,
                        stepNegative,
                        descendingCond)));
            
            // i_num += step_num
            var incrementor = AssignmentExpression(
                SyntaxKind.AddAssignmentExpression,
                IdentifierName(loopNumVar),
                IdentifierName(stepNumVarName));
            
            // Generate loop body
            var loopBodyStatements = new List<StatementSyntax>();
            
            // Update loop variable: loopVar = new LuaNumber(i_num)
            loopBodyStatements.Add(ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(loopVar),
                    ObjectCreationExpression(IdentifierName("LuaNumber"))
                        .AddArgumentListArguments(Argument(IdentifierName(loopNumVar))))));
            
            // Store loop variable in environment
            loopBodyStatements.Add(ExpressionStatement(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("env"),
                        IdentifierName("SetVariable")))
                .AddArgumentListArguments(
                    Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(varName))),
                    Argument(IdentifierName(loopVar)))));
            
            // Add user body statements
            loopBodyStatements.AddRange(body.SelectMany(s => GenerateStatement(s)));
            
            var forLoop = ForStatement(Block(loopBodyStatements))
                .WithDeclaration(VariableDeclaration(IdentifierName("double"))
                    .AddVariables(VariableDeclarator(Identifier(loopNumVar))
                        .WithInitializer(EqualsValueClause(IdentifierName(startNumVarName)))))
                .WithCondition(condition)
                .AddIncrementors(incrementor);
            
            statements.Add(forLoop);
            ExitScope();
            
            return Block(statements);
        }
        
        private StatementSyntax GenerateGenericFor(IList<(string, LuaAttribute)> vars, IList<Expr> exprs, IList<Statement> body)
        {
            EnterScope();
            var statements = new List<StatementSyntax>();
            
            // Lua generic for loops work with iterators
            // for k,v in pairs(t) do ... end
            // Translates to calling the iterator function repeatedly until it returns nil
            
            // Get the iterator function (first expression)
            if (exprs.Count == 0)
            {
                throw new InvalidOperationException("Generic for loop requires at least one expression");
            }
            
            var iterExpr = exprs[0];
            
            // Call the iterator to get the actual iterator function, state, and initial value
            var iterCallName = "_iter_call";
            statements.Add(LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("var"))
                    .AddVariables(
                        VariableDeclarator(Identifier(iterCallName))
                            .WithInitializer(EqualsValueClause(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        ParenthesizedExpression(
                                            CastExpression(
                                                IdentifierName("LuaFunction"),
                                                GenerateExpression(iterExpr))),
                                        IdentifierName("Call")))
                                .AddArgumentListArguments(
                                    Argument(ArrayCreationExpression(
                                        ArrayType(IdentifierName("LuaValue"))
                                            .WithRankSpecifiers(SingletonList(ArrayRankSpecifier())))
                                        .WithInitializer(InitializerExpression(
                                            SyntaxKind.ArrayInitializerExpression)))))))));
            
            // Extract iterator function, state, and control variable
            var iterFuncName = "_iter_func";
            var iterStateName = "_iter_state";
            var iterControlName = "_iter_control";
            
            statements.Add(LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("var"))
                    .AddVariables(
                        VariableDeclarator(Identifier(iterFuncName))
                            .WithInitializer(EqualsValueClause(
                                ConditionalExpression(
                                    BinaryExpression(
                                        SyntaxKind.GreaterThanOrEqualExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName(iterCallName),
                                            IdentifierName("Length")),
                                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1))),
                                    ElementAccessExpression(IdentifierName(iterCallName))
                                        .AddArgumentListArguments(
                                            Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))),
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("LuaValue"),
                                        IdentifierName("Nil"))))))));
            
            statements.Add(LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("var"))
                    .AddVariables(
                        VariableDeclarator(Identifier(iterStateName))
                            .WithInitializer(EqualsValueClause(
                                ConditionalExpression(
                                    BinaryExpression(
                                        SyntaxKind.GreaterThanOrEqualExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName(iterCallName),
                                            IdentifierName("Length")),
                                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(2))),
                                    ElementAccessExpression(IdentifierName(iterCallName))
                                        .AddArgumentListArguments(
                                            Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(1)))),
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("LuaValue"),
                                        IdentifierName("Nil"))))))));
            
            statements.Add(LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("var"))
                    .AddVariables(
                        VariableDeclarator(Identifier(iterControlName))
                            .WithInitializer(EqualsValueClause(
                                ConditionalExpression(
                                    BinaryExpression(
                                        SyntaxKind.GreaterThanOrEqualExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName(iterCallName),
                                            IdentifierName("Length")),
                                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(3))),
                                    ElementAccessExpression(IdentifierName(iterCallName))
                                        .AddArgumentListArguments(
                                            Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(2)))),
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("LuaValue"),
                                        IdentifierName("Nil"))))))));
            
            // while (true)
            var loopBodyStatements = new List<StatementSyntax>();
            
            // Call iterator function
            var iterResultName = "_iter_result";
            loopBodyStatements.Add(LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("var"))
                    .AddVariables(
                        VariableDeclarator(Identifier(iterResultName))
                            .WithInitializer(EqualsValueClause(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        ParenthesizedExpression(
                                            CastExpression(
                                                IdentifierName("LuaFunction"),
                                                IdentifierName(iterFuncName))),
                                        IdentifierName("Call")))
                                .AddArgumentListArguments(
                                    Argument(ArrayCreationExpression(
                                        ArrayType(IdentifierName("LuaValue"))
                                            .WithRankSpecifiers(SingletonList(ArrayRankSpecifier())))
                                        .WithInitializer(InitializerExpression(
                                            SyntaxKind.ArrayInitializerExpression,
                                            SeparatedList(new ExpressionSyntax[] {
                                                IdentifierName(iterStateName),
                                                IdentifierName(iterControlName)
                                            }))))))))));
            
            // Check if first result is nil (end of iteration)
            var firstResultName = "_first_result";
            loopBodyStatements.Add(LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("var"))
                    .AddVariables(
                        VariableDeclarator(Identifier(firstResultName))
                            .WithInitializer(EqualsValueClause(
                                ConditionalExpression(
                                    BinaryExpression(
                                        SyntaxKind.GreaterThanExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName(iterResultName),
                                            IdentifierName("Length")),
                                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0))),
                                    ElementAccessExpression(IdentifierName(iterResultName))
                                        .AddArgumentListArguments(
                                            Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))),
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("LuaValue"),
                                        IdentifierName("Nil"))))))));
            
            // if (firstResult.IsNil) break;
            loopBodyStatements.Add(IfStatement(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(firstResultName),
                    IdentifierName("IsNil")),
                BreakStatement()));
            
            // Update control variable for next iteration
            loopBodyStatements.Add(ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    IdentifierName(iterControlName),
                    IdentifierName(firstResultName))));
            
            // Assign loop variables
            for (int i = 0; i < vars.Count; i++)
            {
                var (varName, attr) = vars[i];
                var mangledName = GetOrCreateMangledName(varName);
                
                // var mangledName = iterResult.Length > i ? iterResult[i] : LuaValue.Nil;
                loopBodyStatements.Add(LocalDeclarationStatement(
                    VariableDeclaration(IdentifierName("var"))
                        .AddVariables(
                            VariableDeclarator(Identifier(SanitizeIdentifier(mangledName)))
                                .WithInitializer(EqualsValueClause(
                                    ConditionalExpression(
                                        BinaryExpression(
                                            SyntaxKind.GreaterThanExpression,
                                            MemberAccessExpression(
                                                SyntaxKind.SimpleMemberAccessExpression,
                                                IdentifierName(iterResultName),
                                                IdentifierName("Length")),
                                            LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(i))),
                                        ElementAccessExpression(IdentifierName(iterResultName))
                                            .AddArgumentListArguments(
                                                Argument(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(i)))),
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("LuaValue"),
                                            IdentifierName("Nil"))))))));
                
                // env.SetVariable(varName, mangledName);
                loopBodyStatements.Add(ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("env"),
                            IdentifierName("SetVariable")))
                    .AddArgumentListArguments(
                        Argument(LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(varName))),
                        Argument(IdentifierName(SanitizeIdentifier(mangledName))))));
            }
            
            // Add user body statements
            loopBodyStatements.AddRange(body.SelectMany(s => GenerateStatement(s)));
            
            statements.Add(WhileStatement(
                LiteralExpression(SyntaxKind.TrueLiteralExpression),
                Block(loopBodyStatements)));
            
            ExitScope();
            
            return Block(statements);
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
        
        private bool IsReturnStatement(StatementSyntax statement)
        {
            return statement is ReturnStatementSyntax;
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