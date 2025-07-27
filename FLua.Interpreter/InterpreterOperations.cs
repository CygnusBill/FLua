using FLua.Ast;
using FLua.Runtime;

namespace FLua.Interpreter;

/// <summary>
/// Provides operations that bridge between the F# AST types and the runtime types
/// </summary>
public static class InterpreterOperations
{
    /// <summary>
    /// Converts an F# Attribute to a runtime LuaAttribute
    /// </summary>
    public static LuaAttribute ConvertAttribute(FLua.Ast.Attribute attribute)
    {
        return attribute.ToString() switch
        {
            "NoAttribute" => LuaAttribute.NoAttribute,
            "Const" => LuaAttribute.Const,
            "Close" => LuaAttribute.Close,
            _ => LuaAttribute.NoAttribute
        };
    }
    
    /// <summary>
    /// Evaluates a binary operation based on the operator type
    /// </summary>
    public static LuaValue EvaluateBinaryOp(LuaValue left, BinaryOp op, LuaValue right)
    {
        // Use extension method for deterministic evaluation
        return op.Evaluate(left, right);
    }

    /// <summary>
    /// Evaluates a unary operation based on the operator type
    /// </summary>
    public static LuaValue EvaluateUnaryOp(UnaryOp op, LuaValue value)
    {
        // Use extension method for deterministic evaluation
        return op.Evaluate(value);
    }
    
    /// <summary>
    /// Gets the metamethod name for a binary operator
    /// </summary>
    public static string? GetMetamethodName(BinaryOp op)
    {
        // Use extension method for deterministic lookup
        return op.GetMetamethodName();
    }

    /// <summary>
    /// Gets the metamethod name for a unary operator
    /// </summary>
    public static string? GetMetamethodName(UnaryOp op)
    {
        // Use extension method for deterministic lookup
        return op.GetMetamethodName();
    }
}