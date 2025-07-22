open System
open System.IO
open System.Text
open FParsec
open FLua.Parser.Lexer

[<EntryPoint>]
let main argv =
    let filePath = "LuaTests/api.lua"
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
    let result = runParserOnFile parser () filePath Encoding.UTF8
    match result with
    | Success(tokens, _, _) ->
        tokens |> List.iter (fun t -> printfn $"(%d{t.Line},%d{t.Column}): %A{t.Token}")
        0
    | Failure(msg, err, _) ->
        printfn $"Lexer error at %A{err.Position}: %s{msg}"
        1
