# FLua.Hosting

High-level hosting API for Lua scripting in .NET applications. Provides easy integration, security controls, and module loading capabilities.

This package offers the most convenient way to embed Lua scripting in your .NET applications, with built-in security, error handling, and extensibility.

## Features

### Easy Integration
- **One-Line Setup**: Simple API for common scripting tasks
- **Multiple Compilation Modes**: Choose between interpretation, JIT, and AOT
- **Automatic Optimization**: Smart selection of execution strategies

### Security & Sandboxing
- **Five Trust Levels**: From `Untrusted` (maximum restrictions) to `FullTrust`
- **Resource Controls**: File I/O, OS command, and network access controls
- **Module Loading**: Secure module resolution with path restrictions
- **Timeout Protection**: Prevent infinite loops and resource exhaustion

### Rich API Surface
- **String-to-Function**: Compile Lua code to strongly-typed .NET delegates
- **Expression Evaluation**: Compile to LINQ expression trees
- **Assembly Generation**: Create standalone .NET assemblies
- **Direct Execution**: Run Lua code with result capture

## Usage

### Basic Scripting
```csharp
using FLua.Hosting;

// Create a host instance
var host = new LuaHost();

// Execute Lua code safely
var result = host.Execute("return 2 + 2 * 3");
// result.Value is LuaValue.Number(8)

// Compile to high-performance function
Func<int, int> doubler = host.CompileToFunction<int, int>("return x * 2");
int result = doubler(21); // 42 - direct .NET call
```

### Advanced Integration
```csharp
// Configure security and modules
var options = new LuaHostOptions {
    TrustLevel = TrustLevel.Sandbox, // Restrict file/network access
    ModuleResolver = new FileSystemModuleResolver("./scripts"),
    HostFunctions = new() {
        ["log"] = args => { Console.WriteLine(args[0]); return LuaNil.Instance; },
        ["getData"] = args => new LuaString(myDataProvider.GetData())
    }
};

// Execute with custom environment
var result = host.Execute("log('Processing data...'); return processData()", options);
```

### Host Function Injection
```csharp
// Expose .NET functionality to Lua
host.HostFunctions["calculate"] = args => {
    double x = args[0].AsNumber();
    double y = args[1].AsNumber();
    return new LuaNumber(x + y * 2);
};

// Lua code can now call .NET functions
var result = host.Execute("return calculate(10, 5)"); // Returns 20
```

### Module System
```csharp
// Set up module loading
options.ModuleResolver = new FileSystemModuleResolver(new[] {
    "./modules",
    "./vendor"
});

// Lua can now use require()
host.Execute(@"
    local utils = require('utils')
    local result = utils.process(42)
    return result
");
```

### Error Handling
```csharp
try {
    var result = host.Execute("return undefinedVariable + 1");
} catch (LuaRuntimeException ex) {
    Console.WriteLine($"Lua Error: {ex.Message}");
    Console.WriteLine($"At: {ex.SourceLocation}");
}
```

## Security Levels

| Level | File I/O | OS Commands | Network | Debug | Modules |
|-------|----------|-------------|---------|-------|---------|
| **Untrusted** | ❌ | ❌ | ❌ | ❌ | Sandbox |
| **Sandbox** | Restricted | ❌ | ❌ | ❌ | Sandbox |
| **Restricted** | Limited | ❌ | ❌ | ❌ | Limited |
| **Trusted** | Yes | ❌ | Limited | ❌ | Yes |
| **FullTrust** | Yes | Yes | Yes | Yes | Yes |

## Compilation Strategies

### Automatic Selection
The hosting API automatically chooses the best compilation strategy:

- **Simple expressions** → Expression trees (fastest)
- **Functions with closures** → Interpreter (most compatible)
- **Complex programs** → AOT compilation (best performance)

### Manual Control
```csharp
// Force specific compilation mode
var options = new LuaHostOptions {
    CompilationMode = CompilationMode.Aot, // Force AOT
    TrustLevel = TrustLevel.Trusted
};
```

## Performance

- **Startup**: Minimal overhead, fast initialization
- **Execution**: Near-native performance for compiled code
- **Memory**: Efficient resource usage with cleanup
- **Concurrency**: Thread-safe execution

## Dependencies

- FLua.Ast (AST definitions)
- FLua.Parser (parsing)
- FLua.Compiler (compilation backends)
- FLua.Runtime (execution environment)
- FLua.Interpreter (fallback execution)

## Integration Examples

### Game Development
```csharp
// AI scripting
var aiScript = host.CompileToFunction<Entity, Vector3>(
    "return calculatePath(entity, target)"
);

// Modding support
var modLoader = new LuaHost();
modLoader.Execute(modCode, new LuaHostOptions {
    TrustLevel = TrustLevel.Sandbox,
    ModuleResolver = new RestrictedFileSystemModuleResolver("./mods")
});
```

### Business Logic
```csharp
// Dynamic business rules
var ruleEngine = new LuaHost();
var validateOrder = ruleEngine.CompileToFunction<Order, bool>(@"
    return order.total > 0 and order.items.count > 0
");
```

### Data Processing
```csharp
// ETL pipelines
var transformer = host.CompileToFunction<RawData, ProcessedData>(@"
    return {
        id = data.id,
        processedValue = data.raw * multiplier + offset,
        timestamp = os.time()
    }
");
```

## License

MIT
