using FLua.Runtime;

var env = new LuaEnvironment();
LuaStringLib.AddStringLibrary(env);

var stringTable = env.GetVariable("string").AsTable<LuaTable>();
var matchFunction = stringTable.Get(LuaValue.String("match")).AsFunction();

// Test 1: Match "test" with pattern "te(st)?"
Console.WriteLine("Test 1: string.match('test', 'te(st)?')");
var results1 = matchFunction.Call(LuaValue.String("test"), LuaValue.String("te(st)?"));
Console.WriteLine($"Results count: {results1.Length}");
for (int i = 0; i < results1.Length; i++)
{
    Console.WriteLine($"Result {i}: '{results1[i].AsString()}'");
}
Console.WriteLine($"Expected: 'st', Got: '{results1[0].AsString()}'");
Console.WriteLine();

// Test 2: Match "te" with pattern "te(st)?" 
Console.WriteLine("Test 2: string.match('te', 'te(st)?')");
var results2 = matchFunction.Call(LuaValue.String("te"), LuaValue.String("te(st)?"));
Console.WriteLine($"Results count: {results2.Length}");
for (int i = 0; i < results2.Length; i++)
{
    Console.WriteLine($"Result {i}: '{results2[i]}'");
}
if (results2.Length > 0)
{
    Console.WriteLine($"Expected: empty string, Got: '{results2[0]}'");
}
else
{
    Console.WriteLine("Expected: empty string, Got: no results");
}