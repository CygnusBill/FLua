# FLua.Ast

Core Abstract Syntax Tree (AST) definitions for FLua, a complete Lua 5.4 implementation for .NET.

This package contains the fundamental data structures used to represent Lua programs in memory, including expressions, statements, literals, and function definitions.

## Features

- Complete Lua 5.4 AST node definitions
- F# discriminated unions for type safety
- C# interoperability methods
- Support for Lua 5.4 features (const/close variables, etc.)
- Strong typing with compile-time safety

## Usage

```csharp
using FLua.Ast;

// Create a simple expression
var number = Expr.CreateLiteral(Literal.CreateInteger(42));
var variable = Expr.CreateVar("myVar");
var assignment = Statement.CreateAssignment(
    new[] { variable },
    new[] { number }
);
```

## Dependencies

- FSharp.Core
- FLua.Common (shared utilities)

## License

MIT
