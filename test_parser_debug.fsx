#r "nuget: FParsec"
open FParsec

// Test the interaction between sepBy1 and expressions with table constructors

let test p str =
    match run p str with
    | Success(result,_,_) -> 
        printfn "Success: %A" result
    | Failure(errorMsg,_,_) -> 
        printfn "Failure: %s" errorMsg

// Simplified expression parser
let expr, exprRef = createParserForwardedToRef<string, unit>()

let identifier = regex @"[a-zA-Z_][a-zA-Z0-9_]*" .>> spaces

let tableConstructor = between (pchar '{' >>. spaces) (pchar '}') (pstring "table") .>> spaces

let functionCall = 
    identifier .>>. opt tableConstructor
    |>> function
        | (name, Some _) -> name + "{table}"
        | (name, None) -> name

do exprRef.Value <- choice [
    attempt functionCall
    identifier
]

// Test direct parsing
printfn "Direct expr parsing:"
test expr "pairs"
test expr "pairs{table}"

// Test with sepBy1
printfn "\nWith sepBy1:"
test (sepBy1 expr (pchar ',')) "pairs{table}"
test (sepBy1 expr (pchar ',')) "pairs{table}, other"

// Lazy version
let lazyExpr = fun stream -> expr stream
printfn "\nWith sepBy1 and lazy:"
test (sepBy1 lazyExpr (pchar ',')) "pairs{table}"
test (sepBy1 lazyExpr (pchar ',')) "pairs{table}, other"