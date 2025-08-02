# Security Levels and Sandboxing Example

This example demonstrates FLua's comprehensive security model with five distinct trust levels for safe script execution.

## Trust Levels Overview

| Level | Use Case | Allowed | Blocked |
|-------|----------|---------|---------|
| **Untrusted** | Malicious/unknown code | Basic math, strings | Everything else |
| **Sandbox** | User scripts, mods | Tables, functions | I/O, OS, debug |
| **Restricted** | Trusted but limited | Read files, time | Write, execute |
| **Trusted** | Internal scripts | Most operations | Debug only |
| **FullTrust** | System scripts | Everything | Nothing |

## Security Features by Level

### Untrusted (Most Restrictive)
- ✓ Basic arithmetic and logic
- ✓ String operations
- ✓ Table creation
- ✗ No function definitions
- ✗ No loops (DoS prevention)
- ✗ No I/O or system access
- ✗ No metatables

### Sandbox (User Content)
- ✓ Everything from Untrusted
- ✓ Function definitions
- ✓ Control structures
- ✓ Standard library (math, string, table)
- ✗ No file I/O
- ✗ No OS access
- ✗ No dynamic code loading
- ✗ No debug access

### Restricted (Limited System Access)
- ✓ Everything from Sandbox
- ✓ Read-only file access
- ✓ Time/date functions
- ✓ Module loading (require)
- ✗ No file writing
- ✗ No process execution
- ✗ No network access

### Trusted (Internal Use)
- ✓ Everything from Restricted
- ✓ Full file I/O
- ✓ OS operations
- ✓ Module compilation
- ✗ No debug library

### FullTrust (System Level)
- ✓ Complete Lua functionality
- ✓ Debug library
- ✓ Raw memory access
- ✓ All metamethods

## Common Scenarios

### Web Application (User Scripts)
```csharp
var options = new LuaHostOptions 
{
    TrustLevel = TrustLevel.Sandbox,
    ExecutionTimeout = TimeSpan.FromSeconds(5)
};
```

### Game Modding
```csharp
var options = new LuaHostOptions 
{
    TrustLevel = TrustLevel.Sandbox,
    HostFunctions = gameApi  // Controlled API
};
```

### Data Processing
```csharp
var options = new LuaHostOptions 
{
    TrustLevel = TrustLevel.Restricted,  // Can read files
    ModuleResolver = moduleResolver
};
```

### System Automation
```csharp
var options = new LuaHostOptions 
{
    TrustLevel = TrustLevel.Trusted,  // Full access
    CompilerOptions = compilerOptions
};
```

## Security Best Practices

1. **Principle of Least Privilege**: Always use the lowest trust level that meets requirements

2. **Defense in Depth**: Combine multiple security measures:
   - Trust levels
   - Execution timeouts  
   - Memory limits
   - Custom validators

3. **Input Validation**: Check script sources:
   ```csharp
   if (IsUserSubmitted(script))
       options.TrustLevel = TrustLevel.Sandbox;
   ```

4. **Audit Logging**: Track security-relevant operations:
   ```csharp
   host.OnSecurityEvent += (op, allowed) => 
       Log($"{op}: {allowed ? "Allowed" : "Blocked"}");
   ```

5. **Custom Policies**: Implement domain-specific rules:
   ```csharp
   public class MyPolicy : ILuaSecurityPolicy
   {
       public bool IsOperationAllowed(string op, TrustLevel level)
       {
           // Custom logic
       }
   }
   ```

## Running the Example

```bash
dotnet run
```

The example will:
1. Test all operations at each trust level
2. Show what's allowed and blocked
3. Demonstrate practical sandboxing scenarios
4. Explain security best practices