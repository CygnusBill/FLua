# FLua.Parser

F# parser for Lua 5.4 syntax using FParsec. Converts Lua source code to FLua Abstract Syntax Tree (AST).

This package provides a complete, scannerless parser that handles all Lua 5.4 language features including expressions, statements, functions, tables, and advanced constructs.

## Features

- Complete Lua 5.4 syntax support
- Scannerless parsing with FParsec
- Proper operator precedence and associativity
- Comprehensive error reporting
- Support for all Lua literals and escape sequences
- Table constructors and method syntax
- Function definitions and closures
- Labels and goto statements

## Usage

```csharp
using FLua.Parser;

// Parse a string to AST
var ast = ParserHelper.ParseString(@"
    local x = 42
    print('Hello, ' .. 'World!')
    return x * 2
");

// Parse with filename for better error messages
var ast = ParserHelper.ParseStringWithFileName(code, "script.lua");
```

## Supported Lua Features

### Expressions
- Literals: `nil`, `true`, `false`, numbers, strings
- Variables and table access: `x`, `table.key`, `array[1]`
- Operators with correct precedence
- Function calls and method calls
- Table constructors and comprehensions

### Statements
- Assignments (single and multiple)
- Local variable declarations
- Control flow: `if`, `while`, `for`, `repeat`
- Function definitions
- `return` statements
- Labels and `goto`

### Advanced Features
- Varargs (`...`)
- Closures and lexical scoping
- Metamethod syntax
- Unicode and hex escapes in strings

## Dependencies

- FParsec (parser combinator library)
- FSharp.Core
- FLua.Ast (AST definitions)
- FLua.Common (diagnostics)

## Performance

The parser uses FParsec's efficient backtracking algorithms and maintains good performance even for complex Lua programs.

## License

MIT
