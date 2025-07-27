#r "nuget: FParsec"
#r "FLua.Ast/bin/Debug/net10.0/FLua.Ast.dll"
#r "FLua.Parser/bin/Debug/net10.0/FLua.Parser.dll"

open FParsec
open FLua.Parser

let testCode = """t[1]"""

// Try parsing as lvalue
let lvalueResult = run Parser.pLvalue testCode
printfn "Lvalue parse result:"
match lvalueResult with
| Success(expr, _, pos) -> 
    printfn "Success: %A" expr
    printfn "Position after: %d" pos.Index
| Failure(msg, _, _) -> printfn "Failed: %s" msg

// Try the full assignment
let testAssign = """t[1] = 100"""
let assignResult = run Parser.pAssignment testAssign
printfn "\nAssignment parse result:"
match assignResult with
| Success(stmt, _, _) -> printfn "Success: %A" stmt
| Failure(msg, _, _) -> printfn "Failed: %s" msg