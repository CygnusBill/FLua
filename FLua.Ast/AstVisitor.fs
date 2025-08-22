namespace FLua.Ast

open FLua.Common.Diagnostics
open FLua.Common

/// Interface for expression visitors
type IExpressionVisitor<'T> =
    abstract VisitLiteral: Literal -> 'T
    abstract VisitVar: string -> 'T
    abstract VisitVarPos: string * SourceLocation -> 'T
    abstract VisitTableAccess: Expr * Expr -> 'T
    abstract VisitTableConstructor: TableField list -> 'T
    abstract VisitFunctionDef: FunctionDef -> 'T
    abstract VisitFunctionCall: Expr * Expr list -> 'T
    abstract VisitFunctionCallPos: Expr * Expr list * SourceLocation -> 'T
    abstract VisitMethodCall: Expr * string * Expr list -> 'T
    abstract VisitUnary: UnaryOp * Expr -> 'T
    abstract VisitBinary: Expr * BinaryOp * Expr -> 'T
    abstract VisitVararg: unit -> 'T
    abstract VisitParen: Expr -> 'T

/// Interface for statement visitors
type IStatementVisitor<'T> =
    abstract VisitEmpty: unit -> 'T
    abstract VisitAssignment: Expr list * Expr list -> 'T
    abstract VisitLocalAssignment: (string * Attribute) list * Expr list option -> 'T
    abstract VisitFunctionCall: Expr -> 'T
    abstract VisitLabel: string -> 'T
    abstract VisitGoto: string -> 'T
    abstract VisitBreak: unit -> 'T
    abstract VisitDoBlock: Block -> 'T
    abstract VisitWhile: Expr * Block -> 'T
    abstract VisitRepeat: Block * Expr -> 'T
    abstract VisitIf: (Expr * Block) list * Block option -> 'T
    abstract VisitNumericFor: string * Expr * Expr * Expr option * Block -> 'T
    abstract VisitGenericFor: (string * Attribute) list * Expr list * Block -> 'T
    abstract VisitFunctionDef: string list * FunctionDef -> 'T
    abstract VisitLocalFunctionDef: string * FunctionDef -> 'T
    abstract VisitReturn: Expr list option -> 'T

/// Interface for Result-based expression visitors (safer error handling)
type IResultExpressionVisitor<'T> =
    abstract VisitLiteral: Literal -> Result<'T>
    abstract VisitVar: string -> Result<'T>
    abstract VisitVarPos: string * SourceLocation -> Result<'T>
    abstract VisitTableAccess: Expr * Expr -> Result<'T>
    abstract VisitTableConstructor: TableField list -> Result<'T>
    abstract VisitFunctionDef: FunctionDef -> Result<'T>
    abstract VisitFunctionCall: Expr * Expr list -> Result<'T>
    abstract VisitFunctionCallPos: Expr * Expr list * SourceLocation -> Result<'T>
    abstract VisitMethodCall: Expr * string * Expr list -> Result<'T>
    abstract VisitUnary: UnaryOp * Expr -> Result<'T>
    abstract VisitBinary: Expr * BinaryOp * Expr -> Result<'T>
    abstract VisitVararg: unit -> Result<'T>
    abstract VisitParen: Expr -> Result<'T>

/// Interface for Result-based statement visitors (safer error handling)
type IResultStatementVisitor<'T> =
    abstract VisitEmpty: unit -> Result<'T>
    abstract VisitAssignment: Expr list * Expr list -> Result<'T>
    abstract VisitLocalAssignment: (string * Attribute) list * Expr list option -> Result<'T>
    abstract VisitFunctionCall: Expr -> Result<'T>
    abstract VisitLabel: string -> Result<'T>
    abstract VisitGoto: string -> Result<'T>
    abstract VisitBreak: unit -> Result<'T>
    abstract VisitDoBlock: Block -> Result<'T>
    abstract VisitWhile: Expr * Block -> Result<'T>
    abstract VisitRepeat: Block * Expr -> Result<'T>
    abstract VisitIf: (Expr * Block) list * Block option -> Result<'T>
    abstract VisitNumericFor: string * Expr * Expr * Expr option * Block -> Result<'T>
    abstract VisitGenericFor: (string * Attribute) list * Expr list * Block -> Result<'T>
    abstract VisitFunctionDef: string list * FunctionDef -> Result<'T>
    abstract VisitLocalFunctionDef: string * FunctionDef -> Result<'T>
    abstract VisitReturn: Expr list option -> Result<'T>

/// Visitor dispatch helper module
module Visitor =
    
    /// Dispatch expression to appropriate visitor method using F# pattern matching
    let dispatchExpr (visitor: IExpressionVisitor<'T>) (expr: Expr) : 'T =
        match expr with
        | Expr.Literal lit -> visitor.VisitLiteral(lit)
        | Expr.Var name -> visitor.VisitVar(name)
        | Expr.VarPos(name, pos) -> visitor.VisitVarPos(name, pos)
        | Expr.TableAccess(table, key) -> visitor.VisitTableAccess(table, key)
        | Expr.TableConstructor fields -> visitor.VisitTableConstructor(fields)
        | Expr.FunctionDef def -> visitor.VisitFunctionDef(def)
        | Expr.FunctionCall(func, args) -> visitor.VisitFunctionCall(func, args)
        | Expr.FunctionCallPos(func, args, pos) -> visitor.VisitFunctionCallPos(func, args, pos)
        | Expr.MethodCall(obj, method, args) -> visitor.VisitMethodCall(obj, method, args)
        | Expr.Unary(op, expr) -> visitor.VisitUnary(op, expr)
        | Expr.Binary(left, op, right) -> visitor.VisitBinary(left, op, right)
        | Expr.Vararg -> visitor.VisitVararg()
        | Expr.Paren expr -> visitor.VisitParen(expr)

    /// Dispatch statement to appropriate visitor method using F# pattern matching  
    let dispatchStmt (visitor: IStatementVisitor<'T>) (stmt: Statement) : 'T =
        match stmt with
        | Statement.Empty -> visitor.VisitEmpty()
        | Statement.Assignment(vars, values) -> visitor.VisitAssignment(vars, values)
        | Statement.LocalAssignment(vars, values) -> visitor.VisitLocalAssignment(vars, values)
        | Statement.FunctionCall expr -> visitor.VisitFunctionCall(expr)
        | Statement.Label name -> visitor.VisitLabel(name)
        | Statement.Goto name -> visitor.VisitGoto(name)
        | Statement.Break -> visitor.VisitBreak()
        | Statement.DoBlock block -> visitor.VisitDoBlock(block)
        | Statement.While(cond, block) -> visitor.VisitWhile(cond, block)
        | Statement.Repeat(block, cond) -> visitor.VisitRepeat(block, cond)
        | Statement.If(clauses, elseBlock) -> visitor.VisitIf(clauses, elseBlock)
        | Statement.NumericFor(var, start, stop, step, block) -> 
            visitor.VisitNumericFor(var, start, stop, step, block)
        | Statement.GenericFor(vars, exprs, block) -> 
            visitor.VisitGenericFor(vars, exprs, block)
        | Statement.FunctionDef(path, def) -> visitor.VisitFunctionDef(path, def)
        | Statement.LocalFunctionDef(name, def) -> visitor.VisitLocalFunctionDef(name, def)
        | Statement.Return exprs -> visitor.VisitReturn(exprs)

    /// Dispatch expression to Result-based visitor method using F# pattern matching
    let dispatchExprResult (visitor: IResultExpressionVisitor<'T>) (expr: Expr) : Result<'T> =
        match expr with
        | Expr.Literal lit -> visitor.VisitLiteral(lit)
        | Expr.Var name -> visitor.VisitVar(name)
        | Expr.VarPos(name, pos) -> visitor.VisitVarPos(name, pos)
        | Expr.TableAccess(table, key) -> visitor.VisitTableAccess(table, key)
        | Expr.TableConstructor fields -> visitor.VisitTableConstructor(fields)
        | Expr.FunctionDef def -> visitor.VisitFunctionDef(def)
        | Expr.FunctionCall(func, args) -> visitor.VisitFunctionCall(func, args)
        | Expr.FunctionCallPos(func, args, pos) -> visitor.VisitFunctionCallPos(func, args, pos)
        | Expr.MethodCall(obj, method, args) -> visitor.VisitMethodCall(obj, method, args)
        | Expr.Unary(op, expr) -> visitor.VisitUnary(op, expr)
        | Expr.Binary(left, op, right) -> visitor.VisitBinary(left, op, right)
        | Expr.Vararg -> visitor.VisitVararg()
        | Expr.Paren expr -> visitor.VisitParen(expr)

    /// Dispatch statement to Result-based visitor method using F# pattern matching  
    let dispatchStmtResult (visitor: IResultStatementVisitor<'T>) (stmt: Statement) : Result<'T> =
        match stmt with
        | Statement.Empty -> visitor.VisitEmpty()
        | Statement.Assignment(vars, values) -> visitor.VisitAssignment(vars, values)
        | Statement.LocalAssignment(vars, values) -> visitor.VisitLocalAssignment(vars, values)
        | Statement.FunctionCall expr -> visitor.VisitFunctionCall(expr)
        | Statement.Label name -> visitor.VisitLabel(name)
        | Statement.Goto name -> visitor.VisitGoto(name)
        | Statement.Break -> visitor.VisitBreak()
        | Statement.DoBlock block -> visitor.VisitDoBlock(block)
        | Statement.While(cond, block) -> visitor.VisitWhile(cond, block)
        | Statement.Repeat(block, cond) -> visitor.VisitRepeat(block, cond)
        | Statement.If(clauses, elseBlock) -> visitor.VisitIf(clauses, elseBlock)
        | Statement.NumericFor(var, start, stop, step, block) -> 
            visitor.VisitNumericFor(var, start, stop, step, block)
        | Statement.GenericFor(vars, exprs, block) -> 
            visitor.VisitGenericFor(vars, exprs, block)
        | Statement.FunctionDef(path, def) -> visitor.VisitFunctionDef(path, def)
        | Statement.LocalFunctionDef(name, def) -> visitor.VisitLocalFunctionDef(name, def)
        | Statement.Return exprs -> visitor.VisitReturn(exprs)