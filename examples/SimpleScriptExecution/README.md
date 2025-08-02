# Simple Script Execution Example

This example demonstrates the most basic use case of FLua hosting: executing Lua scripts in a controlled environment.

## Key Features Demonstrated

- Basic script execution using `LuaHost.Execute()`
- Sandbox trust level for safe execution
- Execution timeout to prevent infinite loops
- Automatic restriction of dangerous functions (like file I/O)

## How It Works

1. Create a `LuaHost` instance
2. Configure `LuaHostOptions` with desired trust level and timeout
3. Execute Lua code and receive results as `LuaValue`
4. Handle any runtime errors

## Trust Level: Sandbox

The Sandbox trust level removes access to:
- File I/O (`io` library)
- OS operations (`os` library)
- Dynamic code loading (`load`, `loadfile`, `dofile`)
- Debug facilities (`debug` library)

This makes it safe to run untrusted scripts while still allowing:
- Mathematical operations
- String manipulation
- Table operations
- Function definitions
- Basic control flow

## Running the Example

```bash
dotnet run
```

## Expected Output

```
Executing Lua script with Sandbox trust level...

Factorial of 5: 120
Fibonacci of 10: 55

Script returned: 175

Execution completed successfully!

--- Testing Restricted Operations ---
Restricted operation blocked (expected): [Error message about io being nil]
```