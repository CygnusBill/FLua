# FLua.Common

Common utilities and infrastructure for FLua, a complete Lua 5.4 implementation for .NET.

This package provides shared functionality used across all FLua components, including diagnostics, error handling, and utility types.

## Features

- Diagnostic and error reporting system
- Result pattern implementation
- Source location tracking
- Common data structures and utilities
- Diagnostic collectors and formatters

## Usage

```csharp
using FLua.Common.Diagnostics;

// Create a diagnostic
var diagnostic = new FLuaDiagnostic(
    ErrorCodes.ParseError,
    ErrorSeverity.Error,
    new SourceLocation("file.lua", 10, 5, 8),
    "Unexpected token"
);

// Use result pattern
var result = Result.Success("parsed successfully");
```

## Dependencies

None - this is a foundational package.

## License

MIT
