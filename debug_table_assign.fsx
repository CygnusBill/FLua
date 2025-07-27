#r "nuget: FParsec"
#r "FLua.Ast/bin/Debug/net10.0/FLua.Ast.dll"
#r "FLua.Parser/bin/Debug/net10.0/FLua.Parser.dll"

open FParsec
open FLua.Parser

let testCode = """t[1] = 100"""

// Try parsing as expression first
let exprResult = run Parser.expr testCode
printfn "Expression parse result:"
match exprResult with
| Success(expr, _, _) -> 
    printfn "Success: %A" expr
    printfn "Type: %A" (expr.GetType().Name)
| Failure(msg, _, _) -> printfn "Failed: %s" msg

// Try parsing as statement
let stmtResult = run Parser.statement testCode
printfn "\nStatement parse result:"
match stmtResult with
| Success(stmt, _, _) -> printfn "Success: %A" stmt
| Failure(msg, _, _) -> printfn "Failed: %s" msg

// Try parsing the assignment part only
let assignResult = run Parser.pAssignmentOrFunctionCall testCode
printfn "\nAssignment parser result:"
match assignResult with
| Success(stmt, _, _) -> printfn "Success: %A" stmt
| Failure(msg, _, _) -> printfn "Failed: %s" msg