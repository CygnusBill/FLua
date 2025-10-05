# FLua CLI Tool

A command-line interface for FLua, a complete Lua 5.4 implementation for .NET.

## Installation

### .NET Tool (Recommended)
```bash
dotnet tool install --global flua --version 1.0.0-alpha.1
```

### Manual Installation
Download the appropriate binary for your platform and add it to your PATH.

## Usage

```bash
# Run a Lua script file
flua run myscript.lua

# Start interactive REPL
flua repl

# Compile Lua to .NET assembly
flua compile myscript.lua --output MyCompiled.dll

# Show help
flua --help
```

## Features

- **Complete Lua 5.4 support** - All language features implemented
- **Fast execution** - JIT-compiled for performance
- **Cross-platform** - Works on Windows, macOS, and Linux
- **Interactive REPL** - Test code snippets instantly
- **Compilation support** - Compile Lua to .NET assemblies
- **Standard library** - Full Lua standard library implementation

## Examples

### Running Scripts
```bash
# Hello world
echo 'print("Hello, FLua!")' > hello.lua
flua run hello.lua

# Complex script
flua run myapp.lua
```

### Interactive Mode
```bash
flua repl
> print("Hello from REPL!")
Hello from REPL!
> 2 + 3 * 4
14
> function factorial(n) return n <= 1 and 1 or n * factorial(n-1) end
> factorial(5)
120
```

### Compilation
```bash
# Compile to DLL
flua compile myscript.lua --output MyScript.dll

# Compile to executable
flua compile myscript.lua --output MyApp.exe --executable
```

## Dependencies

- .NET 8.0 or later
- All dependencies are bundled in the tool package

## License

MIT
