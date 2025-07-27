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

Error codes follow the pattern: `FLU-XXXX`

- `FLU-0xxx`: Parser errors
- `FLU-1xxx`: Runtime errors
- `FLU-2xxx`: Compiler errors
- `FLU-3xxx`: Type-related errors
- `FLU-4xxx`: Module/require errors
- `FLU-5xxx`: Built-in library errors
- `FLU-9xxx`: Internal errors

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

## Common Error Categories

### Parser Errors
- `FLU-0001`: Unexpected token
- `FLU-0002`: Missing closing delimiter
- `FLU-0003`: Invalid expression
- `FLU-0004`: Reserved word misuse
- `FLU-0005`: Invalid number format

### Runtime Errors
- `FLU-1001`: Nil value access
- `FLU-1002`: Type mismatch
- `FLU-1003`: Unknown variable
- `FLU-1004`: Stack overflow
- `FLU-1005`: Invalid operation

### Compiler Errors
- `FLU-2001`: Unsupported feature in compiled code
- `FLU-2002`: Code generation failure
- `FLU-2003`: Invalid compilation target
- `FLU-2004`: Missing runtime dependency

### Compiler Warnings
- `FLU-2501`: Dynamic feature used (e.g., load())
- `FLU-2502`: Performance warning
- `FLU-2503`: Potential runtime incompatibility

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