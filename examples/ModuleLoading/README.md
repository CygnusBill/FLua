# Module Loading Example

This example demonstrates FLua's module system with automatic compilation and caching for optimal performance.

## Overview

Module loading is essential for organizing large Lua applications. FLua provides:
- File-based module resolution
- Automatic compilation for performance
- Dependency management
- Security-aware loading
- Circular dependency handling

## Code Walkthrough

### Step 1: Understanding Lua Modules

A Lua module is simply a script that returns a value (usually a table):

```lua
-- utils.lua
local M = {}  -- Module table

function M.add(a, b)
    return a + b
end

return M  -- Return the module
```

When you `require('utils')`, Lua:
1. Searches for the module file
2. Loads and executes it once
3. Caches the return value
4. Returns the cached value on subsequent requires

### Step 2: Setting Up Module Resolution

```csharp
// Create module directory
var moduleDir = Path.Combine(Directory.GetCurrentDirectory(), "modules");
Directory.CreateDirectory(moduleDir);

// Configure resolver
var moduleResolver = new FileSystemModuleResolver(new[] { moduleDir });
```

**FileSystemModuleResolver** searches for modules in specified directories:
- Looks for `modulename.lua` files
- Supports multiple search paths
- Returns source code and metadata
- Handles path normalization

### Step 3: Configuring the Host

```csharp
var options = new LuaHostOptions
{
    TrustLevel = TrustLevel.Trusted,  // Enables compilation
    ModuleResolver = moduleResolver
};
```

Key points:
- `TrustLevel.Trusted` or higher enables module compilation
- Lower trust levels use interpretation only
- Module resolver is passed through options

### Step 4: How Module Loading Works

When a script calls `require('modulename')`:

```
require('modulename')
    ↓
ModuleResolver.ResolveModuleAsync()
    ↓
Check if already loaded (package.loaded)
    ↓
If not loaded:
    - Read module source
    - Parse to AST
    - Compile (if trusted) or interpret
    - Execute in new environment
    - Cache result
    ↓
Return module value
```

### Step 5: Module Compilation Process

```csharp
private LuaValue[] ExecuteModule(ModuleResolutionResult result, string moduleName, 
                                 LuaEnvironment environment, TrustLevel trustLevel)
{
    // Check compilation cache
    var cacheKey = $"{result.ResolvedPath}:{trustLevel}";
    if (_compiledModuleCache.TryGetValue(cacheKey, out var cachedModule))
    {
        return cachedModule.Execute(environment);
    }
    
    // Parse module
    var statements = ParserHelper.ParseString(result.SourceCode);
    
    // Compile if trusted
    if (_compiler != null && trustLevel >= TrustLevel.Trusted)
    {
        var compilerOptions = new CompilerOptions
        {
            Target = CompilationTarget.Lambda,
            AssemblyName = $"LuaModule_{moduleName}_{Guid.NewGuid():N}",
            GenerateInMemory = true
        };
        
        var compilationResult = _compiler.Compile(statements, compilerOptions);
        
        if (compilationResult.Success)
        {
            // Cache compiled module
            var compiledModule = new CompiledModule(compilationResult.CompiledDelegate);
            _compiledModuleCache[cacheKey] = compiledModule;
            return compiledModule.Execute(environment);
        }
    }
    
    // Fall back to interpretation
    return interpreter.ExecuteStatements(statements, moduleEnv);
}
```

### Step 6: Module Types and Patterns

#### Standard Module (Table)
```lua
-- math_utils.lua
local M = {}

M.PI = 3.14159

function M.area_circle(radius)
    return M.PI * radius * radius
end

function M.area_rectangle(width, height)
    return width * height
end

return M
```

Usage:
```lua
local math_utils = require('math_utils')
print(math_utils.area_circle(5))
```

#### Function Module
```lua
-- validator.lua
return function(value, min, max)
    return value >= min and value <= max
end
```

Usage:
```lua
local validate = require('validator')
if validate(age, 18, 65) then
    -- Valid age
end
```

#### Data Module
```lua
-- config.lua
return {
    app_name = "My Application",
    version = "1.0.0",
    debug = true,
    database = {
        host = "localhost",
        port = 5432
    }
}
```

Usage:
```lua
local config = require('config')
print("Running " .. config.app_name .. " v" .. config.version)
```

#### Class-like Module
```lua
-- person.lua
local Person = {}
Person.__index = Person

function Person.new(name, age)
    return setmetatable({
        name = name,
        age = age
    }, Person)
end

function Person:greet()
    return "Hello, I'm " .. self.name
end

function Person:haveBirthday()
    self.age = self.age + 1
end

return Person
```

Usage:
```lua
local Person = require('person')
local alice = Person.new("Alice", 30)
print(alice:greet())
```

### Step 7: Module Dependencies

Modules can require other modules:

```lua
-- calculator.lua
local utils = require('utils')  -- Dependency

local M = {}

function M.distance(x1, y1, x2, y2)
    local dx = x2 - x1
    local dy = y2 - y1
    -- Use utils module
    return math.sqrt(utils.add(dx * dx, dy * dy))
end

return M
```

Dependency resolution:
1. Each `require` triggers module loading
2. Modules are loaded in dependency order
3. Circular dependencies return partial modules
4. All modules share the same security context

### Step 8: Caching Behavior

FLua implements two levels of caching:

1. **Runtime Cache** (`package.loaded`):
   - Stores module return values
   - Prevents re-execution within a script
   - Cleared between script executions

2. **Compilation Cache**:
   - Stores compiled module assemblies
   - Persists across script executions
   - Keyed by path and trust level
   - Dramatically improves performance

```csharp
// First require - compiles and caches
local mod1 = require('heavy_module')  -- Slow (compilation)

// Second require - uses runtime cache
local mod2 = require('heavy_module')  -- Fast (cached value)

// In a new script execution - uses compilation cache
local mod3 = require('heavy_module')  -- Fast (pre-compiled)
```

### Step 9: Security Considerations

Module loading respects trust levels:

```csharp
// Check if module is allowed
if (!moduleResolver.IsModuleAllowed(moduleName, trustLevel))
{
    throw new LuaRuntimeException($"Module '{moduleName}' not allowed at trust level {trustLevel}");
}
```

Security features:
- Modules inherit parent script's trust level
- Custom resolvers can implement access control
- Compilation only at Trusted+ levels
- Module code is sandboxed

### Step 10: Custom Module Resolvers

You can implement custom resolution logic:

```csharp
public class DatabaseModuleResolver : IModuleResolver
{
    public async Task<ModuleResolutionResult> ResolveModuleAsync(string moduleName, ModuleContext context)
    {
        // Load from database
        var moduleCode = await LoadFromDatabase(moduleName);
        
        if (moduleCode != null)
        {
            return ModuleResolutionResult.CreateSuccess(
                sourceCode: moduleCode,
                resolvedPath: $"db://{moduleName}",
                cacheable: true
            );
        }
        
        return ModuleResolutionResult.CreateFailure($"Module {moduleName} not found in database");
    }
    
    public bool IsModuleAllowed(string moduleName, TrustLevel trustLevel)
    {
        // Custom security logic
        return !moduleName.StartsWith("internal_") || trustLevel >= TrustLevel.Trusted;
    }
}
```

## Complete Example: Plugin System

```csharp
public class PluginSystem
{
    private readonly LuaHost _host;
    private readonly Dictionary<string, object> _plugins = new();
    
    public PluginSystem(string pluginDirectory)
    {
        _host = new LuaHost();
        
        // Custom resolver that validates plugins
        var resolver = new PluginModuleResolver(pluginDirectory);
        
        _options = new LuaHostOptions
        {
            TrustLevel = TrustLevel.Restricted,  // Limited access
            ModuleResolver = resolver,
            HostFunctions = CreatePluginAPI()
        };
    }
    
    private Dictionary<string, Func<LuaValue[], LuaValue>> CreatePluginAPI()
    {
        return new()
        {
            ["register_command"] = args =>
            {
                var name = args[0].AsString();
                var handler = args[1];
                _plugins[name] = handler;
                return LuaValue.Nil;
            },
            
            ["log"] = args =>
            {
                Console.WriteLine($"[Plugin] {args[0]}");
                return LuaValue.Nil;
            }
        };
    }
    
    public void LoadPlugin(string pluginName)
    {
        var script = $@"
            local plugin = require('{pluginName}')
            plugin.initialize()
        ";
        
        _host.Execute(script, _options);
    }
}

// Example plugin (plugins/hello.lua):
local M = {}

function M.initialize()
    register_command('hello', function(args)
        log('Hello from plugin!')
        return 'Hello, ' .. (args[1] or 'World')
    end)
end

return M
```

## Performance Tips

1. **Enable Compilation**: Use Trusted+ trust level
2. **Preload Common Modules**: Load frequently used modules at startup
3. **Minimize Module Size**: Smaller modules compile faster
4. **Use Local Caching**: Store module references locally
5. **Avoid Circular Dependencies**: They prevent optimization

## Error Handling

Common module loading errors:

```lua
-- Module not found
module 'nonexistent' not found:
no field package.preload['nonexistent']
no file 'modules/nonexistent.lua'

-- Syntax error in module
module 'broken' parse error: unexpected symbol near 'end'

-- Runtime error in module
module 'buggy' execution error: attempt to index nil value

-- Circular dependency (handled gracefully)
-- Returns partial module to break cycle
```

## Best Practices

1. **Module Structure**:
   - One module per file
   - Clear, descriptive names
   - Document public API
   - Hide internal functions

2. **Dependencies**:
   - Minimize dependencies
   - Avoid circular references
   - Load at module top

3. **Performance**:
   - Cache module references
   - Initialize once
   - Avoid global state

4. **Security**:
   - Validate module sources
   - Use appropriate trust levels
   - Implement access control

## Next Steps

- Create your own modules
- Implement a custom module resolver
- Try [AOT Compilation](../AotCompilation) for modules
- Explore [Host Function Injection](../HostFunctionInjection) for module APIs

## Running the Example

```bash
dotnet run
```

The example will:
1. Create sample modules
2. Demonstrate loading and dependencies
3. Show compilation performance benefits
4. Illustrate different module patterns