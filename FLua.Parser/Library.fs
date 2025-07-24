namespace FLua.Parser

// Re-export AST types for backward compatibility
open FLua.Ast

// Export all AST types from FLua.Ast for backward compatibility
type Attribute = FLua.Ast.Attribute
type Literal = FLua.Ast.Literal
type UnaryOp = FLua.Ast.UnaryOp
type BinaryOp = FLua.Ast.BinaryOp
type Expr = FLua.Ast.Expr
type TableField = FLua.Ast.TableField
type FunctionDef = FLua.Ast.FunctionDef
type Parameter = FLua.Ast.Parameter
type Block = FLua.Ast.Block
type Statement = FLua.Ast.Statement

// Type alias for backward compatibility
type Identifier = string
