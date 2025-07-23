open System
open System.IO
open System.Text
open FParsec
open FLua.Parser.Lexer

[<EntryPoint>]
let main argv =
    let luaDir = "LuaTests"
    let files = Directory.GetFiles(luaDir, "*.lua")
    let parser =
        many (
            attempt pShebang
            <|> attempt pSingleLineComment
            <|> attempt pAngleAttribute
            <|> attempt pWhitespace
            <|> pKeyword
            <|> pNumber
            <|> pString
            <|> pIdentifier
            <|> pSymbol
        )
    let mutable anyError = false
    for filePath in files do
        let fileName = Path.GetFileName(filePath)
        printfn $"START: {fileName}"
        let result = runParserOnFile parser () filePath Encoding.UTF8
        match result with
        | Success(_, _, _) ->
            printfn $"FINISH: {fileName}"
        | Failure(msg, err, _) ->
            anyError <- true
            printfn $"ERROR: {fileName}"
            printfn $"Lexer error at %A{err.Position}: %s{msg}"
    if anyError then 1 else 0
