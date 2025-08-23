using FLua.Hosting;
using FLua.Hosting.Security;

var host = new LuaHost();
var options = new LuaHostOptions
{
    TrustLevel = TrustLevel.Sandbox,
    HostContext = new()
    {
        ["player"] = new { Name = "Hero", Health = 100, Level = 5 }
    }
};

string luaCode = @"
print('Player:', player)
print('Player type:', type(player))
if player then
    print('Player Name:', player.Name)
    print('Player Level:', player.Level)
    print('Player Health:', player.Health)
else
    print('Player is nil!')
end
";

try 
{
    var result = host.Execute(luaCode, options);
    Console.WriteLine($"Result: {result}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Stack: {ex.StackTrace}");
}