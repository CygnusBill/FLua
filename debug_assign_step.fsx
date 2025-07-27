#r "nuget: FParsec"
#r "FLua.Ast/bin/Debug/net10.0/FLua.Ast.dll"
#r "FLua.Parser/bin/Debug/net10.0/FLua.Parser.dll"

open FParsec
open FLua.Parser

// Step by step parsing
let testCode = """t[1] = 100"""

// First, parse lvalue
let lvalueParser = Parser.pLvalue .>> Parser.ws
let step1 = run lvalueParser testCode
printfn "Step 1 - pLvalue:"
match step1 with
| Success(expr, _, pos) -> 
    printfn "  Success: %A" expr
    printfn "  Position: %d" pos.Index
    printfn "  Remaining: '%s'" testCode.[int pos.Index..]
| Failure(msg, _, _) -> printfn "  Failed: %s" msg

// Parse lvalue then equals
let lvalueEquals = Parser.pLvalue .>> Parser.symbol "="
let step2 = run lvalueEquals testCode
printfn "\nStep 2 - pLvalue .>> symbol '=':"
match step2 with
| Success(expr, _, pos) -> 
    printfn "  Success: %A" expr
    printfn "  Remaining: '%s'" testCode.[int pos.Index..]
| Failure(msg, _, _) -> printfn "  Failed: %s" msg