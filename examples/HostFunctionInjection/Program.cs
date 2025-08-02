using System.Text;
using FLua.Hosting;
using FLua.Hosting.Security;
using FLua.Runtime;

// Example: Host Function Injection
// This example shows how to extend Lua with custom C# functions,
// enabling scripts to interact with your application's functionality.

Console.WriteLine("=== FLua Host Function Injection ===\n");

// Example 1: Basic function injection
Console.WriteLine("Example 1: Basic Host Functions");
Console.WriteLine("-------------------------------");

var host = new LuaHost();

var options = new LuaHostOptions
{
    TrustLevel = TrustLevel.Sandbox,
    HostFunctions = new Dictionary<string, Func<LuaValue[], LuaValue>>
    {
        // Simple logging function
        ["log"] = args =>
        {
            Console.WriteLine($"[LUA LOG] {string.Join(" ", args.Select(a => a.ToString()))}");
            return LuaValue.Nil;
        },
        
        // Math helper
        ["clamp"] = args =>
        {
            if (args.Length != 3)
                throw new LuaRuntimeException("clamp expects 3 arguments: value, min, max");
                
            var value = args[0].AsDouble();
            var min = args[1].AsDouble();
            var max = args[2].AsDouble();
            
            return Math.Max(min, Math.Min(max, value));
        },
        
        // String formatter
        ["format_currency"] = args =>
        {
            if (args.Length == 0)
                return "$0.00";
                
            var amount = args[0].AsDouble();
            var currency = args.Length > 1 ? args[1].AsString() : "USD";
            
            return currency switch
            {
                "USD" => $"${amount:F2}",
                "EUR" => $"€{amount:F2}",
                "GBP" => $"£{amount:F2}",
                _ => $"{currency} {amount:F2}"
            };
        }
    }
};

var basicScript = @"
    log('Script started')
    
    local value = 150
    local clamped = clamp(value, 0, 100)
    log('Clamped value:', value, 'to', clamped)
    
    local price = 49.99
    log('Price in USD:', format_currency(price))
    log('Price in EUR:', format_currency(price, 'EUR'))
    
    return 'Basic functions work!'
";

var result = host.Execute(basicScript, options);
Console.WriteLine($"Result: {result.AsString()}\n");

// Example 2: Application integration
Console.WriteLine("Example 2: Application Integration");
Console.WriteLine("----------------------------------");

// Simulate an application with data
var userData = new Dictionary<string, (string Name, int Level, double Score)>
{
    ["player1"] = ("Alice", 42, 9500.5),
    ["player2"] = ("Bob", 38, 8200.0),
    ["player3"] = ("Charlie", 45, 10200.75)
};

var appIntegrationOptions = new LuaHostOptions
{
    TrustLevel = TrustLevel.Sandbox,
    HostFunctions = new Dictionary<string, Func<LuaValue[], LuaValue>>
    {
        // Get user data
        ["get_user"] = args =>
        {
            if (args.Length == 0)
                return LuaValue.Nil;
                
            var userId = args[0].AsString();
            if (userData.TryGetValue(userId, out var user))
            {
                var userTable = new LuaTable();
                userTable.Set("name", user.Name);
                userTable.Set("level", user.Level);
                userTable.Set("score", user.Score);
                return userTable;
            }
            
            return LuaValue.Nil;
        },
        
        // Update user score
        ["update_score"] = args =>
        {
            if (args.Length < 2)
                return false;
                
            var userId = args[0].AsString();
            var newScore = args[1].AsDouble();
            
            if (userData.ContainsKey(userId))
            {
                var user = userData[userId];
                userData[userId] = (user.Name, user.Level, newScore);
                Console.WriteLine($"[HOST] Updated {userId} score to {newScore}");
                return true;
            }
            
            return false;
        },
        
        // List all users
        ["list_users"] = args =>
        {
            var list = new LuaTable();
            var index = 1;
            foreach (var userId in userData.Keys)
            {
                list.Set(index++, userId);
            }
            return list;
        }
    }
};

var appScript = @"
    -- Get all users and show their data
    local users = list_users()
    
    for i = 1, #users do
        local userId = users[i]
        local user = get_user(userId)
        
        if user then
            print(string.format('%s: Level %d, Score %.2f', 
                user.name, user.level, user.score))
        end
    end
    
    -- Update a score
    local success = update_score('player2', 8500.0)
    if success then
        print('\nScore updated successfully!')
    end
    
    -- Verify the update
    local updated = get_user('player2')
    print('New score for ' .. updated.name .. ': ' .. updated.score)
";

host.Execute(appScript, appIntegrationOptions);
Console.WriteLine();

// Example 3: Async operations
Console.WriteLine("Example 3: Async Operations");
Console.WriteLine("---------------------------");

var asyncOptions = new LuaHostOptions
{
    TrustLevel = TrustLevel.Sandbox,
    HostFunctions = new Dictionary<string, Func<LuaValue[], LuaValue>>
    {
        // Simulated async operation
        ["fetch_data"] = args =>
        {
            var url = args.Length > 0 ? args[0].AsString() : "default";
            
            Console.WriteLine($"[HOST] Fetching data from '{url}'...");
            
            // Simulate async work (in real app, use proper async)
            Thread.Sleep(100);
            
            var data = new LuaTable();
            data.Set("url", url);
            data.Set("timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            data.Set("data", $"Response from {url}");
            
            return data;
        },
        
        // Progress callback
        ["on_progress"] = args =>
        {
            var progress = args[0].AsDouble();
            var message = args.Length > 1 ? args[1].AsString() : "";
            
            Console.Write($"\r[PROGRESS] {progress:F0}% {message}".PadRight(50));
            
            if (progress >= 100)
                Console.WriteLine();
                
            return LuaValue.Nil;
        }
    }
};

var asyncScript = @"
    -- Simulate a multi-step process
    on_progress(0, 'Starting...')
    
    local result1 = fetch_data('api/users')
    on_progress(33, 'Users loaded')
    
    local result2 = fetch_data('api/settings')
    on_progress(66, 'Settings loaded')
    
    local result3 = fetch_data('api/data')
    on_progress(100, 'Complete!')
    
    print('\nFetched data:')
    print('- ' .. result1.data)
    print('- ' .. result2.data)
    print('- ' .. result3.data)
";

host.Execute(asyncScript, asyncOptions);
Console.WriteLine();

// Example 4: Error handling and validation
Console.WriteLine("Example 4: Error Handling");
Console.WriteLine("-------------------------");

var errorHandlingOptions = new LuaHostOptions
{
    TrustLevel = TrustLevel.Sandbox,
    HostFunctions = new Dictionary<string, Func<LuaValue[], LuaValue>>
    {
        ["divide"] = args =>
        {
            if (args.Length < 2)
                throw new LuaRuntimeException("divide requires 2 arguments");
                
            var a = args[0].AsDouble();
            var b = args[1].AsDouble();
            
            if (b == 0)
                throw new LuaRuntimeException("Division by zero!");
                
            return a / b;
        },
        
        ["validate_email"] = args =>
        {
            if (args.Length == 0)
                return false;
                
            var email = args[0].AsString();
            return email.Contains("@") && email.Contains(".");
        }
    }
};

var errorScript = @"
    -- Test error handling
    local ok, result = pcall(divide, 10, 2)
    if ok then
        print('10 / 2 = ' .. result)
    end
    
    ok, result = pcall(divide, 10, 0)
    if not ok then
        print('Error caught: ' .. result)
    end
    
    -- Test validation
    local emails = {'user@example.com', 'invalid-email', 'test@domain.org'}
    for _, email in ipairs(emails) do
        local valid = validate_email(email)
        print(email .. ': ' .. (valid and 'Valid' or 'Invalid'))
    end
";

host.Execute(errorScript, errorHandlingOptions);

// Summary
Console.WriteLine("\n=== Host Function Best Practices ===\n");
Console.WriteLine("1. **Type Safety**: Always validate argument counts and types");
Console.WriteLine("2. **Error Handling**: Throw LuaRuntimeException for Lua-friendly errors");
Console.WriteLine("3. **Return Values**: Return LuaValue types (nil, bool, number, string, table)");
Console.WriteLine("4. **Performance**: Keep host functions fast, avoid blocking operations");
Console.WriteLine("5. **Security**: Don't expose sensitive operations to untrusted scripts");
Console.WriteLine("6. **Documentation**: Clearly document what each function does and expects");
Console.WriteLine("7. **Naming**: Use Lua conventions (snake_case) for consistency");