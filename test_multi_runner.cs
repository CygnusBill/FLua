using FLua.Runtime;
using System;

var env = LuaEnvironment.CreateStandardEnvironment();
var result = CompiledLuaScript.LuaScript.Execute(env);