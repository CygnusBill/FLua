using System;
using System.Collections.Generic;
using System.Linq;
using FLua.Ast;
using FLua.Parser;
using Microsoft.FSharp.Collections;

namespace FLua.CSharpTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("FLua AST Test from C#");
            Console.WriteLine("=====================\n");

            // Test parsing an expression
            TestExpression("1 + 2");

            // Test parsing a function
            TestFunction("function add(a, b) return a + b end");

            // Test parsing a program
            TestProgram("local x = 10; print(x)");

            // Test a more complex program
            TestComplexProgram();
        }

        static void TestExpression(string expression)
        {
            Console.WriteLine($"Expression: {expression}");
            
            try
            {
                var expr = ParserHelper.ParseExpression(expression);
                if (expr is Expr.Binary binary)
                {
                    Console.WriteLine($"AST Type: Binary");
                    Console.WriteLine($"Operator: {binary.Item2}");
                }
                else
                {
                    Console.WriteLine($"AST Type: {expr.GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Parse error: {ex.Message}");
            }
            
            Console.WriteLine();
        }

        static void TestFunction(string code)
        {
            Console.WriteLine($"Function: {code}");
            
            try
            {
                var statements = ParserHelper.ParseString(code);
                
                // Debug what we got back
                Console.WriteLine($"Parsed {statements.Length} statements");
                
                for (int i = 0; i < statements.Length; i++)
                {
                    var stmt = statements[i];
                    Console.WriteLine($"Statement {i + 1} type: {GetStatementType(stmt)}");
                    
                    if (stmt.IsFunctionDef)
                    {
                        var funcDef = (Statement.FunctionDef)stmt;
                        var path = funcDef.Item1;
                        var func = funcDef.Item2;
                        
                        Console.WriteLine($"Name: {path[0]}");
                        Console.WriteLine($"Parameter count: {func.Parameters.Length}");
                        Console.WriteLine($"Body statement count: {func.Body.Length}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Parse error: {ex.Message}");
            }
            
            Console.WriteLine();
        }

        static void TestProgram(string code)
        {
            Console.WriteLine($"Program: {code}");
            
            try
            {
                var statements = ParserHelper.ParseString(code);
                Console.WriteLine($"Statement count: {statements.Length}");
                
                for (int i = 0; i < statements.Length; i++)
                {
                    Console.WriteLine($"Statement {i + 1}: {GetStatementType(statements[i])}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Parse error: {ex.Message}");
            }
            
            Console.WriteLine();
        }

        static void TestComplexProgram()
        {
            Console.WriteLine("Parsing Lua Code Test");
            Console.WriteLine("====================");
            
            // Fix the multi-line string to use proper semicolons between statements
            string luaCode = @"
local x = 10;
local y = 20;

function add(a, b)
    return a + b;
end;

local result = add(x, y);

print('Result: ' .. result);
print('x + y = ' .. result);
print('Done!');
";
            
            try
            {
                var statements = ParserHelper.ParseString(luaCode);
                Console.WriteLine("Successfully parsed Lua code into AST!");
                Console.WriteLine($"AST contains {statements.Length} statements");
                
                // Walk the AST to find variables and function calls
                int localVarCount = 0;
                int funcDefCount = 0;
                int funcCallCount = 0;
                
                foreach (var stmt in statements)
                {
                    if (stmt.IsLocalAssignment)
                    {
                        localVarCount++;
                        var localAssign = (Statement.LocalAssignment)stmt;
                        var vars = localAssign.Item1;
                        foreach (var (name, _) in vars)
                        {
                            Console.WriteLine($"Found local variable: {name}");
                        }
                    }
                    else if (stmt.IsFunctionDef)
                    {
                        funcDefCount++;
                        var funcDef = (Statement.FunctionDef)stmt;
                        Console.WriteLine($"Found function: {funcDef.Item1[0]}");
                    }
                    else if (stmt.IsFunctionCall)
                    {
                        funcCallCount++;
                        var funcCall = (Statement.FunctionCall)stmt;
                        if (funcCall.Item is Expr.FunctionCall call && call.Item1 is Expr.Var varExpr)
                        {
                            Console.WriteLine($"Found function call: {varExpr.Item}");
                        }
                    }
                }
                
                Console.WriteLine("\nAST Validation Summary:");
                Console.WriteLine($"Total statements: {statements.Length}");
                Console.WriteLine($"Local variable declarations: {localVarCount}");
                Console.WriteLine($"Function definitions: {funcDefCount}");
                Console.WriteLine($"Function calls: {funcCallCount}");
                
                if (localVarCount >= 3 && funcCallCount >= 3)
                {
                    Console.WriteLine("\nAST validation successful! ✅");
                    Console.WriteLine("The AST contains the expected elements.");
                }
                else
                {
                    Console.WriteLine("\nAST validation failed! ❌");
                    Console.WriteLine("The AST does not contain the expected elements.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Parse error: {ex.Message}");
            }
        }

        static string GetStatementType(Statement stmt)
        {
            // Use type testing instead of pattern matching for F# discriminated unions
            if (stmt.IsEmpty) return "Empty";
            if (stmt.IsAssignment) return "Assignment";
            if (stmt.IsLocalAssignment) return "LocalAssignment";
            if (stmt.IsFunctionCall) return "FunctionCall";
            if (stmt.IsReturn) return "Return";
            if (stmt.IsBreak) return "Break";
            if (stmt.IsLabel) return "Label";
            if (stmt.IsGoto) return "Goto";
            if (stmt.IsDoBlock) return "DoBlock";
            if (stmt.IsWhile) return "While";
            if (stmt.IsRepeat) return "Repeat";
            if (stmt.IsIf) return "If";
            if (stmt.IsNumericFor) return "NumericFor";
            if (stmt.IsGenericFor) return "GenericFor";
            if (stmt.IsFunctionDef) return "FunctionDef";
            if (stmt.IsLocalFunctionDef) return "LocalFunctionDef";
            return stmt.GetType().Name;
        }
    }
}
