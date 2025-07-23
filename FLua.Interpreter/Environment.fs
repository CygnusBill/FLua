/// FLua.Interpreter.Environment - Environment and Built-in Functions
/// 
/// This module manages the Lua runtime environment and provides built-in functions.
/// It sets up the global environment with standard Lua functions like print, type, etc.
///
module FLua.Interpreter.Environment

open System
open System.Collections.Generic
open FLua.Interpreter.Values

/// Exception thrown by Lua runtime errors
exception LuaRuntimeError of string

/// Built-in Lua functions
module Builtins =
    
    /// print(...) - prints values to console
    let print (args: LuaValue list) =
        let output = 
            args 
            |> List.map LuaValue.toString
            |> String.concat "\t"
        printfn "%s" output
        []  // print returns no values
    
    /// type(v) - returns the type of a value as a string
    let typeOf (args: LuaValue list) =
        match args with
        | [value] -> [LuaString (LuaValue.typeName value)]
        | _ -> raise (LuaRuntimeError "type expects exactly one argument")
    
    /// tostring(v) - converts a value to string
    let tostring (args: LuaValue list) =
        match args with
        | [value] -> [LuaString (LuaValue.toString value)]
        | _ -> raise (LuaRuntimeError "tostring expects exactly one argument")
    
    /// tonumber(v) - converts a value to number
    let tonumber (args: LuaValue list) =
        match args with
        | [value] ->
            match LuaValue.tryToNumber value with
            | Some n -> [LuaNumber n]
            | None -> [LuaNil]
        | _ -> raise (LuaRuntimeError "tonumber expects exactly one argument")
    
    /// next(table, key) - iterator for tables (simplified version)
    let next (args: LuaValue list) =
        match args with
        | [LuaTable table; key] ->
            // Simplified implementation - just return nil for now
            // TODO: Implement proper table iteration
            [LuaNil]
        | _ -> raise (LuaRuntimeError "next expects table and key arguments")
    
    /// pairs(table) - returns iterator for table (simplified)
    let pairs (args: LuaValue list) =
        match args with
        | [LuaTable table] ->
            // Return the next function and the table
            [LuaFunction (LuaBuiltin { Name = "next"; Function = next }); LuaTable table; LuaNil]
        | _ -> raise (LuaRuntimeError "pairs expects a table argument")
    
    /// ipairs(table) - returns iterator for array part (simplified)
    let ipairs (args: LuaValue list) =
        match args with
        | [LuaTable table] ->
            // TODO: Implement proper ipairs
            [LuaFunction (LuaBuiltin { Name = "next"; Function = next }); LuaTable table; LuaInteger 0L]
        | _ -> raise (LuaRuntimeError "ipairs expects a table argument")
    
    /// error(message) - raises an error
    let error (args: LuaValue list) =
        match args with
        | [LuaString msg] -> raise (LuaRuntimeError msg)
        | [value] -> raise (LuaRuntimeError (LuaValue.toString value))
        | _ -> raise (LuaRuntimeError "error")
    
    /// assert(v, message) - asserts a condition
    let assertFunc (args: LuaValue list) =
        match args with
        | value :: rest ->
            if LuaValue.isTruthy value then
                value :: rest  // Return all arguments if assertion passes
            else
                let message = 
                    match rest with
                    | [LuaString msg] -> msg
                    | _ -> "assertion failed"
                raise (LuaRuntimeError message)
        | [] -> raise (LuaRuntimeError "assert expects at least one argument")

/// Standard global environment setup
module GlobalEnvironment =
    
    /// Create the standard Lua global environment
    let createStandard () =
        let globals = Dictionary<string, LuaValue>()
        
        // Add built-in functions
        globals.["print"] <- LuaFunction (LuaBuiltin { Name = "print"; Function = Builtins.print })
        globals.["type"] <- LuaFunction (LuaBuiltin { Name = "type"; Function = Builtins.typeOf })
        globals.["tostring"] <- LuaFunction (LuaBuiltin { Name = "tostring"; Function = Builtins.tostring })
        globals.["tonumber"] <- LuaFunction (LuaBuiltin { Name = "tonumber"; Function = Builtins.tonumber })
        globals.["next"] <- LuaFunction (LuaBuiltin { Name = "next"; Function = Builtins.next })
        globals.["pairs"] <- LuaFunction (LuaBuiltin { Name = "pairs"; Function = Builtins.pairs })
        globals.["ipairs"] <- LuaFunction (LuaBuiltin { Name = "ipairs"; Function = Builtins.ipairs })
        globals.["error"] <- LuaFunction (LuaBuiltin { Name = "error"; Function = Builtins.error })
        globals.["assert"] <- LuaFunction (LuaBuiltin { Name = "assert"; Function = Builtins.assertFunc })
        
        // Add constants
        globals.["_VERSION"] <- LuaString "FLua 1.0"
        globals.["nil"] <- LuaNil  // Not really needed, but for completeness
        
        // Create the root environment
        LuaEnvironment.create globals
    
    /// Add math library functions (basic set)
    let addMathLibrary (globals: Dictionary<string, LuaValue>) =
        let mathTable = LuaTable.empty ()
        
        // Basic math functions
        let mathFunctions = [
            "abs", fun args -> 
                match args with
                | [LuaNumber n] -> [LuaNumber (abs n)]
                | [LuaInteger i] -> [LuaInteger (abs i)]
                | _ -> raise (LuaRuntimeError "math.abs expects a number")
            
            "max", fun args ->
                match args with
                | [] -> raise (LuaRuntimeError "math.max expects at least one argument")
                | values ->
                    let numbers = values |> List.choose LuaValue.tryToNumber
                    if numbers.Length <> values.Length then
                        raise (LuaRuntimeError "math.max expects numbers")
                    [LuaNumber (List.max numbers)]
            
            "min", fun args ->
                match args with
                | [] -> raise (LuaRuntimeError "math.min expects at least one argument")
                | values ->
                    let numbers = values |> List.choose LuaValue.tryToNumber
                    if numbers.Length <> values.Length then
                        raise (LuaRuntimeError "math.min expects numbers")
                    [LuaNumber (List.min numbers)]
            
            "floor", fun args ->
                match args with
                | [LuaNumber n] -> [LuaNumber (Math.Floor n)]
                | [LuaInteger i] -> [LuaInteger i]  // Already an integer
                | _ -> raise (LuaRuntimeError "math.floor expects a number")
            
            "ceil", fun args ->
                match args with
                | [LuaNumber n] -> [LuaNumber (Math.Ceiling n)]
                | [LuaInteger i] -> [LuaInteger i]  // Already an integer
                | _ -> raise (LuaRuntimeError "math.ceil expects a number")
        ]
        
        // Add functions to math table
        for name, func in mathFunctions do
            LuaTable.set mathTable (LuaString name) (LuaFunction (LuaBuiltin { Name = $"math.{name}"; Function = func }))
        
        // Add constants
        LuaTable.set mathTable (LuaString "pi") (LuaNumber Math.PI)
        LuaTable.set mathTable (LuaString "huge") (LuaNumber Double.PositiveInfinity)
        
        globals.["math"] <- LuaTable mathTable 