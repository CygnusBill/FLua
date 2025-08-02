# FLua Examples

This directory contains examples demonstrating various features of the FLua hosting infrastructure. Each example focuses on a specific use case to facilitate understanding and reuse.

## Examples Overview

### 1. [Simple Script Execution](./SimpleScriptExecution)
**Focus**: Basic script execution with sandboxing

Demonstrates the simplest way to run Lua scripts safely with automatic security restrictions.

```csharp
var host = new LuaHost();
var result = host.Execute("return 2 + 2", new LuaHostOptions { TrustLevel = TrustLevel.Sandbox });
```

### 2. [Lambda Compilation](./LambdaCompilation)
**Focus**: Compiling Lua to .NET delegates

Shows how to compile Lua scripts into high-performance .NET lambdas for repeated execution.

```csharp
var func = host.CompileToFunction<double>("return math.sqrt(x*x + y*y)");
var distance = func(); // Direct C# call
```

### 3. [Expression Tree Compilation](./ExpressionTreeCompilation)
**Focus**: LINQ expression tree generation

Demonstrates experimental support for compiling Lua to LINQ expression trees for integration with query providers.

```csharp
var expr = host.CompileToExpression<double>("return 10 + 20 * 3");
var compiled = expr.Compile();
```

### 4. [Module Loading](./ModuleLoading)
**Focus**: Module system with automatic compilation

Shows how to load Lua modules with dependency resolution, compilation, and caching.

```csharp
var options = new LuaHostOptions {
    ModuleResolver = new FileSystemModuleResolver(["./modules"]),
    TrustLevel = TrustLevel.Trusted // Enables compilation
};
```

### 5. [AOT Compilation](./AotCompilation)
**Focus**: Native executable generation

Demonstrates compiling Lua scripts into standalone native executables that run without .NET.

```bash
flua compile script.lua -t NativeAot -o myapp
./myapp  # Runs without .NET installed
```

### 6. [Security Levels](./SecurityLevels)
**Focus**: Trust levels and sandboxing

Explores FLua's five security levels and how they protect against malicious code.

- **Untrusted**: Maximum restrictions
- **Sandbox**: Safe for user content
- **Restricted**: Limited system access
- **Trusted**: Full access except debug
- **FullTrust**: Complete Lua features

### 7. [Host Function Injection](./HostFunctionInjection)
**Focus**: Extending Lua with C# functions

Shows how to expose application functionality to Lua scripts through custom host functions.

```csharp
hostFunctions["log"] = args => {
    Console.WriteLine(args[0].AsString());
    return LuaValue.Nil;
};
```

## Quick Start

1. **Clone the repository**
   ```bash
   git clone https://github.com/YourUsername/FLua.git
   cd FLua/examples
   ```

2. **Choose an example**
   ```bash
   cd SimpleScriptExecution
   ```

3. **Run it**
   ```bash
   dotnet run
   ```

## Use Case Matrix

| Use Case | Recommended Example |
|----------|-------------------|
| Embedding scripts in app | Simple Script Execution |
| User-provided formulas | Lambda Compilation |
| Dynamic queries | Expression Tree Compilation |
| Plugin system | Module Loading |
| CLI tools | AOT Compilation |
| Multi-tenant SaaS | Security Levels |
| Game modding | Host Function Injection |

## Key Concepts

### Trust Levels
Control what Lua scripts can do:
- File I/O access
- OS command execution
- Dynamic code loading
- Debug capabilities

### Compilation Modes
Choose based on your needs:
- **Interpreted**: Maximum flexibility
- **JIT Lambda**: High performance
- **AOT Native**: Standalone deployment
- **Expression Tree**: LINQ integration

### Module System
- File-based module resolution
- Automatic compilation with caching
- Circular dependency handling
- Security-aware loading

### Host Integration
- Inject custom functions
- Share application data
- Control script capabilities
- Handle errors gracefully

## Best Practices

1. **Start with Sandbox**: Use the least privileged trust level that works
2. **Set Timeouts**: Prevent infinite loops with execution timeouts
3. **Validate Input**: Check all data from scripts before using
4. **Cache Compiled Code**: Compile once, execute many times
5. **Handle Errors**: Use try-catch around script execution
6. **Document APIs**: Clearly explain injected functions

## Going Further

- See [FLua.Hosting API docs](../docs/hosting-api.md) for detailed reference
- Check [FLua.Compiler docs](../docs/compiler.md) for compilation options
- Read [Security Guide](../docs/security.md) for production deployments

## Contributing

Have an interesting use case? Submit a PR with a new example following the pattern:
1. Single-purpose focus
2. Clear documentation
3. Minimal dependencies
4. Runnable with `dotnet run`