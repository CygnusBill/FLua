namespace FLua.Parser

open FParsec
open FLua.Ast
open FLua.Parser.Parser
open FLua.Common.Diagnostics

/// Helper module with static methods for C# interoperability
module ParserHelper =
    
    /// Parse a string of Lua code and return the AST
    let ParseString (code: string) =
        match run luaFile code with
        | Success(result, _, _) -> result
        | Failure(errorMsg, error, _) -> 
            // Extract position information from FParsec error
            let line = int error.Position.Line
            let column = int error.Position.Column
            let location = SourceLocation(
                FileName = "input",
                Line = line,
                Column = column,
                Length = 1
            )
            
            // Create a more user-friendly error message
            let friendlyMsg = 
                if errorMsg.Contains("Expecting:") then
                    let lines = errorMsg.Split('\n')
                    let firstLine = lines.[0]
                    $"Syntax error at line {line}, column {column}: {firstLine}"
                else
                    $"Parse error at line {line}, column {column}: {errorMsg}"
            
            failwith friendlyMsg
    
    /// Parse a string of Lua code with filename and return the AST
    let ParseStringWithFileName (code: string) (fileName: string) =
        match runParserOnString luaFile () fileName code with
        | Success(result, _, _) -> result
        | Failure(errorMsg, error, _) -> 
            // Extract position information from FParsec error
            let line = int error.Position.Line
            let column = int error.Position.Column
            let location = SourceLocation(
                FileName = fileName,
                Line = line,
                Column = column,
                Length = 1
            )
            
            // Create a more user-friendly error message
            let friendlyMsg = 
                if errorMsg.Contains("Expecting:") then
                    let lines = errorMsg.Split('\n')
                    let firstLine = lines.[0]
                    $"Syntax error in {fileName} at line {line}, column {column}: {firstLine}"
                else
                    $"Parse error in {fileName} at line {line}, column {column}: {errorMsg}"
            
            failwith friendlyMsg
    
    /// Parse a Lua expression and return the AST
    let ParseExpression (code: string) =
        match run expr code with
        | Success(result, _, _) -> result
        | Failure(errorMsg, error, _) -> 
            // Extract position information from FParsec error
            let line = int error.Position.Line
            let column = int error.Position.Column
            let location = SourceLocation(
                FileName = "input",
                Line = line,
                Column = column,
                Length = 1
            )
            
            // Create a more user-friendly error message
            let friendlyMsg = 
                if errorMsg.Contains("Expecting:") then
                    let lines = errorMsg.Split('\n')
                    let firstLine = lines.[0]
                    $"Syntax error at line {line}, column {column}: {firstLine}"
                else
                    $"Parse error at line {line}, column {column}: {errorMsg}"
            
            failwith friendlyMsg 