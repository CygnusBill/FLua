#load "FLua.Parser/Parser.fs"

open FLua.Parser.Parser
open FParsec

// Test parsing table access in expressions
let testExpr input =
    match run pExpr input with
    | Success(result, _, _) -> 
        printfn "Success: %A" result
    | Failure(msg, _, _) -> 
        printfn "Failed: %s" msg

// Test basic table access
printfn "Testing basic table access:"
testExpr "t[1]"

// Test table access in binary expression
printfn "\nTesting table access in binary expression:"
testExpr "t[1] + t[2]"

// Test dot notation in expression
printfn "\nTesting dot notation in expression:"
testExpr "a.x + b.x"

// Let's test the expression parser step by step
printfn "\nTesting expression parser components:"

// Test primary expression
match run pPrimaryExpr "t[1]" with
| Success(result, _, _) -> printfn "Primary expr: %A" result
| Failure(msg, _, _) -> printfn "Primary failed: %s" msg

// Test with binary op parser
match run pBinExpr "t[1] + t[2]" with
| Success(result, _, _) -> printfn "Binary expr: %A" result  
| Failure(msg, _, _) -> printfn "Binary failed: %s" msg