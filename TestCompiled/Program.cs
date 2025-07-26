using System;
using System.Reflection;
using FLua.Runtime;

// Load the compiled Lua library
var assembly = Assembly.LoadFile("/Users/bill/Repos/FLua/test_compile.dll");
var luaScriptType = assembly.GetType("CompiledLuaScript.LuaScript");
var executeMethod = luaScriptType.GetMethod("Execute");

// Create environment with print function
var env = LuaEnvironment.CreateStandardEnvironment();

// Execute the compiled Lua script
var result = (LuaValue[])executeMethod.Invoke(null, new object[] { env });

Console.WriteLine("Compiled Lua script executed successfully!");
