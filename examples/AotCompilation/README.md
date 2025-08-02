# AOT Compilation Example

This example demonstrates compiling Lua scripts into standalone native executables using .NET's Native AOT technology.

## What is AOT Compilation?

AOT (Ahead-of-Time) compilation transforms Lua scripts into native machine code executables that:
- Run without .NET installed
- Start instantly (no JIT compilation)
- Deploy as single files
- Work like any native application

## Key Features Demonstrated

- Compiling Lua to native executables
- Command-line argument handling
- Exit code support
- Size and performance characteristics
- Platform-specific compilation

## How It Works

1. **Lua → C#**: First, Lua is compiled to C# code
2. **C# → IL**: Roslyn compiles C# to IL bytecode  
3. **IL → Native**: .NET Native AOT compiles to machine code
4. **Single File**: Everything bundled into one executable

## Use Cases

### CLI Tools
```lua
-- backup.lua
local source = arg[1] or error("Source directory required")
local dest = arg[2] or error("Destination required")
-- ... backup logic ...
```
Compile: `flua compile backup.lua -t NativeAot -o backup`
Use: `./backup /home/user /backup/location`

### System Utilities
```lua
-- monitor.lua
while true do
    local cpu = os.clock()
    print(string.format("CPU: %.2f%%", cpu))
    os.execute("sleep 1")
end
```

### Game Launchers
```lua
-- launcher.lua  
print("Starting game...")
os.execute("./game-engine --config=settings.ini")
```

## Platform Considerations

| Platform | Extension | Size | Notes |
|----------|-----------|------|-------|
| Windows  | .exe      | ~3MB | Requires VS Build Tools |
| Linux    | (none)    | ~2MB | Works on most distros |
| macOS    | (none)    | ~2MB | Universal or arch-specific |

## Performance

- **Startup**: 10-50ms (vs 100-200ms for JIT)
- **Runtime**: Same as JIT after warmup
- **Memory**: Lower baseline usage
- **Size**: 2-3MB typical executable

## Limitations

- No `load()` or `loadfile()` (no dynamic code)
- Platform-specific binaries needed
- Longer compilation time (30-60 seconds)
- Larger than script + interpreter

## Running the Example

```bash
dotnet run
```

This will:
1. Create a Fibonacci calculator script
2. Compile it to a native executable
3. Run the executable with arguments
4. Show the results

## Advanced Usage

### Cross-compilation
```bash
# On macOS, compile for Linux
flua compile script.lua -t NativeAot -r linux-x64

# On Linux, compile for Windows  
flua compile script.lua -t NativeAot -r win-x64
```

### Optimization
```bash
# Release mode for smaller size
flua compile script.lua -t NativeAot -O Release

# Include debug info
flua compile script.lua -t NativeAot --include-debug
```