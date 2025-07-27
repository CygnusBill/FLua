# FLua Error and Warning System Design

## Overview

This document outlines the design for a structured error and warning system for FLua that will provide consistent, actionable error messages across the parser, interpreter, and compiler.

## Goals

1. **Consistent Error Format**: All components should produce errors in the same format
2. **Error Codes**: Each error type should have a unique code for easy reference
3. **Source Location**: Errors should include file, line, and column information
4. **Context**: Errors should show the relevant source code with highlighting
5. **Suggestions**: Where possible, provide hints for fixing the error
6. **Severity Levels**: Support errors, warnings, and informational messages
7. **Error Recovery**: Parser should continue after errors to find multiple issues

## Error Code Structure

Error codes follow the pattern: `FLU-XYZZ` where:

- **X** = Severity
  - `1` = Error (prevents execution)
  - `2` = Warning (potential issue)
  - `3` = Info (informational)
  - `4` = Hint (suggestion)

- **Y** = Area
  - `0` = Parser
  - `1` = Runtime
  - `2` = Compiler
  - `3` = Type system
  - `4` = Module/require
  - `5` = Built-in libraries
  - `9` = Internal

- **ZZ** = Sequential number (01-99)

Examples:
- `FLU-1001`: Error in Parser (Unexpected token)
- `FLU-2201`: Warning in Compiler (Dynamic feature used)
- `FLU-4101`: Hint for Runtime (Use local variable)

## Error Message Format

```
error[FLU-0001]: unexpected token
  --> script.lua:12:15
   |
12 | local x = 10 +
   |               ^ expected expression after '+'
   |
   = help: add an expression after the operator
```

## Severity Levels

1. **Error**: Prevents execution/compilation
2. **Warning**: Potential issue but execution continues
3. **Info**: Informational message
4. **Hint**: Suggestion for improvement

## Implementation Components

### 1. Error Types

```csharp
public enum ErrorSeverity
{
    Error,
    Warning,
    Info,
    Hint
}

public class SourceLocation
{
    public string FileName { get; set; }
    public int Line { get; set; }
    public int Column { get; set; }
    public int Length { get; set; }
}

public class FLuaDiagnostic
{
    public string Code { get; set; }
    public ErrorSeverity Severity { get; set; }
    public string Message { get; set; }
    public SourceLocation Location { get; set; }
    public string? Help { get; set; }
    public string? SourceContext { get; set; }
}
```

### 2. Error Collection

```csharp
public interface IDiagnosticCollector
{
    void Report(FLuaDiagnostic diagnostic);
    IReadOnlyList<FLuaDiagnostic> GetDiagnostics();
    bool HasErrors { get; }
    void Clear();
}
```

### 3. Context Providers

- Parser context: Track current parsing state
- Runtime context: Track call stack and variable scopes
- Compiler context: Track compilation phase and current construct

## Common Error Examples

### Parser Errors (1-0-xx)
- `FLU-1001`: Encountered '{' while parsing function call. Expected '(' or string.
- `FLU-1002`: Missing closing ')'. The '(' opened here needs to be closed.
- `FLU-1003`: 'then' is not a valid expression in assignment.
- `FLU-1004`: Cannot use 'end' here. 'end' is a reserved word that can only be used to close blocks.
- `FLU-1005`: '123abc' is not a valid number. Numbers should be like 123, 3.14, or 0xFF.

### Runtime Errors (1-1-xx)
- `FLU-1101`: Attempted to index a nil value. Check that the table exists before accessing it.
- `FLU-1102`: Cannot concatenate: expected string but got number.
- `FLU-1103`: 'printt' is not defined. Did you forget to declare it with 'local printt'?
- `FLU-1104`: Stack overflow in function call. Check for infinite recursion.
- `FLU-1105`: Cannot perform arithmetic on table. This operation is not supported for this type.

### Compiler Errors (1-2-xx)
- `FLU-1201`: Dynamic code loading (load/loadfile/dofile) is not supported in compiled executables.
- `FLU-1202`: Failed to compile table constructor: invalid syntax.
- `FLU-1203`: 'WebAssembly' is not a valid compilation target.
- `FLU-1204`: Runtime library 'FLua.Runtime.dll' not found.

### Compiler Warnings (2-2-xx)
- `FLU-2201`: Using 'load' in compiled code will return an error at runtime.
- `FLU-2202`: This loop could be optimized by moving invariant expressions outside.
- `FLU-2203`: Code uses features that may not work in all runtime environments.
- `FLU-2204`: Variable 'temp' is defined but never used.
- `FLU-2205`: Variable 'x' shadows a previous declaration at line 10.

## Integration Points

1. **Parser**: Modify FParsec error handling to produce structured errors
2. **Interpreter**: Wrap exceptions with diagnostic information
3. **Compiler**: Add diagnostic collection during code generation
4. **CLI**: Format and display diagnostics appropriately

## Future Enhancements

1. **Error Recovery**: Parser continues after errors
2. **Quick Fixes**: Automated corrections for common issues
3. **IDE Integration**: Language Server Protocol support
4. **Error Suppression**: Pragma/comment-based suppression
5. **Custom Error Handlers**: User-defined error formatting