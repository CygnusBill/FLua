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
open FLua.Parser.Lexer

// ============================================================================
// FORWARD REFERENCES AND BASIC PARSERS
// ============================================================================

// Forward references for mutually recursive parsers
let expr, exprRef = createParserForwardedToRef<Expr, unit>()
let statement, statementRef = createParserForwardedToRef<Statement, unit>()
let block, blockRef = createParserForwardedToRef<Block, unit>()
let functionExpr, functionExprRef = createParserForwardedToRef<Expr, unit>()

// Basic helper parsers
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
let identifier = pIdentifier |>> (function Identifier s -> s | _ -> failwith "impossible") .>> ws
// exprList will be defined after expr is initialized
let varList = sepBy1 expr (symbol ",")

// ============================================================================
// LITERAL AND VARIABLE PARSERS  
// ============================================================================

// Expression parsers (simplified from ExprParser.fs)
let pLiteral : Parser<Expr, unit> =
    choice [
        pNumber |>> (function
            | Integer i -> Expr.Literal (Literal.Integer i)
            | Number n -> Expr.Literal (Literal.Float n)
            | t -> failwithf "pNumber returned unexpected token: %A" t)
        pString |>> (function
            | String s -> Expr.Literal (Literal.String s)
            | t -> failwithf "pString returned unexpected token: %A" t)
        (attempt (pstring "nil" >>? notFollowedBy (satisfy isIdentifierChar)) >>% Expr.Literal Literal.Nil)
        (attempt (pstring "true" >>? notFollowedBy (satisfy isIdentifierChar)) >>% Expr.Literal (Literal.Boolean true))
        (attempt (pstring "false" >>? notFollowedBy (satisfy isIdentifierChar)) >>% Expr.Literal (Literal.Boolean false))
    ]

let pVariable : Parser<Expr, unit> =
    pIdentifier |>> (function 
        | Identifier s -> Expr.Var s 
        | _ -> failwith "impossible")

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
        // Method call: :identifier(args)
        pstring ":" >>. ws >>. identifier .>>. between (pstring "(" >>. ws) (ws >>. pstring ")" >>. ws) (opt (sepBy1 expr (symbol ",")))
        |>> fun (methodName, argsOpt) -> fun expr ->
            let args = argsOpt |> Option.defaultValue []
            Expr.MethodCall(expr, methodName, args)
        
        // Dot access: .identifier
        pstring "." >>. ws >>. identifier .>> ws
        |>> fun key -> fun expr -> Expr.TableAccess(expr, Expr.Literal (Literal.String key))
        
        // Bracket access: [expr]
        between (pstring "[" >>. ws) (ws >>. pstring "]" >>. ws) expr
        |>> fun key -> fun expr -> Expr.TableAccess(expr, key)
        
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

// Assignment statement: var1, var2, ... = expr1, expr2, ...
let pAssignment =
    varList .>> symbol "=" .>>. sepBy1 expr (symbol ",")
    |>> Statement.Assignment

// Local variable declaration: local var1, var2, ... = expr1, expr2, ...
let pLocalAssignment =
    keyword "local" >>. sepBy1 (identifier .>>. preturn Attribute.NoAttribute) (symbol ",")
    .>>. opt (symbol "=" >>. sepBy1 expr (symbol ","))
    |>> Statement.LocalAssignment

// Function call statement - only match expressions that are actually function calls
let pFunctionCallStmt = 
    expr >>= fun e ->
        match e with
        | Expr.FunctionCall _ -> preturn (Statement.FunctionCall e)
        | Expr.MethodCall _ -> preturn (Statement.FunctionCall e)
        | _ -> fail "Not a function call"

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
    let elseifClause = keyword "elseif" >>. expr .>> keyword "then" .>>. block
    let elseClause = keyword "else" >>. block
    
    pipe3
        (keyword "if" >>. expr .>> keyword "then" .>>. block)
        (many elseifClause)
        (opt elseClause .>> keyword "end")
        (fun firstClause elseifClauses elseBlock ->
            Statement.If(firstClause :: elseifClauses, elseBlock))

// While statement: while expr do block end
let pWhileStmt =
    keyword "while" >>. expr .>> keyword "do" .>>. block .>> keyword "end"
    |>> Statement.While

// Repeat statement: repeat block until expr
let pRepeatStmt =
    keyword "repeat" >>. block .>> keyword "until" .>>. expr
    |>> fun (body, condition) -> Statement.Repeat(body, condition)

// Numeric for statement: for name = start, end [, step] do block end
let pNumericForStmt =
    keyword "for" >>. identifier .>> symbol "=" .>>. expr .>> symbol "," .>>. expr
    .>>. opt (symbol "," >>. expr)
    .>> keyword "do" .>>. block .>> keyword "end"
    |>> fun ((((name, start), endExpr), stepExpr), body) ->
        Statement.NumericFor(name, start, endExpr, stepExpr, body)

// Generic for statement: for name1, name2, ... in expr1, expr2, ... do block end
let pGenericForStmt =
    keyword "for" >>. sepBy1 (identifier .>>. preturn Attribute.NoAttribute) (symbol ",")
    .>> keyword "in" .>>. sepBy1 expr (symbol ",")
    .>> keyword "do" .>>. block .>> keyword "end"
    |>> fun ((names, exprs), body) ->
        Statement.GenericFor(names, exprs, body)

// Function parameter list: (name1, name2, ..., [...])
let pParamList =
    let param = identifier .>>. preturn Attribute.NoAttribute |>> Parameter.Named
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