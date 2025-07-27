namespace FLua.Runtime;

/// <summary>
/// Binary operators in Lua
/// </summary>
public enum LuaBinaryOp
{
    Add,          // +
    Subtract,     // -
    Multiply,     // *
    FloatDiv,     // /
    FloorDiv,     // //
    Modulo,       // %
    Power,        // ^
    Concat,       // ..
    BitAnd,       // &
    BitOr,        // |
    BitXor,       // ~
    ShiftLeft,    // <<
    ShiftRight,   // >>
    Equal,        // ==
    NotEqual,     // ~=
    Less,         // <
    LessEqual,    // <=
    Greater,      // >
    GreaterEqual, // >=
    And,          // and
    Or            // or
}