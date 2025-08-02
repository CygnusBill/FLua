# Lambda Compilation Example

This example demonstrates how to compile Lua scripts into .NET delegates (lambdas) for high-performance execution.

## Overview

Lambda compilation transforms Lua code into native .NET code that runs 10-100x faster than interpreted scripts. This is ideal for:
- Mathematical formulas evaluated millions of times
- Data transformation functions
- Business rules that change at runtime
- Performance-critical calculations

## Code Walkthrough

### Step 1: Understanding the Compilation Process

```
Lua Script → Parser → AST → C# Code Generation → Roslyn Compilation → IL Assembly → Delegate
```

The compilation pipeline:
1. **Parse**: Convert Lua text to Abstract Syntax Tree (AST)
2. **Generate**: Transform AST to C# code using Roslyn
3. **Compile**: Use Roslyn to compile C# to IL
4. **Load**: Load assembly and create delegate
5. **Cache**: Store compiled delegate for reuse

### Step 2: Basic Lambda Compilation

```csharp
var distanceFormula = @"
    local x1, y1, x2, y2 = ...  -- Varargs become parameters
    local dx = x2 - x1
    local dy = y2 - y1
    return math.sqrt(dx * dx + dy * dy)
";

var distanceFunc = host.CompileToFunction<double>(distanceFormula, new LuaHostOptions
{
    TrustLevel = TrustLevel.Trusted  // Required for compilation
});
```

Key concepts:
- **Varargs (`...`)**: Become function parameters in compiled code
- **Trust Level**: Must be `Trusted` or higher for compilation
- **Type Safety**: Return type specified as generic parameter
- **One-time Cost**: Compilation happens once, execution is fast

### Step 3: Generated C# Code

The Lua distance formula compiles to approximately:

```csharp
public static class LuaScript
{
    public static LuaValue[] Execute(LuaEnvironment env)
    {
        // local x1, y1, x2, y2 = ...
        var args = env.GetVariable("arg").AsTable<LuaTable>();
        var x1 = args.Get(1).AsDouble();
        var y1 = args.Get(2).AsDouble();
        var x2 = args.Get(3).AsDouble();
        var y2 = args.Get(4).AsDouble();
        
        // local dx = x2 - x1
        var dx = x2 - x1;
        
        // local dy = y2 - y1  
        var dy = y2 - y1;
        
        // return math.sqrt(dx * dx + dy * dy)
        var math = env.GetVariable("math").AsTable<LuaTable>();
        var sqrt = math.Get("sqrt").AsFunction();
        return new[] { sqrt.Call(new[] { LuaValue.Number(dx * dx + dy * dy) })[0] };
    }
}
```

### Step 4: Performance Comparison

```csharp
// Interpreted version
var sw = Stopwatch.StartNew();
var interpretedResult = host.Execute(interpretedScript);
sw.Stop();
var interpretedTime = sw.ElapsedMilliseconds;

// Compiled version
sw.Restart();
var compiledFunc = host.CompileToFunction<double>(compiledScript);
var compiledResult = compiledFunc();
sw.Stop();
var compiledTime = sw.ElapsedMilliseconds;

Console.WriteLine($"Speedup: {(double)interpretedTime / compiledTime:F2}x");
```

Typical performance gains:
- Simple math: 10-20x faster
- Complex calculations: 50-100x faster
- String operations: 5-10x faster
- Table operations: 3-5x faster

### Step 5: CompileToFunction<T> API

```csharp
// Different return types
var boolFunc = host.CompileToFunction<bool>("return 5 > 3");
var stringFunc = host.CompileToFunction<string>("return 'Hello'");
var doubleFunc = host.CompileToFunction<double>("return 42.5");
var objectFunc = host.CompileToFunction<object>("return {1,2,3}");
```

Supported return types:
- **Primitives**: `bool`, `int`, `long`, `double`, `string`
- **Objects**: `object` (returns LuaValue)
- **Tables**: Access via `object` then cast
- **Functions**: Return as `object`, cast to callable

### Step 6: Advanced Patterns

#### Pattern 1: Parameterized Functions
```csharp
var luaCode = @"
    local base, exponent = ...
    return base ^ exponent
";

// Compile once
var powerFunc = host.CompileToFunction<double>(luaCode);

// Execute many times with different inputs
for (int i = 1; i <= 10; i++)
{
    // Set up args before calling
    var result = powerFunc();  // Uses environment args
}
```

#### Pattern 2: Factory Functions
```csharp
var factoryCode = @"
    return function(x)
        return x * 2
    end
";

var factory = host.CompileToFunction<object>(factoryCode);
// Returns a LuaFunction that can be called
```

#### Pattern 3: Complex Calculations
```csharp
var complexCalc = @"
    local data = ...
    local sum = 0
    local count = 0
    
    for i = 1, #data do
        if data[i] > 0 then
            sum = sum + data[i]
            count = count + 1
        end
    end
    
    return count > 0 and sum / count or 0
";

var avgPositive = host.CompileToFunction<double>(complexCalc);
```

### Step 7: Error Handling

Compilation can fail for several reasons:

```csharp
try
{
    var func = host.CompileToFunction<double>(luaCode);
}
catch (LuaRuntimeException ex)
{
    // Parse errors: Invalid Lua syntax
    // "Parse error: unexpected symbol near 'end'"
}
catch (InvalidOperationException ex)
{
    // Compilation errors: Unsupported features
    // "Dynamic code loading not supported in compiled mode"
}
catch (InvalidCastException ex)
{
    // Type errors: Return type mismatch
    // "Cannot convert LuaValue to type System.Double"
}
```

### Step 8: Compilation Limitations

Features not supported in compiled mode:
- `load()` and `loadfile()` - No dynamic code
- `debug` library - No runtime introspection
- Some metatable operations
- Coroutines (limited support)

These limitations exist because:
- Compiled code is static
- No interpreter available at runtime
- Security and performance guarantees

## Complete Example: Math Expression Evaluator

```csharp
public class MathEvaluator
{
    private readonly LuaHost _host;
    private readonly Dictionary<string, Func<double>> _compiledFormulas;
    
    public MathEvaluator()
    {
        _host = new LuaHost();
        _compiledFormulas = new Dictionary<string, Func<double>>();
    }
    
    public void RegisterFormula(string name, string luaExpression)
    {
        // Compile and cache
        var func = _host.CompileToFunction<double>(luaExpression, 
            new LuaHostOptions { TrustLevel = TrustLevel.Trusted });
        _compiledFormulas[name] = func;
    }
    
    public double Evaluate(string formulaName)
    {
        return _compiledFormulas[formulaName]();
    }
}

// Usage
var evaluator = new MathEvaluator();
evaluator.RegisterFormula("circle_area", "local r = ... return math.pi * r^2");
evaluator.RegisterFormula("compound_interest", @"
    local p, r, n, t = ...
    return p * (1 + r/n)^(n*t)
");

// Fast execution
var area = evaluator.Evaluate("circle_area");  // With radius from environment
```

## Performance Best Practices

1. **Compile Once, Execute Many**: Compilation is expensive, execution is cheap
2. **Batch Operations**: Process arrays in Lua rather than individual calls
3. **Minimize Interop**: Keep data in Lua format during processing
4. **Use Appropriate Types**: Match Lua and C# types to avoid conversions
5. **Cache Compiled Functions**: Store delegates for reuse

## Memory Considerations

- Each compilation creates a new in-memory assembly
- Assemblies cannot be unloaded in .NET Framework
- In .NET Core/5+, use AssemblyLoadContext for unloading
- Monitor memory usage for long-running applications

## Security Notes

- Compiled code runs with full CLR permissions
- Trust level still applies to available Lua functions
- Validate scripts before compilation
- Consider signing assemblies for production

## Debugging Compiled Code

Enable debug info in compiler options:
```csharp
var options = new LuaHostOptions
{
    CompilerOptions = new CompilerOptions
    {
        IncludeDebugInfo = true,
        OutputPath = "debug_output.cs"  // See generated C#
    }
};
```

## Next Steps

- Experiment with different formula types
- Measure performance in your use case
- Try [Expression Tree Compilation](../ExpressionTreeCompilation) for LINQ
- Explore [AOT Compilation](../AotCompilation) for deployment

## Running the Example

```bash
dotnet run
```

Expected output shows:
- Mathematical function compilation
- String processing examples
- Performance comparison (10x+ speedup)
- Error handling demonstration