namespace FLua.Parser

// Abstract Syntax Tree (AST) for Lua 5.4

/// Represents a Lua identifier (variable/function name)
type Identifier = string

/// Represents a literal value in Lua
 type Literal =
    | Nil
    | Boolean of bool
    | Integer of int64
    | Float of float
    | String of string

/// Represents a unary operator in Lua
 type UnaryOp =
    | Negate      // -
    | Not         // not
    | Length      // #
    | BitNot      // ~

/// Represents a binary operator in Lua
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

/// Represents an expression in Lua
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

/// Represents a field in a table constructor
 and TableField =
    | ExprField of Expr
    | NamedField of Identifier * Expr
    | KeyField of Expr * Expr

/// Represents a function definition
 and FunctionDef = {
    Parameters: Parameter list
    IsVararg: bool
    Body: Block
 }

/// Represents a function parameter
 and Parameter =
    | Param of Identifier
    | VarargParam

/// Represents a block (sequence of statements)
 and Block = Statement list

/// Represents a statement in Lua
 and Statement =
    | Empty
    | Assignment of Expr list * Expr list
    | LocalAssignment of Identifier list * Expr list option
    | FunctionCallStmt of Expr
    | Label of Identifier
    | Goto of Identifier
    | Break
    | DoBlock of Block
    | While of Expr * Block
    | Repeat of Block * Expr
    | If of (Expr * Block) list * Block option
    | NumericFor of Identifier * Expr * Expr * Expr option * Block
    | GenericFor of Identifier list * Expr list * Block
    | FunctionDefStmt of Identifier list * FunctionDef // supports t.a.b.c:f
    | LocalFunctionDef of Identifier * FunctionDef
    | Return of Expr list option
