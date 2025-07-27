using System;
using FLua.Parser;
using FLua.Runtime;
using Microsoft.FSharp.Collections;

namespace FLua.Interpreter;

/// <summary>
/// Provides the load() function implementation for the interpreter
/// </summary>
public static class InterpreterLoadFunction
{
    /// <summary>
    /// Registers the interpreter's load implementation with the runtime
    /// </summary>
    public static void Register()
    {
        LuaEnvironment.LoadImplementation = LoadImplementation;
    }
    
    /// <summary>
    /// Unregisters the load implementation
    /// </summary>
    public static void Unregister()
    {
        LuaEnvironment.LoadImplementation = null;
    }
    
    /// <summary>
    /// Implementation of the load function that parses and returns a compiled chunk
    /// </summary>
    private static LuaValue[] LoadImplementation(LuaValue[] args)
    {
        if (args.Length == 0)
            return [LuaNil.Instance, new LuaString("no chunk to load")];
        
        if (!args[0].IsString)
            return [LuaNil.Instance, new LuaString("bad argument #1 to 'load' (string expected)")];
        
        var code = args[0].AsString();
        var chunkName = args.Length > 1 && args[1].IsString ? args[1].ToString() : "=(load)";
        
        try
        {
            // Parse the code
            var statements = ParserHelper.ParseString(code);
            
            // Create a function that executes the parsed statements
            var chunk = new LoadedChunk(statements, chunkName);
            
            // Return the chunk function and no error
            return [chunk];
        }
        catch (Exception ex)
        {
            // Return nil and the error message
            return [LuaNil.Instance, new LuaString($"{chunkName}: {ex.Message}")];
        }
    }
    
    /// <summary>
    /// Represents a dynamically loaded chunk of Lua code
    /// </summary>
    private class LoadedChunk : LuaFunction
    {
        private readonly FSharpList<Ast.Statement> _statements;
        private readonly string _chunkName;
        
        public LoadedChunk(FSharpList<Ast.Statement> statements, string chunkName)
        {
            _statements = statements;
            _chunkName = chunkName;
        }
        
        public override LuaValue[] Call(LuaValue[] arguments)
        {
            // Create a new interpreter instance to execute the chunk
            var interpreter = new LuaInterpreter();
            
            // Execute the statements and return the result
            try
            {
                var result = interpreter.ExecuteStatements(_statements);
                return result;
            }
            catch (Exception ex)
            {
                throw new LuaRuntimeException($"error in chunk '{_chunkName}': {ex.Message}");
            }
        }
    }
}