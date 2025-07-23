module FLua.Interpreter.Tests

open Expecto
open FParsec
open FLua.Parser.Parser
open FLua.Interpreter.Interpreter
open FLua.Interpreter.Values

// Helper function to parse and interpret Lua code
let interpretLua code =
    match FParsec.CharParsers.run luaFile code with
    | FParsec.CharParsers.Success (result, _, _) -> 
        Interpreter.execute result
    | FParsec.CharParsers.Failure (errorMsg, _, _) -> 
        Error $"Parse error: {errorMsg}"

// Helper function to parse and evaluate a Lua expression
let evaluateLua code =
    match FParsec.CharParsers.run expr code with
    | FParsec.CharParsers.Success (result, _, _) -> 
        Interpreter.evaluateExpression result
    | FParsec.CharParsers.Failure (errorMsg, _, _) -> 
        Error $"Parse error: {errorMsg}"

let tests = 
    testList "Interpreter Tests" [
        
        testList "Expression Evaluation" [
            test "literal numbers" {
                match evaluateLua "42" with
                | Success [LuaInteger 42L] -> ()
                | result -> failwithf "Expected integer 42, got %A" result
            }
            
            test "literal strings" {
                match evaluateLua "\"hello\"" with
                | Success [LuaString "hello"] -> ()
                | result -> failwithf "Expected string \"hello\", got %A" result
            }
            
            test "arithmetic" {
                match evaluateLua "1 + 2 * 3" with
                | Success [LuaNumber 7.0] -> ()
                | result -> failwithf "Expected number 7.0, got %A" result
            }
            
            test "modulo operator" {
                match evaluateLua "10 % 3" with
                | Success [LuaNumber 1.0] -> ()
                | result -> failwithf "Expected number 1.0, got %A" result
            }
            
            test "floor division operator" {
                match evaluateLua "10 // 3" with
                | Success [LuaNumber 3.0] -> ()
                | result -> failwithf "Expected number 3.0, got %A" result
            }
            
            test "boolean logic" {
                match evaluateLua "true and false" with
                | Success [LuaBool false] -> ()
                | result -> failwithf "Expected boolean false, got %A" result
            }
        ]
        
        testList "Statement Execution" [
            test "print statement" {
                match interpretLua "print(\"Hello, World!\")" with
                | Success [] -> ()  // print returns no values
                | result -> failwithf "Expected empty result, got %A" result
            }
            
            test "local assignment" {
                match interpretLua "local x = 42; return x" with
                | Success [LuaInteger 42L] -> ()
                | result -> failwithf "Expected integer 42, got %A" result
            }
            
            test "arithmetic assignment" {
                match interpretLua "local x = 10; local y = 20; return x + y" with
                | Success [LuaNumber 30.0] -> ()
                | result -> failwithf "Expected number 30.0, got %A" result
            }
            
            test "while loop" {
                match interpretLua "local sum = 0; local i = 1; while i <= 3 do sum = sum + i; i = i + 1 end; return sum" with
                | Success [LuaNumber 6.0] -> ()
                | result -> failwithf "Expected number 6.0, got %A" result
            }
            
            test "numeric for loop" {
                match interpretLua "local sum = 0; for i = 1, 3 do sum = sum + i end; return sum" with
                | Success [LuaNumber 6.0] -> ()
                | result -> failwithf "Expected number 6.0, got %A" result
            }
            
            test "local function definition" {
                match interpretLua "local function add(a, b) return a + b end; return add(5, 3)" with
                | Success [LuaNumber 8.0] -> ()
                | result -> failwithf "Expected number 8.0, got %A" result
            }
        ]
        
        testList "Built-in Functions" [
            test "type function" {
                match interpretLua "return type(42)" with
                | Success [LuaString "number"] -> ()
                | result -> failwithf "Expected string \"number\", got %A" result
            }
            
            test "tostring function" {
                match interpretLua "return tostring(123)" with
                | Success [LuaString "123"] -> ()
                | result -> failwithf "Expected string \"123\", got %A" result
            }
        ]
    ]

[<EntryPoint>]
let main argv =
    // Run the demo first
    printfn "🚀 FLua Interpreter Demo:"
    printfn "========================"
    ManualTest.runDemo ()
    printfn ""
    printfn "🧪 Running Unit Tests:"
    printfn "====================="
    
    runTestsWithCLIArgs [] argv tests 