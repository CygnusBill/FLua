module FLua.Parser.Tests.ParserComprehensiveTests

open System
open System.Numerics
open Expecto
open FParsec
open FLua.Ast
open FLua.Parser.Parser

/// Helper to parse a string and assert success
let parseSuccess parser input =
    match run parser input with
    | Success(result, _, _) -> result
    | Failure(msg, _, _) -> 
        failwithf "Parse failed for '%s': %s" input msg

/// Helper to assert parse failure
let parseFailure parser input =
    match run parser input with
    | Success(_, _, _) -> 
        failwithf "Expected parse failure for '%s' but succeeded" input
    | Failure(_, _, _) -> ()

[<Tests>]
let hexLiteralTests =
    testList "Hex literal parsing" [
        testCase "Standard hex values" <| fun () ->
            let result = parseSuccess pNumberLiteral "0xFF"
            match result with
            | Expr.Literal(Literal.Integer(value)) -> 
                Expect.equal value (bigint 255) "0xFF should be 255"
            | _ -> failtest "Expected integer literal"
        
        testCase "Max Int64 value" <| fun () ->
            let result = parseSuccess pNumberLiteral "0x7fffffffffffffff"
            match result with
            | Expr.Literal(Literal.Integer(value)) -> 
                Expect.equal value (bigint Int64.MaxValue) "Should parse max int64"
            | _ -> failtest "Expected integer literal"
        
        testCase "Min Int64 value (was causing overflow)" <| fun () ->
            let result = parseSuccess pNumberLiteral "0x8000000000000000"
            match result with
            | Expr.Literal(Literal.Integer(value)) -> 
                Expect.equal value (bigint Int64.MinValue) "Should parse min int64"
            | _ -> failtest "Expected integer literal"
        
        testCase "-1 as hex" <| fun () ->
            let result = parseSuccess pNumberLiteral "0xffffffffffffffff"
            match result with
            | Expr.Literal(Literal.Integer(value)) -> 
                Expect.equal value (bigint -1L) "0xffffffffffffffff should be -1"
            | _ -> failtest "Expected integer literal"
    ]

[<Tests>]
let decimalNumberTests =
    testList "Decimal number parsing" [
        testCase "Numbers starting with dot" <| fun () ->
            let test1 = parseSuccess pNumberLiteral ".5"
            match test1 with
            | Expr.Literal(Literal.Float(value)) -> 
                Expect.floatClose Accuracy.high value 0.5 ".5 should be 0.5"
            | _ -> failtest "Expected float literal"
            
            let test2 = parseSuccess pNumberLiteral ".123"
            match test2 with
            | Expr.Literal(Literal.Float(value)) -> 
                Expect.floatClose Accuracy.high value 0.123 ".123 should be 0.123"
            | _ -> failtest "Expected float literal"
        
        testCase "Numbers ending with dot" <| fun () ->
            let result = parseSuccess pNumberLiteral "3."
            match result with
            | Expr.Literal(Literal.Float(value)) -> 
                Expect.floatClose Accuracy.high value 3.0 "3. should be 3.0"
            | _ -> failtest "Expected float literal"
    ]

[<Tests>]
let scientificNotationTests =
    testList "Scientific notation parsing" [
        testCase "Positive exponent" <| fun () ->
            let result = parseSuccess pNumberLiteral "1E5"
            match result with
            | Expr.Literal(Literal.Float(value)) -> 
                Expect.floatClose Accuracy.high value 100000.0 "1E5 should be 100000"
            | _ -> failtest "Expected float literal"
        
        testCase "Negative exponent" <| fun () ->
            let result = parseSuccess pNumberLiteral "1e-5"
            match result with
            | Expr.Literal(Literal.Float(value)) -> 
                Expect.floatClose Accuracy.high value 0.00001 "1e-5 should be 0.00001"
            | _ -> failtest "Expected float literal"
        
        testCase "Explicit positive exponent" <| fun () ->
            let result = parseSuccess pNumberLiteral "3.14E+2"
            match result with
            | Expr.Literal(Literal.Float(value)) -> 
                Expect.floatClose Accuracy.high value 314.0 "3.14E+2 should be 314"
            | _ -> failtest "Expected float literal"
    ]

[<Tests>]
let longStringTests =
    testList "Long string parsing" [
        testCase "Initial newline removal" <| fun () ->
            Expect.equal (parseSuccess pString "[[\nHello]]") "Hello" 
                "Should remove initial \\n"
            Expect.equal (parseSuccess pString "[[\r\nHello]]") "Hello" 
                "Should remove initial \\r\\n"
            Expect.equal (parseSuccess pString "[[\rHello]]") "Hello" 
                "Should remove initial \\r"
        
        testCase "No initial newline" <| fun () ->
            Expect.equal (parseSuccess pString "[[Hello]]") "Hello" 
                "Should not modify string without initial newline"
        
        testCase "Nested brackets" <| fun () ->
            Expect.equal (parseSuccess pString "[=[\nTest]=]") "Test" 
                "Should handle nested bracket syntax"
    ]

[<Tests>]
let identifierTests =
    testList "Identifier parsing" [
        testCase "Underscore as identifier" <| fun () ->
            Expect.equal (parseSuccess identifier "_") "_" 
                "Single underscore should be valid"
            Expect.equal (parseSuccess identifier "_test") "_test" 
                "Underscore prefix should be valid"
            Expect.equal (parseSuccess identifier "test_") "test_" 
                "Underscore suffix should be valid"
            Expect.equal (parseSuccess identifier "__internal__") "__internal__" 
                "Multiple underscores should be valid"
        
        testCase "Reserved words" <| fun () ->
            let reserved = ["and"; "break"; "do"; "else"; "elseif"; "end"; 
                            "false"; "for"; "function"; "if"; "in"; "local"; 
                            "nil"; "not"; "or"; "repeat"; "return"; "then"; 
                            "true"; "until"; "while"]
            
            for word in reserved do
                parseFailure identifier word
    ]

[<Tests>]
let stringEscapeTests =
    testList "String escape sequences" [
        testCase "Basic escapes" <| fun () ->
            Expect.equal (parseSuccess pString "\"\\n\\t\\r\"") "\n\t\r" 
                "Should parse basic escape sequences"
        
        testCase "Hex escape" <| fun () ->
            Expect.equal (parseSuccess pString "\"\\x41\"") "A" 
                "\\x41 should be 'A'"
        
        testCase "Decimal escape" <| fun () ->
            Expect.equal (parseSuccess pString "\"\\65\"") "A" 
                "\\65 should be 'A'"
        
        testCase "Unicode escape" <| fun () ->
            Expect.equal (parseSuccess pString "\"\\u{41}\"") "A" 
                "\\u{41} should be 'A'"
    ]

[<Tests>]
let genericForTests =
    testList "Generic for loop parsing" [
        testCase "Standard syntax with parentheses" <| fun () ->
            let result = parseSuccess statement "for k, v in pairs({1, 2, 3}) do end"
            match result with
            | Statement.GenericFor(names, exprs, _) ->
                Expect.equal (List.length names) 2 "Should have 2 variables"
                Expect.equal (List.length exprs) 1 "Should have 1 expression"
            | _ -> failtest "Expected generic for statement"
        
        testCase "Multiple iterator values" <| fun () ->
            let result = parseSuccess statement "for a, b, c in x, y, z do end"
            match result with
            | Statement.GenericFor(names, exprs, _) ->
                Expect.equal (List.length names) 3 "Should have 3 variables"
                Expect.equal (List.length exprs) 3 "Should have 3 expressions"
            | _ -> failtest "Expected generic for statement"
        
        testCase "Known limitation: f{} syntax" <| fun () ->
            // This is a known limitation - should fail
            parseFailure statement "for k in pairs{1,2,3} do end"
            
            // Workaround with parentheses should work
            let result = parseSuccess statement "for k in pairs({1,2,3}) do end"
            match result with
            | Statement.GenericFor(_, _, _) -> () // Success
            | _ -> failtest "Expected generic for statement"
    ]

[<Tests>]
let operatorPrecedenceTests =
    testList "Operator precedence" [
        testCase "Arithmetic precedence" <| fun () ->
            let result = parseSuccess expr "1 + 2 * 3"
            match result with
            | Expr.Binary(Expr.Literal(Literal.Integer(one)), BinaryOp.Add, 
                          Expr.Binary(Expr.Literal(Literal.Integer(two)), BinaryOp.Multiply, 
                                     Expr.Literal(Literal.Integer(three)))) ->
                Expect.equal one (bigint 1L) "First operand"
                Expect.equal two (bigint 2L) "Second operand"
                Expect.equal three (bigint 3L) "Third operand"
            | _ -> failtest "Incorrect precedence parsing"
        
        testCase "Power is right associative" <| fun () ->
            let result = parseSuccess expr "2 ^ 3 ^ 2"
            match result with
            | Expr.Binary(_, BinaryOp.Power, Expr.Binary(_, BinaryOp.Power, _)) ->
                () // Correct: 2^(3^2)
            | _ -> failtest "Power should be right associative"
    ]

[<Tests>]
let functionCallTests =
    testList "Function call parsing" [
        testCase "Standard call" <| fun () ->
            let result = parseSuccess expr "f(1, 2, 3)"
            match result with
            | Expr.FunctionCall(_, args) -> 
                Expect.equal (List.length args) 3 "Should have 3 arguments"
            | _ -> failtest "Expected function call"
        
        testCase "Call with table constructor (no parens)" <| fun () ->
            let result = parseSuccess expr "f{a = 1}"
            match result with
            | Expr.FunctionCall(_, args) -> 
                Expect.equal (List.length args) 1 "Should have 1 argument"
                match args.[0] with
                | Expr.TableConstructor(_) -> () // Success
                | _ -> failtest "Expected table constructor argument"
            | _ -> failtest "Expected function call"
        
        testCase "Call with string literal (no parens)" <| fun () ->
            let result = parseSuccess expr "print \"hello\""
            match result with
            | Expr.FunctionCall(_, args) -> 
                Expect.equal (List.length args) 1 "Should have 1 argument"
                match args.[0] with
                | Expr.Literal(Literal.String(s)) -> 
                    Expect.equal s "hello" "String argument"
                | _ -> failtest "Expected string literal argument"
            | _ -> failtest "Expected function call"
    ]

[<Tests>]
let variableAttributeTests =
    testList "Variable attributes (Lua 5.4)" [
        testCase "Const attribute" <| fun () ->
            let result = parseSuccess statement "local x <const> = 1"
            match result with
            | Statement.LocalAssignment([(name, attr)], _) ->
                Expect.equal name "x" "Variable name"
                Expect.equal attr Attribute.Const "Should have const attribute"
            | _ -> failtest "Expected local assignment with const"
        
        testCase "Close attribute" <| fun () ->
            let result = parseSuccess statement "local f <close> = io.open('file')"
            match result with
            | Statement.LocalAssignment([(name, attr)], _) ->
                Expect.equal name "f" "Variable name"
                Expect.equal attr Attribute.Close "Should have close attribute"
            | _ -> failtest "Expected local assignment with close"
    ]