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

## High Priority

## Medium Priority
- [ ] Add AOT/standalone executable support (PublishSingleFile, self-contained)
- [ ] Implement load() function for dynamic code loading
- [ ] Design and implement structured error/warning system
- [ ] Improved error messages with line numbers
- [ ] Add IL.Emit backend for size optimization

## Low Priority
- [ ] Review parser ordering and overlapping conditions
- [ ] Add Lua bytecode backend
- [ ] Implement sandboxing configuration
- [ ] Fix _ENV = nil handling in modules
- [ ] Improved warning messages

## Notes
- Console app support helps with testing - we can now compile and run standalone programs
- All 24 compiler tests are passing in FLua.Compiler.Tests.Minimal (including inline functions)
- RoslynCodeGenerator is the preferred implementation (syntax factory based)
- CSharpCodeGenerator kept for reference but could be removed
- Control structures implemented: if/elseif/else, while, repeat/until, numeric for, generic for, break
- Assignment statements now support both local and non-local variables
- Unary operators implemented for proper handling of negative numbers in for loops
- Parser fixes enable full table support: assignment (t[1] = 100) and expressions (t[1] + t[2])
- Inline function expressions supported (function() ... end) with proper code generation
- Note: Closures not yet supported (accessing outer scope variables from inner functions)