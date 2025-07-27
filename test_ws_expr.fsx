#r "nuget: FParsec"
#I "FLua.Ast/bin/Debug/net10.0"
#r "FLua.Ast.dll"
#I "FLua.Parser/bin/Debug/net10.0" 
#r "FLua.Parser.dll"

open FLua.Parser

// Test with different whitespace
let tests = [
    "a[1]+b[2]"          // No spaces
    "a[1] +b[2]"         // Space before +
    "a[1]+ b[2]"         // Space after +
    "a[1] + b[2]"       // Spaces around +
    "a[ 1 ] + b[ 2 ]"   // Spaces inside brackets
]

for test in tests do
    try
        let result = ParserHelper.ParseString($"local x = {test}")
        printfn "✓ %s" test
    with
    | ex -> 
        let msg = ex.Message.Split('\n').[0]
        let col = if msg.Contains("Col:") then 
                    msg.Substring(msg.IndexOf("Col:"))
                  else ""
        printfn "✗ %s - %s" test col