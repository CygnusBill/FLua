using FLua.Hosting;
using FLua.Hosting.Security;
using FLua.Runtime;

// Example: Security Levels and Sandboxing
// This example demonstrates FLua's five security levels and how they protect your application
// from potentially dangerous Lua code.

var host = new LuaHost();

Console.WriteLine("=== FLua Security Levels Demo ===\n");
Console.WriteLine("FLua provides 5 security levels, from most to least restrictive:\n");

// Test script that tries various operations
var testScript = @"
    local results = {}
    
    -- Test file I/O
    local ok, err = pcall(function()
        local f = io.open('test.txt', 'w')
        f:write('test')
        f:close()
        return 'File I/O works'
    end)
    table.insert(results, 'File I/O: ' .. (ok and err or 'BLOCKED'))
    
    -- Test OS execution
    ok, err = pcall(function()
        os.execute('echo test')
        return 'OS execution works'
    end)
    table.insert(results, 'OS execute: ' .. (ok and 'ALLOWED' or 'BLOCKED'))
    
    -- Test dynamic code loading
    ok, err = pcall(function()
        load('return 42')()
        return 'Dynamic loading works'
    end)
    table.insert(results, 'Dynamic load: ' .. (ok and 'ALLOWED' or 'BLOCKED'))
    
    -- Test debug library
    ok, err = pcall(function()
        debug.getinfo(1)
        return 'Debug library works'
    end)
    table.insert(results, 'Debug library: ' .. (ok and 'ALLOWED' or 'BLOCKED'))
    
    -- Test require
    ok, err = pcall(function()
        require('nonexistent')
        return 'Require works'
    end)
    table.insert(results, 'Require: ' .. (ok and 'ALLOWED' or 'BLOCKED'))
    
    -- Test metatable access
    ok, err = pcall(function()
        setmetatable({}, {})
        return 'Metatables work'
    end)
    table.insert(results, 'Metatables: ' .. (ok and 'ALLOWED' or 'BLOCKED'))
    
    -- Test basic operations (always allowed)
    ok, err = pcall(function()
        local t = {1, 2, 3}
        table.insert(t, 4)
        return 'Basic operations work'
    end)
    table.insert(results, 'Basic ops: ' .. (ok and 'ALLOWED' or 'BLOCKED'))
    
    return table.concat(results, '\n')
";

// Test each security level
var levels = new[]
{
    (TrustLevel.Untrusted, "UNTRUSTED - Maximum restrictions for completely untrusted code"),
    (TrustLevel.Sandbox, "SANDBOX - Safe for user scripts with basic functionality"),
    (TrustLevel.Restricted, "RESTRICTED - Some system access but no file writing"),
    (TrustLevel.Trusted, "TRUSTED - Full access except debugging"),
    (TrustLevel.FullTrust, "FULL TRUST - Complete access to all Lua features")
};

foreach (var (level, description) in levels)
{
    Console.WriteLine($"\n{level} Level");
    Console.WriteLine(new string('=', description.Length));
    Console.WriteLine(description);
    Console.WriteLine();
    
    var options = new LuaHostOptions { TrustLevel = level };
    
    try
    {
        var result = host.Execute(testScript, options);
        Console.WriteLine(result.AsString());
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Script failed: {ex.Message}");
    }
}

// Demonstrate practical sandboxing scenarios
Console.WriteLine("\n\n=== Practical Sandboxing Examples ===\n");

// Example 1: User-submitted calculation
Console.WriteLine("Example 1: Safe User Calculations");
Console.WriteLine("---------------------------------");

var userCalculation = @"
    -- User wants to calculate compound interest
    function compound_interest(principal, rate, time, n)
        return principal * (1 + rate/n)^(n*time)
    end
    
    local p = 1000  -- $1000
    local r = 0.05  -- 5% annual
    local t = 10    -- 10 years
    local n = 12    -- Monthly compounding
    
    local amount = compound_interest(p, r, t, n)
    return string.format('$%.2f after %d years', amount, t)
";

var sandboxOptions = new LuaHostOptions { TrustLevel = TrustLevel.Sandbox };
var calcResult = host.Execute(userCalculation, sandboxOptions);
Console.WriteLine($"Result: {calcResult.AsString()}");
Console.WriteLine("✓ Safe math operations allowed");
Console.WriteLine("✓ No access to file system or OS\n");

// Example 2: Game modding scenario
Console.WriteLine("Example 2: Game Mod Security");
Console.WriteLine("----------------------------");

var gameMod = @"
    -- Mod that enhances game behavior
    local mod = {}
    
    function mod.on_player_damage(player, damage)
        -- Mods can modify game state
        if player.armor > 0 then
            damage = damage * 0.5  -- Armor reduces damage by 50%
            player.armor = player.armor - 1
        end
        return damage
    end
    
    function mod.on_level_complete(score, time)
        -- Calculate bonus
        local time_bonus = math.max(0, 1000 - time * 10)
        return score + time_bonus
    end
    
    -- Mods cannot:
    -- - Access files: io.open('/etc/passwd')
    -- - Execute commands: os.execute('rm -rf /')
    -- - Load arbitrary code: load(malicious_code)
    
    return mod
";

var modOptions = new LuaHostOptions 
{ 
    TrustLevel = TrustLevel.Sandbox,
    HostFunctions = new Dictionary<string, Func<LuaValue[], LuaValue>>
    {
        ["log"] = args => 
        {
            Console.WriteLine($"[MOD LOG] {args[0].AsString()}");
            return LuaValue.Nil;
        }
    }
};

Console.WriteLine("Loading game mod in sandbox...");
host.Execute("log('Mod initialized safely')", modOptions);
Console.WriteLine("✓ Mod can interact with game API");
Console.WriteLine("✓ Cannot access system resources\n");

// Example 3: Custom security policy
Console.WriteLine("Example 3: Custom Security Policy");
Console.WriteLine("---------------------------------");

// Define custom policy at the top of the file or in a separate class
var customHost = new LuaHost(securityPolicy: new StandardSecurityPolicy());
Console.WriteLine("Created host with standard security policy");
Console.WriteLine("✓ Define your own security rules by implementing ILuaSecurityPolicy");
Console.WriteLine("✓ Context-aware trust levels");
Console.WriteLine("✓ Fine-grained permission control");

Console.WriteLine(@"
Example custom policy implementation:

public class CustomSecurityPolicy : ILuaSecurityPolicy
{
    public bool IsOperationAllowed(string operation, TrustLevel trustLevel)
    {
        return operation switch
        {
            ""file.read"" => trustLevel >= TrustLevel.Restricted,
            ""file.write"" => trustLevel >= TrustLevel.Trusted,
            ""network.connect"" => trustLevel >= TrustLevel.Trusted,
            _ => true
        };
    }
}");

// Summary
Console.WriteLine("\n=== Security Best Practices ===\n");
Console.WriteLine("1. Always use the LEAST privileged level that works");
Console.WriteLine("2. User content → Sandbox or lower");  
Console.WriteLine("3. Internal scripts → Restricted or Trusted");
Console.WriteLine("4. System scripts → FullTrust only when necessary");
Console.WriteLine("5. Set execution timeouts to prevent DoS");
Console.WriteLine("6. Validate script sources before elevating trust");
Console.WriteLine("7. Log security-relevant operations");
Console.WriteLine("8. Consider custom security policies for your domain");