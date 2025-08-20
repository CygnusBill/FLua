# FLua Publishing Script

This script automates the process of publishing FLua CLI (with integrated REPL) as AOT-compiled, self-contained executables for multiple platforms.

## Usage

```bash
./publish.sh [command]
```

## Commands

- **(no arguments)**: Full build and publish for all supported platforms
- **clean**: Clean previous builds only
- **test**: Build and test only (no publishing)

## Supported Runtimes

- `linux-x64` - Linux x64
- `linux-arm64` - Linux ARM64
- `osx-x64` - macOS Intel
- `osx-arm64` - macOS Apple Silicon (M1/M2/M3)
- `win-x64` - Windows x64
- `win-arm64` - Windows ARM64

## Examples

```bash
# Full publish for all platforms (recommended)
./publish.sh

# Clean previous builds
./publish.sh clean

# Build and test only
./publish.sh test
```

## Features

- ✅ **Multi-Platform**: Automatically builds for all major platforms
- ✅ **AOT Compilation**: Creates native executables with fast startup
- ✅ **Self-Contained**: No .NET runtime required on target machine
- ✅ **Optimized**: Uses PublishAot, trimming, and symbol stripping for minimal size
- ✅ **Package Creation**: Automatically creates distribution packages
- ✅ **Progress Tracking**: Colored output with clear progress indicators
- ✅ **Error Handling**: Comprehensive validation and error recovery
- ✅ **Testing**: Includes automated testing of published executables

## Output Structure

After running `./publish.sh`, you'll get:

```
./publish/
├── linux-x64/
│   └── flua              # Linux x64 executable
├── linux-arm64/
│   └── flua              # Linux ARM64 executable
├── osx-x64/
│   └── flua              # macOS Intel executable
├── osx-arm64/
│   └── flua              # macOS Apple Silicon executable
├── win-x64/
│   └── flua.exe          # Windows x64 executable
├── win-arm64/
│   └── flua.exe          # Windows ARM64 executable
└── packages/
    ├── flua-linux-x64.tar.gz
    ├── flua-linux-arm64.tar.gz
    ├── flua-osx-x64.tar.gz
    ├── flua-osx-arm64.tar.gz
    ├── flua-win-x64.tar.gz
    └── flua-win-arm64.tar.gz
```

## Usage of Published Binaries

The `flua` executable combines both REPL and script execution functionality:

### Interactive REPL Mode
```bash
# Start interactive REPL (no arguments)
./flua

lua> 1 + 2 * 3
= 7
lua> print("Hello, World!")
Hello, World!
lua> .quit
```

### Script Execution Mode
```bash
# Execute a Lua script file
./flua script.lua

# Show help
./flua --help

# Show version
./flua --version

# Verbose script execution
./flua --verbose script.lua
```

## Typical File Sizes

- **Executable**: ~3-5MB per platform (self-contained, no dependencies)
- **Package**: ~1-2MB compressed (varies by platform)

## Requirements

- .NET SDK (version 10.0 or later)
- `tar` command (for package creation)
- `git` command (optional, for version information)

## Distribution

The script creates both individual executables and compressed distribution packages:

1. **Individual Executables**: Use the `publish/[platform]/flua` files directly
2. **Distribution Packages**: Use the `publish/packages/flua-[platform].tar.gz` files for easy distribution

## Platform-Specific Notes

- **macOS**: Executables may need to be signed for distribution
- **Windows**: `.exe` extension is automatically added
- **Linux**: Executables have no extension, mark as executable with `chmod +x`

## Example Distribution

```bash
# Extract package on target system
tar -xzf flua-linux-x64.tar.gz
cd linux-x64

# Make executable (Linux/macOS)
chmod +x flua

# Test installation
./flua --version
./flua --help

# Start REPL
./flua
``` 