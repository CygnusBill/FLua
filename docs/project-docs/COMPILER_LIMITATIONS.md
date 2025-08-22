# FLua Compiler Limitations

This document outlines the known limitations of compiled Lua code versus interpreted code.

## Compiler Backend Support

### Supported Backends
1. **Roslyn Backend** (Only)
   - Generates C# source code
   - Better debugging experience
   - Complete feature support for AOT compilation

## Dynamic Code Loading

### load() Function
- **Status**: Not supported in compiled code
- **Error Code**: `FLU-1201`
- **Runtime Error**: Returns `nil, "dynamic loading not supported"`
- **Compile Warning**: `FLU-2201` - Using 'load' in compiled code will return an error at runtime
- **Reason**: Would require embedding the parser/interpreter or runtime compiler
- **Impact**: Low - mostly used in REPLs and debugging tools
- **Workaround**: Use the interpreter for dynamic code execution

### loadfile() Function
- **Status**: Not supported in compiled code
- **Error Code**: `FLU-1201`
- **Runtime Error**: Returns `nil, "dynamic loading not supported"`
- **Compile Warning**: `FLU-2201` - Using 'loadfile' in compiled code will return an error at runtime
- **Reason**: Same as load() - requires runtime compilation
- **Impact**: Low - can pre-compile all needed files
- **Workaround**: Compile all Lua files ahead of time

### dofile() Function
- **Status**: Not supported in compiled code
- **Error Code**: `FLU-1201`
- **Runtime Error**: Throws "dynamic loading not supported"
- **Compile Warning**: `FLU-2201` - Using 'dofile' in compiled code will return an error at runtime
- **Reason**: Same as load() - requires runtime compilation
- **Impact**: Low to Medium - some scripts use dofile for configuration
- **Workaround**: Use require() with pre-compiled modules

## Module System

### require() Function
- **Status**: Limited support
- **Limitation**: Can only load pre-compiled modules, not .lua source files
- **Workaround**: Compile all modules before deployment

## Debug Library

### debug.* Functions
- **Status**: Partially supported
- **Limitation**: No source-level debugging, limited stack introspection
- **Reason**: Compiled code lacks source mapping and full debug info

## Environment Manipulation

### _ENV Manipulation
- **Status**: Limited support
- **Limitation**: Cannot dynamically change global environment of compiled functions
- **Reason**: Global access is compiled to direct calls

## String Compilation

### String-based eval patterns
- **Status**: Not supported
- **Example**: `load("return " .. expr)()` won't work
- **Workaround**: Use proper expressions instead of string building

## Current Implementation Status (Roslyn Backend)

### Implemented Features
- ✅ Print statements
- ✅ Variable assignments (local and global)
- ✅ Binary operations (arithmetic, comparison, logical)
- ✅ If/elseif/else statements
- ✅ While loops (bug: infinite loop with local variables)
- ✅ Repeat/until loops
- ✅ Numeric for loops
- ✅ Break statements
- ✅ Do blocks
- ✅ Function calls
- ✅ Console application support

### Not Yet Implemented
- ❌ Table support (constructors, access, methods)
- ❌ Function definitions
- ❌ Generic for loops
- ❌ Method calls (`:` syntax)
- ❌ Unary operators
- ❌ Varargs
- ❌ Closures
- ❌ Metatables
- ❌ Coroutines
- ❌ Error handling (pcall/xpcall)

### Known Bugs
- While/repeat loops with local variable conditions cause infinite loops
- Local variable assignments inside loops don't update the outer variable
- Some edge cases in numeric for loops

## Numeric Type Representation

### Current Status
- All numeric values use `LuaValue` objects (specifically `LuaInteger` and `LuaNumber`)
- This ensures compatibility with Lua's numeric semantics but has performance overhead

### Future Optimization Considerations
1. **Loop Counters**: When we can prove bounds, use native int32/int64
2. **Overflow Concerns**: Lua numbers can exceed int32/int64 range
   - Lua uses double-precision floating point (53-bit integer precision)
   - Loop counters could theoretically overflow int32 (±2.1 billion)
   - Need type inference or runtime checks for safety
3. **Type Choices**:
   - **Safe but slow**: Always use `LuaValue` objects (current approach)
   - **Fast but limited**: Use int32 with overflow checks
   - **Balanced**: Use int64 for integers, double for floats
   - **Complex**: Type inference to choose optimal representation
4. **BigInteger Option**: Would handle all cases but very wasteful for typical usage

### Recommendations
- Phase 1: Fix correctness issues with current `LuaValue` approach
- Phase 2: Add optimization passes for provable integer loops
- Phase 3: Implement type inference for broader optimizations

## Future Considerations

These limitations are by design to keep compiled binaries small and fast. For use cases requiring these features, use the interpreter or consider hybrid approaches where critical paths are compiled and dynamic features use the interpreter.

The Roslyn backend prioritizes feature completeness and debugging experience. For applications requiring full Lua compatibility, use the interpreter or continue developing the Roslyn backend.