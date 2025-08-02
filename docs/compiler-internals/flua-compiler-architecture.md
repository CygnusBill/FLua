# FLua Compiler Architecture

## Overview
This document describes the architecture of the FLua compiler, focusing on how Lua AST nodes are transformed into C# code using Roslyn.

## Key Components

### 1. AST Types (F#)
Located in `FLua.Ast/AstTypes.fs`, defines the Lua language structure:

```fsharp
type Expr =
    | Nil
    | Bool of bool
    | Number of double
    | Integer of int64
    | String of string
    | Var of string
    | FunctionCall of Expr * Expr list
    | TableConstructor of TableField list
    | TableAccess of Expr * Expr
    | BinaryOp of BinaryOp * Expr * Expr
    | UnaryOp of UnaryOp * Expr
    | FunctionDef of FunctionDef
    // ... more variants

type Statement =
    | Assignment of VarExpr list * Expr list
    | LocalDeclaration of string list * Expr list option
    | FunctionCall of Expr * Expr list
    | If of (Expr * Statement list) list * Statement list option
    | While of Expr * Statement list
    | For of string * Expr * Expr * Expr option * Statement list
    | Return of Expr list option
    // ... more variants
```

### 2. Code Generation Pipeline

#### RoslynCodeGenerator
The main code generator that transforms Lua AST to C# syntax trees.

Key methods:
- `GenerateExpression(Expr)` - Transforms Lua expressions to C# expressions
- `GenerateStatement(Statement)` - Transforms Lua statements to C# statements
- `GenerateFunctionCall()` - Handles Lua function calls
- `GenerateTableConstructor()` - Creates Lua tables

#### Code Generation Strategy

1. **Type Mapping**:
   - Lua values → `LuaValue` struct
   - Lua tables → `LuaTable` class
   - Lua functions → `BuiltinFunction` or `LuaUserFunction`

2. **Variable Management**:
   - Local variables → C# local variables with name mangling
   - Global variables → Environment lookups
   - Upvalues → Closure captures

3. **Control Flow**:
   - Lua conditions → `value.IsTruthy()` calls
   - Lua loops → C# loops with proper setup
   - Break statements → C# break (within loop context)

## Compilation Patterns

### Expression Compilation

#### Literals
```lua
-- Lua
42
"hello"
true
nil
```

```csharp
// Generated C#
LuaValue.Number(42)
LuaValue.String("hello")
LuaValue.Bool(true)
LuaValue.Nil
```

#### Binary Operations
```lua
-- Lua
a + b
a < b
a and b
```

```csharp
// Generated C#
LuaOperations.Add(a, b)
LuaOperations.LessThan(a, b)
LuaOperations.LogicalAnd(env, () => a, () => b)
```

#### Table Access
```lua
-- Lua
t[key]
t.field
```

```csharp
// Generated C#
t.AsTable<LuaTable>().Get(key)
t.AsTable<LuaTable>().Get(LuaValue.String("field"))
```

### Statement Compilation

#### Variable Assignment
```lua
-- Lua
local x = 10
y = 20
```

```csharp
// Generated C#
var x = LuaValue.Number(10);
env.SetVariable("y", LuaValue.Number(20));
```

#### Control Structures
```lua
-- Lua
if condition then
    -- body
end
```

```csharp
// Generated C#
if (condition.IsTruthy())
{
    // body
}
```

#### Function Definitions
```lua
-- Lua
local function f(a, b)
    return a + b
end
```

```csharp
// Generated C#
LuaValue[] f(LuaEnvironment env, params LuaValue[] args)
{
    var a = args.Length > 0 ? args[0] : LuaValue.Nil;
    var b = args.Length > 1 ? args[1] : LuaValue.Nil;
    return new[] { LuaOperations.Add(a, b) };
}
var f_func = LuaValue.Function(new BuiltinFunction(f));
```

### Advanced Patterns

#### Multiple Assignment
```lua
-- Lua
local a, b, c = f()
```

```csharp
// Generated C#
var _results = f();
var a = _results.Length > 0 ? _results[0] : LuaValue.Nil;
var b = _results.Length > 1 ? _results[1] : LuaValue.Nil;
var c = _results.Length > 2 ? _results[2] : LuaValue.Nil;
```

#### Numeric For Loop
```lua
-- Lua
for i = start, stop, step do
    -- body
end
```

```csharp
// Generated C#
var start_num = LuaTypeConversion.ToNumber(start) ?? 0.0;
var stop_num = LuaTypeConversion.ToNumber(stop) ?? 0.0;
var step_num = LuaTypeConversion.ToNumber(step) ?? 1.0;

for (double i_num = start_num; 
     (step_num > 0 && i_num <= stop_num) || (step_num < 0 && i_num >= stop_num); 
     i_num += step_num)
{
    var i = LuaValue.Number(i_num);
    env.SetVariable("i", i);
    // body
}
```

## Scope Management

### Variable Scoping
The compiler maintains a scope tree to handle variable shadowing:

```csharp
private class Scope
{
    public Dictionary<string, string> Variables { get; }
    public Scope? Parent { get; set; }
}
```

- Each block creates a new scope
- Variables are mangled to prevent conflicts: `var_1`, `var_2`, etc.
- Local variables are tracked in the current scope
- Global variables use environment lookups

### Name Resolution
1. Check if variable is in current scope
2. Walk up parent scopes
3. If not found, treat as global (environment lookup)

## Error Handling

### Compilation Errors
- Dynamic features (load, loadfile, dofile) → Compilation error
- Invalid syntax → Parser error (handled by F# parser)

### Runtime Errors
Generated code includes error handling:
- Nil checks for table access
- Type conversion validation
- Function call error propagation

## Optimization Opportunities

### Current Optimizations
1. **Local Variable Caching**: Local variables use direct C# variables
2. **Constant Folding**: Some literals are pre-computed
3. **Direct Method Calls**: Known functions use direct invocation

### Future Optimizations
1. **Type Inference**: Track variable types to avoid conversions
2. **Inline Caching**: Cache method lookups for table access
3. **Dead Code Elimination**: Remove unreachable code
4. **Loop Optimizations**: Specialized numeric loops

## Integration with Runtime

### LuaValue Operations
All value operations go through the runtime:
- `LuaOperations` - Binary/unary operations
- `LuaTypeConversion` - Type conversions
- `LuaMetamethods` - Metamethod dispatch

### Environment Management
- `LuaEnvironment` - Variable storage and lookup
- Supports lexical scoping
- Handles global environment (_G)

### Standard Libraries
Generated code can call into:
- Built-in functions (print, type, etc.)
- Standard libraries (string, table, math, etc.)
- User-defined functions

## Testing Strategy

### Compiler Tests
1. **Unit Tests**: Individual AST node compilation
2. **Integration Tests**: Complete Lua programs
3. **Compatibility Tests**: Official Lua test suite
4. **Performance Tests**: Benchmark against interpreter

### Test Categories (Lee Copeland)
- Boundary Value Analysis
- Equivalence Partitioning
- Decision Table Testing
- State Transition Testing
- Error Condition Testing

## Known Limitations

1. **No Closures Over Loop Variables**: Each iteration doesn't create new bindings
2. **No Weak Tables**: Not implemented
3. **No Coroutines in Compiled Code**: Interpreter only
4. **No Debug Library**: Limited debugging support
5. **No Dynamic Code Loading**: load/loadfile not supported