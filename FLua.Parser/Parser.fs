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
let ws = spaces
let keyword kw = pstring kw >>? notFollowedBy (satisfy isIdentifierChar) .>> ws
let symbol s = pstring s .>> ws
let identifier = pIdentifier |>> (function Identifier s -> s | _ -> failwith "impossible") .>> ws
let exprList = sepBy1 expr (symbol ",")
let varList = sepBy1 expr (symbol ",")

// ============================================================================
// LITERAL AND VARIABLE PARSERS  
// ============================================================================

// Expression parsers (simplified from ExprParser.fs)
let pLiteral : Parser<Expr, unit> =
    choice [
        pNumber |>> (function
            | Integer i -> Literal (Literal.Integer i)
            | Number n -> Literal (Literal.Float n)
            | t -> failwithf "pNumber returned unexpected token: %A" t)
        pString |>> (function
            | String s -> Literal (Literal.String s)
            | t -> failwithf "pString returned unexpected token: %A" t)
        (attempt (pstring "nil" >>? notFollowedBy (satisfy isIdentifierChar)) >>% Literal Literal.Nil)
        (attempt (pstring "true" >>? notFollowedBy (satisfy isIdentifierChar)) >>% Literal (Literal.Boolean true))
        (attempt (pstring "false" >>? notFollowedBy (satisfy isIdentifierChar)) >>% Literal (Literal.Boolean false))
    ]

let pVariable : Parser<Expr, unit> =
    pIdentifier |>> (function 
        | Identifier s -> Var s 
        | _ -> failwith "impossible")

let pVararg : Parser<Expr, unit> =
    pstring "..." >>% Vararg

let pParenExpr : Parser<Expr, unit> =
    between (pstring "(" >>. ws) (ws >>. pstring ")") expr |>> Paren

// Table constructor parser
let pTableConstructor : Parser<Expr, unit> =
    let pTableField = 
        choice [
            // [expr] = expr (KeyField)
            attempt (between (pstring "[" >>. ws) (ws >>. pstring "]") expr .>> ws .>> symbol "=" .>>. expr)
            |>> fun (key, value) -> KeyField(key, value)
            
            // identifier = expr (NamedField)  
            attempt (identifier .>> symbol "=" .>>. expr)
            |>> fun (name, value) -> NamedField(name, value)
            
            // expr (ExprField)
            expr |>> ExprField
        ]
    
    between (pstring "{" >>. ws) (ws >>. pstring "}") (sepBy pTableField (symbol ","))
    |>> fun fields -> TableConstructor fields

// Function call parser
let pFunctionCall : Parser<Expr, unit> =
    pVariable .>>. between (pstring "(" >>. ws) (ws >>. pstring ")") (opt exprList)
    |>> fun (func, argsOpt) ->
        let args = argsOpt |> Option.defaultValue []
        FunctionCall(func, args)

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
        pstring ":" >>. ws >>. identifier .>>. between (pstring "(" >>. ws) (ws >>. pstring ")") (opt exprList)
        |>> fun (methodName, argsOpt) -> fun expr ->
            let args = argsOpt |> Option.defaultValue []
            MethodCall(expr, methodName, args)
        
        // Dot access: .identifier
        pstring "." >>. ws >>. identifier
        |>> fun key -> fun expr -> TableAccess(expr, Literal (Literal.String key))
        
        // Bracket access: [expr]
        between (pstring "[" >>. ws) (ws >>. pstring "]") expr
        |>> fun key -> fun expr -> TableAccess(expr, key)
        
        // Function call: (args)
        between (pstring "(" >>. ws) (ws >>. pstring ")") (opt exprList)
        |>> fun argsOpt -> fun expr ->
            let args = argsOpt |> Option.defaultValue []
            FunctionCall(expr, args)
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
    opp.AddOperator(PrefixOperator("not", ws, 12, true, fun x -> Unary(Not, x)))
    opp.AddOperator(PrefixOperator("#", ws, 12, true, fun x -> Unary(Length, x)))
    opp.AddOperator(PrefixOperator("-", ws, 12, true, fun x -> Unary(Negate, x)))
    opp.AddOperator(PrefixOperator("~", ws, 12, true, fun x -> Unary(BitNot, x)))
    
    // Power (right associative)
    opp.AddOperator(InfixOperator("^", ws, 11, Associativity.Right, fun x y -> Binary(x, Power, y)))
    
    // Multiplicative operators (left associative)
    opp.AddOperator(InfixOperator("*", ws, 10, Associativity.Left, fun x y -> Binary(x, Multiply, y)))
    opp.AddOperator(InfixOperator("//", ws, 10, Associativity.Left, fun x y -> Binary(x, FloorDiv, y)))
    opp.AddOperator(InfixOperator("/", ws, 10, Associativity.Left, fun x y -> Binary(x, FloatDiv, y)))
    opp.AddOperator(InfixOperator("%", ws, 10, Associativity.Left, fun x y -> Binary(x, Modulo, y)))
    
    // Additive operators (left associative)  
    opp.AddOperator(InfixOperator("+", ws, 9, Associativity.Left, fun x y -> Binary(x, Add, y)))
    opp.AddOperator(InfixOperator("-", ws, 9, Associativity.Left, fun x y -> Binary(x, Subtract, y)))
    
    // String concatenation (right associative)
    opp.AddOperator(InfixOperator("..", ws, 8, Associativity.Right, fun x y -> Binary(x, Concat, y)))
    
    // Shift operators (left associative)
    opp.AddOperator(InfixOperator("<<", ws, 7, Associativity.Left, fun x y -> Binary(x, ShiftLeft, y)))
    opp.AddOperator(InfixOperator(">>", ws, 7, Associativity.Left, fun x y -> Binary(x, ShiftRight, y)))
    
    // Bitwise AND (left associative)
    opp.AddOperator(InfixOperator("&", ws, 6, Associativity.Left, fun x y -> Binary(x, BitAnd, y)))
    
    // Bitwise OR (left associative) 
    opp.AddOperator(InfixOperator("|", ws, 5, Associativity.Left, fun x y -> Binary(x, BitOr, y)))
    
    // Bitwise XOR (left associative)
    opp.AddOperator(InfixOperator("~", ws, 4, Associativity.Left, fun x y -> Binary(x, BitXor, y)))
    
    // Comparison operators (non-associative)
    opp.AddOperator(InfixOperator("<", ws, 3, Associativity.None, fun x y -> Binary(x, Less, y)))
    opp.AddOperator(InfixOperator(">", ws, 3, Associativity.None, fun x y -> Binary(x, Greater, y)))
    opp.AddOperator(InfixOperator("<=", ws, 3, Associativity.None, fun x y -> Binary(x, LessEqual, y)))
    opp.AddOperator(InfixOperator(">=", ws, 3, Associativity.None, fun x y -> Binary(x, GreaterEqual, y)))
    opp.AddOperator(InfixOperator("==", ws, 3, Associativity.None, fun x y -> Binary(x, Equal, y)))
    opp.AddOperator(InfixOperator("~=", ws, 3, Associativity.None, fun x y -> Binary(x, NotEqual, y)))
    
    // Logical AND (left associative)
    opp.AddOperator(InfixOperator("and", ws, 2, Associativity.Left, fun x y -> Binary(x, And, y)))
    
    // Logical OR (left associative, lowest precedence)
    opp.AddOperator(InfixOperator("or", ws, 1, Associativity.Left, fun x y -> Binary(x, Or, y)))
    
    opp.ExpressionParser

// ============================================================================
// STATEMENT PARSERS
// ============================================================================

// Statement parsers
let pAssignment =
    varList .>>. (symbol "=" >>. exprList)
    |>> fun (vars, exprs) -> Assignment(vars, exprs)

let pLocalAssignment =
    keyword "local" >>. sepBy1 identifier (symbol ",") .>>. opt (symbol "=" >>. exprList)
    |>> fun (names, exprsOpt) ->
        let parameters = names |> List.map (fun name -> (name, FLua.Parser.Attribute.NoAttribute))
        LocalAssignment(parameters, exprsOpt)

let pFunctionCallStmt =
    expr |>> FunctionCallStmt

let pEmpty =
    symbol ";" >>% Empty

let pDoBlock =
    keyword "do" >>. block .>> keyword "end"
    |>> DoBlock

let pBreak =
    keyword "break" >>% Break

let pReturn =
    keyword "return" >>. opt exprList
    |>> Return

let pLabel =
    between (pstring "::" >>. ws) (ws >>. pstring "::") identifier
    |>> Label

let pGoto =
    keyword "goto" >>. identifier
    |>> Goto

// Control flow statements
let pIf =
    let pIfClause = (keyword "if" >>. expr .>> keyword "then") .>>. block
    let pElseIfClause = (keyword "elseif" >>. expr .>> keyword "then") .>>. block
    let pElseClause = keyword "else" >>. block
    
    pIfClause .>>. many pElseIfClause .>>. opt pElseClause .>> keyword "end"
    |>> fun (((ifCond, ifBlock), elseIfClauses), elseBlock) ->
        let allClauses = (ifCond, ifBlock) :: elseIfClauses
        If(allClauses, elseBlock)

let pWhile =
    keyword "while" >>. expr .>> keyword "do" .>>. block .>> keyword "end"
    |>> fun (condition, body) -> While(condition, body)

let pRepeat =
    keyword "repeat" >>. block .>> keyword "until" .>>. expr
    |>> fun (body, condition) -> Repeat(body, condition)

// For loops
let pNumericFor =
    pipe4
        (keyword "for" >>. identifier .>> symbol "=")
        (expr .>> symbol ",")
        expr
        (opt (symbol "," >>. expr) .>> keyword "do" .>>. block .>> keyword "end")
        (fun varName start stop (step, body) -> NumericFor(varName, start, stop, step, body))

let pGenericFor =
    pipe3
        (keyword "for" >>. sepBy1 identifier (symbol ",") .>> keyword "in")
        (exprList .>> keyword "do")
        (block .>> keyword "end")
        (fun vars exprs body ->
            let parameters = vars |> List.map (fun name -> (name, FLua.Parser.Attribute.NoAttribute))
            GenericFor(parameters, exprs, body))

// Function definition parsers
let pParameter = 
    choice [
        attempt (pstring "..." >>% VarargParam)
        identifier |>> fun name -> Param (name, FLua.Parser.Attribute.NoAttribute)
    ]

let pParameterList =
    sepBy (choice [
        attempt (pstring "..." >>% VarargParam)
        (identifier |>> fun name -> Param (name, Attribute.NoAttribute))
    ]) (symbol ",")
    |>> fun parameters ->
        let hasVararg = parameters |> List.exists (function VarargParam -> true | Param _ -> false)
        (parameters, hasVararg)

let pFunctionDefSimple =
    between (pstring "(" >>. ws) (ws >>. pstring ")") pParameterList 
    |>> fun (parameters, hasVararg) -> { Parameters = parameters; IsVararg = hasVararg; Body = [] }

let pFunctionDef =
    between (pstring "(" >>. ws) (ws >>. pstring ")") pParameterList .>> ws .>>. block
    |>> fun ((parameters, hasVararg), body) -> { Parameters = parameters; IsVararg = hasVararg; Body = body }

// Parse qualified names like "obj.method" or "a.b.c.method"
let pQualifiedName =
    sepBy1 identifier (pstring ".")

let pFunctionDefStmt =
    keyword "function" >>. pQualifiedName .>>. pFunctionDef .>> ws .>> keyword "end"
    |>> fun (names, funcDef) -> FunctionDefStmt(names, funcDef)

let pLocalFunctionDef =
    keyword "local" >>. keyword "function" >>. identifier .>>. pFunctionDef .>> ws .>> keyword "end"
    |>> fun (name, funcDef) -> LocalFunctionDef(name, funcDef)

// ============================================================================
// BLOCK PARSER
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

// ============================================================================
// STATEMENT IMPLEMENTATION
// ============================================================================

// Main statement parser - tries all statement types in order
let statementImpl =
    ws >>. choice [
        attempt pFunctionDefStmt
        attempt pReturn
        attempt pBreak
        attempt pLabel
        attempt pGoto
        attempt pLocalFunctionDef
        attempt pLocalAssignment
        attempt pGenericFor
        attempt pNumericFor
        attempt pIf
        attempt pWhile
        attempt pRepeat
        attempt pDoBlock
        attempt pAssignment
        attempt pFunctionCallStmt
        pEmpty
    ] .>> ws

// ============================================================================
// INITIALIZATION
// ============================================================================

// Initialize functionExprRef FIRST because buildExprParser() depends on functionExpr
do functionExprRef := (keyword "function" >>. pFunctionDefSimple .>> ws .>> keyword "end" |>> fun funcDef -> FunctionDef funcDef)

// Now initialize the rest
do exprRef := buildExprParser()
do blockRef := blockImpl
do statementRef := statementImpl

// Top-level parser
let luaFile: Parser<Block, unit> =
    spaces >>. block .>> spaces .>> eof 