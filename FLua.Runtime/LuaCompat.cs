// namespace FLua.Runtime
// {
//     /// <summary>
//     /// Compatibility helpers for old LuaValue class hierarchy
//     /// </summary>
//     public static class LuaNil
//     {
//         public static LuaValue Instance => LuaValue.Nil;
//     }
//     
//     public class LuaBoolean
//     {
//         public bool Value { get; }
//         
//         public LuaBoolean(bool value)
//         {
//             Value = value;
//         }
//         
//         public static implicit operator LuaValue(LuaBoolean b) => LuaValue.Boolean(b.Value);
//         
//         public static LuaValue True => LuaValue.Boolean(true);
//         public static LuaValue False => LuaValue.Boolean(false);
//     }
//     
//     public class LuaInteger
//     {
//         public long Value { get; }
//         
//         public LuaInteger(long value)
//         {
//             Value = value;
//         }
//         
//         public static implicit operator LuaValue(LuaInteger i) => LuaValue.Integer(i.Value);
//         
//         // Helper for pattern matching
//         public static bool IsInstance(LuaValue value) => value.Type == LuaType.Integer;
//         public static long GetValue(LuaValue value) => value.AsInteger();
//     }
//     
//     public class LuaNumber  
//     {
//         public double DoubleValue { get; }
//         public long IntegerValue { get; }
//         private bool IsInteger { get; }
//         
//         public LuaNumber(double doubleValue)
//         {
//             DoubleValue = doubleValue;
//             IntegerValue = 0;
//             IsInteger = false;
//         }
//
//         public LuaNumber(long longValue)
//         {
//             DoubleValue = 0;
//             IntegerValue = longValue;
//             IsInteger = true;
//         }
//         
//         public static implicit operator LuaValue(LuaNumber n) => LuaValue.Float(n.DoubleValue);
//         
//         // Helper for pattern matching
//         public static bool IsInstance(LuaValue value) => value.Type == LuaType.Float;
//         public static double GetValue(LuaValue value) => value.AsFloat();
//     }
//     
//     public class LuaString
//     {
//         public string Value { get; }
//         
//         public LuaString(string value)
//         {
//             Value = value ?? string.Empty;
//         }
//         
//         public static implicit operator LuaValue(LuaString s) => LuaValue.String(s.Value);
//         
//         // Helper for pattern matching
//         public static bool IsInstance(LuaValue value) => value.Type == LuaType.String;
//         public static string GetValue(LuaValue value) => value.AsString();
//     }
// }