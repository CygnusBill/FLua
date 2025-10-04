# FLua.Compiler

Lua-to-.NET compiler with multiple compilation backends. Transforms Lua Abstract Syntax Tree (AST) into executable .NET code.

This package provides various compilation strategies for different use cases, from ahead-of-time (AOT) compilation to just-in-time (JIT) execution.

## Features

### Compilation Backends

#### RoslynLuaCompiler (Primary)
- **C# Code Generation**: Compiles Lua to C# source code using Roslyn
- **Multiple Output Formats**:
  - Console applications (`dotnet run`)
  - Class libraries (`.dll` files)
  - Native AOT executables (no .NET runtime required)
- **Full Lua Support**: Handles functions, closures, tables, and complex control flow
- **Optimization**: Generates efficient C# code

#### ContextBoundCompiler (Advanced)
- **Configuration Lambdas**: Compile Lua expressions to strongly-typed .NET delegates
- **Direct .NET Types**: No LuaValue wrapping for maximum performance
- **Name Translation**: Automatic PascalCase/snake_case/camelCase conversion
- **Type Safety**: Compile-time type checking with context objects

Example:
```csharp
// Define a context type
public record Calculator(double BaseRate, int Multiplier);

// Compile Lua expression to delegate
var calcFunc = ContextBoundCompiler.Create<Calculator, double>(
    "return (value * multiplier) + (value * baseRate)"
);

// Execute with different contexts
var result = calcFunc(new Calculator(0.1, 2));
```

#### Expression Tree Compiler
- **LINQ Integration**: Compile Lua to .NET expression trees
- **Query Provider Compatible**: Works with LINQ providers
- **Limited Scope**: Best for simple expressions without functions

### Compilation Targets

- **ConsoleApp**: Standalone console application
- **Library**: .NET class library
- **NativeAot**: Self-contained native executable
- **Lambda**: In-memory delegate
- **Expression**: Expression tree

## Usage

### Basic Compilation
```csharp
using FLua.Compiler;
using FLua.Parser;

// Parse Lua code
var ast = ParserHelper.ParseString(@"
    function factorial(n)
        if n <= 1 then return 1 end
        return n * factorial(n - 1)
    end
    return factorial(5)
");

// Compile to executable
var compiler = new RoslynLuaCompiler();
var result = compiler.Compile(ast, new CompilerOptions {
    OutputPath = "output.exe",
    Target = CompilationTarget.ConsoleApp
});

if (result.Success) {
    // Run the compiled executable
    System.Diagnostics.Process.Start("output.exe");
}
```

### Advanced Compilation with Context
```csharp
// Define strongly-typed context
public record GameContext {
    public int PlayerLevel { get; init; }
    public double ExperienceMultiplier { get; init; }
}

// Compile Lua formula to delegate
var xpCalculator = ContextBoundCompiler.Create<GameContext, int>(
    "return math.floor(level * 100 * multiplier)"
);

// Use in game logic
int CalculateXP(GameContext ctx) => xpCalculator(ctx);
```

### Expression Tree Compilation
```csharp
// Simple expressions for LINQ integration
var exprCompiler = new ExpressionTreeCompiler();
var expr = exprCompiler.Compile("x * 2 + offset");

// Use in LINQ queries
var query = data.Where(expr);
```

## Architecture

### Compilation Pipeline
1. **AST Analysis**: Examine Lua code structure
2. **Scope Resolution**: Resolve variables and closures
3. **Code Generation**: Generate target language code
4. **Optimization**: Apply platform-specific optimizations
5. **Output**: Produce executable artifacts

### Backend Selection
- **Roslyn**: Best for complex programs, AOT compilation
- **ContextBound**: Best for high-performance formulas
- **Expression Trees**: Best for LINQ integration

## Dependencies

- FLua.Ast (AST definitions)
- FLua.Parser (parsing functionality)
- FLua.Runtime (Lua runtime types)
- Microsoft.CodeAnalysis.CSharp (Roslyn compiler)

## Performance

- **Roslyn Backend**: Near-native performance after compilation
- **ContextBound**: Maximum performance with direct .NET interop
- **Expression Trees**: Good performance for query scenarios
- **Compilation Speed**: Fast incremental compilation

## Limitations

- Complex closures may fall back to interpretation
- Some Lua features require runtime support
- Expression trees don't support function definitions

## License

MIT
