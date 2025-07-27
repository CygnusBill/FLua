#r "nuget: FParsec"
#r "FLua.Ast/bin/Debug/net10.0/FLua.Ast.dll"
#r "FLua.Parser/bin/Debug/net10.0/FLua.Parser.dll"

open FParsec
open FLua.Parser

// Test just the sepBy1 part
let testCode = """t[1]"""
let sepByResult = run (sepBy1 Parser.pLvalue (Parser.symbol ",")) testCode
printfn "sepBy1 pLvalue result:"
match sepByResult with
| Success(exprs, _, pos) -> 
    printfn "Success: %A" exprs
    printfn "Remaining: '%s'" testCode.[int pos.Index..]
| Failure(msg, _, _) -> printfn "Failed: %s" msg

// Test with assignment
let testAssign = """t[1] = 100"""  
let assignResult = run Parser.pAssignmentOrCall testAssign
printfn "\npAssignmentOrCall result:"
match assignResult with
| Success(stmt, _, _) -> printfn "Success: %A" stmt
| Failure(msg, _, _) -> printfn "Failed: %s" msg