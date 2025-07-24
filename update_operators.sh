#!/bin/bash
operators=(
  "FloatDiv"
  "Power"
  "Modulo"
  "FloorDiv"
  "BitAnd"
  "BitOr"
  "BitXor"
  "ShiftLeft"
  "ShiftRight"
  "Less"
  "Greater"
  "LessEqual"
  "GreaterEqual"
  "Equal"
  "NotEqual"
  "And"
  "Or"
  "Concat"
)

for op in "${operators[@]}"; do
  sed -i '' "s/| $op ->/| BinaryOp.$op ->/" FLua.Interpreter/Interpreter.fs
done

# Update unary operators
unary_operators=(
  "Not"
  "Negate"
  "Length"
  "BitNot"
)

for op in "${unary_operators[@]}"; do
  sed -i '' "s/| $op ->/| UnaryOp.$op ->/" FLua.Interpreter/Interpreter.fs
done

# Update statement types
statements=(
  "Empty"
  "Assignment"
  "LocalAssignment"
  "FunctionCallStmt"
  "Return"
  "Break"
  "LocalFunctionDef"
  "If"
  "While"
  "NumericFor"
  "DoBlock"
  "Repeat"
  "Label"
  "Goto"
  "GenericFor"
  "FunctionDefStmt"
)

for stmt in "${statements[@]}"; do
  sed -i '' "s/| $stmt /| Statement.$stmt /" FLua.Interpreter/Interpreter.fs
done

# Update expression types
expressions=(
  "Literal"
  "Var"
  "Binary"
  "Unary"
  "TableAccess"
  "TableConstructor"
  "FunctionCall"
  "MethodCall"
  "FunctionDef"
  "Vararg"
  "Paren"
)

for expr in "${expressions[@]}"; do
  sed -i '' "s/| $expr /| Expr.$expr /" FLua.Interpreter/Interpreter.fs
done

# Update table field types
fields=(
  "ExprField"
  "NamedField"
  "KeyField"
)

for field in "${fields[@]}"; do
  sed -i '' "s/| $field /| TableField.$field /" FLua.Interpreter/Interpreter.fs
done

# Update parameter types
sed -i '' "s/| Param /| Parameter.Named /" FLua.Interpreter/Interpreter.fs
sed -i '' "s/| VarargParam/| Parameter.Vararg/" FLua.Interpreter/Interpreter.fs
