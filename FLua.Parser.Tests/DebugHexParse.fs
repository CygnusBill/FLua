module DebugHexParse

open System
open FParsec
open FLua.Ast
open FLua.Parser.Parser

[<EntryPoint>]
let main argv =
    // Test various number formats
    let testCases = [
        "42"
        "3.14"
        "0xff"
        "0xFF"
        "0x1.5"
        "0xABCp-3"
    ]
    
    for test in testCases do
        printf "Parsing '%s': " test
        match run expr test with
        | Success(result, _, _) -> 
            printfn "%A" result
        | Failure(errorMsg, _, _) -> 
            printfn "FAILED - %s" errorMsg
    
    0
