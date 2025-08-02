# AOT Compilation Example

This example demonstrates compiling Lua scripts into standalone native executables using .NET's Native AOT technology.

## Overview

AOT (Ahead-of-Time) compilation transforms Lua scripts into native machine code executables that:
- Run without .NET runtime installed
- Start instantly (no JIT compilation)
- Deploy as single files
- Work on any compatible OS

## Understanding AOT Compilation

### Traditional Execution Flow
```
Lua Script → Parser → Interpreter → Results
                         ↓
                    Requires .NET
```

### AOT Compilation Flow
```
Lua Script → Parser → C# Code → Roslyn → IL → Native AOT → Machine Code
                                                     ↓
                                              Standalone EXE
```

## Code Walkthrough

### Step 1: Creating the Lua Script

```lua
-- Fibonacci calculator
local function fibonacci(n)
    if n <= 1 then
        return n
    end
    local a, b = 0, 1
    for i = 2, n do
        a, b = b, a + b
    end
    return b
end

-- Command line argument handling
local n = tonumber(arg and arg[1]) or 10
```

Key points:
- Standard Lua code works as-is
- `arg` table contains command-line arguments
- Return value becomes exit code

### Step 2: The Compilation Process

```csharp
var compileProcess = new Process
{
    StartInfo = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"run --project FLua.Cli -- compile \"{scriptPath}\" -t NativeAot -o \"{outputPath}\""
    }
};
```

The compilation steps:
1. **Parse**: Convert Lua to AST
2. **Generate**: Create C# code from AST
3. **Compile**: Use Roslyn to create IL
4. **AOT Compile**: Use .NET Native AOT to create machine code
5. **Link**: Create standalone executable

### Step 3: Generated C# Structure

The Lua script becomes approximately:

```csharp
using System;
using FLua.Runtime;

public class LuaScript
{
    public static int Main(string[] args)
    {
        try
        {
            var runner = new LuaConsoleRunner();
            return runner.Run(Execute, args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }
    
    public static LuaValue[] Execute(LuaEnvironment env)
    {
        // Fibonacci function implementation
        env.SetLocalVariable("fibonacci", new LuaUserFunction(...));
        
        // Get command line argument
        var arg = env.GetVariable("arg").AsTable();
        var n = arg.Get(1).AsDouble();
        
        // Calculate and print results
        // ...
        
        return new[] { LuaValue.Number(0) }; // Exit code
    }
}
```

### Step 4: The AOT Project Structure

AOT compilation creates a temporary .NET project:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <PublishAot>true</PublishAot>
    <StripSymbols>true</StripSymbols>
    <IlcGenerateCompleteTypeMetadata>false</IlcGenerateCompleteTypeMetadata>
    <IlcOptimizationPreference>Size</IlcOptimizationPreference>
  </PropertyGroup>
  
  <ItemGroup>
    <Reference Include="FLua.Runtime.dll" />
    <TrimmerRootAssembly Include="FLua.Runtime" />
  </ItemGroup>
</Project>
```

Key settings:
- `PublishAot`: Enables native compilation
- `StripSymbols`: Reduces size
- `IlcOptimizationPreference`: Size vs Speed tradeoff
- `TrimmerRootAssembly`: Preserves FLua runtime

### Step 5: Compilation Time Breakdown

```
Total time: ~30-60 seconds

1. Lua → C# Generation: ~100ms
2. C# → IL Compilation: ~500ms
3. IL → Native Code: ~25-50s
4. Linking & Optimization: ~5-10s
```

The native compilation is slow because it:
- Analyzes entire call graph
- Generates optimized machine code
- Removes unused code (tree shaking)
- Creates self-contained binary

### Step 6: Executable Characteristics

```bash
# Size comparison
script.lua:        500 bytes   # Original
script.dll:        10 KB       # JIT compiled
script.exe:        2-3 MB      # AOT compiled

# Startup time
Interpreted:       150ms
JIT compiled:      100ms
AOT compiled:      10ms
```

### Step 7: Platform-Specific Compilation

```bash
# Compile for current platform
flua compile script.lua -t NativeAot

# Cross-compile for other platforms
flua compile script.lua -t NativeAot -r linux-x64
flua compile script.lua -t NativeAot -r win-x64
flua compile script.lua -t NativeAot -r osx-arm64
```

Runtime identifiers:
- `win-x64`: Windows 64-bit
- `linux-x64`: Linux 64-bit
- `linux-arm64`: Linux ARM64 (Raspberry Pi)
- `osx-x64`: macOS Intel
- `osx-arm64`: macOS Apple Silicon

### Step 8: Limitations and Workarounds

AOT limitations:
1. **No Dynamic Loading**: `load()`, `loadfile()` won't work
   ```lua
   -- Won't work in AOT:
   local func = load("return 42")
   
   -- Workaround: Use static functions
   local function getAnswer() return 42 end
   ```

2. **Limited Reflection**: Some runtime features unavailable
   ```lua
   -- Limited in AOT:
   debug.getinfo()
   
   -- Workaround: Design without reflection
   ```

3. **Fixed Functionality**: Can't add features at runtime
   ```lua
   -- Won't work:
   require('new_module')  -- No dynamic requires
   
   -- Workaround: Include all code at compile time
   ```

### Step 9: Optimization Techniques

#### Size Optimization
```csharp
var compilerOptions = new CompilerOptions
{
    Target = CompilationTarget.NativeAot,
    Optimization = OptimizationLevel.Release,
    // Additional flags for size
    AdditionalCompilerFlags = new[]
    {
        "-p:IlcOptimizationPreference=Size",
        "-p:StackTraceSupport=false",
        "-p:InvariantGlobalization=true"
    }
};
```

Results in ~1.5MB executables

#### Performance Optimization
```csharp
var compilerOptions = new CompilerOptions
{
    Target = CompilationTarget.NativeAot,
    Optimization = OptimizationLevel.Release,
    AdditionalCompilerFlags = new[]
    {
        "-p:IlcOptimizationPreference=Speed",
        "-p:IlcGenerateCompleteTypeMetadata=true"
    }
};
```

Faster but larger (~3MB)

### Step 10: Real-World Applications

#### CLI Tool Example
```lua
-- backup.lua
local source = arg[1] or error("Usage: backup <source> <destination>")
local dest = arg[2] or error("Usage: backup <source> <destination>")

local function copyFile(src, dst)
    local input = io.open(src, "rb")
    local output = io.open(dst, "wb")
    output:write(input:read("*a"))
    input:close()
    output:close()
end

-- Recursively copy directory
local function copyDir(src, dst)
    os.execute("mkdir -p " .. dst)
    for file in io.popen("ls " .. src):lines() do
        -- Copy logic
    end
end

print("Backing up " .. source .. " to " .. dest)
copyDir(source, dest)
print("Backup complete!")
```

Compile: `flua compile backup.lua -t NativeAot -o backup`
Use: `./backup /home/user/documents /backup/documents`

#### System Monitor Example
```lua
-- monitor.lua
local interval = tonumber(arg[1]) or 5

while true do
    -- Get system stats (simplified)
    local meminfo = io.open("/proc/meminfo"):read("*a")
    local total = meminfo:match("MemTotal:%s+(%d+)")
    local free = meminfo:match("MemFree:%s+(%d+)")
    
    local used = (total - free) / total * 100
    print(string.format("Memory: %.1f%% used", used))
    
    os.execute("sleep " .. interval)
end
```

## Complete Build Script Example

```bash
#!/bin/bash
# build-native.sh

SCRIPT=$1
OUTPUT=${2:-${SCRIPT%.lua}}

echo "Building $SCRIPT -> $OUTPUT"

# Create temp directory
TEMP_DIR=$(mktemp -d)
trap "rm -rf $TEMP_DIR" EXIT

# Generate project
cat > $TEMP_DIR/Native.csproj << EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <PublishAot>true</PublishAot>
    <StripSymbols>true</StripSymbols>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="$(pwd)/FLua.Runtime.dll" />
  </ItemGroup>
</Project>
EOF

# Compile Lua to C#
dotnet run --project FLua.Cli -- compile $SCRIPT --output $TEMP_DIR/Program.cs

# Build native
cd $TEMP_DIR
dotnet publish -c Release -o .

# Copy result
cp $(basename $OUTPUT) $OLDPWD/$OUTPUT
cd $OLDPWD

echo "Built: $OUTPUT ($(du -h $OUTPUT | cut -f1))"
```

## Deployment Scenarios

### Single File Distribution
```bash
# Just copy the executable
scp myapp user@server:/usr/local/bin/
ssh user@server chmod +x /usr/local/bin/myapp
```

### Container Images
```dockerfile
FROM scratch
COPY myapp /
ENTRYPOINT ["/myapp"]
# Ultra-minimal container (~3MB)
```

### Embedded Systems
- No runtime dependencies
- Predictable memory usage
- Fast cold start
- Works on minimal Linux

## Performance Characteristics

| Metric | Interpreted | JIT | AOT |
|--------|------------|-----|-----|
| Startup Time | 150ms | 100ms | 10ms |
| Memory Usage | 50MB | 40MB | 20MB |
| First Run | Slow | Medium | Fast |
| Peak Performance | Slow | Fast | Fast |
| File Size | 1KB | 10KB | 2MB |

## Security Benefits

1. **No JIT**: Can run in JIT-restricted environments
2. **Static Analysis**: All code paths known at compile time
3. **No Reflection**: Reduced attack surface
4. **Signed Binaries**: Can be code-signed
5. **Read-Only Execution**: No runtime code generation

## Next Steps

- Try compiling your own scripts
- Experiment with cross-compilation
- Optimize for size vs performance
- Explore [Security Levels](../SecurityLevels) for runtime safety
- Consider [Lambda Compilation](../LambdaCompilation) for dynamic scenarios

## Running the Example

```bash
dotnet run
```

The example will:
1. Create a Fibonacci calculator script
2. Compile it to native code (~30-60s)
3. Execute the native binary
4. Show size and performance characteristics