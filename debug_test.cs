using System;
using FLua.Parser;
using FLua.Interpreter;

class Program 
{
    static void Main() 
    {
        try 
        {
            // Test just the parser first
            Console.WriteLine("Testing parser...");
            var expr = ParserHelper.ParseExpression("9+8");
            Console.WriteLine($"Parsed expression: {expr}");
            
            // Test the interpreter
            Console.WriteLine("Testing interpreter...");
            var interpreter = new LuaInterpreter();
            var result = interpreter.EvaluateExpression("9+8");
            Console.WriteLine($"Result: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}