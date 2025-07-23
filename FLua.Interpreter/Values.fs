/// FLua.Interpreter.Values - Lua Runtime Value System
/// 
/// This module defines the runtime representation of Lua values.
/// Lua is dynamically typed, so all values are represented by a discriminated union.
///
/// Lua has the following basic types:
/// - nil: represents absence of value
/// - boolean: true/false
/// - number: both integers and floats (Lua 5.3+ distinguishes these)
/// - string: immutable text
/// - function: Lua functions (both Lua-defined and built-in)
/// - table: Lua's only data structure (arrays, objects, etc.)
/// - thread: coroutines (for future implementation)
/// - userdata: for C integration (for future implementation)
///
module FLua.Interpreter.Values

open System
open System.Collections.Generic
open FLua.Parser

/// Represents a Lua runtime value
[<CustomEquality; NoComparison>]
type LuaValue =
    | LuaNil
    | LuaBool of bool
    | LuaNumber of float  // Lua numbers are typically double precision
    | LuaInteger of int64 // Lua 5.3+ separates integers
    | LuaString of string
    | LuaTable of LuaTable
    | LuaFunction of LuaFunction
    // Future: LuaThread, LuaUserdata
    
    override this.Equals(other) =
        match other with
        | :? LuaValue as otherValue ->
            match this, otherValue with
            | LuaNil, LuaNil -> true
            | LuaBool a, LuaBool b -> a = b
            | LuaNumber a, LuaNumber b -> a = b
            | LuaInteger a, LuaInteger b -> a = b
            | LuaString a, LuaString b -> a = b
            | LuaTable a, LuaTable b -> System.Object.ReferenceEquals(a, b)  // Reference equality for tables
            | LuaFunction a, LuaFunction b -> System.Object.ReferenceEquals(a, b)  // Reference equality for functions
            | _ -> false
        | _ -> false
    
    override this.GetHashCode() =
        match this with
        | LuaNil -> 0
        | LuaBool b -> b.GetHashCode()
        | LuaNumber n -> n.GetHashCode()
        | LuaInteger i -> i.GetHashCode()
        | LuaString s -> s.GetHashCode()
        | LuaTable t -> System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(t)
        | LuaFunction f -> System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(f)

/// Represents a Lua table - the universal data structure
and [<StructuralEquality; NoComparison>] LuaTable = {
    /// Array part (1-indexed like Lua)
    Array: ResizeArray<LuaValue>
    /// Hash part (for non-integer keys)
    Hash: Dictionary<LuaValue, LuaValue>
    /// Metatable (for advanced features)
    Metatable: LuaTable option
}

/// Represents a Lua function
and [<NoEquality; NoComparison>] LuaFunction =
    | LuaClosure of LuaClosureInfo
    | LuaBuiltin of LuaBuiltinInfo

/// Information for Lua-defined functions (closures)
and [<NoEquality; NoComparison>] LuaClosureInfo = {
    Parameters: Parameter list
    IsVararg: bool
    Body: Block
    Environment: LuaEnvironment
}

/// Information for built-in F# functions
and [<NoEquality; NoComparison>] LuaBuiltinInfo = {
    Name: string
    Function: LuaValue list -> LuaValue list
}

/// Represents a variable environment (scope chain)
and [<StructuralEquality; NoComparison>] LuaEnvironment = {
    /// Local variables in current scope
    Locals: Dictionary<string, LuaValue>
    /// Parent environment (for nested scopes)
    Parent: LuaEnvironment option
    /// Global environment reference
    Globals: Dictionary<string, LuaValue>
}

/// Helper functions for working with Lua values
module LuaValue =
    
    /// Convert a Lua value to a string (for print, etc.)
    let toString = function
        | LuaNil -> "nil"
        | LuaBool true -> "true"
        | LuaBool false -> "false"
        | LuaNumber n -> n.ToString()
        | LuaInteger i -> i.ToString()
        | LuaString s -> s
        | LuaTable _ -> "table"  // TODO: add table address
        | LuaFunction _ -> "function"  // TODO: add function address
    
    /// Check if a value is "truthy" in Lua (everything except nil and false)
    let isTruthy = function
        | LuaNil | LuaBool false -> false
        | _ -> true
    
    /// Convert to boolean for logical operations
    let toBool value = isTruthy value
    
    /// Try to convert to number for arithmetic
    let tryToNumber (value: LuaValue) : float option =
        match value with
        | LuaNumber n -> Some n
        | LuaInteger i -> Some (float i)
        | LuaString s ->
            let (success, result) = System.Double.TryParse s
            if success then Some result else None
        | _ -> None
    
    /// Try to convert to integer
    let tryToInteger (value: LuaValue) : int64 option =
        match value with
        | LuaInteger i -> Some i
        | LuaNumber n when n = System.Math.Truncate(n) -> Some (int64 n)
        | LuaString s ->
            let (success, result) = System.Int64.TryParse s
            if success then Some result else None
        | _ -> None
    
    /// Get the Lua type name
    let typeName = function
        | LuaNil -> "nil"
        | LuaBool _ -> "boolean"
        | LuaNumber _ -> "number"
        | LuaInteger _ -> "number"  // In Lua, integers are still type "number"
        | LuaString _ -> "string"
        | LuaTable _ -> "table"
        | LuaFunction _ -> "function"

/// Helper functions for working with Lua tables
module LuaTable =
    
    /// Create a new empty table
    let empty () = {
        Array = ResizeArray<LuaValue>()
        Hash = Dictionary<LuaValue, LuaValue>()
        Metatable = None
    }
    
    /// Get a value from a table
    let get (table: LuaTable) (key: LuaValue) =
        match key with
        | LuaInteger i when i >= 1L && i <= int64 table.Array.Count ->
            table.Array.[int i - 1]  // Convert to 0-indexed
        | _ ->
            match table.Hash.TryGetValue key with
            | true, value -> value
            | false, _ -> LuaNil
    
    /// Set a value in a table
    let set (table: LuaTable) (key: LuaValue) (value: LuaValue) =
        match key with
        | LuaInteger i when i >= 1L ->
            // Extend array if necessary
            while table.Array.Count < int i do
                table.Array.Add(LuaNil)
            table.Array.[int i - 1] <- value
        | _ ->
            if value = LuaNil then
                table.Hash.Remove key |> ignore
            else
                table.Hash.[key] <- value

/// Helper functions for working with environments
module LuaEnvironment =
    
    /// Create a new environment with given globals
    let create (globals: Dictionary<string, LuaValue>) = {
        Locals = Dictionary<string, LuaValue>()
        Parent = None
        Globals = globals
    }
    
    /// Create a child environment
    let createChild (parent: LuaEnvironment) = {
        Locals = Dictionary<string, LuaValue>()
        Parent = Some parent
        Globals = parent.Globals
    }
    
    /// Get a variable value (searches locals, then parent chain, then globals)
    let rec getValue (env: LuaEnvironment) (name: string) =
        match env.Locals.TryGetValue name with
        | true, value -> value
        | false, _ ->
            match env.Parent with
            | Some parent -> getValue parent name
            | None ->
                match env.Globals.TryGetValue name with
                | true, value -> value
                | false, _ -> LuaNil
    
    /// Set a local variable
    let setLocal (env: LuaEnvironment) (name: string) (value: LuaValue) =
        env.Locals.[name] <- value
    
    /// Set a global variable
    let setGlobal (env: LuaEnvironment) (name: string) (value: LuaValue) =
        env.Globals.[name] <- value
    
    /// Set a variable (searches scope chain to find where it should be updated)
    let rec setVariable (env: LuaEnvironment) (name: string) (value: LuaValue) =
        // Check if variable exists in current environment's locals
        if env.Locals.ContainsKey name then
            env.Locals.[name] <- value
        else
            // Check parent environments
            match env.Parent with
            | Some parent ->
                // Recursively check if it exists in parent chain
                if containsVariable parent name then
                    setVariable parent name value
                else
                    // Variable doesn't exist in scope chain, create locally
                    env.Locals.[name] <- value
            | None ->
                // No parent, check globals
                if env.Globals.ContainsKey name then
                    env.Globals.[name] <- value
                else
                    // Variable doesn't exist anywhere, create locally
                    env.Locals.[name] <- value
    
    /// Check if a variable exists anywhere in the scope chain
    and containsVariable (env: LuaEnvironment) (name: string) =
        env.Locals.ContainsKey name ||
        env.Globals.ContainsKey name ||
        (env.Parent |> Option.map (fun parent -> containsVariable parent name) |> Option.defaultValue false)