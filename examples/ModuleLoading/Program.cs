using FLua.Hosting;
using FLua.Hosting.Security;

// Example: Module Loading with Compilation
// This example shows how FLua can load and compile Lua modules with automatic caching.
// Modules are compiled to .NET assemblies for optimal performance when trust level permits.

// Setup module directory
var moduleDir = Path.Combine(Directory.GetCurrentDirectory(), "modules");
Directory.CreateDirectory(moduleDir);

// Create some example modules
CreateExampleModules(moduleDir);

// Create host with module resolver
var moduleResolver = new FileSystemModuleResolver(new[] { moduleDir });
var host = new LuaHost();

Console.WriteLine("=== FLua Module Loading Example ===\n");

// Example 1: Basic module loading
Console.WriteLine("Example 1: Loading a Simple Module");
Console.WriteLine("----------------------------------");

var basicScript = @"
    local utils = require('utils')
    
    print('Using utils module:')
    print('5 + 3 = ' .. utils.add(5, 3))
    print('5 * 3 = ' .. utils.multiply(5, 3))
    
    return utils.add(10, 20)
";

var options = new LuaHostOptions
{
    TrustLevel = TrustLevel.Trusted, // Enables module compilation
    ModuleResolver = moduleResolver
};

var result = host.Execute(basicScript, options);
Console.WriteLine($"Result: {result.AsDouble()}\n");

// Example 2: Module with dependencies
Console.WriteLine("Example 2: Modules with Dependencies");
Console.WriteLine("------------------------------------");

var dependencyScript = @"
    local calculator = require('calculator')
    
    print('Calculator operations:')
    print('Distance from (0,0) to (3,4): ' .. calculator.distance(0, 0, 3, 4))
    print('Average of 10, 20, 30: ' .. calculator.average({10, 20, 30}))
    
    return calculator.factorial(5)
";

result = host.Execute(dependencyScript, options);
Console.WriteLine($"Factorial(5) = {result.AsDouble()}\n");

// Example 3: Module caching demonstration
Console.WriteLine("Example 3: Module Caching");
Console.WriteLine("-------------------------");

var sw = System.Diagnostics.Stopwatch.StartNew();

// First load - will compile
var cacheTest1 = @"
    local heavy = require('heavy_computation')
    return heavy.compute(100)
";

sw.Restart();
result = host.Execute(cacheTest1, options);
sw.Stop();
var firstLoadTime = sw.ElapsedMilliseconds;
Console.WriteLine($"First load (with compilation): {firstLoadTime}ms, Result: {result.AsDouble()}");

// Second load - should use cache
sw.Restart();
result = host.Execute(cacheTest1, options);
sw.Stop();
var secondLoadTime = sw.ElapsedMilliseconds;
Console.WriteLine($"Second load (from cache): {secondLoadTime}ms, Result: {result.AsDouble()}");
Console.WriteLine($"Cache speedup: {(double)firstLoadTime / secondLoadTime:F2}x\n");

// Example 4: Module returning different types
Console.WriteLine("Example 4: Module Return Types");
Console.WriteLine("------------------------------");

var returnTypesScript = @"
    -- Modules can return tables (most common)
    local config = require('config')
    print('App name: ' .. config.appName)
    print('Version: ' .. config.version)
    
    -- Modules can return functions
    local greeter = require('greeter')
    print(greeter('World'))
    
    -- Modules can return any value
    local pi = require('constants')
    print('Pi value: ' .. pi)
    
    return 'All modules loaded successfully'
";

result = host.Execute(returnTypesScript, options);
Console.WriteLine($"\nResult: {result.AsString()}\n");

// Example 5: Trust level affects compilation
Console.WriteLine("Example 5: Trust Levels and Compilation");
Console.WriteLine("---------------------------------------");

var sandboxOptions = new LuaHostOptions
{
    TrustLevel = TrustLevel.Sandbox, // Too low for compilation
    ModuleResolver = moduleResolver
};

Console.WriteLine("With Sandbox trust level (interpreted):");
sw.Restart();
result = host.Execute("return require('utils').add(1, 2)", sandboxOptions);
sw.Stop();
Console.WriteLine($"Time: {sw.ElapsedMilliseconds}ms, Result: {result.AsDouble()}");

Console.WriteLine("\nWith Trusted level (compiled):");
sw.Restart();
result = host.Execute("return require('utils').add(1, 2)", options);
sw.Stop();
Console.WriteLine($"Time: {sw.ElapsedMilliseconds}ms, Result: {result.AsDouble()}");

// Helper method to create example modules
void CreateExampleModules(string dir)
{
    // utils.lua - Basic utilities
    File.WriteAllText(Path.Combine(dir, "utils.lua"), @"
local M = {}

function M.add(a, b)
    return a + b
end

function M.multiply(a, b)
    return a * b
end

function M.factorial(n)
    if n <= 1 then return 1 end
    return n * M.factorial(n - 1)
end

return M
");

    // calculator.lua - Depends on utils
    File.WriteAllText(Path.Combine(dir, "calculator.lua"), @"
local utils = require('utils')
local M = {}

function M.distance(x1, y1, x2, y2)
    local dx = x2 - x1
    local dy = y2 - y1
    return math.sqrt(utils.add(dx * dx, dy * dy))
end

function M.average(numbers)
    local sum = 0
    for _, n in ipairs(numbers) do
        sum = utils.add(sum, n)
    end
    return sum / #numbers
end

M.factorial = utils.factorial  -- Re-export

return M
");

    // heavy_computation.lua - Simulates expensive computation
    File.WriteAllText(Path.Combine(dir, "heavy_computation.lua"), @"
local M = {}

function M.compute(n)
    local result = 0
    for i = 1, n do
        for j = 1, n do
            result = result + i * j
        end
    end
    return result
end

return M
");

    // config.lua - Returns a configuration table
    File.WriteAllText(Path.Combine(dir, "config.lua"), @"
return {
    appName = 'FLua Module Example',
    version = '1.0.0',
    features = {
        'compilation',
        'caching',
        'security'
    }
}
");

    // greeter.lua - Returns a function
    File.WriteAllText(Path.Combine(dir, "greeter.lua"), @"
return function(name)
    return 'Hello, ' .. name .. '!'
end
");

    // constants.lua - Returns a simple value
    File.WriteAllText(Path.Combine(dir, "constants.lua"), @"
return 3.14159265359
");
}