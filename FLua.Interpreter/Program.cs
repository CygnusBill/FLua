using System;
using System.Collections.Generic;
using System.Linq;
using FLua.Parser;

namespace FLua.Interpreter
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--test")
            {
                RunTests();
                return;
            }
            
            Console.WriteLine("FLua Interpreter Test Program");
            Console.WriteLine("Use the REPL project for interactive use.");
            
            // Simple test
            var interpreter = new LuaInterpreter();
            var result = interpreter.EvaluateExpression("1 + 2 * 3");
            Console.WriteLine($"1 + 2 * 3 = {result}");
        }
        
        static void RunTests()
        {
            Console.WriteLine("Running tests...");
            
            var interpreter = new LuaInterpreter();
            
            // Test basic expressions
            TestExpression(interpreter, "1 + 2", "3");
            TestExpression(interpreter, "2 * 3", "6");
            TestExpression(interpreter, "5 - 2", "3");
            TestExpression(interpreter, "6 / 2", "3");
            TestExpression(interpreter, "2 ^ 3", "8");
            TestExpression(interpreter, "10 % 3", "1");
            TestExpression(interpreter, "10 // 3", "3");
            
            // Test local variables
            TestCode(interpreter, "local x = 10; return x", "10");
            
            // Test tables
            TestCode(interpreter, "local t = {10, 20, 30}; return t[2]", "20");
            
            Console.WriteLine("All tests completed.");
        }
        
        static void TestExpression(LuaInterpreter interpreter, string expr, string expected)
        {
            try
            {
                var result = interpreter.EvaluateExpression(expr);
                Console.WriteLine($"Expression: {expr} = {result}");
                
                if (result.ToString() != expected)
                {
                    Console.WriteLine($"  ERROR: Expected {expected}, got {result}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ERROR evaluating {expr}: {ex.Message}");
            }
        }
        
        static void TestCode(LuaInterpreter interpreter, string code, string expected)
        {
            try
            {
                var results = interpreter.ExecuteCode(code);
                string result = results.Length > 0 ? results[0].ToString() : "nil";
                Console.WriteLine($"Code: {code} => {result}");
                
                if (result != expected)
                {
                    Console.WriteLine($"  ERROR: Expected {expected}, got {result}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ERROR executing {code}: {ex.Message}");
            }
        }
    }
} 