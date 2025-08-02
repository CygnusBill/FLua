using FLua.Hosting;
using FLua.Hosting.Security;

// Example: Simple Script Execution
// This example demonstrates basic Lua script execution using the FLua hosting infrastructure.
// The script runs in a sandboxed environment with restricted access to dangerous functions.

var host = new LuaHost();

// Simple Lua script that performs calculations
var luaScript = @"
    -- Calculate factorial
    function factorial(n)
        if n <= 1 then
            return 1
        end
        return n * factorial(n - 1)
    end
    
    -- Calculate fibonacci
    function fibonacci(n)
        if n <= 1 then
            return n
        end
        return fibonacci(n - 1) + fibonacci(n - 2)
    end
    
    -- Perform some calculations
    local fact5 = factorial(5)
    local fib10 = fibonacci(10)
    
    print('Factorial of 5: ' .. fact5)
    print('Fibonacci of 10: ' .. fib10)
    
    -- Return the sum as the script result
    return fact5 + fib10
";

// Execute with sandbox trust level (safe but restricted)
var options = new LuaHostOptions
{
    TrustLevel = TrustLevel.Sandbox,
    ExecutionTimeout = TimeSpan.FromSeconds(5) // Prevent infinite loops
};

try
{
    Console.WriteLine("Executing Lua script with Sandbox trust level...\n");
    
    var result = host.Execute(luaScript, options);
    
    Console.WriteLine($"\nScript returned: {result.AsDouble()}");
    Console.WriteLine("\nExecution completed successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

// Demonstrate what happens with restricted operations
Console.WriteLine("\n--- Testing Restricted Operations ---");

var restrictedScript = @"
    -- This will fail in sandbox mode
    local file = io.open('test.txt', 'w')
    return 'File opened'
";

try
{
    var result = host.Execute(restrictedScript, options);
    Console.WriteLine("Restricted operation succeeded (unexpected!)");
}
catch (Exception ex)
{
    Console.WriteLine($"Restricted operation blocked (expected): {ex.Message}");
}