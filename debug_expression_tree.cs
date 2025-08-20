using System;
using FLua.Hosting;
using FLua.Runtime;

class Program 
{
    static void Main() 
    {
        try 
        {
            var host = new LuaHost();
            
            // Test local variables
            Console.WriteLine("Testing local variables...");
            string localVarCode = @"
                local x = 10
                local y = 20
                return x + y
            ";
            
            // Try compiling without specifying return type to see raw result
            var expr = host.CompileToExpression<LuaValue>(localVarCode);
            var compiled = expr.Compile();
            var result = compiled();
            Console.WriteLine($"Local variables result: Type={result.Type}, Value={result}");
            
            // Test table operations  
            Console.WriteLine("Testing table operations...");
            string tableCode = @"
                local t = {a = 10, b = 20}
                return t.a + t.b
            ";
            
            var tableExpr = host.CompileToExpression<LuaValue>(tableCode);
            var tableCompiled = tableExpr.Compile();
            var tableResult = tableCompiled();
            Console.WriteLine($"Table operations result: Type={tableResult.Type}, Value={tableResult}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}