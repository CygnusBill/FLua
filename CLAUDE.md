# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

FLua is a comprehensive Lua 5.4 parser and interpreter implementation using F# (parser) and C# (interpreter/runtime). The project aims for near-complete Lua 5.4 compatibility (~95% implemented).

## Build Commands

```bash
# Main build command - creates AOT-compiled executable
./publish.sh osx-arm64  # For macOS Apple Silicon
./publish.sh linux-x64  # For Linux x64
./publish.sh win-x64    # For Windows x64

# Build for all platforms
./publish.sh

# Clean and rebuild
./clean_and_build.sh

# Alternative: Direct dotnet commands
dotnet build              # Build solution without AOT
dotnet run --project FLua.Cli  # Run REPL directly
```

## Running Tests

```bash
# Run all tests
dotnet test

# Run parser tests (159 tests)
dotnet run --project FLua.Parser.Tests

# Run interpreter tests
dotnet run --project FLua.Interpreter.Tests

# Run runtime tests
dotnet test FLua.Runtime.Tests

# Run variable attributes tests
dotnet test FLua.VariableAttributes.Tests

# Run Lua test files
./run_tests.sh  # Requires built executable in publish/osx-arm64/
```

## Architecture Overview

### Hybrid F#/C# Design
- **Parser** (F#): Uses FParsec for parsing, leverages F#'s pattern matching and DSL capabilities
- **Runtime** (C#): Object-oriented design for Lua values, better .NET interop
- **Interpreter** (C#): Tree-walking interpreter that evaluates the F# AST

### Key Components

1. **FLua.Ast/** - AST type definitions (F#)
   - `AstTypes.fs`: Discriminated unions for all Lua 5.4 constructs
   - Complete coverage of expressions, statements, and operators

2. **FLua.Parser/** - Scannerless parser (F#)
   - `Lexer.fs`: Token definitions and literal parsing
   - `Parser.fs`: Main parser with mutual recursion and operator precedence
   - `ParserHelper.fs`: C# interop helpers
   - Uses forward references to handle circular dependencies

3. **FLua.Runtime/** - Core runtime system (C#)
   - **CRITICAL REQUIREMENT**: All runtime functionality used by either the interpreter or future compiled code MUST be implemented here
   - This ensures no code duplication and consistent runtime behavior
   - `LuaValue.cs`: Type hierarchy (nil, bool, number, string, table, function)
   - `LuaEnvironment.cs`: Variable scoping and environment chains
   - `LuaTable.cs`: Optimized table implementation with array/hash parts
   - `Lua*Lib.cs`: Standard library implementations (math, string, table, etc.)
   - `LuaCoroutine.cs`: Coroutine support with proper state management
   - `LuaVariable.cs`: Variable attributes support (<const>, <close>)

4. **FLua.Interpreter/** - Execution engine (C#)
   - `LuaInterpreter.cs`: Main interpreter with AST evaluation
   - `LuaRepl.cs`: REPL implementation with multi-line support
   - Direct tree-walking without bytecode compilation
   - Exception-based error handling

### Parser Architecture Details
- **Scannerless**: Direct character stream processing without separate tokenization
- **Operator Precedence Parser**: Uses FParsec's OperatorPrecedenceParser for correct evaluation order
- **Postfix Parsing**: Handles chaining of calls, method calls, and table access
- **Mutual Recursion**: All parsers in single module with forward references

### Interpreter Pattern
- **StatementResult**: Encodes control flow (normal, break, return, goto)
- **Environment Chain**: Lexical scoping via parent environment references
- **Closure Support**: Proper upvalue capture and environment binding
- **Multiple Returns**: Lists for multiple assignment/return values

## Architectural Principles

### Runtime Library Separation

**Critical Design Decision**: The FLua.Runtime project serves as the single source of truth for all runtime functionality. This architectural principle ensures:

1. **No Code Duplication**: Both the interpreter and future compiler use the same runtime
2. **Consistent Behavior**: Same LuaValue operations whether interpreted or compiled
3. **Maintainability**: Bug fixes and improvements benefit both execution models
4. **Clear Boundaries**: Parser → AST → Runtime (used by both Interpreter and Compiler)

#### What Belongs in FLua.Runtime:
- LuaValue type system and operations
- Built-in libraries (string, table, math, io, os, etc.)
- Environment and variable management
- Coroutine implementation
- Error handling and exceptions
- Module system (require/package)
- Any functionality called at runtime

#### What Does NOT Belong in FLua.Runtime:
- AST types (FLua.Ast)
- Parsing logic (FLua.Parser)
- AST evaluation/interpretation (FLua.Interpreter)
- Code generation (Future FLua.Compiler)
- CLI handling (FLua.Cli)

This separation is crucial for the planned compiler, as it will generate code that calls into the same FLua.Runtime library that the interpreter uses.

## Common Development Tasks

### Adding a New Built-in Function
1. Add method to appropriate `Lua*Lib.cs` file in FLua.Runtime
2. Register in `LuaEnvironment.CreateGlobalEnvironment()` 
3. Follow existing patterns for parameter validation and type conversion

### Modifying the Parser
1. Edit `Parser.fs` - all parsers are in this single file
2. Update forward references if adding new recursive parsers
3. Add test cases to `FLua.Parser.Tests/Program.fs`
4. Run parser tests to verify: `dotnet run --project FLua.Parser.Tests`

### Debugging Parser Issues
1. Use the debug scripts: `debug_parser.fsx`, `test_parser_debug.fsx`
2. Enable parser tracing in tests by uncommenting trace lines
3. Check `PARSER_KNOWN_ISSUES.md` for documented limitations

### Running Individual Lua Tests
```bash
# After building executable
./publish/osx-arm64/flua test_script.lua

# Or via dotnet run
echo "print('test')" | dotnet run --project FLua.Cli
```

## Known Issues and Limitations

### Parser
- Function calls with long strings without parentheses: `print[[hello]]` fails (use `print([[hello]])`)
- Unicode escapes beyond U+10FFFF accepted for test compatibility

### Not Yet Implemented
- Weak tables and weak references
- Debug library (partial implementation)
- Binary chunks and bytecode
- Some metamethods (__gc, __mode)
- Package preload functionality

See `FLua-Gap-Analysis.md` for comprehensive compatibility status.

## Testing Philosophy
- Parser tests use Expecto (F# functional testing framework) for AST correctness
- Interpreter tests verify execution semantics
- Runtime tests use xUnit for standard library behavior
- LuaTests/ contains official Lua test suite files
- Test frameworks chosen to match language: Expecto for F#, xUnit for C#

## Important Files
- `FLua-Gap-Analysis.md`: Detailed compatibility analysis
- `PARSER_KNOWN_ISSUES.md`: Known parser limitations
- `test_*.lua`: Various test scripts for specific features
- `LuaTests/`: Official Lua test suite (for compatibility testing)