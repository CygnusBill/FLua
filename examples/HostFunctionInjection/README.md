# Host Function Injection Example

This example demonstrates how to extend Lua scripts with custom C# functionality, enabling seamless integration between your .NET application and Lua scripts.

## Overview

Host function injection is the bridge between your application and Lua scripts. It allows you to:
- Expose C# methods as Lua functions
- Provide domain-specific APIs
- Control data flow between environments
- Maintain security boundaries

## Understanding Host Functions

### What Are Host Functions?

Host functions are C# methods exposed to Lua scripts:

```
C# Application
     ↓
Host Functions  ←→  Lua Script
     ↓
Application Data
```

They act as a controlled API surface, allowing scripts to interact with your application safely.

## Code Walkthrough

### Step 1: Defining a Host Function

```csharp
var hostFunctions = new Dictionary<string, Func<LuaValue[], LuaValue>>
{
    ["log"] = args =>
    {
        var message = string.Join(" ", args.Select(a => a.ToString()));
        Console.WriteLine($"[Lua] {message}");
        return LuaValue.Nil;
    }
};
```

**Anatomy of a host function:**
- **Name**: String key in dictionary ("log")
- **Arguments**: Array of LuaValue objects
- **Body**: C# code to execute
- **Return**: LuaValue result (can be Nil)

### Step 2: Type Conversion

```csharp
["calculate"] = args =>
{
    // Validate argument count
    if (args.Length < 2)
        throw new LuaRuntimeException("calculate requires 2 arguments");
    
    // Convert Lua values to C# types
    var a = args[0].AsDouble();  // Throws if not a number
    var b = args[1].AsDouble();
    
    // Perform calculation
    var result = Math.Pow(a, b);
    
    // Convert back to Lua value
    return LuaValue.Number(result);
}
```

**Type conversion methods:**
- `AsDouble()` - Convert to double
- `AsInteger()` - Convert to long
- `AsString()` - Convert to string
- `AsBoolean()` - Convert to bool
- `AsTable<LuaTable>()` - Get as table
- `AsFunction()` - Get as callable

### Step 3: Working with Tables

```csharp
["get_user_info"] = args =>
{
    var userId = args[0].AsInteger();
    
    // Create Lua table
    var userTable = new LuaTable();
    userTable.Set("id", LuaValue.Number(userId));
    userTable.Set("name", LuaValue.String("Alice"));
    userTable.Set("email", LuaValue.String("alice@example.com"));
    userTable.Set("active", LuaValue.Boolean(true));
    
    // Nested table
    var settings = new LuaTable();
    settings.Set("theme", LuaValue.String("dark"));
    settings.Set("notifications", LuaValue.Boolean(true));
    userTable.Set("settings", settings);
    
    return userTable;
}
```

**Usage in Lua:**
```lua
local user = get_user_info(123)
print(user.name)  -- "Alice"
print(user.settings.theme)  -- "dark"
```

### Step 4: Multiple Return Values

```csharp
["divmod"] = args =>
{
    var dividend = args[0].AsDouble();
    var divisor = args[1].AsDouble();
    
    if (divisor == 0)
        throw new LuaRuntimeException("Division by zero");
    
    var quotient = Math.Floor(dividend / divisor);
    var remainder = dividend % divisor;
    
    // Return multiple values as array
    return new[] { 
        LuaValue.Number(quotient), 
        LuaValue.Number(remainder) 
    };
}
```

**Usage in Lua:**
```lua
local q, r = divmod(17, 5)  -- q = 3, r = 2
```

### Step 5: Error Handling

```csharp
["read_file"] = args =>
{
    try
    {
        var filename = args[0].AsString();
        
        // Security check
        if (filename.Contains(".."))
            throw new LuaRuntimeException("Path traversal not allowed");
        
        var content = File.ReadAllText(filename);
        return LuaValue.String(content);
    }
    catch (FileNotFoundException)
    {
        throw new LuaRuntimeException("File not found");
    }
    catch (UnauthorizedAccessException)
    {
        throw new LuaRuntimeException("Access denied");
    }
}
```

**Error propagation:**
- C# exceptions become Lua errors
- Use LuaRuntimeException for clean error messages
- Always validate inputs

### Step 6: Async Operations

```csharp
["fetch_data"] = args =>
{
    var url = args[0].AsString();
    
    // Create promise-like table
    var promise = new LuaTable();
    var resultTable = new LuaTable();
    
    // Start async operation
    Task.Run(async () =>
    {
        try
        {
            using var client = new HttpClient();
            var data = await client.GetStringAsync(url);
            
            resultTable.Set("success", LuaValue.Boolean(true));
            resultTable.Set("data", LuaValue.String(data));
        }
        catch (Exception ex)
        {
            resultTable.Set("success", LuaValue.Boolean(false));
            resultTable.Set("error", LuaValue.String(ex.Message));
        }
        
        resultTable.Set("completed", LuaValue.Boolean(true));
    });
    
    promise.Set("result", resultTable);
    promise.Set("is_complete", new LuaUserFunction(args2 =>
    {
        return resultTable.Get("completed");
    }));
    
    return promise;
}
```

### Step 7: Application Integration

```csharp
public class GameScriptingHost
{
    private readonly Game _game;
    
    public Dictionary<string, Func<LuaValue[], LuaValue>> CreateGameAPI()
    {
        return new()
        {
            ["spawn_entity"] = args =>
            {
                var type = args[0].AsString();
                var x = args[1].AsDouble();
                var y = args[2].AsDouble();
                
                var entity = _game.SpawnEntity(type, x, y);
                return LuaValue.Number(entity.Id);
            },
            
            ["get_entity"] = args =>
            {
                var id = args[0].AsInteger();
                var entity = _game.GetEntity(id);
                
                if (entity == null)
                    return LuaValue.Nil;
                
                var table = new LuaTable();
                table.Set("id", LuaValue.Number(entity.Id));
                table.Set("type", LuaValue.String(entity.Type));
                table.Set("x", LuaValue.Number(entity.X));
                table.Set("y", LuaValue.Number(entity.Y));
                table.Set("health", LuaValue.Number(entity.Health));
                
                return table;
            },
            
            ["damage_entity"] = args =>
            {
                var id = args[0].AsInteger();
                var damage = args[1].AsDouble();
                
                var entity = _game.GetEntity(id);
                if (entity != null)
                {
                    entity.TakeDamage(damage);
                    return LuaValue.Boolean(entity.IsAlive);
                }
                
                return LuaValue.Boolean(false);
            }
        };
    }
}
```

### Step 8: Security Patterns

#### Pattern 1: Sandboxed File Access
```csharp
public class SecureFileAPI
{
    private readonly string _sandboxPath;
    
    public Dictionary<string, Func<LuaValue[], LuaValue>> Create()
    {
        return new()
        {
            ["read_file"] = args =>
            {
                var filename = args[0].AsString();
                var safePath = GetSafePath(filename);
                
                if (!File.Exists(safePath))
                    throw new LuaRuntimeException("File not found");
                
                return LuaValue.String(File.ReadAllText(safePath));
            },
            
            ["write_file"] = args =>
            {
                var filename = args[0].AsString();
                var content = args[1].AsString();
                var safePath = GetSafePath(filename);
                
                File.WriteAllText(safePath, content);
                return LuaValue.Nil;
            }
        };
    }
    
    private string GetSafePath(string filename)
    {
        // Remove any path components
        filename = Path.GetFileName(filename);
        
        // Ensure it's within sandbox
        var fullPath = Path.Combine(_sandboxPath, filename);
        var resolved = Path.GetFullPath(fullPath);
        
        if (!resolved.StartsWith(_sandboxPath))
            throw new LuaRuntimeException("Access denied");
            
        return resolved;
    }
}
```

#### Pattern 2: Rate Limiting
```csharp
public class RateLimitedAPI
{
    private readonly Dictionary<string, DateTime> _lastCall = new();
    private readonly TimeSpan _cooldown = TimeSpan.FromSeconds(1);
    
    public Func<LuaValue[], LuaValue> WrapWithRateLimit(
        string name, 
        Func<LuaValue[], LuaValue> function)
    {
        return args =>
        {
            if (_lastCall.TryGetValue(name, out var last))
            {
                var elapsed = DateTime.UtcNow - last;
                if (elapsed < _cooldown)
                {
                    var wait = (_cooldown - elapsed).TotalSeconds;
                    throw new LuaRuntimeException(
                        $"Rate limit: wait {wait:F1} seconds");
                }
            }
            
            _lastCall[name] = DateTime.UtcNow;
            return function(args);
        };
    }
}
```

### Step 9: Complex Data Types

```csharp
["create_image"] = args =>
{
    var width = (int)args[0].AsInteger();
    var height = (int)args[1].AsInteger();
    
    // Create wrapper table with methods
    var image = new LuaTable();
    var pixels = new byte[width * height * 4]; // RGBA
    
    // Properties
    image.Set("width", LuaValue.Number(width));
    image.Set("height", LuaValue.Number(height));
    
    // Methods
    image.Set("set_pixel", new LuaUserFunction(pixelArgs =>
    {
        var x = (int)pixelArgs[0].AsInteger();
        var y = (int)pixelArgs[1].AsInteger();
        var r = (byte)pixelArgs[2].AsInteger();
        var g = (byte)pixelArgs[3].AsInteger();
        var b = (byte)pixelArgs[4].AsInteger();
        var a = pixelArgs.Length > 5 ? (byte)pixelArgs[5].AsInteger() : (byte)255;
        
        var index = (y * width + x) * 4;
        pixels[index] = r;
        pixels[index + 1] = g;
        pixels[index + 2] = b;
        pixels[index + 3] = a;
        
        return LuaValue.Nil;
    }));
    
    image.Set("save", new LuaUserFunction(saveArgs =>
    {
        var filename = saveArgs[0].AsString();
        SaveImage(filename, pixels, width, height);
        return LuaValue.Nil;
    }));
    
    return image;
}
```

**Usage in Lua:**
```lua
local img = create_image(100, 100)
img:set_pixel(50, 50, 255, 0, 0)  -- Red pixel
img:save("output.png")
```

### Step 10: Event System

```csharp
public class EventSystemAPI
{
    private readonly Dictionary<string, List<LuaFunction>> _handlers = new();
    
    public Dictionary<string, Func<LuaValue[], LuaValue>> Create()
    {
        return new()
        {
            ["on"] = args =>
            {
                var eventName = args[0].AsString();
                var handler = args[1].AsFunction();
                
                if (!_handlers.ContainsKey(eventName))
                    _handlers[eventName] = new List<LuaFunction>();
                
                _handlers[eventName].Add(handler);
                return LuaValue.Nil;
            },
            
            ["emit"] = args =>
            {
                var eventName = args[0].AsString();
                var eventArgs = args.Skip(1).ToArray();
                
                if (_handlers.TryGetValue(eventName, out var handlers))
                {
                    foreach (var handler in handlers)
                    {
                        handler.Call(eventArgs);
                    }
                }
                
                return LuaValue.Nil;
            },
            
            ["off"] = args =>
            {
                var eventName = args[0].AsString();
                _handlers.Remove(eventName);
                return LuaValue.Nil;
            }
        };
    }
}
```

## Real-World Examples

### Example 1: Database Access Layer
```csharp
public class DatabaseAPI
{
    private readonly string _connectionString;
    
    public Dictionary<string, Func<LuaValue[], LuaValue>> Create()
    {
        return new()
        {
            ["query"] = args =>
            {
                var sql = args[0].AsString();
                var parameters = args.Length > 1 ? args[1].AsTable<LuaTable>() : null;
                
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand(sql, conn);
                
                // Add parameters safely
                if (parameters != null)
                {
                    foreach (var kvp in parameters)
                    {
                        cmd.Parameters.AddWithValue($"@{kvp.Key}", 
                            ConvertToSqlValue(kvp.Value));
                    }
                }
                
                conn.Open();
                using var reader = cmd.ExecuteReader();
                
                var results = new LuaTable();
                var rowIndex = 1;
                
                while (reader.Read())
                {
                    var row = new LuaTable();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var name = reader.GetName(i);
                        var value = reader.GetValue(i);
                        row.Set(name, ConvertFromSqlValue(value));
                    }
                    results.Set(rowIndex++, row);
                }
                
                return results;
            }
        };
    }
}
```

### Example 2: UI Automation
```csharp
public class UIAutomationAPI
{
    private readonly Application _app;
    
    public Dictionary<string, Func<LuaValue[], LuaValue>> Create()
    {
        return new()
        {
            ["find_element"] = args =>
            {
                var selector = args[0].AsString();
                var element = _app.FindElement(selector);
                
                if (element == null)
                    return LuaValue.Nil;
                
                return CreateElementWrapper(element);
            },
            
            ["wait_for"] = args =>
            {
                var condition = args[0].AsFunction();
                var timeout = args.Length > 1 ? args[1].AsDouble() : 5.0;
                
                var stopwatch = Stopwatch.StartNew();
                while (stopwatch.Elapsed.TotalSeconds < timeout)
                {
                    var result = condition.Call(new LuaValue[0]);
                    if (result[0].AsBoolean())
                        return LuaValue.Boolean(true);
                    
                    Thread.Sleep(100);
                }
                
                return LuaValue.Boolean(false);
            }
        };
    }
    
    private LuaTable CreateElementWrapper(UIElement element)
    {
        var wrapper = new LuaTable();
        
        wrapper.Set("click", new LuaUserFunction(args =>
        {
            element.Click();
            return LuaValue.Nil;
        }));
        
        wrapper.Set("set_text", new LuaUserFunction(args =>
        {
            element.SetText(args[0].AsString());
            return LuaValue.Nil;
        }));
        
        wrapper.Set("get_text", new LuaUserFunction(args =>
        {
            return LuaValue.String(element.GetText());
        }));
        
        return wrapper;
    }
}
```

## Best Practices

1. **Input Validation**: Always validate argument count and types
2. **Error Messages**: Provide clear, actionable error messages
3. **Documentation**: Document each function's signature and behavior
4. **Consistency**: Use consistent naming and patterns
5. **Performance**: Cache delegates, avoid unnecessary allocations
6. **Security**: Never expose sensitive operations directly
7. **Testing**: Unit test host functions independently

## Performance Optimization

```csharp
// Bad: Creates new function on each call
hostFunctions["bad"] = args =>
{
    return new LuaUserFunction(innerArgs => /* ... */);
};

// Good: Reuse function instance
private readonly LuaFunction _cachedFunction = new LuaUserFunction(args => /* ... */);
hostFunctions["good"] = args => _cachedFunction;

// Good: Pre-compile regex patterns
private readonly Regex _validator = new Regex(@"^\w+$", RegexOptions.Compiled);
hostFunctions["validate"] = args =>
{
    return LuaValue.Boolean(_validator.IsMatch(args[0].AsString()));
};
```

## Debugging Host Functions

```csharp
public class DebugHostFunctions
{
    public static Dictionary<string, Func<LuaValue[], LuaValue>> Wrap(
        Dictionary<string, Func<LuaValue[], LuaValue>> functions)
    {
        var wrapped = new Dictionary<string, Func<LuaValue[], LuaValue>>();
        
        foreach (var kvp in functions)
        {
            wrapped[kvp.Key] = args =>
            {
                Console.WriteLine($"Call: {kvp.Key}({string.Join(", ", args.Select(a => a.ToString()))})");
                
                try
                {
                    var result = kvp.Value(args);
                    Console.WriteLine($"Return: {result}");
                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    throw;
                }
            };
        }
        
        return wrapped;
    }
}
```

## Next Steps

- Create your own domain-specific APIs
- Combine with appropriate [Security Levels](../SecurityLevels)
- Try [Lambda Compilation](../LambdaCompilation) for performance
- Explore [Module Loading](../ModuleLoading) for larger APIs

## Running the Example

```bash
dotnet run
```

The example will demonstrate:
1. Basic function injection patterns
2. Type conversion and validation
3. Complex data structure handling
4. Async operation patterns
5. Real-world integration scenarios