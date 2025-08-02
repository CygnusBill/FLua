# Lambda Compilation Example

This example demonstrates how to compile Lua scripts into .NET delegates (lambdas) for high-performance execution.

## Key Features Demonstrated

- Compiling Lua code to strongly-typed .NET delegates
- Performance comparison between interpreted and compiled execution
- Type-safe function signatures
- Error handling in compiled code

## Benefits of Lambda Compilation

1. **Performance**: Compiled code runs 10-100x faster than interpreted
2. **Type Safety**: Get compile-time type checking for inputs/outputs
3. **Integration**: Use Lua functions as regular C# delegates
4. **Caching**: Compile once, execute many times

## How It Works

1. Write Lua code that returns a value or function
2. Use `CompileToFunction<T>()` to compile to a typed delegate
3. Call the resulting delegate like any C# function
4. Trust level must be `Trusted` or higher for compilation

## Use Cases

- Mathematical formulas that need frequent evaluation
- Data transformation functions
- Business rules that change at runtime
- Performance-critical calculations
- User-defined functions in applications

## Running the Example

```bash
dotnet run
```

## Expected Output

Shows various compilation examples including:
- Mathematical calculations
- String processing
- Performance comparisons showing 10x+ speedup
- Error handling demonstration