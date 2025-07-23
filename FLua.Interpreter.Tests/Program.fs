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
            
            test "bitwise AND operator" {
                match evaluateLua "12 & 7" with
                | Success [LuaInteger 4L] -> ()
                | result -> failwithf "Expected integer 4, got %A" result
            }
            
            test "bitwise OR operator" {
                match evaluateLua "12 | 7" with
                | Success [LuaInteger 15L] -> ()
                | result -> failwithf "Expected integer 15, got %A" result
            }
            
            test "bitwise NOT operator" {
                match evaluateLua "~12" with
                | Success [LuaInteger -13L] -> ()
                | result -> failwithf "Expected integer -13, got %A" result
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
            
            test "repeat until loop" {
                match interpretLua "local sum = 0; local i = 1; repeat sum = sum + i; i = i + 1 until i > 3; return sum" with
                | Success [LuaNumber 6.0] -> ()
                | result -> failwithf "Expected number 6.0, got %A" result
            }
            
            test "varargs function" {
                match interpretLua "local function test(a, ...) return a end; return test(42, 1, 2, 3)" with
                | Success [LuaInteger 42L] -> ()
                | result -> failwithf "Expected integer 42, got %A" result
            }
            
            test "break statement in for loop" {
                match interpretLua "local sum = 0; for i = 1, 10 do sum = sum + i; if i >= 3 then break end end; return sum" with
                | Success [LuaNumber 6.0] -> ()  // 1 + 2 + 3 = 6
                | result -> failwithf "Expected number 6.0 (sum of 1+2+3), got %A" result
            }
            
            test "function expression with body" {
                match interpretLua "local add = function(x, y) return x + y end; return add(5, 3)" with
                | Success [LuaNumber 8.0] -> ()
                | result -> failwithf "Expected number 8.0, got %A" result
            }
            
            test "anonymous function expression" {
                match interpretLua "local calc = function(n) return n * 2 + 1 end; return calc(10)" with
                | Success [LuaNumber 21.0] -> ()  // 10 * 2 + 1 = 21
                | result -> failwithf "Expected number 21.0, got %A" result
            }
            
            test "simple goto and label" {
                match interpretLua "local x = 1; goto skip; x = 2; ::skip:: return x" with
                | Success [LuaInteger 1L] -> ()  // Should skip x = 2
                | result -> failwithf "Expected integer 1, got %A" result
            }
            
            // Complex goto cases (nested blocks) not yet implemented
            // test "goto with backward jump" {
            //     match interpretLua "local i = 0; ::loop:: i = i + 1; if i < 3 then goto loop end; return i" with
            //     | Success [LuaNumber 3.0] -> ()  // Should loop until i = 3
            //     | result -> failwithf "Expected number 3.0, got %A" result
            // }
            // 
            // test "goto out of nested block" {
            //     match interpretLua "local x = 1; do x = 2; goto exit; x = 3 end; x = 4; ::exit:: return x" with
            //     | Success [LuaNumber 2.0] -> ()  // Should jump out of do block
            //     | result -> failwithf "Expected number 2.0, got %A" result
            // }
            
            // Generic for loops need both parser and interpreter fixes
            // test "generic for with simple variable" {
            //     match interpretLua "local t = {10, 20, 30}; local sum = 0; for k, v in t do sum = sum + (v or 0) end; return sum" with
            //     | Success [LuaNumber 60.0] -> ()  // 10 + 20 + 30 = 60
            //     | result -> failwithf "Expected number 60.0, got %A" result
            // }
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
            
            test "string.len function" {
                match interpretLua "return string.len(\"hello\")" with
                | Success [LuaInteger 5L] -> ()
                | result -> failwithf "Expected integer 5, got %A" result
            }
            
            test "string.upper function" {
                match interpretLua "return string.upper(\"hello\")" with
                | Success [LuaString "HELLO"] -> ()
                | result -> failwithf "Expected string \"HELLO\", got %A" result
            }
            
            test "string.sub function" {
                match interpretLua "return string.sub(\"hello\", 2, 4)" with
                | Success [LuaString "ell"] -> ()
                | result -> failwithf "Expected string \"ell\", got %A" result
            }
            
            test "string.rep function" {
                match interpretLua "return string.rep(\"ha\", 3)" with
                | Success [LuaString "hahaha"] -> ()
                | result -> failwithf "Expected string \"hahaha\", got %A" result
            }
            
            test "io.write function" {
                // io.write should work but return value is io table (complex to test)
                match interpretLua "io.write(\"test\"); return \"done\"" with
                | Success [LuaString "done"] -> ()
                | result -> failwithf "Expected string \"done\", got %A" result
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