# FLua - Complete Lua 5.4 Implementation for .NET

A comprehensive Lua 5.4 implementation for .NET with multiple execution backends including interpreter, compiler, and native AOT. Features a robust F# parser, C# runtime, and extensive hosting capabilities.

## 🚀 Quick Start

### **Interactive REPL (Recommended)**
```bash
# Start the interactive Lua REPL
dotnet run --project FLua.Cli

# Or after building:
./flua

# Try some Lua code:
lua> 1 + 2 * 3
= 7

lua> local x = "Hello"
lua> print(x, "World!")
Hello   World!

lua> .help        # Show help
lua> .quit        # Exit
```

### **Run Test Suite**
```bash
# Run parser tests (159 tests)
dotnet run --project FLua.Parser.Tests

# Run interpreter tests (9 tests + demo)
dotnet run --project FLua.Interpreter.Tests
```

### **Execute Lua Files** 
```bash
# Execute a Lua script file
dotnet run --project FLua.Cli -- script.lua

# Or after building:
./flua script.lua

# Show help
./flua --help
```

### **Compile Lua Scripts**
```bash
# Compile to .NET console application (can run with 'dotnet')
./flua compile script.lua -o script.dll --target ConsoleApp
dotnet script.dll

# Compile to native executable (1MB, no .NET required)
./flua compile script.lua -o script --target NativeAot
./script

# Compile to library for use from other .NET code
./flua compile script.lua -o script.dll --target Library
```

## 🎯 Usage Examples

### **REPL Commands**
```lua
-- Expressions (show result)
lua> 42
= 42

lua> "hello" .. " " .. "world"
= hello world

lua> math.max(10, 20, 5)
= 20

-- Statements (no result shown)
lua> local name = "FLua"
lua> print("Welcome to", name)
Welcome to  FLua

-- Built-in functions
lua> type(42)
= number

lua> tostring(3.14)
= 3.14

-- Variables persist across lines
lua> x = 100
lua> y = x * 2
lua> print("Result:", y)
Result: 200

-- Multi-line input (automatic detection)
lua> local function factorial(n)
  >>   if n <= 1 then return 1
  >>   else return n * factorial(n-1) end
  >> end
  >>
lua> factorial(5)
= 120

-- REPL commands
lua> .env         # Show variables
lua> .clear       # Clear screen  
lua> .help        # Show help
lua> .quit        # Exit
```

## Features

### ✅ Complete Lua 5.4 Implementation (~97% Complete)
- **Core Language**: All Lua 5.4 language features with correct semantics
- **Standard Libraries**: Comprehensive standard library implementation
- **Multiple Backends**: Interpreter, Compiler, Expression Trees, Native AOT
- **Test Coverage**: 519+ tests with 97% pass rate

### ✅ Multiple Execution Modes

#### Interpreter
- **AST-based**: Direct evaluation of parsed AST
- **Full Lua Support**: Complete language feature support
- **Interactive REPL**: Immediate feedback with persistent environment
- **Debug-friendly**: Easy to trace and debug

#### Compiler (RoslynLuaCompiler)
- **C# Code Generation**: Compiles Lua to C# using Roslyn
- **Multiple Targets**: Console apps, libraries, lambdas
- **Native AOT**: Compile to standalone executables (no .NET required)
- **High Performance**: Near-native execution speed

#### ContextBoundCompiler (NEW)
- **Configuration Lambdas**: Compile Lua expressions to strongly-typed .NET delegates
- **Direct .NET Types**: No LuaValue wrapping for maximum performance
- **Name Translation**: Automatic PascalCase/snake_case/camelCase conversion
- **Type Safety**: Compile-time type checking with context objects

Example:
```csharp
// Define a context type
public record Context(Calculator Calc, int Threshold);

// Compile Lua expression to delegate
var func = ContextBoundCompiler.Create<Context, bool>(
    "calc.calculate_value(10) > threshold"
);

// Execute with different contexts
var result = func(new Context(myCalc, 50));
```

#### Expression Tree Compilation
- **Simple Expressions**: Compile to .NET expression trees
- **LINQ Integration**: Compatible with LINQ providers
- **Limited Scope**: Best for simple calculations without functions

### ✅ Hosting API
- **Embedded Scripting**: Host Lua scripts in .NET applications
- **Security Levels**: Five trust levels from Untrusted to FullTrust
- **Module System**: File-based module loading with security controls
- **Host Integration**: Inject .NET functions and objects into Lua

Example:
```csharp
var host = new LuaHost();
var options = new LuaHostOptions
{
    TrustLevel = TrustLevel.Sandbox,
    HostFunctions = new() { ["log"] = args => Console.WriteLine(args[0]) }
};

var result = host.Execute("log('Hello from Lua!'); return 42", options);
```

### ✅ Interactive REPL
- **Expression evaluation**: Immediate results for expressions
- **Statement execution**: Full Lua statement support
- **Persistent environment**: Variables persist across REPL sessions
- **Multi-line input**: Support for functions and complex constructs
- **Built-in commands**: Help, environment inspection, clear screen

### ✅ Comprehensive Testing
- **Parser Tests**: 266 tests covering all syntax
- **Runtime Tests**: 131 tests for runtime behavior
- **Compiler Tests**: 6 compiler-specific tests
- **Hosting Tests**: 94 tests for hosting scenarios
- **Integration Tests**: Real-world Lua code execution

## Supported Syntax

### Expressions
```lua
-- Literals
42, 3.14, 0xff, "hello", true, nil

-- Variables and table access
x, table.field, obj["key"], nested.table[index]

-- Method calls
obj:method(), data:process(args)

-- Function calls
print(42), math.max(1, 2, 3)

-- All operators with correct precedence
1 + 2 * 3^4, a and b or c, x << 2 | y & z
```

### Statements
```lua
-- Assignments (single and multiple)
x = 42
a, b, c = 1, 2, 3
local x, y = func()

-- Control flow
if condition then ... elseif ... else ... end
while condition do ... end
repeat ... until condition
for i = 1, 10 do ... end
for k, v in pairs(table) do ... end

-- Functions
function name(params) ... end
function obj.method(self, ...) ... end
local function helper() ... end

-- Labels and goto
::label::
goto label
```

### Advanced Features
```lua
-- Table constructors
{1, 2, 3}
{x = 1, y = 2}
{[key] = value, [func()] = result}
{mixed, values, x = 1, [key] = 2}

-- Method calls
obj:method(args)
data:process():transform():save()

-- Multiple return/assignment
return a, b, c
local x, y, z = func()
```

## Architecture

### Parser Design
- **Centralized**: All parsers in single module for mutual recursion
- **Forward References**: Proper handling of circular dependencies  
- **Operator Precedence**: Uses FParsec's OperatorPrecedenceParser
- **Postfix Parsing**: Handles chaining of calls and access operations

### Interpreter Design
- **AST-based**: Direct evaluation of parsed AST
- **Environment Chain**: Proper lexical scoping with closures
- **Built-in Functions**: Standard Lua functions (`print`, `type`, `math.*`)
- **Error Handling**: Comprehensive runtime error reporting
- **Value System**: Full Lua type system with proper conversions

### AST Structure
The parser produces a clean Abstract Syntax Tree defined in `Library.fs`:

```fsharp
type Expr = 
    | Literal of Literal
    | Var of Identifier  
    | FunctionCall of Expr * Expr list
    | MethodCall of Expr * Identifier * Expr list
    | TableAccess of Expr * Expr
    | Binary of Expr * BinaryOp * Expr
    // ... and more

type Statement =
    | Assignment of Expr list * Expr list
    | FunctionDefStmt of Identifier list * FunctionDef
    | If of (Expr * Block) list * Block option
    // ... and more
```

## Testing

The parser includes comprehensive tests covering:
- **Parser Tests**: 159 tests covering all expression types, operator precedence, statements, and edge cases
- **Interpreter Tests**: 9 tests covering expression evaluation, statement execution, and built-in functions
- **Integration Tests**: Real Lua code execution and validation
- **REPL Testing**: Interactive testing with immediate feedback

```bash
# Run all tests
dotnet test

# Run specific test suites
dotnet run --project FLua.Parser.Tests
dotnet run --project FLua.Interpreter.Tests
```

## Project Structure

```
FLua/
├── FLua.Ast/                # F# AST type definitions
├── FLua.Parser/             # F# Parser using FParsec
│   ├── Lexer.fs            # Tokenizer
│   ├── Parser.fs           # Main parser implementation
│   └── ParserHelper.fs     # Parser utilities
├── FLua.Common/             # Shared utilities and diagnostics
├── FLua.Runtime/            # Runtime system and standard libraries
│   ├── LuaTypes.cs         # Value types and operations
│   ├── LuaEnvironment.cs   # Environment and scoping
│   └── Libraries/          # Standard library implementations
├── FLua.Interpreter/        # AST interpreter
│   ├── LuaInterpreter.cs   # Core interpreter
│   └── LuaRepl.cs          # REPL implementation
├── FLua.Compiler/           # Compilation backends
│   ├── RoslynLuaCompiler.cs        # Roslyn C# code generation
│   ├── RoslynCodeGenerator.cs      # Code generation logic
│   ├── CecilCodeGenerator.cs       # IL generation with Mono.Cecil
│   └── ContextBoundCompiler.cs     # Config lambda compiler
├── FLua.Hosting/            # Hosting API
│   ├── LuaHost.cs          # Main hosting interface
│   ├── Environment/        # Environment providers
│   └── Security/           # Security policies
├── FLua.Cli/                # Command-line interface
├── examples/                # Usage examples
│   ├── SimpleScriptExecution/
│   ├── LambdaCompilation/
│   ├── ExpressionTreeCompilation/
│   └── ModuleLoading/
└── Tests/
    ├── FLua.Parser.Tests/          # 266 parser tests
    ├── FLua.Runtime.Tests/         # 131 runtime tests
    ├── FLua.Interpreter.Tests/     # 3 interpreter tests
    ├── FLua.Compiler.Tests/        # 6 compiler tests
    ├── FLua.Hosting.Tests/         # 110 hosting tests
    └── FLua.VariableAttributes.Tests/ # 19 attribute tests
```

## Status

**Production Ready**: 519+ tests with 97% pass rate, ~95% Lua 5.4 compatibility

### Test Results (December 2025)
- ✅ **Parser**: 266/266 tests passing
- ✅ **Runtime**: 131/131 tests passing  
- ✅ **Interpreter**: 3/3 tests passing
- ✅ **Compiler**: 6/6 tests passing
- ✅ **Variable Attributes**: 19/19 tests passing
- ✅ **Hosting**: 94/110 tests passing (2 known limitations, 14 legacy)

### Working Features ✅

#### Core Language
- All Lua 5.4 expressions and statements
- Control flow (if, while, for, repeat, goto)
- Functions (definitions, calls, closures, varargs)
- Tables (constructors, access, methods)
- Operators (all with correct precedence)
- Multiple assignment/return
- Local variables with attributes
- Labels and goto
- Coroutines

#### Standard Libraries
- **Basic**: print, type, tostring, tonumber, pairs, ipairs, next
- **Math**: All math functions (sin, cos, sqrt, random, etc.)
- **String**: All string operations (sub, find, gsub, format, etc.)
- **Table**: insert, remove, sort, concat, unpack
- **IO**: Basic file operations (with security controls)
- **OS**: Date/time, environment (with security controls)
- **Coroutine**: create, resume, yield, status
- **UTF8**: All UTF-8 operations
- **Package**: Module system with require

#### Compilation & Hosting
- Roslyn-based C# code generation
- Native AOT compilation support
- Expression tree compilation
- Context-bound lambda compilation
- Embedded hosting with security levels
- Module system with path resolution
- Host function injection
- .NET interop

### Known Limitations 📋
See [ARCHITECTURAL_LIMITATIONS.md](docs/project-docs/ARCHITECTURAL_LIMITATIONS.md) for details:
- Expression trees cannot compile function definitions
- Modules with closures fall back to interpreter
- No Lua bytecode compatibility (by design - compiles to .NET IL)

### Future Enhancements
- Weak tables and references
- Complete debug library
- Performance optimizations
- Enhanced error messages
- More code generation targets

## Documentation

- [Architecture Compliance Report](docs/project-docs/ARCHITECTURE_COMPLIANCE_REPORT.md)
- [Architectural Limitations](docs/project-docs/ARCHITECTURAL_LIMITATIONS.md)  
- [Compiler Limitations](docs/project-docs/COMPILER_LIMITATIONS.md)
- [Gap Analysis](docs/project-docs/FLua-Gap-Analysis.md)
- [Examples](examples/README.md)

## License

MIT License - see LICENSE file for details. 