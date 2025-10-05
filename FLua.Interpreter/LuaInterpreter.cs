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
    /// C# implementation of a Lua interpreter that walks the F# AST using visitor pattern
    /// </summary>
    public class LuaInterpreter
    {
        private LuaEnvironment _environment;
        private ExpressionEvaluator _expressionEvaluator;
        private StatementExecutor _statementExecutor;

        public LuaInterpreter()
        {
            _environment = LuaEnvironment.CreateStandardEnvironment();
            _expressionEvaluator = new ExpressionEvaluator(_environment);
            _statementExecutor = new StatementExecutor(_environment);
            
            // Configure the package library to use this interpreter for loading files
            LuaPackageLib.LuaFileLoader = LoadLuaModule;
            
            // Register the load function implementation
            InterpreterLoadFunction.Register();
        }

        /// <summary>
        /// Loads a Lua module from source code
        /// </summary>
        /// <param name="code">The Lua source code</param>
        /// <param name="moduleName">The name of the module (for error reporting)</param>
        /// <returns>The result of executing the module</returns>
        private LuaValue[] LoadLuaModule(string code, string moduleName)
        {
            try
            {
                // Parse the Lua code
                var ast = ParserHelper.ParseString(code);
                
                // Create a new environment for the module
                var moduleEnv = _environment.CreateChild();
                
                // Execute the module in its own environment
                var previousEnv = _environment;
                _environment = moduleEnv;
                
                try
                {
                    var result = ExecuteStatementsWithResult(ast);
                    return result.ReturnValues ?? [LuaValue.Nil];
                }
                finally
                {
                    _environment = previousEnv;
                }
            }
            catch (LuaRuntimeException ex)
            {
                // Enhance error message with module context
                throw new LuaRuntimeException($"Error loading module '{moduleName}': {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new LuaRuntimeException($"Error loading module '{moduleName}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets the current Lua environment
        /// </summary>
        public LuaEnvironment Environment => _environment;

        /// <summary>
        /// Executes a string of Lua code
        /// </summary>
        public LuaValue[] ExecuteString(string code)
        {
            var ast = ParserHelper.ParseString(code);
            var result = ExecuteStatementsWithResult(ast);
            return result.ReturnValues ?? []; // Return empty array, not [Nil]
        }

        /// <summary>
        /// Executes Lua code from string (legacy method name)
        /// </summary>
        public LuaValue[] ExecuteCode(string code)
        {
            return ExecuteString(code);
        }

        /// <summary>
        /// Executes a list of statements directly (legacy method)
        /// </summary>
        public LuaValue[] ExecuteStatements(FSharpList<Statement> statements)
        {
            var result = ExecuteStatementsWithResult(statements);
            return result.ReturnValues ?? [LuaValue.Nil];
        }

        /// <summary>
        /// Evaluates a Lua expression from string
        /// </summary>
        public LuaValue EvaluateExpression(string expressionText)
        {
            var expr = ParserHelper.ParseExpression(expressionText);
            return EvaluateExpr(expr);
        }

        /// <summary>
        /// Executes an F# AST block with result handling
        /// </summary>
        public StatementResult ExecuteStatementsWithResult(FSharpList<Statement> statements)
        {
            StatementResult result = new StatementResult();

            foreach (var stmt in statements)
            {
                var stmtResult = ExecuteStatement(stmt);

                if (stmtResult.ReturnValues != null)
                {
                    result.ReturnValues = stmtResult.ReturnValues;
                    return result;
                }

                if (stmtResult.Break)
                {
                    result.Break = true;
                    return result;
                }

                if (stmtResult.GotoLabel != null)
                {
                    result.GotoLabel = stmtResult.GotoLabel;
                    return result;
                }
            }

            return result;
        }

        /// <summary>
        /// Executes a single Lua statement using visitor pattern
        /// </summary>
        private StatementResult ExecuteStatement(Statement stmt)
        {
            return _statementExecutor.Execute(stmt);
        }

        /// <summary>
        /// Evaluates a Lua expression using visitor pattern
        /// </summary>
        private LuaValue EvaluateExpr(Expr expr)
        {
            var results = _expressionEvaluator.Evaluate(expr);
            return results.Length > 0 ? results[0] : LuaValue.Nil;
        }

        /// <summary>
        /// Evaluates a Lua expression and returns all values it produces using visitor pattern
        /// </summary>
        private LuaValue[] EvaluateExprWithMultipleReturns(Expr expr)
        {
            return _expressionEvaluator.Evaluate(expr);
        }
    }

    /// <summary>
    /// Represents the result of executing a statement
    /// </summary>
    public class StatementResult
    {
        public LuaValue[]? ReturnValues { get; set; }
        public bool Break { get; set; }
        public string? GotoLabel { get; set; }
    }
}