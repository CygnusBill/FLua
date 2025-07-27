#r "nuget: FParsec"
#I "FLua.Ast/bin/Debug/net10.0"
#r "FLua.Ast.dll"
#I "FLua.Parser/bin/Debug/net10.0"
#r "FLua.Parser.dll"

open FLua.Parser

// Test different expression types
let tests = [
    "1 + 2"              // Basic binary
    "a + b"              // Variable binary  
    "f() + g()"          // Function call binary
    "(a) + (b)"          // Parenthesized
    "a.x + b.y"          // Dot access (works)
    "a[1] + b[2]"        // Bracket access (fails)
    "a[x] + b[y]"        // Bracket with var index
    "{1} + {2}"          // Table constructor  
]

for test in tests do
    try
        let result = ParserHelper.ParseString($"local x = {test}")
        printfn "✓ %s" test
    with
    | ex -> printfn "✗ %s - %s" test (ex.Message.Split('\n').[0])