using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Compiler;
using FLua.Runtime;
using FLua.Ast;
using FLua.Common.Diagnostics;
using System;
using System.Linq.Expressions;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace FLua.Compiler.Tests
{
    /// <summary>
    /// Tests for MinimalExpressionTreeGenerator to ensure it handles basic arithmetic operations correctly.
    /// This addresses the testing gap where REPL arithmetic operations were failing but not caught by tests.
    /// </summary>
    [TestClass]
    public class MinimalExpressionTreeGeneratorTests
    {
        private MinimalExpressionTreeGenerator _generator = null!;
        private IDiagnosticCollector _diagnostics = null!;
        private LuaEnvironment _environment = null!;

        [TestInitialize]
        public void Setup()
        {
            _diagnostics = new DiagnosticCollector();
            _generator = new MinimalExpressionTreeGenerator(_diagnostics);
            _environment = new LuaEnvironment();
        }

        [TestMethod]
        public void Generate_SimpleArithmetic_CreatesCorrectExpression()
        {
            // Create AST for "9 + 8"
            var left = Expr.NewLiteral(Literal.NewInteger(9));
            var right = Expr.NewLiteral(Literal.NewInteger(8));
            var binary = Expr.NewBinary(left, BinaryOp.Add, right);
            var returnStmt = Statement.NewReturn(FSharpOption<FSharpList<Expr>>.Some(ListModule.OfArray(new[] { binary })));
            var statements = ListModule.OfArray(new[] { returnStmt });

            var lambda = _generator.Generate(statements.ToArray());
            var compiled = lambda.Compile();
            var result = compiled(_environment);

            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(17.0, result[0].AsDouble());
        }

        [TestMethod]
        public void Generate_MultipleArithmeticOperations_AllWork()
        {
            // Test subtraction: 10 - 3
            var subLeft = Expr.NewLiteral(Literal.NewInteger(10));
            var subRight = Expr.NewLiteral(Literal.NewInteger(3));
            var subBinary = Expr.NewBinary(subLeft, BinaryOp.Subtract, subRight);
            var subReturn = Statement.NewReturn(FSharpOption<FSharpList<Expr>>.Some(ListModule.OfArray(new[] { subBinary })));
            
            var subLambda = _generator.Generate(ListModule.OfArray(new[] { subReturn }).ToArray());
            var subCompiled = subLambda.Compile();
            var subResult = subCompiled(_environment);
            
            Assert.AreEqual(7.0, subResult[0].AsDouble(), "Subtraction failed");

            // Reset generator for next test
            _generator = new MinimalExpressionTreeGenerator(_diagnostics);

            // Test multiplication: 12 * 2
            var mulLeft = Expr.NewLiteral(Literal.NewInteger(12));
            var mulRight = Expr.NewLiteral(Literal.NewInteger(2));
            var mulBinary = Expr.NewBinary(mulLeft, BinaryOp.Multiply, mulRight);
            var mulReturn = Statement.NewReturn(FSharpOption<FSharpList<Expr>>.Some(ListModule.OfArray(new[] { mulBinary })));
            
            var mulLambda = _generator.Generate(ListModule.OfArray(new[] { mulReturn }).ToArray());
            var mulCompiled = mulLambda.Compile();
            var mulResult = mulCompiled(_environment);
            
            Assert.AreEqual(24.0, mulResult[0].AsDouble(), "Multiplication failed");
        }

        [TestMethod]
        public void Generate_LocalVariables_WorkCorrectly()
        {
            // Create AST for "local x = 9; local y = 5; return x + y"
            var xInit = Expr.NewLiteral(Literal.NewInteger(9));
            var yInit = Expr.NewLiteral(Literal.NewInteger(5));
            
            var xAssign = Statement.NewLocalAssignment(
                ListModule.OfArray(new[] { Tuple.Create("x", FLua.Ast.Attribute.NoAttribute) }),
                FSharpOption<FSharpList<Expr>>.Some(ListModule.OfArray(new[] { xInit }))
            );
            
            var yAssign = Statement.NewLocalAssignment(
                ListModule.OfArray(new[] { Tuple.Create("y", FLua.Ast.Attribute.NoAttribute) }),
                FSharpOption<FSharpList<Expr>>.Some(ListModule.OfArray(new[] { yInit }))
            );
            
            var xVar = Expr.NewVar("x");
            var yVar = Expr.NewVar("y");
            var addExpr = Expr.NewBinary(xVar, BinaryOp.Add, yVar);
            var returnStmt = Statement.NewReturn(FSharpOption<FSharpList<Expr>>.Some(ListModule.OfArray(new[] { addExpr })));
            
            var statements = ListModule.OfArray(new[] { xAssign, yAssign, returnStmt });

            var lambda = _generator.Generate(statements.ToArray());
            var compiled = lambda.Compile();
            var result = compiled(_environment);

            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(14.0, result[0].AsDouble());
        }

        [TestMethod]
        public void Generate_StringConcatenation_WorksCorrectly()
        {
            // Create AST for "'Hello' .. ' World'"
            var left = Expr.NewLiteral(Literal.NewString("Hello"));
            var right = Expr.NewLiteral(Literal.NewString(" World"));
            var concat = Expr.NewBinary(left, BinaryOp.Concat, right);
            var returnStmt = Statement.NewReturn(FSharpOption<FSharpList<Expr>>.Some(ListModule.OfArray(new[] { concat })));

            var lambda = _generator.Generate(ListModule.OfArray(new[] { returnStmt }).ToArray());
            var compiled = lambda.Compile();
            var result = compiled(_environment);

            Assert.AreEqual(1, result.Length);
            Assert.AreEqual("Hello World", result[0].AsString());
        }

        [TestMethod]
        public void Generate_BooleanOperations_WorkCorrectly()
        {
            // Test equality: 5 == 5
            var left = Expr.NewLiteral(Literal.NewInteger(5));
            var right = Expr.NewLiteral(Literal.NewInteger(5));
            var equal = Expr.NewBinary(left, BinaryOp.Equal, right);
            var returnStmt = Statement.NewReturn(FSharpOption<FSharpList<Expr>>.Some(ListModule.OfArray(new[] { equal })));

            var lambda = _generator.Generate(ListModule.OfArray(new[] { returnStmt }).ToArray());
            var compiled = lambda.Compile();
            var result = compiled(_environment);

            Assert.AreEqual(1, result.Length);
            Assert.IsTrue(result[0].AsBoolean());
        }

        [TestMethod]
        public void Generate_EnvironmentVariableAccess_WorksCorrectly()
        {
            // Set up environment variable
            _environment.SetVariable("test_var", LuaValue.Number(42));
            
            // Create AST for accessing "test_var"
            var varExpr = Expr.NewVar("test_var");
            var returnStmt = Statement.NewReturn(FSharpOption<FSharpList<Expr>>.Some(ListModule.OfArray(new[] { varExpr })));

            var lambda = _generator.Generate(ListModule.OfArray(new[] { returnStmt }).ToArray());
            var compiled = lambda.Compile();
            var result = compiled(_environment);

            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(42.0, result[0].AsDouble());
        }
    }
}