# Security Levels Example

This example demonstrates FLua's security trust levels and how they control script capabilities.

## Overview

FLua provides five trust levels to control what scripts can do:
- **Untrusted**: Extremely limited, safe for malicious code
- **Sandbox**: Standard sandboxing, safe for user scripts
- **Restricted**: Limited file access, controlled operations
- **Trusted**: Full functionality except debugging
- **FullTrust**: Complete access to everything

## Understanding Trust Levels

### The Security Model

FLua's security is based on capability restriction:

```
FullTrust → Everything allowed
    ↓
Trusted → No debug access
    ↓
Restricted → Limited I/O
    ↓
Sandbox → No I/O at all
    ↓
Untrusted → Basic operations only
```

Each level is a subset of the level above it.

## Code Walkthrough

### Step 1: Trust Level Configuration

```csharp
var options = new LuaHostOptions
{
    TrustLevel = TrustLevel.Sandbox
};
```

The trust level determines:
- Which Lua standard libraries are available
- Which functions within libraries are accessible
- Whether compilation is allowed
- What host functions can be called

### Step 2: Untrusted Level (Most Restrictive)

```csharp
// Untrusted: For potentially malicious code
var untrustedOptions = new LuaHostOptions
{
    TrustLevel = TrustLevel.Untrusted
};
```

**What's Allowed:**
- Basic arithmetic and logic
- String concatenation
- Simple variables
- Limited table access

**What's Blocked:**
- Functions (no function definitions)
- Loops (no for, while, repeat)
- Complex tables
- All standard libraries
- Metatables

**Use Cases:**
- User-provided math expressions
- Simple configuration values
- Formula evaluation
- Safe property access

**Example:**
```lua
-- ALLOWED in Untrusted
return 2 + 3 * 4
return "Hello " .. "World"
return x > 10 and y < 20

-- BLOCKED in Untrusted
function add(a, b) return a + b end  -- No functions
for i = 1, 10 do end                  -- No loops
{1, 2, 3}                             -- No complex tables
math.sqrt(16)                         -- No libraries
```

### Step 3: Sandbox Level (Safe for Users)

```csharp
var sandboxOptions = new LuaHostOptions
{
    TrustLevel = TrustLevel.Sandbox
};
```

**What's Allowed:**
- Everything from Untrusted
- Functions and closures
- All loop types
- Tables and metatables
- Safe libraries (math, string, table)
- Basic coroutines

**What's Blocked:**
- File I/O (io library)
- System operations (os.execute)
- Code loading (load, loadfile)
- Debug access
- Package/require system

**Use Cases:**
- Game scripting
- User automation
- Plugin systems
- Report generation
- Data transformation

**Example:**
```lua
-- ALLOWED in Sandbox
function factorial(n)
    if n <= 1 then return 1 end
    return n * factorial(n - 1)
end

local data = {1, 2, 3, 4, 5}
local sum = 0
for i, v in ipairs(data) do
    sum = sum + v
end

local person = {}
setmetatable(person, {
    __index = function(t, k)
        return "Unknown: " .. k
    end
})

-- BLOCKED in Sandbox
local file = io.open("data.txt")     -- No file access
os.execute("rm -rf /")               -- No system commands
local func = load("return 42")       -- No dynamic loading
```

### Step 4: Restricted Level (Controlled Access)

```csharp
var restrictedOptions = new LuaHostOptions
{
    TrustLevel = TrustLevel.Restricted
};
```

**What's Allowed:**
- Everything from Sandbox
- Read-only file operations
- Safe OS functions (time, date)
- Module loading (require)
- Limited process info

**What's Blocked:**
- File writing
- Process execution
- Network access
- Debug library
- Environment modification

**Use Cases:**
- Configuration scripts
- Data analysis
- Report generation
- Build scripts (read-only)
- Template processing

**Example:**
```lua
-- ALLOWED in Restricted
local file = io.open("config.json", "r")
local content = file:read("*all")
file:close()

local timestamp = os.date("%Y-%m-%d %H:%M:%S")
local utils = require("utils")

-- BLOCKED in Restricted
local out = io.open("output.txt", "w")  -- No writing
os.execute("make build")                 -- No execution
debug.traceback()                        -- No debug
```

### Step 5: Trusted Level (Internal Use)

```csharp
var trustedOptions = new LuaHostOptions
{
    TrustLevel = TrustLevel.Trusted
};
```

**What's Allowed:**
- Everything from Restricted
- Full file I/O (read/write)
- Process execution
- Environment access
- Compilation to delegates
- Most standard libraries

**What's Blocked:**
- Debug library only
- Some introspection

**Use Cases:**
- Build automation
- System administration
- Development tools
- Test scripts
- Deployment scripts

**Example:**
```lua
-- ALLOWED in Trusted
local output = io.open("results.txt", "w")
output:write("Test results:\n")
output:close()

os.execute("git commit -m 'Update'")
local env = os.getenv("PATH")

-- BLOCKED in Trusted
debug.sethook()   -- No debug hooks
debug.setupvalue() -- No value manipulation
```

### Step 6: FullTrust Level (Complete Access)

```csharp
var fullTrustOptions = new LuaHostOptions
{
    TrustLevel = TrustLevel.FullTrust
};
```

**What's Allowed:**
- Absolutely everything
- Full debug library
- All introspection
- Hook installation
- Value manipulation

**Use Cases:**
- Debuggers
- Profilers
- Development tools
- System diagnostics
- Testing frameworks

### Step 7: Security Implementation Details

```csharp
public LuaEnvironment CreateEnvironment(TrustLevel trustLevel)
{
    var env = new LuaEnvironment();
    
    // Add allowed libraries based on trust level
    switch (trustLevel)
    {
        case TrustLevel.Untrusted:
            // Minimal: basic operations only
            AddBasicOperations(env);
            break;
            
        case TrustLevel.Sandbox:
            // Safe libraries only
            env.SetGlobal("math", LuaMathLib.CreateTable());
            env.SetGlobal("string", LuaStringLib.CreateTable());
            env.SetGlobal("table", LuaTableLib.CreateTable());
            break;
            
        case TrustLevel.Restricted:
            // Add limited I/O
            AddSandboxLibraries(env);
            env.SetGlobal("io", CreateRestrictedIoLib());
            env.SetGlobal("os", CreateRestrictedOsLib());
            break;
            
        case TrustLevel.Trusted:
            // Full libraries except debug
            AddAllStandardLibraries(env);
            env.SetGlobal("debug", null); // Remove debug
            break;
            
        case TrustLevel.FullTrust:
            // Everything
            AddAllStandardLibraries(env);
            env.SetGlobal("debug", LuaDebugLib.CreateTable());
            break;
    }
    
    return env;
}
```

### Step 8: Common Security Patterns

#### Pattern 1: Gradual Trust Elevation
```csharp
public class ScriptRunner
{
    public LuaValue RunWithElevation(string script, TrustLevel maxTrust)
    {
        // Try lowest trust first
        var levels = new[] { 
            TrustLevel.Untrusted, 
            TrustLevel.Sandbox, 
            TrustLevel.Restricted 
        };
        
        foreach (var level in levels)
        {
            if (level > maxTrust) break;
            
            try
            {
                return _host.Execute(script, new LuaHostOptions { 
                    TrustLevel = level 
                });
            }
            catch (SecurityException)
            {
                // Try next level
                continue;
            }
        }
        
        throw new SecurityException("Script requires too high trust level");
    }
}
```

#### Pattern 2: Capability Detection
```csharp
public class CapabilityChecker
{
    public ScriptCapabilities AnalyzeScript(string script)
    {
        var capabilities = new ScriptCapabilities();
        
        // Check for I/O usage
        if (script.Contains("io.") || script.Contains("file:"))
            capabilities.RequiresIO = true;
            
        // Check for OS usage
        if (script.Contains("os.execute") || script.Contains("os.exit"))
            capabilities.RequiresOS = true;
            
        // Check for require
        if (script.Contains("require"))
            capabilities.RequiresModules = true;
            
        // Determine minimum trust level
        if (capabilities.RequiresIO || capabilities.RequiresOS)
            capabilities.MinimumTrust = TrustLevel.Restricted;
        else if (capabilities.RequiresModules)
            capabilities.MinimumTrust = TrustLevel.Restricted;
        else
            capabilities.MinimumTrust = TrustLevel.Sandbox;
            
        return capabilities;
    }
}
```

#### Pattern 3: Sandbox Escape Prevention
```csharp
public class SecureHost
{
    private readonly Dictionary<string, object> _originalGlobals = new();
    
    public void ExecuteUntrusted(string script)
    {
        // Snapshot globals
        SnapshotGlobals();
        
        try
        {
            _host.Execute(script, new LuaHostOptions { 
                TrustLevel = TrustLevel.Untrusted 
            });
        }
        finally
        {
            // Restore globals to prevent contamination
            RestoreGlobals();
        }
    }
}
```

### Step 9: Security Best Practices

1. **Principle of Least Privilege**: Always use the lowest trust level that works
2. **Input Validation**: Validate scripts before execution
3. **Timeout Protection**: Always set execution timeouts
4. **Resource Limits**: Monitor memory and CPU usage
5. **Audit Logging**: Log security-relevant operations
6. **Isolation**: Run untrusted scripts in separate processes

### Step 10: Real-World Security Scenarios

#### Scenario 1: Multi-Tenant SaaS
```csharp
public class TenantScriptRunner
{
    public LuaValue ExecuteTenantScript(string tenantId, string script)
    {
        // Each tenant gets sandboxed environment
        var options = new LuaHostOptions
        {
            TrustLevel = TrustLevel.Sandbox,
            ExecutionTimeout = TimeSpan.FromSeconds(30),
            HostFunctions = GetTenantFunctions(tenantId)
        };
        
        return _host.Execute(script, options);
    }
}
```

#### Scenario 2: Plugin System
```csharp
public class PluginHost
{
    public void LoadPlugin(PluginInfo plugin)
    {
        var trustLevel = plugin.IsSigned 
            ? TrustLevel.Restricted 
            : TrustLevel.Sandbox;
            
        var options = new LuaHostOptions
        {
            TrustLevel = trustLevel,
            ModuleResolver = new PluginModuleResolver(plugin.Directory)
        };
        
        _host.Execute(plugin.InitScript, options);
    }
}
```

#### Scenario 3: Educational Platform
```csharp
public class CodeLearningPlatform
{
    public TestResult RunStudentCode(string code, TestCase test)
    {
        var options = new LuaHostOptions
        {
            TrustLevel = TrustLevel.Sandbox,
            ExecutionTimeout = TimeSpan.FromSeconds(5),
            MemoryLimit = 50 * 1024 * 1024, // 50MB
            HostFunctions = new Dictionary<string, Func<LuaValue[], LuaValue>>
            {
                ["assert"] = args => /* custom assert */,
                ["print"] = args => /* capture output */
            }
        };
        
        return ExecuteWithTestHarness(code, test, options);
    }
}
```

## Security Comparison Table

| Feature | Untrusted | Sandbox | Restricted | Trusted | FullTrust |
|---------|-----------|---------|------------|---------|----------|
| Basic Math | ✅ | ✅ | ✅ | ✅ | ✅ |
| Functions | ❌ | ✅ | ✅ | ✅ | ✅ |
| Loops | ❌ | ✅ | ✅ | ✅ | ✅ |
| Tables | Limited | ✅ | ✅ | ✅ | ✅ |
| Math Library | ❌ | ✅ | ✅ | ✅ | ✅ |
| String Library | ❌ | ✅ | ✅ | ✅ | ✅ |
| Table Library | ❌ | ✅ | ✅ | ✅ | ✅ |
| Read Files | ❌ | ❌ | ✅ | ✅ | ✅ |
| Write Files | ❌ | ❌ | ❌ | ✅ | ✅ |
| Execute Commands | ❌ | ❌ | ❌ | ✅ | ✅ |
| Load Code | ❌ | ❌ | ❌ | Limited | ✅ |
| Debug Access | ❌ | ❌ | ❌ | ❌ | ✅ |
| Compilation | ❌ | ❌ | ❌ | ✅ | ✅ |

## Error Messages by Trust Level

```lua
-- Untrusted
"function definitions not allowed at trust level Untrusted"
"loops not allowed at trust level Untrusted"

-- Sandbox
"attempt to index global 'io' (a nil value)"
"attempt to call global 'require' (a nil value)"

-- Restricted
"io.open: write mode not allowed at trust level Restricted"
"os.execute not allowed at trust level Restricted"

-- Trusted
"debug library not available at trust level Trusted"

-- FullTrust
(No restrictions)
```

## Performance Implications

- **Untrusted**: Fastest (minimal features)
- **Sandbox**: Fast (no I/O overhead)
- **Restricted**: Moderate (file checks)
- **Trusted**: Full speed (compilation enabled)
- **FullTrust**: Varies (debug overhead)

## Next Steps

- Experiment with different trust levels
- Build a custom security policy
- Try [Host Function Injection](../HostFunctionInjection) for custom APIs
- Explore [Lambda Compilation](../LambdaCompilation) (requires Trusted+)

## Running the Example

```bash
dotnet run
```

The example will:
1. Demonstrate each trust level's capabilities
2. Show blocked operations and error messages
3. Illustrate common security patterns
4. Provide performance comparisons