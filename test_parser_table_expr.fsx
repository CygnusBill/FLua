#r "nuget: FParsec"
#I "FLua.Ast/bin/Debug/net10.0"
#r "FLua.Ast.dll"
#I "FLua.Parser/bin/Debug/net10.0"
#r "FLua.Parser.dll"

open FLua.Parser

// Test parsing table access in expressions
let testParse code =
    try
        let result = ParserHelper.ParseString(code)
        printfn "Success parsing: %s" code
        printfn "AST: %A" result
    with
    | ex -> printfn "Failed parsing: %s\nError: %s" code ex.Message

// Test cases
testParse "local t = {10, 20}"
testParse "local x = t[1]"
testParse "local sum = t[1] + t[2]"  // This should fail
testParse "local sum = a.x + b.y"    // This should also fail