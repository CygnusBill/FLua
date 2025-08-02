# FLua Hosting Model Architecture

## Overview
The FLua.Hosting project provides a secure and flexible way to embed Lua scripting in .NET applications. It offers string-to-lambda transformation, assembly generation, and security-controlled execution environments.

## Key Components

### 1. Security Model
- **TrustLevel Enumeration**: Five levels from Untrusted to FullTrust
  - Untrusted: No I/O, minimal functionality
  - Sandbox: Safe computation only
  - Restricted: Limited I/O in designated areas
  - Trusted: Full standard library except debug
  - FullTrust: Complete Lua functionality

- **ILuaSecurityPolicy**: Interface for custom security policies
- **StandardSecurityPolicy**: Default implementation with sensible restrictions

### 2. Module Resolution
- **IModuleResolver**: Interface for host-controlled module loading
- **FileSystemModuleResolver**: Default implementation with search paths
- **ModuleContext**: Provides context for resolution (trust level, requesting module)
- **ModuleResolutionResult**: Encapsulates resolution success/failure

### 3. Environment Management
- **IEnvironmentProvider**: Creates custom Lua environments
- **FilteredEnvironmentProvider**: Creates security-filtered environments
- Configurable library availability based on trust level
- Host function injection support

### 4. Hosting Interface
- **ILuaHost**: Main interface for hosting operations
  - CompileToFunction<T>: String to typed function
  - CompileToDelegate: String to delegate with parameters
  - CompileToExpression<T>: String to expression tree
  - CompileToAssembly: String to assembly
  - Execute/ExecuteAsync: Direct execution with security
  - ValidateCode: Syntax validation without execution

### 5. Configuration
- **LuaHostOptions**: Comprehensive configuration
  - Trust level settings
  - Module resolver configuration
  - Host function replacements
  - Module search paths
  - Execution timeouts and memory limits

## Integration Points

### Compiler Integration
Extended `CompilerOptions` with:
- GenerateExpressionTree: For lambda compilation
- GenerateInMemory: For in-memory execution
- ModuleResolverTypeName: For custom resolver injection
- HostProvidedTypes: For type mapping

Extended `CompilationTarget` enum:
- Lambda: In-memory delegate generation
- Expression: Expression tree generation

Extended `CompilationResult` with:
- CompiledDelegate: Generated delegate
- ExpressionTree: Generated expression
- GeneratedType: Generated type info

### Runtime Integration
- Uses existing FLua.Runtime libraries
- No duplication of runtime functionality
- Clean separation of concerns

## Usage Patterns

### Basic String Execution
```csharp
var host = new LuaHost();
var result = host.Execute("return 2 + 2");
```

### Sandboxed Execution
```csharp
var options = new LuaHostOptions 
{ 
    TrustLevel = TrustLevel.Sandbox,
    ExecutionTimeout = TimeSpan.FromSeconds(5)
};
var func = host.CompileToFunction<int>("return math.sqrt(16)", options);
```

### Module Resolution
```csharp
var resolver = new FileSystemModuleResolver(new[] { "./scripts", "./lib" });
var options = new LuaHostOptions { ModuleResolver = resolver };
var result = host.Execute("local m = require('mymodule'); return m.doWork()", options);
```

### Host Function Injection
```csharp
var options = new LuaHostOptions 
{
    HostFunctions = new()
    {
        ["readFile"] = args => File.ReadAllText(args[0].AsString()),
        ["writeFile"] = args => { File.WriteAllText(args[0].AsString(), args[1].AsString()); return LuaValue.Nil; }
    }
};
```

## Security Considerations

1. **Trust Level Enforcement**: Functions and libraries filtered by trust level
2. **Module Access Control**: Host controls which modules can be loaded
3. **Path Restrictions**: Module resolver enforces path-based security
4. **Timeout Support**: Prevents infinite loops/resource exhaustion
5. **Memory Limits**: Can restrict memory usage (implementation pending)

## Next Steps

1. Implement main LuaHost class
2. Add lambda generation to RoslynCodeGenerator
3. Create comprehensive test suite
4. Add performance monitoring
5. Implement resource limits (CPU, memory)
6. Add debugging support for hosted code