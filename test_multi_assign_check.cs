using System;
using System.Reflection;
using FLua.Runtime;

// Load the compiled assembly
var assembly = Assembly.LoadFrom("test_multi_assign.dll");
var type = assembly.GetType("CompiledLuaScript.LuaScript");
var method = type.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);

// Create environment and capture output
var env = LuaEnvironment.CreateStandardEnvironment();
var outputs = new System.Collections.Generic.List<string>();

// Override print to capture output
env.SetVariable("print", new LuaUserFunction((args) => {
    var parts = new string[args.Length];
    for (int i = 0; i < args.Length; i++) {
        parts[i] = args[i].ToString();
    }
    outputs.Add(string.Join("\t", parts));
    return new LuaValue[0];
}));

// Execute the compiled Lua script
method.Invoke(null, new object[] { env });

// Check outputs
var expected = new[] {
    "a =\t10",
    "b =\t20", 
    "c =\t30",
    "x =\t10",
    "y =\t20",
    "p =\t1",
    "q =\t2",
    "r =\tnil"
};

bool success = true;
for (int i = 0; i < expected.Length; i++) {
    if (i >= outputs.Count || outputs[i] != expected[i]) {
        Console.WriteLine($"FAIL: Expected '{expected[i]}' but got '{(i < outputs.Count ? outputs[i] : "nothing")}'");
        success = false;
    }
}

if (success) {
    Console.WriteLine("SUCCESS: Multiple assignment from function calls works correctly!");
}