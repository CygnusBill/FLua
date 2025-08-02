using FLua.Hosting;
using FLua.Hosting.Security;

// Example: Lambda Compilation
// This example shows how to compile Lua code into .NET delegates for high-performance execution.
// Compiled lambdas are much faster than interpreted scripts for repeated execution.

var host = new LuaHost();

// Example 1: Simple mathematical function
Console.WriteLine("=== Example 1: Mathematical Function ===");

var distanceFormula = @"
    local x1, y1, x2, y2 = ...
    local dx = x2 - x1
    local dy = y2 - y1
    return math.sqrt(dx * dx + dy * dy)
";

// Compile to a typed delegate
var distanceFunc = host.CompileToFunction<double>(distanceFormula, new LuaHostOptions
{
    TrustLevel = TrustLevel.Trusted // Required for compilation
});

// Now we can call it like a regular C# function
Console.WriteLine($"Distance from (0,0) to (3,4): {distanceFunc()}");

// Example 2: String manipulation function
Console.WriteLine("\n=== Example 2: String Processing ===");

var titleCaseScript = @"
    return function(str)
        return str:gsub('(%a)([%w]*)', function(first, rest)
            return first:upper() .. rest:lower()
        end)
    end
";

// Compile to a delegate that returns a function
var createTitleCase = host.CompileToFunction<object>(titleCaseScript);
Console.WriteLine("Compiled string processor created");

// Example 3: Complex calculation with caching
Console.WriteLine("\n=== Example 3: Fibonacci with Memoization ===");

var memoizedFibScript = @"
    local cache = {}
    
    return function(n)
        if cache[n] then
            return cache[n]
        end
        
        local result
        if n <= 1 then
            result = n
        else
            -- Recursive calls will also use the cache
            result = fib(n - 1) + fib(n - 2)
        end
        
        cache[n] = result
        return result
    end
";

// Performance comparison
Console.WriteLine("\nPerformance Comparison:");

// Interpreted version
var interpretedScript = @"
    function fib(n)
        if n <= 1 then return n end
        return fib(n - 1) + fib(n - 2)
    end
    return fib(30)
";

var sw = System.Diagnostics.Stopwatch.StartNew();
var interpretedResult = host.Execute(interpretedScript);
sw.Stop();
var interpretedTime = sw.ElapsedMilliseconds;

Console.WriteLine($"Interpreted fibonacci(30): {interpretedResult.AsDouble()} in {interpretedTime}ms");

// Compiled version
var compiledFibScript = @"
    local n = 30
    function fib(x)
        if x <= 1 then return x end
        return fib(x - 1) + fib(x - 2)
    end
    return fib(n)
";

sw.Restart();
var compiledFunc = host.CompileToFunction<double>(compiledFibScript);
var compiledResult = compiledFunc();
sw.Stop();
var compiledTime = sw.ElapsedMilliseconds;

Console.WriteLine($"Compiled fibonacci(30): {compiledResult} in {compiledTime}ms");
Console.WriteLine($"Speedup: {(double)interpretedTime / compiledTime:F2}x");

// Example 4: Error handling in compiled code
Console.WriteLine("\n=== Example 4: Error Handling ===");

try
{
    var errorScript = @"
        error('This is a runtime error in compiled code')
    ";
    
    var errorFunc = host.CompileToFunction<object>(errorScript);
    errorFunc(); // This will throw
}
catch (Exception ex)
{
    Console.WriteLine($"Caught error from compiled function: {ex.Message}");
}