namespace FLua.Ast

open System
open System.Numerics
open System.Runtime.CompilerServices
open FLua.Common.Diagnostics

/// Represents attributes that can be applied to variables and parameters in Lua 5.4
[<RequireQualifiedAccess>]
type Attribute =
    | NoAttribute
    | Const
    | Close

/// Represents a Lua identifier (variable/function name)
type Identifier = string

/// Wrapper for AST nodes with optional source position information
type Positioned<'T> = {
    Value: 'T
    Position: SourceLocation option
}

/// Extension methods for creating positioned values
[<RequireQualifiedAccess>]
module Positioned =
    let create value position = { Value = value; Position = position }
    let withoutPosition value = { Value = value; Position = None }
    let withPosition value position = { Value = value; Position = Some position }

/// Represents a literal value in Lua
[<RequireQualifiedAccess>]
type Literal =
    | Nil
    | Boolean of bool
    | Integer of BigInteger
    | Float of float
    | String of string
    
    // Factory methods for C# interoperability
    static member CreateNil() = Literal.Nil
    static member CreateBoolean(value: bool) = Literal.Boolean value
    static member CreateInteger(value: BigInteger) = Literal.Integer value
    static member CreateFloat(value: float) = Literal.Float value
    static member CreateString(value: string) = Literal.String value

/// Represents a unary operator in Lua
[<RequireQualifiedAccess>]
type UnaryOp =
    | Negate      // -
    | Not         // not
    | Length      // #
    | BitNot      // ~

/// Represents a binary operator in Lua
[<RequireQualifiedAccess>]
type BinaryOp =
    | Add         // +
    | Subtract    // -
    | Multiply    // *
    | FloatDiv    // /
    | FloorDiv    // //
    | Modulo      // %
    | Power       // ^
    | Concat      // ..
    | BitAnd      // &
    | BitOr       // |
    | BitXor      // ~
    | ShiftLeft   // <<
    | ShiftRight  // >>
    | Equal       // ==
    | NotEqual    // ~=
    | Less        // <
    | LessEqual   // <=
    | Greater     // >
    | GreaterEqual// >=
    | And         // and
    | Or          // or

// Forward declarations for mutually recursive types
type Expr = 
    | Literal of Literal
    | Var of Identifier
    | TableAccess of Expr * Expr
    | TableConstructor of TableField list
    | FunctionDef of FunctionDef
    | FunctionCall of Expr * Expr list
    | MethodCall of Expr * Identifier * Expr list
    | Unary of UnaryOp * Expr
    | Binary of Expr * BinaryOp * Expr
    | Vararg
    | Paren of Expr
    // Positioned variants for error reporting
    | VarPos of Identifier * SourceLocation
    | FunctionCallPos of Expr * Expr list * SourceLocation
    
    // Factory methods for C# interoperability
    static member CreateLiteral(literal: Literal) = Expr.Literal literal
    static member CreateVar(name: string) = Expr.Var name
    static member CreateTableAccess(table: Expr, key: Expr) = Expr.TableAccess(table, key)
    static member CreateTableConstructor(fields: TableField list) = Expr.TableConstructor fields
    static member CreateFunctionDef(funcDef: FunctionDef) = Expr.FunctionDef funcDef
    static member CreateFunctionCall(func: Expr, args: Expr list) = Expr.FunctionCall(func, args)
    static member CreateMethodCall(obj: Expr, methodName: string, args: Expr list) = Expr.MethodCall(obj, methodName, args)
    static member CreateUnary(op: UnaryOp, expr: Expr) = Expr.Unary(op, expr)
    static member CreateBinary(left: Expr, op: BinaryOp, right: Expr) = Expr.Binary(left, op, right)
    static member CreateVararg() = Expr.Vararg
    static member CreateParen(expr: Expr) = Expr.Paren expr
    // Positioned variants
    static member CreateVarPos(name: string, location: SourceLocation) = Expr.VarPos(name, location)
    static member CreateFunctionCallPos(func: Expr, args: Expr list, location: SourceLocation) = Expr.FunctionCallPos(func, args, location)

/// Represents a field in a table constructor
and TableField =
    | ExprField of Expr
    | NamedField of Identifier * Expr
    | KeyField of Expr * Expr
    
    // Factory methods for C# interoperability
    static member CreateExprField(expr: Expr) = TableField.ExprField expr
    static member CreateNamedField(name: string, expr: Expr) = TableField.NamedField(name, expr)
    static member CreateKeyField(key: Expr, value: Expr) = TableField.KeyField(key, value)

/// Represents a function parameter
and Parameter =
    | Named of Identifier * Attribute
    | Vararg
    
    // Factory methods for C# interoperability
    static member CreateNamed(name: string, attr: Attribute) = Parameter.Named(name, attr)
    static member CreateVararg() = Parameter.Vararg

/// Represents a block (sequence of statements)
and Block = Statement list

/// Represents a function definition
and FunctionDef = {
    Parameters: Parameter list
    IsVararg: bool
    Body: Block
}

/// Represents a statement in Lua
and Statement =
    | Empty
    | Assignment of Expr list * Expr list
    | LocalAssignment of (Identifier * Attribute) list * Expr list option
    | FunctionCall of Expr
    | Label of Identifier
    | Goto of Identifier
    | Break
    | DoBlock of Block
    | While of Expr * Block
    | Repeat of Block * Expr
    | If of (Expr * Block) list * Block option
    | NumericFor of Identifier * Expr * Expr * Expr option * Block
    | GenericFor of (Identifier * Attribute) list * Expr list * Block
    | FunctionDef of Identifier list * FunctionDef // supports t.a.b.c:f
    | LocalFunctionDef of Identifier * FunctionDef
    | Return of Expr list option
    
    // Factory methods for C# interoperability
    static member CreateEmpty() = Statement.Empty
    static member CreateAssignment(vars: Expr list, exprs: Expr list) = Statement.Assignment(vars, exprs)
    static member CreateLocalAssignment(vars: (string * Attribute) list, exprs: Expr list option) = 
        Statement.LocalAssignment(vars, exprs)
    static member CreateFunctionCall(expr: Expr) = Statement.FunctionCall expr
    static member CreateLabel(name: string) = Statement.Label name
    static member CreateGoto(name: string) = Statement.Goto name
    static member CreateBreak() = Statement.Break
    static member CreateDoBlock(block: Block) = Statement.DoBlock block
    static member CreateWhile(condition: Expr, block: Block) = Statement.While(condition, block)
    static member CreateRepeat(block: Block, condition: Expr) = Statement.Repeat(block, condition)
    static member CreateIf(clauses: (Expr * Block) list, elseBlock: Block option) = Statement.If(clauses, elseBlock)
    static member CreateNumericFor(var: string, start: Expr, stop: Expr, step: Expr option, block: Block) = 
        Statement.NumericFor(var, start, stop, step, block)
    static member CreateGenericFor(vars: (string * Attribute) list, exprs: Expr list, block: Block) = 
        Statement.GenericFor(vars, exprs, block)
    static member CreateFunctionDef(path: string list, funcDef: FunctionDef) = 
        Statement.FunctionDef(path, funcDef)
    static member CreateLocalFunctionDef(name: string, funcDef: FunctionDef) = 
        Statement.LocalFunctionDef(name, funcDef)
    static member CreateReturn(exprs: Expr list option) = Statement.Return exprs

/// Helper functions for extracting position information from AST nodes
[<RequireQualifiedAccess>]
module AstPosition =
    /// Extract position information from an expression
    let getPosition = function
        | Expr.VarPos(_, pos) -> Some pos
        | Expr.FunctionCallPos(_, _, pos) -> Some pos
        | _ -> None
    
    /// Extract the underlying expression without position information
    let stripPosition = function
        | Expr.VarPos(name, _) -> Expr.Var name
        | Expr.FunctionCallPos(func, args, _) -> Expr.FunctionCall(func, args)
        | expr -> expr

/// Extension methods to make working with the AST more convenient from C#
[<Extension>]
type AstExtensions =
    
    /// Creates a nil literal expression
    [<Extension>]
    static member CreateNilLiteral() = 
        Expr.Literal(Literal.Nil)
    
    /// Creates a boolean literal expression
    [<Extension>]
    static member CreateBooleanLiteral(value: bool) = 
        Expr.Literal(Literal.Boolean value)
    
    /// Creates an integer literal expression
    [<Extension>]
    static member CreateIntegerLiteral(value: int) = 
        Expr.Literal(Literal.Integer(BigInteger(value)))
    
    /// Creates a float literal expression
    [<Extension>]
    static member CreateFloatLiteral(value: float) = 
        Expr.Literal(Literal.Float value)
    
    /// Creates a string literal expression
    [<Extension>]
    static member CreateStringLiteral(value: string) = 
        Expr.Literal(Literal.String value)
    
    /// Creates a variable reference expression
    [<Extension>]
    static member CreateVarExpr(name: string) =
        Expr.Var name
    
    /// Creates a binary expression
    [<Extension>]
    static member CreateBinaryExpr(left: Expr, op: BinaryOp, right: Expr) =
        Expr.Binary(left, op, right)
    
    /// Creates a function call expression
    [<Extension>]
    static member CreateFunctionCall(func: Expr, args: Expr list) =
        Expr.FunctionCall(func, args)
    
    /// Creates a method call expression
    [<Extension>]
    static member CreateMethodCall(obj: Expr, methodName: string, args: Expr list) =
        Expr.MethodCall(obj, methodName, args) 