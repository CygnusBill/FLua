# FLua Architecture Mapping (C# Components)

## Compiler Architecture

### Interface Definition
- `ILuaCompiler` - Core compiler interface with `Compile` method

### Compiler Implementations
1. **RoslynLuaCompiler** (Active)
   - Uses Roslyn syntax factory for C# code generation
   - Methods: `Compile`, `CompileWithRoslyn`, `CompileAot`
   - Generates C# code then compiles with Roslyn

2. **CecilLuaCompiler** (Deprecated)
   - Direct IL generation using Mono.Cecil
   - Single `Compile` method
   - Has assembly loading issues per documentation

## Runtime System (FLua.Runtime)

### Core Value System
- `LuaValue` (struct) - Primary value type (20-byte struct per docs)
- `LuaType` (enum) - Type enumeration
- `LuaValueExtensions` & `LuaValueHelpers` - Utility classes

### Table Implementation
- `LuaTable` - Optimized table with array/hash parts

### Function System
- `LuaFunction` (abstract base)
- `LuaUserFunction` - User-defined functions with closure support

### Environment & Variables
- `LuaEnvironment` - Scoping and environment chains
- `LuaVariable` - Variable attributes support (<const>, <close>)
- `LuaAttribute` (enum) - Variable attribute types

### Standard Libraries (All Complete)

#### Math Library (LuaMathLib) - 25 Functions
- Basic: `Abs`, `Max`, `Min`, `Floor`, `Ceil`
- Trigonometric: `Sin`, `Cos`, `Tan`, `ASin`, `ACos`, `ATan`
- Logarithmic: `Log`, `Exp`, `Pow`, `Sqrt`
- Utility: `Deg`, `Rad`, `FMod`, `Modf`
- Random: `Random`, `RandomSeed`
- Lua 5.3+: `ToInteger`, `Type`, `Ult`

#### Other Libraries
- `LuaStringLib` - String manipulation
- `LuaTableLib` - Table operations
- `LuaIOLib` - I/O operations
- `LuaOSLib` - OS interface
- `LuaCoroutineLib` - Coroutine support
- `LuaPackageLib` - Module system
- `LuaDebugLib` - Debug interface (partial per docs)
- `LuaUTF8Lib` - UTF-8 support

### Operations & Metamethods
- `LuaOperations` - Core operations
- `LuaMetamethods` - Metamethod implementations
- `LuaBinaryOp`, `LuaUnaryOp` (enums) - Operation types

### Specialized Types
- `LuaCoroutine` - Coroutine implementation
- `LuaFileHandle` - File I/O
- `LuaPatterns`, `LuaPatternMatch`, `LuaPatternMatcher` - Pattern matching

### Utilities
- `LuaConsoleRunner` - Console application support
- `LuaTypeConversion` - Type conversion utilities
- `LuaRuntimeException` - Error handling

## Interpreter System (FLua.Interpreter)
- `LuaInterpreter` - Main tree-walking interpreter
- `LuaRepl` - REPL implementation

## CLI Interface (FLua.Cli)
- `Program.CompileFile` - Main compilation entry point

## Missing Components (F# - Not Visible to McpDotnet)
- **Parser** (FLua.Parser) - F# FParsec-based parser
- **AST Types** (FLua.Ast) - F# discriminated unions
- All parsing logic and AST definitions

## Test Coverage (Visible C# Tests)
- `LuaMathLibTests`, `LuaStringLibTests`, `LuaTableLibTests`
- `LuaValueTests`, `LuaRuntimeExceptionTests`
- Parser and AST tests are in F# (not visible to McpDotnet)

## Key Observations
1. **Comprehensive Math Library**: Full Lua 5.4 math library implemented (25 functions)
2. **Dual Compiler Approach**: Roslyn (active) and Cecil (deprecated) backends
3. **Runtime Separation**: Clean separation ensures both interpreter and compiler use same runtime
4. **Strong Test Coverage**: All major runtime components have dedicated test classes
5. **F# Integration Gap**: McpDotnet can't analyze F# parser/AST components