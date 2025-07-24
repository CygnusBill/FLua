namespace FLua.Parser

open FParsec
open FLua.Ast
open FLua.Parser.Parser

/// Helper module with static methods for C# interoperability
module ParserHelper =
    
    /// Parse a string of Lua code and return the AST
    let ParseString (code: string) =
        match run luaFile code with
        | Success(result, _, _) -> result
        | Failure(errorMsg, _, _) -> 
            failwith $"Parse error: {errorMsg}"
    
    /// Parse a Lua expression and return the AST
    let ParseExpression (code: string) =
        match run expr code with
        | Success(result, _, _) -> result
        | Failure(errorMsg, _, _) -> 
            failwith $"Parse error: {errorMsg}" 