# Module Loading Example

This example demonstrates FLua's module system with automatic compilation and caching for optimal performance.

## Key Features Demonstrated

- Loading Lua modules using `require()`
- Automatic module compilation when trust level permits
- Module dependency resolution
- Compilation caching for performance
- Different module return types (tables, functions, values)
- Trust level impact on compilation

## How Module Loading Works

1. **Resolution**: The `FileSystemModuleResolver` searches configured paths for `.lua` files
2. **Compilation**: With `Trusted` level or higher, modules are compiled to .NET assemblies
3. **Caching**: Compiled modules are cached to avoid recompilation
4. **Execution**: Modules run in isolated environments with proper scoping
5. **Return Values**: Module returns are cached in `package.loaded`

## Module Best Practices

### Standard Module Pattern
```lua
local M = {}  -- Module table

function M.publicFunction()
    -- Public API
end

local function privateFunction()
    -- Internal helper
end

return M  -- Return module table
```

### Module Types

1. **Table Modules** (most common):
   ```lua
   return { add = function(a,b) return a+b end }
   ```

2. **Function Modules**:
   ```lua
   return function(x) return x * 2 end
   ```

3. **Value Modules**:
   ```lua
   return 3.14159  -- Constants, config values
   ```

## Performance Benefits

- First load includes compilation time
- Subsequent loads use cached compiled version
- 10-50x performance improvement for complex modules
- Automatic fallback to interpretation if compilation fails

## Security Considerations

- Modules respect the host's trust level
- Sandbox/Restricted levels use interpretation only
- Module access can be controlled via custom resolvers
- Circular dependencies are handled gracefully

## Running the Example

```bash
dotnet run
```

The example will:
1. Create sample modules in a `modules` directory
2. Demonstrate various loading scenarios
3. Show performance benefits of compilation
4. Compare trust level impacts