# FLua Publishing Script

This script automates the process of publishing FLua REPL and CLI as AOT-compiled, self-contained executables.

## Usage

```bash
./publish.sh [runtime|--all] [configuration] [output_dir]
```

## Arguments

- **runtime**: Target runtime platform (default: `osx-arm64`)
- **--all**: Build for all supported runtimes
- **configuration**: Build configuration (default: `Release`)
- **output_dir**: Output directory (default: `./publish`)

## Supported Runtimes

- `osx-arm64` - macOS Apple Silicon (M1/M2/M3)
- `osx-x64` - macOS Intel
- `linux-x64` - Linux x64
- `linux-arm64` - Linux ARM64
- `win-x64` - Windows x64
- `win-arm64` - Windows ARM64

## Examples

```bash
# Use defaults (macOS ARM64, Release build)
./publish.sh

# Build for all supported runtimes
./publish.sh --all

# Build all runtimes in Debug mode
./publish.sh --all Debug

# Linux x64 build
./publish.sh linux-x64

# Windows x64 Debug build
./publish.sh win-x64 Debug

# macOS Intel with custom output directory
./publish.sh osx-x64 Release ./dist

# Show help
./publish.sh --help
```

## Features

- ✅ **AOT Compilation**: Creates native executables with fast startup
- ✅ **Self-Contained**: No .NET runtime required on target machine
- ✅ **Multi-Platform**: Supports all major platforms
- ✅ **Colored Output**: Easy to read status messages
- ✅ **Error Handling**: Validates inputs and handles errors gracefully
- ✅ **Size Reporting**: Shows executable sizes after compilation
- ✅ **Optional Testing**: Prompts to test the REPL after publishing (single runtime only)
- ✅ **Batch Building**: `--all` flag builds for all supported platforms at once

## Output Structure

### Single Runtime Build
```
./publish/
├── Repl/
│   ├── FLua.Repl           # Main REPL executable
│   ├── FLua.Repl.dSYM/     # Debug symbols (macOS)
│   └── [support files]
└── Cli/
    ├── FLua.Cli            # Main CLI executable
    ├── FLua.Cli.dSYM/      # Debug symbols (macOS)
    └── [support files]
```

### Multi-Runtime Build (--all flag)
```
./publish/
├── osx-arm64/
│   ├── Repl/FLua.Repl
│   └── Cli/FLua.Cli
├── osx-x64/
│   ├── Repl/FLua.Repl
│   └── Cli/FLua.Cli
├── linux-x64/
│   ├── Repl/FLua.Repl
│   └── Cli/FLua.Cli
├── linux-arm64/
│   ├── Repl/FLua.Repl
│   └── Cli/FLua.Cli
├── win-x64/
│   ├── Repl/FLua.Repl.exe
│   └── Cli/FLua.Cli.exe
└── win-arm64/
    ├── Repl/FLua.Repl.exe
    └── Cli/FLua.Cli.exe
```

## Typical File Sizes

- **Executable**: ~4.3MB (self-contained, no dependencies)
- **Debug Symbols**: ~16MB (can be stripped for distribution)

## Requirements

- .NET SDK (version 10.0 or later)
- Target platform toolchain (for cross-compilation)

## Notes

- The script automatically cleans the output directory before publishing
- All executables are optimized for size and performance
- Cross-compilation may require additional setup depending on the target platform
- Debug symbols are included but can be removed to reduce distribution size 