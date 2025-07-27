# FLua Compiler Limitations

This document outlines the known limitations of compiled Lua code versus interpreted code.

## Dynamic Code Loading

### load() Function
- **Status**: Not supported in compiled code
- **Error**: Returns `nil, "dynamic loading not supported"`
- **Reason**: Would require embedding the parser/interpreter or runtime compiler
- **Impact**: Low - mostly used in REPLs and debugging tools
- **Workaround**: Use the interpreter for dynamic code execution

### loadfile() Function
- **Status**: Not supported in compiled code
- **Error**: Returns `nil, "dynamic loading not supported"`
- **Reason**: Same as load() - requires runtime compilation
- **Impact**: Low - can pre-compile all needed files
- **Workaround**: Compile all Lua files ahead of time

### dofile() Function
- **Status**: Not supported in compiled code
- **Error**: Throws "dynamic loading not supported"
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

## Future Considerations

These limitations are by design to keep compiled binaries small and fast. For use cases requiring these features, use the interpreter or consider hybrid approaches where critical paths are compiled and dynamic features use the interpreter.