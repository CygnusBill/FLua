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
    
    /// next(table, key) - iterator for tables
    let next (args: LuaValue list) =
        match args with
        | [LuaTable table; LuaNil] ->
            // Start iteration: return first key-value pair
            if table.Array.Count > 0 then
                [LuaInteger 1L; table.Array.[0]]  // First array element
            elif table.Hash.Count > 0 then
                let firstPair = table.Hash |> Seq.head
                [firstPair.Key; firstPair.Value]  // First hash element
            else
                [LuaNil]  // Empty table
        | [LuaTable table; LuaInteger index] when index >= 1L && index < int64 table.Array.Count ->
            // Continue array iteration
            let nextIndex = index + 1L
            if nextIndex <= int64 table.Array.Count then
                [LuaInteger nextIndex; table.Array.[int (nextIndex - 1L)]]
            elif table.Hash.Count > 0 then
                // Switch to hash part
                let firstPair = table.Hash |> Seq.head
                [firstPair.Key; firstPair.Value]
            else
                [LuaNil]  // End of iteration
        | [LuaTable table; key] ->
            // Continue hash iteration
            let currentKeys = table.Hash.Keys |> Seq.toList
            match currentKeys |> List.tryFindIndex (fun k -> k = key) with
            | Some index when index + 1 < currentKeys.Length ->
                let nextKey = currentKeys.[index + 1]
                [nextKey; table.Hash.[nextKey]]
            | _ -> [LuaNil]  // End of iteration
        | _ -> raise (LuaRuntimeError "next expects table and key arguments")
    
    /// pairs(table) - returns iterator for table (simplified)
    let pairs (args: LuaValue list) =
        match args with
        | [LuaTable table] ->
            // Return the next function and the table
            [LuaFunction (LuaBuiltin { Name = "next"; Function = next }); LuaTable table; LuaNil]
        | _ -> raise (LuaRuntimeError "pairs expects a table argument")
    
    /// ipairs iterator function
    let ipairsNext (args: LuaValue list) =
        match args with
        | [LuaTable table; LuaInteger index] ->
            let nextIndex = index + 1L
            if nextIndex <= int64 table.Array.Count then
                [LuaInteger nextIndex; table.Array.[int (nextIndex - 1L)]]
            else
                [LuaNil]  // End of array iteration
        | [LuaTable table; LuaNil] ->
            // Start ipairs iteration
            if table.Array.Count > 0 then
                [LuaInteger 1L; table.Array.[0]]
            else
                [LuaNil]
        | _ -> raise (LuaRuntimeError "ipairs iterator expects table and index arguments")
    
    /// ipairs(table) - returns iterator for array part only
    let ipairs (args: LuaValue list) =
        match args with
        | [LuaTable table] ->
            [LuaFunction (LuaBuiltin { Name = "ipairsNext"; Function = ipairsNext }); LuaTable table; LuaInteger 0L]
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
    
    /// Add string library functions to the global environment
    let addStringLibrary (globals: Dictionary<string, LuaValue>) =
        let stringTable = LuaTable.empty ()
        
        // Basic string functions
        let stringFunctions = [
            "len", fun args ->
                match args with
                | [LuaString s] -> [LuaInteger (int64 s.Length)]
                | _ -> raise (LuaRuntimeError "string.len expects a string")
            
            "sub", fun args ->
                match args with
                | [LuaString s; LuaInteger start] ->
                    let startIdx = max 0 (int (start - 1L))  // Lua is 1-indexed
                    if startIdx >= s.Length then [LuaString ""]
                    else [LuaString (s.Substring(startIdx))]
                | [LuaString s; LuaInteger start; LuaInteger endIdx] ->
                    let startIdx = max 0 (int (start - 1L))
                    let endIdx = min s.Length (int endIdx)
                    if startIdx >= s.Length || startIdx >= endIdx then [LuaString ""]
                    else [LuaString (s.Substring(startIdx, endIdx - startIdx))]
                | _ -> raise (LuaRuntimeError "string.sub expects string and number arguments")
            
            "upper", fun args ->
                match args with
                | [LuaString s] -> [LuaString (s.ToUpper())]
                | _ -> raise (LuaRuntimeError "string.upper expects a string")
            
            "lower", fun args ->
                match args with
                | [LuaString s] -> [LuaString (s.ToLower())]
                | _ -> raise (LuaRuntimeError "string.lower expects a string")
            
            "rep", fun args ->
                match args with
                | [LuaString s; LuaInteger n] when n >= 0L ->
                    [LuaString (String.replicate (int n) s)]
                | [LuaString _; LuaInteger n] when n < 0L ->
                    [LuaString ""]
                | _ -> raise (LuaRuntimeError "string.rep expects a string and non-negative number")
            
            "reverse", fun args ->
                match args with
                | [LuaString s] -> 
                    let chars = s.ToCharArray()
                    Array.Reverse(chars)
                    [LuaString (new string(chars))]
                | _ -> raise (LuaRuntimeError "string.reverse expects a string")
                
            "char", fun args ->
                try
                    let chars = 
                        args 
                        |> List.map (function 
                            | LuaInteger i when i >= 0L && i <= 255L -> char (int i)
                            | LuaNumber n when n >= 0.0 && n <= 255.0 && n = Math.Truncate(n) -> char (int n)
                            | _ -> raise (LuaRuntimeError "string.char expects numbers in range 0-255"))
                        |> List.toArray
                    [LuaString (new string(chars))]
                with
                | :? LuaRuntimeError as e -> raise e
                | _ -> raise (LuaRuntimeError "string.char expects valid character codes")
            
            "byte", fun args ->
                match args with
                | [LuaString s] when s.Length > 0 -> [LuaInteger (int64 s.[0])]
                | [LuaString s] -> [] // Empty string
                | [LuaString s; LuaInteger pos] ->
                    let idx = int (pos - 1L)  // Lua is 1-indexed
                    if idx >= 0 && idx < s.Length then [LuaInteger (int64 s.[idx])]
                    else []
                | _ -> raise (LuaRuntimeError "string.byte expects string and optional position")
        ]
        
        // Add functions to string table
        for name, func in stringFunctions do
            LuaTable.set stringTable (LuaString name) (LuaFunction (LuaBuiltin { Name = $"string.{name}"; Function = func }))
        
        // Add string table to globals
        globals.["string"] <- LuaTable stringTable
    
    /// Add I/O library functions to the global environment
    let addIOLibrary (globals: Dictionary<string, LuaValue>) =
        let ioTable = LuaTable.empty ()
        
        // Basic I/O functions
        let ioFunctions = [
            "write", fun args ->
                // io.write(...) - writes to stdout without newline
                let output = 
                    args 
                    |> List.map LuaValue.toString
                    |> String.concat ""
                printf "%s" output
                [LuaTable ioTable]  // Return io table (like Lua)
            
            "read", fun args ->
                // io.read() - reads from stdin
                match args with
                | [] | [LuaString "*l"] ->
                    // Read line (default)
                    try
                        let line = System.Console.ReadLine()
                        if line = null then [LuaNil] else [LuaString line]
                    with
                    | _ -> [LuaNil]
                | [LuaString "*a"] ->
                    // Read all
                    try
                        let mutable lines = []
                        let mutable line = System.Console.ReadLine()
                        while line <> null do
                            lines <- line :: lines
                            line <- System.Console.ReadLine()
                        let content = lines |> List.rev |> String.concat "\n"
                        [LuaString content]
                    with
                    | _ -> [LuaNil]
                | [LuaInteger n] when n > 0L ->
                    // Read n characters
                    try
                        let chars = Array.zeroCreate (int n)
                        let bytesRead = System.Console.In.ReadBlock(chars, 0, int n)
                        let content = new string(chars, 0, bytesRead)
                        [LuaString content]
                    with
                    | _ -> [LuaNil]
                | _ -> raise (LuaRuntimeError "invalid arguments to io.read")
            
            "open", fun args ->
                // io.open(filename, mode) - opens a file
                match args with
                | [LuaString filename] | [LuaString filename; LuaString "r"] ->
                    // Read mode (default)
                    try
                        if System.IO.File.Exists(filename) then
                            let content = System.IO.File.ReadAllText(filename)
                            let fileTable = LuaTable.empty ()
                            
                            // Add file methods
                            let readMethod = LuaFunction (LuaBuiltin { 
                                Name = "file:read"
                                Function = fun _ -> [LuaString content]
                            })
                            let closeMethod = LuaFunction (LuaBuiltin { 
                                Name = "file:close"
                                Function = fun _ -> [LuaTable fileTable]
                            })
                            
                            LuaTable.set fileTable (LuaString "read") readMethod
                            LuaTable.set fileTable (LuaString "close") closeMethod
                            
                            [LuaTable fileTable]
                        else
                            [LuaNil; LuaString "No such file or directory"]
                    with
                    | ex -> [LuaNil; LuaString ex.Message]
                | [LuaString filename; LuaString "w"] ->
                    // Write mode
                    try
                        let fileTable = LuaTable.empty ()
                        let mutable fileContent = ""
                        
                        let writeMethod = LuaFunction (LuaBuiltin { 
                            Name = "file:write"
                            Function = fun args ->
                                let text = args |> List.map LuaValue.toString |> String.concat ""
                                fileContent <- fileContent + text
                                [LuaTable fileTable]
                        })
                        let closeMethod = LuaFunction (LuaBuiltin { 
                            Name = "file:close"
                            Function = fun _ ->
                                System.IO.File.WriteAllText(filename, fileContent)
                                [LuaTable fileTable]
                        })
                        
                        LuaTable.set fileTable (LuaString "write") writeMethod
                        LuaTable.set fileTable (LuaString "close") closeMethod
                        
                        [LuaTable fileTable]
                    with
                    | ex -> [LuaNil; LuaString ex.Message]
                | _ -> raise (LuaRuntimeError "io.open expects filename and optional mode")
            
            "close", fun args ->
                // io.close(file) - closes a file
                match args with
                | [LuaTable fileTable] ->
                    // Call the file's close method if it exists
                    match LuaTable.get fileTable (LuaString "close") with
                    | LuaFunction closeFunc -> 
                        // This would normally call the function, but for simplicity just return success
                        [LuaTable fileTable]
                    | _ -> [LuaTable fileTable]
                | [] -> [LuaTable ioTable]  // Close stdout (no-op)
                | _ -> raise (LuaRuntimeError "io.close expects file handle")
        ]
        
        // Add functions to io table
        for name, func in ioFunctions do
            LuaTable.set ioTable (LuaString name) (LuaFunction (LuaBuiltin { Name = $"io.{name}"; Function = func }))
        
        // Add io table to globals
        globals.["io"] <- LuaTable ioTable 