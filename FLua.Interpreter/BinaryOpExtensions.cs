using System;
using FLua.Ast;
using FLua.Runtime;

namespace FLua.Interpreter
{
    /// <summary>
    /// Extension methods for BinaryOp to provide deterministic operator identification
    /// </summary>
    public static class BinaryOpExtensions
    {
        /// <summary>
        /// Gets the operator kind as a string for deterministic switching
        /// </summary>
        public static string GetOperatorKind(this BinaryOp op)
        {
            // F# discriminated unions have an implicit ToString that returns the case name
            return op.ToString();
        }

        /// <summary>
        /// Evaluates a binary operation using string-based dispatch for determinism
        /// </summary>
        public static LuaValue Evaluate(this BinaryOp op, LuaValue left, LuaValue right)
        {
            switch (op.GetOperatorKind())
            {
                case "Add": return LuaOperations.Add(left, right);
                case "Subtract": return LuaOperations.Subtract(left, right);
                case "Multiply": return LuaOperations.Multiply(left, right);
                case "FloatDiv": return LuaOperations.FloatDivide(left, right);
                case "FloorDiv": return LuaOperations.FloorDivide(left, right);
                case "Modulo": return LuaOperations.Modulo(left, right);
                case "Power": return LuaOperations.Power(left, right);
                case "Concat": return LuaOperations.Concat(left, right);
                case "BitAnd": return LuaOperations.BitAnd(left, right);
                case "BitOr": return LuaOperations.BitOr(left, right);
                case "BitXor": return LuaOperations.BitXor(left, right);
                case "ShiftLeft": return LuaOperations.ShiftLeft(left, right);
                case "ShiftRight": return LuaOperations.ShiftRight(left, right);
                case "Equal": return LuaOperations.Equal(left, right);
                case "NotEqual": return LuaOperations.NotEqual(left, right);
                case "Less": return LuaOperations.Less(left, right);
                case "LessEqual": return LuaOperations.LessEqual(left, right);
                case "Greater": return LuaOperations.Greater(left, right);
                case "GreaterEqual": return LuaOperations.GreaterEqual(left, right);
                case "And": return LuaOperations.And(left, right);
                case "Or": return LuaOperations.Or(left, right);
                default:
                    throw new NotImplementedException($"Binary operator not implemented: {op}");
            }
        }

        /// <summary>
        /// Gets the metamethod name for a binary operator
        /// </summary>
        public static string? GetMetamethodName(this BinaryOp op)
        {
            switch (op.GetOperatorKind())
            {
                case "Add": return "__add";
                case "Subtract": return "__sub";
                case "Multiply": return "__mul";
                case "FloatDiv": return "__div";
                case "FloorDiv": return "__idiv";
                case "Modulo": return "__mod";
                case "Power": return "__pow";
                case "Concat": return "__concat";
                case "BitAnd": return "__band";
                case "BitOr": return "__bor";
                case "BitXor": return "__bxor";
                case "ShiftLeft": return "__shl";
                case "ShiftRight": return "__shr";
                case "Equal": return "__eq";
                case "NotEqual": return null; // Uses __eq and negates
                case "Less": return "__lt";
                case "LessEqual": return "__le";
                case "Greater": return null; // Uses __lt with swapped operands
                case "GreaterEqual": return null; // Uses __le with swapped operands
                case "And": return null; // No metamethod
                case "Or": return null; // No metamethod
                default: return null;
            }
        }
    }

    /// <summary>
    /// Extension methods for UnaryOp to provide deterministic operator identification
    /// </summary>
    public static class UnaryOpExtensions
    {
        /// <summary>
        /// Gets the operator kind as a string for deterministic switching
        /// </summary>
        public static string GetOperatorKind(this UnaryOp op)
        {
            // F# discriminated unions have an implicit ToString that returns the case name
            return op.ToString();
        }

        /// <summary>
        /// Evaluates a unary operation using string-based dispatch for determinism
        /// </summary>
        public static LuaValue Evaluate(this UnaryOp op, LuaValue value)
        {
            switch (op.GetOperatorKind())
            {
                case "Negate": return LuaOperations.Negate(value);
                case "Not": return LuaOperations.Not(value);
                case "Length": return LuaOperations.Length(value);
                case "BitNot": return LuaOperations.BitNot(value);
                default:
                    throw new NotImplementedException($"Unary operator not implemented: {op}");
            }
        }

        /// <summary>
        /// Gets the metamethod name for a unary operator
        /// </summary>
        public static string? GetMetamethodName(this UnaryOp op)
        {
            switch (op.GetOperatorKind())
            {
                case "Negate": return "__unm";
                case "Not": return null; // No metamethod
                case "Length": return "__len";
                case "BitNot": return "__bnot";
                default: return null;
            }
        }
    }
}