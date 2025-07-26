// Add the parser DLL reference
#r "/Users/bill/Repos/FLua/FLua.Parser/bin/Debug/net8.0/FLua.Parser.dll"
#r "/Users/bill/Repos/FLua/FLua.Ast/bin/Debug/net8.0/FLua.Ast.dll"

open FLua.Parser

// Test parsing
let testParse code =
    printfn "Parsing: %s" code
    match ParserHelper.ParseString(code) with
    | Ok ast -> 
        printfn "Success!"
        printfn "AST: %A" ast
    | Error err -> 
        printfn "Error: %s" err
    printfn ""

// Test cases
testParse "for k in pairs{1,2,3} do end"
testParse "for k in pairs({1,2,3}) do end"
testParse "if f{} then end"
testParse "if f({}) then end"
testParse "print{1,2,3}"  // This should work