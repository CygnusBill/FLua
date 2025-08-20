# FLua Compiler TODO List

## Completed ✓
- [x] Fix integer overflow with large hex literals (0x7fffffffffffffff)
- [x] Add support for underscore as identifier
- [x] Add comprehensive parser test suite
- [x] Comprehensive code review for architectural compliance
- [x] Refactor runtime operations from Interpreter to Runtime
- [x] Create LuaOperations class in Runtime
- [x] Create LuaTypeConversion class in Runtime
- [x] Create LuaMetamethods class in Runtime
- [x] Fix shift count too large error for bitwise operations
- [x] Add support for piping input to FLua interpreter
- [x] Implement string.format with format specifiers
- [x] Implement string.packsize function
- [x] Create tests for string.pack/unpack/packsize
- [x] Fix function calls with table constructors in for/if
- [x] Fix function calls with long strings (no parentheses)
- [x] Add shebang (#!) support to parser
- [x] Fix reserved word handling in identifier parser
- [x] Create FLua.Compiler project with Roslyn backend
- [x] Implement CLI with CommandLineParser verbs
- [x] Fix generated C# code - runtime references and API usage
- [x] Add compiler output targets (library + console)
- [x] Create comprehensive test coverage following Lee Copeland standards
- [x] Test compiler against Lua torture tests
- [x] Implement return statement in compiler
- [x] Fix IsTruthy property access in control structures
- [x] Implement local function definitions in compiler
- [x] Fix variable shadowing in nested scopes for compiler
- [x] Fix FSharpOption usage in RoslynCodeGenerator
- [x] Fix function call generation in RoslynCodeGenerator for statements
- [x] Refactor code generator to use Roslyn syntax factory
- [x] Commit Roslyn code generator implementation
- [x] Fix local function calls to use environment lookup
- [x] Create generic runner class for console applications
- [x] Add basic console application support (dotnet foo.dll)
- [x] Implement control structures in compiler (if/while/for)
- [x] Implement assignment statements (non-local)
- [x] Implement unary operators (-, not, #, ~)
- [x] Add break statement support
- [x] Fix variable types (LuaValue vs specific types)
- [x] Handle nullable double conversions in for loops
- [x] Implement table support in compiler (literals, indexing, methods)
  - [x] Table constructor literals
  - [x] Table indexing (get)
  - [x] Table indexing (set) - with parser limitations
  - [x] Method calls on tables
- [x] Fix multiple assignment from function calls
- [x] Fix parser table assignment limitation (t[1] = 100 now works)
- [x] Fix parser table access in expressions (t[1] + t[2] now works)
- [x] Add inline function expression support (function() ... end)
- [x] Add AOT/standalone executable support (fully working)
  - Native executables can be generated with -t NativeAot
  - Creates single file native executables (~2.3MB on macOS ARM64)
  - No runtime dependencies - truly standalone executables
- [x] Fix AOT runtime dependencies (resolved by removing FLua.Ast from Runtime)

## High Priority
- [x] Integrate error system into parser/interpreter/compiler
  - [x] Wrap runtime exceptions with diagnostic information
  - [x] Add compile-time errors for dynamic features (load/loadfile/dofile)
  - [x] Update CLI to display warnings and structured error messages
  - [x] Complete integration into compiler with error detection
  - [x] Convert FParsec errors to structured diagnostics (enhanced with line/column info)

## Medium Priority
- [x] Implement load() function for dynamic code loading (interpreter only)
  - Fully working in the interpreter with proper error handling
  - Parses code dynamically and returns a callable chunk
  - Reports syntax errors with line/column information
  - Not supported in compiled code (returns "dynamic loading not supported")
- [x] Design and implement structured error/warning system
  - [x] Created FLua.Common project with diagnostic infrastructure
  - [x] Error codes use logical scheme: FLU-XYZZ (severity-area-number)
  - [x] User-friendly error messages (no technical jargon)
  - [x] Rust-like error formatting with source context
  - [x] DiagnosticBuilder for consistent error creation
  - [x] Support for errors, warnings, info, and hints
  - [x] Full integration into compiler and runtime
  - [x] Dynamic loading functions now cause compilation errors (not warnings)
- [x] Improved error messages with line numbers
  - [x] Enhanced FParsec error handling with precise source location information
  - [x] Added positioned AST variants (VarPos, FunctionCallPos) for semantic error support
  - [x] User-friendly parse error messages: "Syntax error in filename.lua at line 2, column 1"
  - [x] Infrastructure ready for enhanced semantic error reporting
- [ ] Add IL.Emit backend for size optimization

## Low Priority
- [ ] Add load() support for compiled code (requires runtime compilation)
- [ ] Review parser ordering and overlapping conditions
- [ ] Add Lua bytecode backend
- [ ] Implement sandboxing configuration
- [ ] Fix _ENV = nil handling in modules
- [ ] Improved warning messages

## Current Status & Notes

### Test Coverage ✅
- **All 24 compiler tests passing** in FLua.Compiler.Tests.Minimal
- **266 parser tests passing** 
- **3 interpreter tests passing**
- Comprehensive coverage following Lee Copeland testing methodologies

### Diagnostic System ✅ 
- **Structured error/warning system** fully integrated
- **Error codes**: FLU-XYZZ format (severity-area-sequence)
- **User-friendly messages** with helpful suggestions
- **Parse error enhancement**: Line and column numbers with filename context
- **Positioned AST infrastructure**: Ready for semantic error reporting
- **Compile-time error detection** for dynamic loading functions
- **CLI integration** with proper warning/error display

### Compiler Features ✅
- **Full Lua 5.4 compatibility** for supported features
- **AOT compilation**: Native executables (~2.3MB, no dependencies)
- **Control structures**: if/while/for/repeat with proper code generation
- **Functions**: Local functions, inline expressions, proper scoping
- **Tables**: Literals, indexing, method calls, assignment
- **Operations**: All binary/unary operators with proper precedence
- **Multiple assignment** from function returns

### Architecture ✅
- **Hybrid F#/C# design**: F# parser, C# runtime/compiler
- **RoslynCodeGenerator**: Syntax factory-based code generation (preferred)
- **Clean separation**: Runtime has no AST dependencies
- **Three execution modes**: Interpreter, Library compilation, AOT compilation

### Implementation Notes
- Console app support enables standalone program testing
- Parser fixes enable full table support: `t[1] = 100` and `t[1] + t[2]`
- Variable shadowing handled with name mangling in nested scopes
- Dynamic loading (load/loadfile/dofile) causes compilation errors (prevents runtime failures)
- Closures not yet supported (accessing outer scope variables from inner functions)