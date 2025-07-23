module ManualTest

open FParsec
open FLua.Parser.Parser
open FLua.Interpreter.Interpreter
open FLua.Interpreter.Values

let testCode = """
    print("Starting FLua interpreter demo...")
    
    local x = 10
    local y = 20
    local sum = x + y
    
    print("x =", x)
    print("y =", y)
    print("x + y =", sum)
    
    print("Type of sum:", type(sum))
    print("Sum as string:", tostring(sum))
    
    return sum * 2
"""

let runDemo () =
    match FParsec.CharParsers.run luaFile testCode with
    | FParsec.CharParsers.Success (ast, _, _) ->
        printfn "✅ Parsed successfully!"
        match Interpreter.execute ast with
        | Success returnValues ->
            printfn "✅ Executed successfully!"
            printfn "Return values: %A" returnValues
        | Error msg ->
            printfn "❌ Runtime error: %s" msg
    | FParsec.CharParsers.Failure (msg, _, _) ->
        printfn "❌ Parse error: %s" msg 