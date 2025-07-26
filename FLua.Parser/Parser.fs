/// FLua Parser - Complete Lua 5.4 Parser Implementation
/// 
/// This module provides a comprehensive parser for Lua 5.4 syntax using FParsec.
/// The parser implements a scannerless approach, operating directly on character streams
/// rather than using a separate tokenization phase.
///
/// Features supported:
/// - All Lua expressions: literals, variables, operators, function calls, method calls
/// - All Lua statements: assignments, control flow, function definitions, loops
/// - Advanced features: table constructors, labels/goto, multiple assignment/return
/// - Proper operator precedence and associativity
/// - Comprehensive error handling and recovery
///
/// Architecture:
/// - Centralized parser design with forward references for mutual recursion
/// - Expression parsing using OperatorPrecedenceParser for correct precedence
/// - Statement parsing with proper termination handling for blocks
/// - Postfix expression parsing for chaining (table access, method calls, function calls)
///
/// Usage:
///   let result = FParsec.CharParsers.run luaFile "your lua code here"
///
module FLua.Parser.Parser

open FParsec
open FLua.Ast
open FLua.Parser
// We'll define our own parsers instead of using the Lexer

// ============================================================================
// FORWARD REFERENCES AND BASIC PARSERS
// ============================================================================

// Forward references for mutually recursive parsers
let expr, exprRef = createParserForwardedToRef<Expr, unit>()
let statement, statementRef = createParserForwardedToRef<Statement, unit>()
let block, blockRef = createParserForwardedToRef<Block, unit>()
let functionExpr, functionExprRef = createParserForwardedToRef<Expr, unit>()

// Lazy expression parser for use in contexts that need delayed evaluation
let lazyExpr : Parser<Expr, unit> = 
    fun stream -> expr stream

// Basic helper parsers
let private isIdentifierChar c = isLetter c || isDigit c || c = '_'
let private isIdentifierFirstChar c = isLetter c || c = '_'

let ws = 
    let wsChar = anyOf " \t\r\n"
    let singleLineComment = pstring "--" >>. skipRestOfLine true
    let multiLineComment = 
        let openBracket = pstring "--[" >>. manyChars (pchar '=') .>> pchar '['
        let closeBracket n = pstring "]" >>. pstring (String.replicate n "=") >>. pstring "]"
        let content n = manyCharsTill anyChar (attempt (closeBracket n))
        attempt (
            openBracket >>= fun eqs ->
                let n = eqs.Length in
                content n >>% ()
        )
    let comment = attempt multiLineComment <|> singleLineComment
    skipMany (skipMany1 wsChar <|> comment)

let keyword kw = pstring kw >>? notFollowedBy (satisfy isIdentifierChar) .>> ws
let symbol s = pstring s .>> ws

// Identifier parser - FIXED TO HANDLE SINGLE CHARACTERS
let pIdentifier : Parser<string, unit> =
    let identifierWithoutWs = 
        satisfy isIdentifierFirstChar >>= fun first ->
            manySatisfy isIdentifierChar >>= fun rest ->
                preturn (string first + rest)
    identifierWithoutWs .>> ws <?> "identifier"

let identifier = pIdentifier

// String parser
let pString : Parser<string, unit> =
    // Helper to parse decimal escape sequences (\0 to \999)
    let decimalEscape =
        satisfy isDigit >>= fun d1 ->
            opt (satisfy isDigit) >>= fun d2opt ->
                opt (satisfy isDigit) >>= fun d3opt ->
                    let digits = 
                        string d1 + 
                        (match d2opt with Some d -> string d | None -> "") +
                        (match d3opt with Some d -> string d | None -> "")
                    let value = int digits
                    if value > 255 then 
                        fail ("decimal escape too large: \\" + digits)
                    else 
                        preturn (char value |> string)
    
    let escape =
        pchar '\\' >>. 
            choice [
                // Single character escapes
                anyOf "abfnrtv\\\"'" |>> function
                    | 'a' -> "\a" | 'b' -> "\b" | 'f' -> "\f"
                    | 'n' -> "\n" | 'r' -> "\r" | 't' -> "\t"
                    | 'v' -> "\v" | '\\' -> "\\" | '"' -> "\""
                    | '\'' -> "'" | c -> string c
                
                // Hex escape: \xHH (exactly 2 hex digits)
                pchar 'x' >>. 
                    (satisfy isHex .>>. satisfy isHex) 
                    |>> fun (h1, h2) -> 
                        let value = System.Convert.ToByte(string h1 + string h2, 16)
                        string (char value)
                
                // Unicode escape: \u{...}
                pstring "u{" >>. many1Satisfy isHex .>> pchar '}'
                |>> fun hex ->
                    let mutable value = 0L
                    if System.Int64.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, &value) then
                        if value > 0x7FFFFFFFL then 
                            failwith "Unicode escape too large"
                        elif value > 0x10FFFF then
                            // WARNING: Values beyond U+10FFFF are invalid Unicode
                            // Lua allows these for testing invalid UTF-8 sequences
                            // This produces byte sequences that are not valid UTF-8
                            // Real applications should not use Unicode escapes > U+10FFFF
                            let v = int value
                            if v <= 0x1FFFFF then
                                // 4-byte sequence for extended range
                                let b1 = char (0xF0 ||| (v >>> 18))
                                let b2 = char (0x80 ||| ((v >>> 12) &&& 0x3F))
                                let b3 = char (0x80 ||| ((v >>> 6) &&& 0x3F))
                                let b4 = char (0x80 ||| (v &&& 0x3F))
                                System.String([| b1; b2; b3; b4 |])
                            elif v <= 0x3FFFFFF then
                                // 5-byte sequence (invalid UTF-8 but used in tests)
                                let b1 = char (0xF8 ||| (v >>> 24))
                                let b2 = char (0x80 ||| ((v >>> 18) &&& 0x3F))
                                let b3 = char (0x80 ||| ((v >>> 12) &&& 0x3F))
                                let b4 = char (0x80 ||| ((v >>> 6) &&& 0x3F))
                                let b5 = char (0x80 ||| (v &&& 0x3F))
                                System.String([| b1; b2; b3; b4; b5 |])
                            else
                                // 6-byte sequence (invalid UTF-8 but used in tests)
                                let b1 = char (0xFC ||| (v >>> 30))
                                let b2 = char (0x80 ||| ((v >>> 24) &&& 0x3F))
                                let b3 = char (0x80 ||| ((v >>> 18) &&& 0x3F))
                                let b4 = char (0x80 ||| ((v >>> 12) &&& 0x3F))
                                let b5 = char (0x80 ||| ((v >>> 6) &&& 0x3F))
                                let b6 = char (0x80 ||| (v &&& 0x3F))
                                System.String([| b1; b2; b3; b4; b5; b6 |])
                        else
                            System.Char.ConvertFromUtf32(int value)
                    else
                        failwith "Invalid hex number in Unicode escape"
                
                // Line continuation: \z skips whitespace
                pchar 'z' >>. skipMany (anyOf " \t\r\n\f\v") >>% ""
                
                // Decimal escape: \DDD (1-3 digits, max 255)
                // Must be last to avoid consuming digits from other escapes
                decimalEscape
            ]
    
    let normalChar quote = noneOf (string quote + "\\") |>> string
    let quotedString quote =
        between (pchar quote) (pchar quote)
            (manyStrings (escape <|> normalChar quote))
    
    // Long bracket string: [=*[ ... ]=*]
    let longBracketString : Parser<string, unit> =
        let openBracket =
            pchar '[' >>. manyChars (pchar '=') .>> pchar '['
        let closeBracket n =
            pchar ']' >>. pstring (String.replicate n "=") >>. pchar ']'
        let content n =
            // If the string starts with a newline, skip it
            let skipInitialNewline = 
                (pchar '\n' >>% ()) <|> (pchar '\r' >>. opt (pchar '\n') >>% ()) <|> preturn ()
            skipInitialNewline >>. manyCharsTill anyChar (attempt (closeBracket n))
        attempt (
            openBracket >>= fun eqs ->
                let n = eqs.Length in
                content n
        )
    
    (attempt longBracketString <|> quotedString '\"' <|> quotedString '\'') .>> ws
// exprList will be defined after expr is initialized
// Use a function to ensure lazy evaluation
let varList = parse { return! sepBy1 expr (symbol ",") }

// ============================================================================
// LITERAL AND VARIABLE PARSERS  
// ============================================================================

// Number parsers for the scannerless parser
let private parseHexFloat (s: string) =
    // Parse hex float with optional binary exponent
    let parts = s.Substring(2) // Remove "0x"
    let mutable mantissaStr = parts
    let mutable exponentValue = 0
    
    // Check for exponent
    let pIndex = parts.IndexOfAny([|'p'; 'P'|])
    if pIndex >= 0 then
        mantissaStr <- parts.Substring(0, pIndex)
        let expStr = parts.Substring(pIndex + 1)
        exponentValue <- int expStr
    
    // Parse mantissa
    let dotIndex = mantissaStr.IndexOf('.')
    let mutable mantissa = 0.0
    
    if dotIndex >= 0 then
        // Has fractional part
        let intPart = if dotIndex > 0 then mantissaStr.Substring(0, dotIndex) else "0"
        let fracPart = mantissaStr.Substring(dotIndex + 1)
        
        mantissa <- float (System.Convert.ToInt64(intPart, 16))
        
        // Add fractional part
        for i = 0 to fracPart.Length - 1 do
            let digit = System.Convert.ToInt32(fracPart.[i].ToString(), 16)
            mantissa <- mantissa + (float digit / (16.0 ** float (i + 1)))
    else
        // Integer only
        mantissa <- float (System.Convert.ToInt64(mantissaStr, 16))
    
    // Apply exponent
    mantissa * (2.0 ** float exponentValue)

let private pHexNumber : Parser<Expr, unit> =
    let hexFloatWithExp = 
        attempt (
            pstringCI "0x" >>. 
            many1Satisfy isHex .>>.
            opt (pchar '.' >>. manySatisfy isHex) .>>.
            anyOf "pP" .>>.
            opt (anyOf "+-") .>>.
            many1Satisfy isDigit
        )
        |>> (fun ((((hexInt, fracPart), _), sign), exp) ->
            // Build the full hex float string
            let fracStr = match fracPart with Some frac -> "." + frac | None -> ""
            let signStr = match sign with Some c -> string c | None -> ""
            let s = sprintf "0x%s%sp%s%s" hexInt fracStr signStr exp
            Expr.Literal (Literal.Float (parseHexFloat s)))
    
    let hexFloatNoExp = 
        attempt (
            pstringCI "0x" >>. 
            many1Satisfy isHex .>>.
            pchar '.' .>>.
            manySatisfy isHex
        )
        |>> (fun ((hexInt, _), hexFrac) -> 
            let intVal = float (System.Convert.ToInt64(hexInt, 16))
            let mutable fracVal = 0.0
            if hexFrac.Length > 0 then
                for i = 0 to hexFrac.Length - 1 do
                    let digit = System.Convert.ToInt32(hexFrac.[i].ToString(), 16)
                    fracVal <- fracVal + (float digit / (16.0 ** float (i + 1)))
            Expr.Literal (Literal.Float (intVal + fracVal)))
    
    let hexInt = 
        attempt (pstringCI "0x" >>. many1Satisfy isHex)
        |>> (fun hexStr -> 
            // Parse hex string as unsigned by prepending "0" to ensure positive
            let paddedHex = if hexStr.Length % 2 = 1 then "0" + hexStr else hexStr
            let value = System.Numerics.BigInteger.Parse("0" + paddedHex, System.Globalization.NumberStyles.HexNumber)
            Expr.Literal (Literal.Integer value))
    
    // Try hex float with exponent first, then hex float without exp, then hex int
    attempt hexFloatWithExp <|> attempt hexFloatNoExp <|> hexInt

let private pDecimalNumber : Parser<Expr, unit> =
    let decFloatWithInt = 
        attempt (many1Satisfy isDigit .>>. pchar '.' .>>. many1Satisfy isDigit)
        |>> (fun ((intPart, _), fracPart) -> 
            let floatStr = intPart + "." + fracPart
            Expr.Literal (Literal.Float (float floatStr)))
    
    let decFloatNoInt =
        attempt (pchar '.' >>. many1Satisfy isDigit)
        |>> (fun fracPart ->
            let floatStr = "0." + fracPart
            Expr.Literal (Literal.Float (float floatStr)))
    
    let decFloatTrailing =
        attempt (many1Satisfy isDigit .>> pchar '.' .>> notFollowedBy (satisfy isDigit))
        |>> (fun intPart ->
            let floatStr = intPart + ".0"
            Expr.Literal (Literal.Float (float floatStr)))
    
    let decInt = 
        many1Satisfy isDigit
        |>> (fun s -> Expr.Literal (Literal.Integer (bigint.Parse s)))
    
    attempt decFloatWithInt <|> attempt decFloatNoInt <|> attempt decFloatTrailing <|> decInt

let private pNumberLiteral : Parser<Expr, unit> =
    attempt pHexNumber <|> pDecimalNumber

// Expression parsers (simplified from ExprParser.fs)
let pLiteral : Parser<Expr, unit> =
    // Put long strings first to avoid conflicts with other parsers
    let pLongString = 
        let openBracket =
            pchar '[' >>. manyChars (pchar '=') .>> pchar '['
        let closeBracket n =
            pchar ']' >>. pstring (String.replicate n "=") >>. pchar ']'
        let content n =
            // If the string starts with a newline, skip it
            let skipInitialNewline = 
                (pchar '\n' >>% ()) <|> (pchar '\r' >>. opt (pchar '\n') >>% ()) <|> preturn ()
            skipInitialNewline >>. manyCharsTill anyChar (attempt (closeBracket n))
        attempt (
            openBracket >>= fun eqs ->
                let n = eqs.Length in
                content n |>> (fun s -> Expr.Literal (Literal.String s))
        )
    
    choice [
        attempt pLongString  // Try long strings first
        pNumberLiteral
        pString |>> (fun s -> Expr.Literal (Literal.String s))
        (attempt (pstring "nil" >>? notFollowedBy (satisfy isIdentifierChar)) >>% Expr.Literal Literal.Nil)
        (attempt (pstring "true" >>? notFollowedBy (satisfy isIdentifierChar)) >>% Expr.Literal (Literal.Boolean true))
        (attempt (pstring "false" >>? notFollowedBy (satisfy isIdentifierChar)) >>% Expr.Literal (Literal.Boolean false))
    ]

let pVariable : Parser<Expr, unit> =
    identifier |>> (fun s -> Expr.Var s)

let pVararg : Parser<Expr, unit> =
    pstring "..." >>% Expr.Vararg

let pParenExpr : Parser<Expr, unit> =
    between (pstring "(" >>. ws) (ws >>. pstring ")") expr |>> Expr.Paren

// Table constructor parser
let pTableConstructor : Parser<Expr, unit> =
    let pTableField = 
        choice [
            // [expr] = expr (KeyField)
            attempt (between (pstring "[" >>. ws) (ws >>. pstring "]") expr .>> ws .>> symbol "=" .>>. expr)
            |>> fun (key, value) -> TableField.KeyField(key, value)
            
            // identifier = expr (NamedField)  
            attempt (identifier .>> symbol "=" .>>. expr)
            |>> fun (name, value) -> TableField.NamedField(name, value)
            
            // expr (ExprField)
            expr |>> TableField.ExprField
        ]
    
    between (pstring "{" >>. ws) (ws >>. pstring "}") (sepBy pTableField (symbol ","))
    |>> fun fields -> Expr.TableConstructor fields

// Function call parser
let pFunctionCall : Parser<Expr, unit> =
    pVariable .>>. between (pstring "(" >>. ws) (ws >>. pstring ")") (opt (sepBy1 expr (symbol ",")))
    |>> fun (func, argsOpt) ->
        let args = argsOpt |> Option.defaultValue []
        Expr.FunctionCall(func, args)

// Primary expressions without postfix operations
let pPrimaryBase : Parser<Expr, unit> =
    ws >>. choice [
        attempt pLiteral
        attempt pTableConstructor
        attempt functionExpr
        attempt pVariable
        attempt pVararg
        attempt pParenExpr
    ] .>> ws

// Table access and function call postfix operations
let pPostfixOp =
    choice [
        // Method syntax - must be followed by arguments
        attempt (
            pstring ":" >>. ws >>. identifier >>= fun methodName ->
                choice [
                    // Method call with parentheses: :identifier(args)
                    between (pstring "(" >>. ws) (ws >>. pstring ")" >>. ws) (opt (sepBy1 expr (symbol ",")))
                    |>> fun argsOpt -> fun expr ->
                        let args = argsOpt |> Option.defaultValue []
                        Expr.MethodCall(expr, methodName, args)
                    
                    // Method call with string literal (no parentheses): :identifier "string" or :identifier [[string]]
                    // Note: No ws consumption before pLiteral to allow [[
                    attempt pLiteral
                    >>= fun lit ->
                        match lit with
                        | Expr.Literal (Literal.String s) -> 
                            preturn (fun expr -> Expr.MethodCall(expr, methodName, [Expr.Literal (Literal.String s)]))
                        | _ -> fail "Expected string literal"
                    
                    // Method call with table constructor (no parentheses): :identifier {table}
                    pTableConstructor
                    |>> fun arg -> fun expr ->
                        Expr.MethodCall(expr, methodName, [arg])
                ]
        )
        
        // Dot access: .identifier
        pstring "." >>. ws >>. identifier .>> ws
        |>> fun key -> fun expr -> Expr.TableAccess(expr, Expr.Literal (Literal.String key))
        
        // Bracket access: [expr]
        between (pstring "[" >>. ws) (ws >>. pstring "]" >>. ws) expr
        |>> fun key -> fun expr -> Expr.TableAccess(expr, key)
        
        // Function call with string literal (no parentheses): func "string" or func [[string]]
        attempt pLiteral
        >>= fun lit ->
            match lit with
            | Expr.Literal (Literal.String s) as stringLit -> 
                preturn (fun expr -> Expr.FunctionCall(expr, [stringLit]))
            | _ -> fail "Expected string literal"
        
        // Function call with table constructor (no parentheses): func {table}
        attempt pTableConstructor
        |>> fun arg -> fun expr -> Expr.FunctionCall(expr, [arg])
        
        // Function call: (args)
        between (pstring "(" >>. ws) (ws >>. pstring ")" >>. ws) (opt (sepBy1 expr (symbol ",")))
        |>> fun argsOpt -> fun expr ->
            let args = argsOpt |> Option.defaultValue []
            Expr.FunctionCall(expr, args)
    ]

// Postfix expression parser (handles chaining of table access and function calls)
let pPostfixExpr =
    let rec parsePostfix expr =
        choice [
            attempt (pPostfixOp >>= fun op -> parsePostfix (op expr))
            preturn expr
        ]
    
    pPrimaryBase >>= parsePostfix

let pPrimary : Parser<Expr, unit> = pPostfixExpr

// Build expression parser with proper operator precedence
let buildExprParser() =
    let opp = new OperatorPrecedenceParser<Expr, unit, unit>()
    
    opp.TermParser <- pPrimary
    
    // Unary operators (highest precedence)
    opp.AddOperator(PrefixOperator("not", ws, 12, true, fun x -> Expr.Unary(UnaryOp.Not, x)))
    opp.AddOperator(PrefixOperator("#", ws, 12, true, fun x -> Expr.Unary(UnaryOp.Length, x)))
    opp.AddOperator(PrefixOperator("-", ws, 12, true, fun x -> Expr.Unary(UnaryOp.Negate, x)))
    opp.AddOperator(PrefixOperator("~", ws, 12, true, fun x -> Expr.Unary(UnaryOp.BitNot, x)))
    
    // Power (right associative)
    opp.AddOperator(InfixOperator("^", ws, 11, Associativity.Right, fun x y -> Expr.Binary(x, BinaryOp.Power, y)))
    
    // Multiplicative operators (left associative)
    opp.AddOperator(InfixOperator("*", ws, 10, Associativity.Left, fun x y -> Expr.Binary(x, BinaryOp.Multiply, y)))
    opp.AddOperator(InfixOperator("//", ws, 10, Associativity.Left, fun x y -> Expr.Binary(x, BinaryOp.FloorDiv, y)))
    opp.AddOperator(InfixOperator("/", ws, 10, Associativity.Left, fun x y -> Expr.Binary(x, BinaryOp.FloatDiv, y)))
    opp.AddOperator(InfixOperator("%", ws, 10, Associativity.Left, fun x y -> Expr.Binary(x, BinaryOp.Modulo, y)))
    
    // Additive operators (left associative)  
    opp.AddOperator(InfixOperator("+", ws, 9, Associativity.Left, fun x y -> Expr.Binary(x, BinaryOp.Add, y)))
    opp.AddOperator(InfixOperator("-", ws, 9, Associativity.Left, fun x y -> Expr.Binary(x, BinaryOp.Subtract, y)))
    
    // String concatenation (right associative)
    opp.AddOperator(InfixOperator("..", ws, 8, Associativity.Right, fun x y -> Expr.Binary(x, BinaryOp.Concat, y)))
    
    // Shift operators (left associative)
    opp.AddOperator(InfixOperator("<<", ws, 7, Associativity.Left, fun x y -> Expr.Binary(x, BinaryOp.ShiftLeft, y)))
    opp.AddOperator(InfixOperator(">>", ws, 7, Associativity.Left, fun x y -> Expr.Binary(x, BinaryOp.ShiftRight, y)))
    
    // Bitwise AND (left associative)
    opp.AddOperator(InfixOperator("&", ws, 6, Associativity.Left, fun x y -> Expr.Binary(x, BinaryOp.BitAnd, y)))
    
    // Bitwise OR (left associative) 
    opp.AddOperator(InfixOperator("|", ws, 5, Associativity.Left, fun x y -> Expr.Binary(x, BinaryOp.BitOr, y)))
    
    // Bitwise XOR (left associative)
    opp.AddOperator(InfixOperator("~", ws, 4, Associativity.Left, fun x y -> Expr.Binary(x, BinaryOp.BitXor, y)))
    
    // Comparison operators (non-associative)
    opp.AddOperator(InfixOperator("<", ws, 3, Associativity.None, fun x y -> Expr.Binary(x, BinaryOp.Less, y)))
    opp.AddOperator(InfixOperator(">", ws, 3, Associativity.None, fun x y -> Expr.Binary(x, BinaryOp.Greater, y)))
    opp.AddOperator(InfixOperator("<=", ws, 3, Associativity.None, fun x y -> Expr.Binary(x, BinaryOp.LessEqual, y)))
    opp.AddOperator(InfixOperator(">=", ws, 3, Associativity.None, fun x y -> Expr.Binary(x, BinaryOp.GreaterEqual, y)))
    opp.AddOperator(InfixOperator("==", ws, 3, Associativity.None, fun x y -> Expr.Binary(x, BinaryOp.Equal, y)))
    opp.AddOperator(InfixOperator("~=", ws, 3, Associativity.None, fun x y -> Expr.Binary(x, BinaryOp.NotEqual, y)))
    
    // Logical AND (left associative)
    opp.AddOperator(InfixOperator("and", ws, 2, Associativity.Left, fun x y -> Expr.Binary(x, BinaryOp.And, y)))
    
    // Logical OR (left associative)
    opp.AddOperator(InfixOperator("or", ws, 1, Associativity.Left, fun x y -> Expr.Binary(x, BinaryOp.Or, y)))
    
    opp.ExpressionParser

// Initialize expression parser
do exprRef := buildExprParser()

// ============================================================================
// STATEMENT PARSERS
// ============================================================================

// Parse variable name with optional attribute: name [<attr>]
let pVariableWithAttribute =
    pIdentifier .>>. choice [
        attempt (ws >>. pstring "<const>" >>% Attribute.Const)
        attempt (ws >>. pstring "<close>" >>% Attribute.Close)
        preturn Attribute.NoAttribute
    ] .>> ws

// Assignment statement: var1, var2, ... = expr1, expr2, ...
let pAssignment =
    varList .>> symbol "=" .>>. sepBy1 expr (symbol ",")
    |>> Statement.Assignment

// Local variable declaration: local var1 [<attr>], var2 [<attr>], ... = expr1, expr2, ...
let pLocalAssignment =
    keyword "local" >>. sepBy1 pVariableWithAttribute (symbol ",")
    .>>. opt (symbol "=" >>. sepBy1 expr (symbol ","))
    |>> Statement.LocalAssignment

// Function call statement - match any expression that is a function or method call
let pFunctionCallStmt = 
    expr >>= fun e ->
        // Check if the expression is a function call or method call at any level
        let rec isFunctionCall expr =
            match expr with
            | Expr.FunctionCall _ -> true
            | Expr.MethodCall _ -> true
            | Expr.Paren inner -> isFunctionCall inner  // Check inside parentheses
            | _ -> false
        
        if isFunctionCall e then
            preturn (Statement.FunctionCall e)
        else
            fail "Not a function call"

// Empty statement (just a semicolon)
let pEmptyStmt = symbol ";" >>% Statement.Empty

// Do block: do ... end
let pDoBlock =
    keyword "do" >>. block .>> keyword "end"
    |>> Statement.DoBlock

// Break statement
let pBreakStmt = keyword "break" >>% Statement.Break

// Return statement: return expr1, expr2, ...
let pReturnStmt =
    keyword "return" >>. opt (sepBy1 expr (symbol ","))
    |>> Statement.Return

// Label statement: ::name::
let pLabelStmt =
    between (pstring "::" >>. ws) (ws .>> pstring "::") identifier
    |>> Statement.Label

// Goto statement: goto name
let pGotoStmt =
    keyword "goto" >>. identifier
    |>> Statement.Goto

// If statement: if expr then block [elseif expr then block]* [else block] end
let pIfStmt =
    parse {
        let! firstExpr = keyword "if" >>. expr
        let! firstBlock = keyword "then" >>. block
        let! elseifClauses = many (
            keyword "elseif" >>. expr .>> keyword "then" .>>. block
        )
        let! elseBlock = opt (keyword "else" >>. block)
        do! keyword "end"
        return Statement.If((firstExpr, firstBlock) :: elseifClauses, elseBlock)
    }

// While statement: while expr do block end
let pWhileStmt =
    parse {
        let! condition = keyword "while" >>. expr
        let! body = keyword "do" >>. block .>> keyword "end"
        return Statement.While(condition, body)
    }

// Repeat statement: repeat block until expr
let pRepeatStmt =
    parse {
        let! body = keyword "repeat" >>. block
        let! condition = keyword "until" >>. expr
        return Statement.Repeat(body, condition)
    }

// Numeric for statement: for name = start, end [, step] do block end
let pNumericForStmt =
    keyword "for" >>. identifier .>> symbol "=" .>>. expr .>> symbol "," .>>. expr
    .>>. opt (symbol "," >>. expr)
    .>> keyword "do" .>>. block .>> keyword "end"
    |>> fun ((((name, start), endExpr), stepExpr), body) ->
        Statement.NumericFor(name, start, endExpr, stepExpr, body)

// Generic for statement: for name1 [<attr>], name2 [<attr>], ... in expr1, expr2, ... do block end
let pGenericForStmt : Parser<Statement, unit> =
    attempt (keyword "for" >>. sepBy1 pVariableWithAttribute (symbol ",")
    .>> keyword "in") .>>. sepBy1 lazyExpr (symbol ",")
    .>> keyword "do" .>>. block .>> keyword "end"
    |>> fun ((names, exprs), body) ->
        Statement.GenericFor(names, exprs, body)

// Function parameter list: (name1 [<attr>], name2 [<attr>], ..., [...])
let pParamList =
    let param = pVariableWithAttribute |>> Parameter.Named
    let varargParam = pstring "..." >>% Parameter.Vararg
    
    between (symbol "(") (symbol ")") 
        (sepEndBy param (symbol ",") .>>. opt (symbol "..." >>% true))
    |>> fun (parameters, isVararg) ->
        match isVararg with
        | Some _ -> parameters @ [Parameter.Vararg]
        | None -> parameters

// Function definition body (without the 'end' keyword)
let pFunctionDefBody =
    pParamList .>>. block
    |>> fun (parameters, body) ->
        { 
            Parameters = parameters
            IsVararg = List.exists (function Parameter.Vararg -> true | _ -> false) parameters
            Body = body
        }

// Simple function definition: function name(params) body end
let pFunctionDefSimple =
    keyword "function" >>. identifier .>>. pFunctionDefBody .>> keyword "end"
    |>> fun (name, def) ->
        Statement.FunctionDef([name], def)

// Method or table function definition: function tbl.name(params) or function tbl:name(params)
let pFunctionDefComplex =
    keyword "function" >>. identifier .>>. many1 (
        (pstring "." >>. ws >>. identifier |>> fun name -> "." + name) <|>
        (pstring ":" >>. ws >>. identifier |>> fun name -> ":" + name)
    ) .>>. pFunctionDefBody .>> keyword "end"
    |>> fun ((first, rest), def) ->
        // Process the path (tbl.a.b or tbl:c)
        let path = first :: (rest |> List.map (fun s -> 
            if s.[0] = '.' then s.[1..] else s.[1..]
        ))
        Statement.FunctionDef(path, def)

// Local function definition: local function name(params) body end
let pLocalFunctionDef =
    keyword "local" >>. keyword "function" >>. identifier .>>. pFunctionDefBody .>> keyword "end"
    |>> Statement.LocalFunctionDef

// Function expression: function(params) body end
do functionExprRef :=
    keyword "function" >>. pFunctionDefBody .>> keyword "end"
    |>> Expr.FunctionDef

// ============================================================================
// BLOCK PARSER WITH PROPER TERMINATION
// ============================================================================

// Block parser that stops at termination keywords
let blockImpl = 
    let terminator = 
        choice [
            attempt (lookAhead (pstring "end" >>? notFollowedBy (satisfy isIdentifierChar)))
            attempt (lookAhead (pstring "else" >>? notFollowedBy (satisfy isIdentifierChar)))
            attempt (lookAhead (pstring "elseif" >>? notFollowedBy (satisfy isIdentifierChar)))
            attempt (lookAhead (pstring "until" >>? notFollowedBy (satisfy isIdentifierChar)))
            eof
        ]
    
    manyTill statement terminator

// Statement parser with proper ordering
let statementImpl =
    ws >>. choice [
        attempt pGenericForStmt       // Try generic for FIRST to avoid conflicts
        attempt pNumericForStmt
        attempt pLocalFunctionDef
        attempt pFunctionDefComplex
        attempt pFunctionDefSimple
        attempt pReturnStmt
        attempt pBreakStmt
        attempt pLabelStmt
        attempt pGotoStmt
        attempt pLocalAssignment
        attempt pIfStmt
        attempt pWhileStmt
        attempt pRepeatStmt
        attempt pDoBlock
        attempt pFunctionCallStmt    // Try function calls BEFORE assignments
        attempt pAssignment
        pEmptyStmt
    ] .>> ws

// Initialize the parsers in correct order
do blockRef := blockImpl
do statementRef := statementImpl

// Lua file parser (entire program)
let luaFile = ws >>. block .>> ws .>> eof 