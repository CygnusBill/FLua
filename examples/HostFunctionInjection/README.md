# Host Function Injection Example

This example demonstrates how to extend Lua scripts with custom C# functionality, enabling seamless integration between your application and Lua scripts.

## What is Host Function Injection?

Host function injection allows you to:
- Expose C# methods to Lua scripts
- Provide application-specific APIs
- Control what functionality scripts can access
- Bridge between Lua and your application's data

## Key Concepts

### Basic Function Definition
```csharp
hostFunctions["my_function"] = args =>
{
    // Process arguments (LuaValue[])
    var result = DoSomething(args[0].AsString());
    
    // Return LuaValue
    return LuaValue.String(result);
};
```

### Argument Handling
```csharp
// Type conversion
var str = args[0].AsString();
var num = args[1].AsDouble();
var bool = args[2].AsBoolean();
var table = args[3].AsTable<LuaTable>();

// Validation
if (args.Length < 2)
    throw new LuaRuntimeException("Expected 2 arguments");
```

### Return Types
```csharp
// Primitives
return LuaValue.Nil;
return LuaValue.Boolean(true);
return LuaValue.Number(42.5);
return LuaValue.String("hello");

// Tables
var table = new LuaTable();
table.Set("key", "value");
return table;

// Multiple returns (use arrays)
return new[] { value1, value2 };
```

## Common Patterns

### 1. Logging and Debugging
```csharp
["log"] = args => {
    _logger.Log(string.Join(" ", args));
    return LuaValue.Nil;
};
```

### 2. Data Access
```csharp
["get_config"] = args => {
    var key = args[0].AsString();
    return _config.GetValue(key);
};
```

### 3. Application Control
```csharp
["set_ui_visible"] = args => {
    _mainWindow.Visible = args[0].AsBoolean();
    return LuaValue.Nil;
};
```

### 4. Computation Helpers
```csharp
["calculate_hash"] = args => {
    var input = args[0].AsString();
    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
    return Convert.ToBase64String(hash);
};
```

## Integration Scenarios

### Game Scripting
```csharp
hostFunctions = new() {
    ["spawn_enemy"] = args => SpawnEnemy(args[0].AsString(), args[1].AsDouble()),
    ["play_sound"] = args => AudioManager.Play(args[0].AsString()),
    ["get_player_health"] = args => Player.Health,
    ["damage_player"] = args => Player.TakeDamage(args[0].AsDouble())
};
```

### Data Processing
```csharp
hostFunctions = new() {
    ["load_csv"] = args => LoadCsvToTable(args[0].AsString()),
    ["save_json"] = args => SaveTableToJson(args[0].AsTable(), args[1].AsString()),
    ["query_database"] = args => QueryToTable(args[0].AsString())
};
```

### Automation
```csharp
hostFunctions = new() {
    ["send_email"] = args => EmailService.Send(args[0].AsString(), args[1].AsString()),
    ["schedule_task"] = args => Scheduler.Add(args[0].AsString(), args[1].AsDouble()),
    ["http_request"] = args => HttpClient.GetStringAsync(args[0].AsString()).Result
};
```

## Security Considerations

1. **Validate All Input**: Never trust script-provided data
2. **Limit Exposure**: Only inject necessary functions
3. **Use Trust Levels**: Combine with appropriate sandbox level
4. **Avoid Blocking**: Use async patterns for long operations
5. **Handle Errors**: Wrap in try-catch, throw LuaRuntimeException

## Performance Tips

- Cache delegate creation (don't recreate on each execution)
- Minimize allocations in hot paths
- Use LuaValue directly when possible
- Batch operations when feasible

## Running the Example

```bash
dotnet run
```

The example demonstrates:
1. Basic function injection (logging, math, formatting)
2. Application data integration
3. Async operation patterns
4. Error handling and validation