# FLua Compiler Limitations

This document outlines the known limitations of compiled Lua code versus interpreted code.

## Compiler Backend Support

### Supported Backends
1. **Mono.Cecil Backend** (Default)
   - Direct IL generation
   - 16MB executables (77% smaller than Roslyn)
   - Faster compilation
   - Currently implements ~30% of Lua features

2. **Roslyn Backend**
   - Generates C# source code
   - 70MB executables
   - Better debugging experience
   - More complete feature support

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

## Current Implementation Status (Cecil Backend)

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
- Some edge cases in numeric for loops

## Future Considerations

These limitations are by design to keep compiled binaries small and fast. For use cases requiring these features, use the interpreter or consider hybrid approaches where critical paths are compiled and dynamic features use the interpreter.

The Cecil backend prioritizes executable size and compilation speed over feature completeness. For applications requiring full Lua compatibility, use the interpreter or Roslyn backend.