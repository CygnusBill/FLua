using System.Linq.Expressions;
using FLua.Hosting;
using FLua.Hosting.Security;

// Example: Expression Tree Compilation
// This example demonstrates compiling Lua code to LINQ expression trees.
// Expression trees enable integration with LINQ providers and dynamic query generation.

var host = new LuaHost();

Console.WriteLine("=== FLua Expression Tree Compilation ===\n");

// Example 1: Simple arithmetic expression
Console.WriteLine("Example 1: Simple Arithmetic Expression");
Console.WriteLine("---------------------------------------");

var arithmeticScript = @"
    return 10 + 20 * 3
";

try
{
    var expr1 = host.CompileToExpression<double>(arithmeticScript);
    Console.WriteLine($"Expression: {expr1}");
    Console.WriteLine($"Result: {expr1.Compile()()}");
}
catch (Exception ex)
{
    Console.WriteLine($"Note: {ex.Message}");
    Console.WriteLine("Expression trees have limited support for complex operations.\n");
}

// Example 2: Variable access (currently limited)
Console.WriteLine("\nExample 2: Variables and Operations");
Console.WriteLine("-----------------------------------");

var variableScript = @"
    local x = 5
    local y = 10
    return x + y
";

try
{
    var expr2 = host.CompileToExpression<double>(variableScript);
    Console.WriteLine($"Expression compiled: {expr2}");
    Console.WriteLine($"Result: {expr2.Compile()()}");
}
catch (Exception ex)
{
    Console.WriteLine($"Current limitation: {ex.Message}");
}

// Example 3: What expression trees are good for
Console.WriteLine("\nExample 3: Use Cases for Expression Trees");
Console.WriteLine("-----------------------------------------");

Console.WriteLine(@"
Expression trees are valuable for:

1. **LINQ Integration**: 
   - Create dynamic queries for Entity Framework
   - Build runtime query predicates
   - Integrate with LINQ providers

2. **Code Analysis**:
   - Inspect code structure at runtime
   - Transform expressions
   - Generate SQL or other query languages

3. **Performance**:
   - Compile once, execute many times
   - JIT optimization opportunities
   - Avoid interpretation overhead

4. **Metaprogramming**:
   - Generate code dynamically
   - Create type-safe builders
   - Implement DSLs (Domain Specific Languages)
");

// Example 4: Practical usage pattern
Console.WriteLine("\nExample 4: Practical Pattern - Dynamic Filters");
Console.WriteLine("----------------------------------------------");

// Simulate a scenario where expression trees would be useful
var data = new[] { 1, 5, 10, 15, 20, 25, 30 };

Console.WriteLine("Original data: " + string.Join(", ", data));

// In a real scenario, you might compile Lua to an expression tree
// that creates a predicate for filtering
Expression<Func<int, bool>> filter = x => x > 10 && x < 25;

var filtered = data.Where(filter.Compile());
Console.WriteLine("Filtered data (10 < x < 25): " + string.Join(", ", filtered));

Console.WriteLine(@"
Note: Full expression tree support would allow compiling Lua code like:
      'return function(x) return x > 10 and x < 25 end'
      into LINQ-compatible expression trees.
");

// Example 5: Current capabilities
Console.WriteLine("\nExample 5: Currently Supported Operations");
Console.WriteLine("-----------------------------------------");

var supportedOps = new[]
{
    ("return 42", "Literals"),
    ("return 1 + 2", "Basic arithmetic"),
    ("return 10 - 5", "Subtraction"),
    ("return 3 * 4", "Multiplication"),
    ("return 20 / 4", "Division"),
    ("return 'hello' .. ' world'", "String concatenation"),
    ("return 5 == 5", "Equality comparison"),
    ("return 10 > 5", "Greater than comparison")
};

foreach (var (script, description) in supportedOps)
{
    try
    {
        var expr = host.CompileToExpression<object>($"{script}");
        var result = expr.Compile()();
        Console.WriteLine($"✓ {description}: {script} => {result}");
    }
    catch
    {
        Console.WriteLine($"✗ {description}: Not yet supported");
    }
}