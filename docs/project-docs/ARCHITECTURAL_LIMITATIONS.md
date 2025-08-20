# FLua Architectural Limitations

## Overview
This document outlines the current architectural limitations of the FLua implementation. These are design constraints that would require significant refactoring to address.

## Expression Tree Compilation Limitations

### Function Definitions Not Supported
**Status**: Known Limitation  
**Impact**: Expression tree compilation cannot handle function definitions within the expression.

**Description**: 
The expression tree compiler (`CompileToExpression`) cannot compile Lua code that contains function definitions. This is because .NET expression trees have fundamental limitations on what can be represented - they cannot define new methods at runtime.

**Example of Failing Code**:
```lua
-- This will fail expression tree compilation
local function factorial(n)
    if n <= 1 then return 1 end
    return n * factorial(n - 1)
end
return factorial(5)
```

**Workaround**: 
- Use the standard compilation mode (`CompileToLambda` or `CompileToAssembly`) instead
- Pre-define functions in the host environment and inject them
- Use the interpreter for dynamic function creation

**Tests Affected**:
- `CompileToExpression_ComplexCalculation_EvaluatesCorrectly` 
- `CompileToExpression_TableOperations_WorksWithTables`

### Limited Table Constructor Support
**Status**: Partially Implemented  
**Impact**: Complex table constructors may not work in expression trees.

**Description**:
Table constructors in expression trees are limited to simple key-value pairs. Dynamic table construction with computed keys or complex nested structures may not compile correctly.

## Module System Limitations

### Module Compilation with Closures
**Status**: Architectural Constraint  
**Impact**: Modules that capture local variables from their enclosing scope cannot be compiled.

**Description**:
When a module captures local variables from outside its definition (creating a closure), the compiler cannot generate a standalone compiled module. This is because the compiled module would need to maintain references to the captured variables, which is complex to implement in the current architecture.

**Example**:
```lua
-- In main script
local sharedState = 0

-- This module captures sharedState
local module = require('module_with_closure')
-- If module_with_closure.lua uses sharedState, compilation will fail
```

**Workaround**:
- Pass state explicitly to module functions rather than capturing
- Use module-level state instead of captured variables
- Fall back to interpreter for modules with closures

## Security Model Limitations

### Sandbox Path Restrictions
**Status**: Fixed (December 2025)  
**Previous Issue**: Module resolver was too restrictive for sandbox trust level.

**Resolution**: 
Sandbox trust level now allows loading modules from any explicitly configured search path, rather than requiring specific directory names.

## Interpreter Environment Limitations

### No Public Constructor for Custom Environment
**Status**: Design Issue  
**Impact**: Cannot easily create interpreter instances with custom environments.

**Description**:
The `LuaInterpreter` class doesn't have a public constructor that accepts a `LuaEnvironment` parameter. This forces the use of reflection to set the environment for module execution.

**Workaround**:
Currently using reflection to set the `_environment` field. Should be addressed by adding a proper constructor.

## Compilation Target Limitations

### No Bytecode Support
**Status**: By Design  
**Impact**: Cannot load or save Lua bytecode files.

**Description**:
FLua compiles to .NET IL or expression trees, not Lua bytecode. This means:
- Cannot load standard Lua bytecode files
- Cannot produce Lua-compatible bytecode
- No compatibility with luac compiled files

**Benefits of This Approach**:
- Better .NET integration
- Type safety when possible
- Better performance in .NET environment
- Easier debugging with .NET tools

## Performance Considerations

### Module Compilation Overhead
**Status**: Optimized with Caching  
**Impact**: First load of a module may be slow.

**Description**:
Compiling modules to IL has overhead on first load. This is mitigated by:
- Module caching system
- Falling back to interpreter for small modules
- Pre-compilation where possible

## Future Improvements

1. **Add LuaInterpreter constructor** that accepts environment
2. **Improve expression tree support** for more Lua constructs
3. **Better closure analysis** for module compilation
4. **Bytecode compatibility layer** (if needed)
5. **Incremental compilation** for large modules

## Test Status

As of December 2025:
- **Hosting Tests**: 94 passing, 2 failing (expression tree limitations), 14 skipped
- **Module Tests**: All passing after fixes
- **Overall Project**: ~98% tests passing

The 2 failing tests are due to documented expression tree limitations and should be marked as expected failures or rewritten to test within the constraints.