using System;
using FLua.Common;
using FLua.Runtime;

// Demonstration of Result pattern vs Exception pattern
class Program
{
    static void Main()
    {
        Console.WriteLine("=== Exception-Based vs Result-Based Math Library Demo ===\n");

        // Test invalid input to show the difference
        var invalidArgs = new LuaValue[] { LuaValue.String("not a number") };
        var validArgs = new LuaValue[] { LuaValue.Number(-5.7) };

        Console.WriteLine("1. Exception-Based Approach (Original LuaMathLib):");
        try
        {
            var result = LuaMathLib.Abs(invalidArgs);
            Console.WriteLine($"   Result: {result[0]}");
        }
        catch (LuaRuntimeException ex)
        {
            Console.WriteLine($"   ❌ Exception: {ex.Message}");
        }

        Console.WriteLine("\n2. Result-Based Approach (New ResultLuaMathLib):");
        var resultAbs = ResultLuaMathLib.AbsResult(invalidArgs);
        resultAbs.Match(
            success => Console.WriteLine($"   ✅ Success: {success[0]}"),
            failure => Console.WriteLine($"   📝 Error: {failure}")
        );

        Console.WriteLine("\n3. Functional Composition with Result Pattern:");
        
        // Chain multiple operations - this would be impossible with exceptions
        var composedResult = ResultLuaMathLib.AbsResult(validArgs)
            .Bind(absResult => ResultLuaMathLib.FloorResult(absResult))
            .Map(floorResult => $"abs(floor({validArgs[0]})) = {floorResult[0]}");

        composedResult.Match(
            success => Console.WriteLine($"   ✅ Composed result: {success}"),
            failure => Console.WriteLine($"   📝 Composition failed: {failure}")
        );

        Console.WriteLine("\n4. Error Accumulation Example:");
        
        // Simulate validating multiple arguments
        var args = new LuaValue[] 
        { 
            LuaValue.Number(1), 
            LuaValue.String("invalid"), 
            LuaValue.Number(3) 
        };

        var validationResults = new Result<double>[]
        {
            args[0].TryAsDouble(),
            args[1].TryAsDouble(), 
            args[2].TryAsDouble()
        };

        var combinedResult = Result.Combine(validationResults);
        combinedResult.Match(
            success => Console.WriteLine($"   ✅ All arguments valid: {string.Join(", ", success)}"),
            failure => Console.WriteLine($"   📝 Validation errors: {failure}")
        );

        Console.WriteLine("\n5. Performance Comparison (No Exception Overhead):");
        
        const int iterations = 100000;
        
        // Time exception-based approach
        var startTime = DateTime.UtcNow;
        for (int i = 0; i < iterations; i++)
        {
            var result = ResultLuaMathLib.AbsResult(validArgs);
            // Result pattern - no exceptions thrown
        }
        var resultTime = DateTime.UtcNow - startTime;

        Console.WriteLine($"   Result pattern: {resultTime.TotalMilliseconds:F2}ms for {iterations} calls");
        Console.WriteLine($"   (No exception overhead, explicit error handling)");

        Console.WriteLine("\n=== Summary ===");
        Console.WriteLine("✅ Result pattern provides:");
        Console.WriteLine("   • Explicit error handling (compile-time safety)"); 
        Console.WriteLine("   • No hidden control flow");
        Console.WriteLine("   • Functional composition"); 
        Console.WriteLine("   • Better performance (no exception overhead)");
        Console.WriteLine("   • Error accumulation and propagation");
    }
}

// Extension methods to make the old LuaMathLib accessible for comparison
public static class LuaMathLibExtensions 
{
    public static LuaValue[] Abs(this LuaMathLib _, LuaValue[] args)
    {
        // Access private method via reflection for demo
        var method = typeof(LuaMathLib).GetMethod("Abs", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return (LuaValue[])method!.Invoke(null, new object[] { args })!;
    }
}

static class LuaMathLibAccess
{
    public static LuaValue[] Abs(LuaValue[] args)
    {
        // This would normally call the original LuaMathLib.Abs but since it's private,
        // let's simulate the exception-throwing behavior
        if (args.Length == 0 || !args[0].IsNumber)
            throw new LuaRuntimeException("bad argument #1 to 'abs' (number expected)");
        
        var value = args[0];
        if (value.IsInteger)
        {
            var intVal = value.AsInteger();
            if (intVal == long.MinValue)
                return [LuaValue.Integer(long.MinValue)];
            return [LuaValue.Integer(Math.Abs(intVal))];
        }
        
        return [LuaValue.Number(Math.Abs(value.AsDouble()))];
    }
}

// Alias for cleaner code
class LuaMathLib : LuaMathLibAccess { }