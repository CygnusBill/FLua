# Simple Script Execution Example

This example demonstrates the most basic use case of FLua hosting: executing Lua scripts in a controlled environment.

## Overview

This is the starting point for understanding FLua hosting. It shows how to:
- Create a host instance
- Configure security settings
- Execute Lua code safely
- Handle results and errors

## Code Walkthrough

### Step 1: Creating the Host

```csharp
var host = new LuaHost();
```

The `LuaHost` class is the main entry point for FLua hosting. It:
- Manages the Lua interpreter
- Handles security policies
- Provides compilation capabilities
- Coordinates module loading

### Step 2: Writing Lua Code

```lua
-- Calculate factorial
function factorial(n)
    if n <= 1 then
        return 1
    end
    return n * factorial(n - 1)
end
```

Key points about the Lua code:
- Standard Lua 5.4 syntax is supported
- Functions can be recursive
- Local and global variables work normally
- The last expression is the return value

### Step 3: Configuring Execution Options

```csharp
var options = new LuaHostOptions
{
    TrustLevel = TrustLevel.Sandbox,
    ExecutionTimeout = TimeSpan.FromSeconds(5)
};
```

**LuaHostOptions** controls execution behavior:
- `TrustLevel`: Determines what the script can access
- `ExecutionTimeout`: Prevents infinite loops
- `ModuleResolver`: For loading external modules (not used here)
- `HostFunctions`: Custom C# functions (not used here)
- `CompilerOptions`: For compilation scenarios (not used here)

### Step 4: Executing the Script

```csharp
try
{
    var result = host.Execute(luaScript, options);
    Console.WriteLine($"Script returned: {result.AsDouble()}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

The execution process:
1. Script is parsed into an AST (Abstract Syntax Tree)
2. Security filters are applied based on trust level
3. Code is interpreted (not compiled at Sandbox level)
4. Result is returned as `LuaValue`

### Step 5: Working with LuaValue

```csharp
result.AsDouble()   // Convert to double
result.AsString()   // Convert to string
result.AsBoolean()  // Convert to bool
result.AsInteger()  // Convert to long
result.AsTable<LuaTable>()  // Get as table
```

`LuaValue` is the universal type that represents any Lua value:
- Numbers (double/long)
- Strings
- Booleans
- Tables (dictionaries/arrays)
- Functions
- Nil

### Step 6: Security Demonstration

```csharp
var restrictedScript = @"
    local file = io.open('test.txt', 'w')  -- This will fail
    return 'File opened'
";
```

The Sandbox trust level blocks:
- File I/O (`io` library)
- OS operations (`os.execute`)
- Dynamic loading (`load`, `loadfile`)
- Debug access (`debug` library)

## Trust Levels Explained

| Level | Use Case | What's Allowed | What's Blocked |
|-------|----------|----------------|----------------|
| **Untrusted** | Malicious code | Basic math, strings | Functions, loops, tables |
| **Sandbox** | User scripts | Functions, tables, math | I/O, OS, loading |
| **Restricted** | Trusted limited | Read files, require | Write, execute |
| **Trusted** | Internal use | Almost everything | Debug only |
| **FullTrust** | System scripts | Everything | Nothing |

## Complete Code Flow

1. **Host Creation** → Initializes interpreter and security
2. **Script Definition** → Lua code as string
3. **Options Setup** → Trust level and timeout
4. **Execution** → Parse → Filter → Interpret
5. **Result Handling** → Convert LuaValue to C# type
6. **Error Handling** → Catch runtime exceptions

## Common Patterns

### Pattern 1: Simple Calculation
```csharp
var result = host.Execute("return 2 + 2", new LuaHostOptions { 
    TrustLevel = TrustLevel.Sandbox 
});
Console.WriteLine(result.AsDouble()); // 4
```

### Pattern 2: Using Functions
```csharp
var script = @"
    function greet(name)
        return 'Hello, ' .. name
    end
    return greet('World')
";
var result = host.Execute(script, options);
Console.WriteLine(result.AsString()); // Hello, World
```

### Pattern 3: Working with Tables
```csharp
var script = @"
    local person = {
        name = 'Alice',
        age = 30
    }
    return person
";
var result = host.Execute(script, options);
var table = result.AsTable<LuaTable>();
Console.WriteLine(table.Get("name").AsString()); // Alice
```

## Error Types

1. **Parse Errors**: Invalid Lua syntax
   ```
   Parse error: unexpected symbol near '}'
   ```

2. **Runtime Errors**: Execution failures
   ```
   attempt to index a nil value
   ```

3. **Security Errors**: Blocked operations
   ```
   attempt to index global 'io' (a nil value)
   ```

4. **Timeout Errors**: Execution time exceeded
   ```
   Script execution exceeded timeout of 00:00:05
   ```

## Best Practices

1. **Always Use Try-Catch**: Scripts can fail in many ways
2. **Set Appropriate Timeouts**: Prevent DoS from infinite loops
3. **Use Minimal Trust Level**: Only grant what's needed
4. **Validate Return Values**: Check types before conversion
5. **Sanitize User Input**: If building scripts dynamically

## Next Steps

- Try modifying the trust level to see different behaviors
- Add more complex Lua functions
- Experiment with different return types
- Move on to [Lambda Compilation](../LambdaCompilation) for performance

## Running the Example

```bash
dotnet run
```

Expected output:
```
Executing Lua script with Sandbox trust level...

Factorial of 5: 120
Fibonacci of 10: 55

Script returned: 175

Execution completed successfully!

--- Testing Restricted Operations ---
Restricted operation blocked (expected): attempt to index global 'io' (a nil value)
```