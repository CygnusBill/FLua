using System;
using FLua.Ast;

namespace FLua.Runtime
{
    /// <summary>
    /// Represents a variable in Lua with its value and attributes
    /// </summary>
    public class LuaVariable
    {
        public LuaValue Value { get; private set; }
        public FLua.Ast.Attribute Attribute { get; }
        public bool IsClosed { get; private set; }

        public LuaVariable(LuaValue value) : this(value, FLua.Ast.Attribute.NoAttribute)
        {
        }

        public LuaVariable(LuaValue value, FLua.Ast.Attribute attribute)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Attribute = attribute;
            IsClosed = false;
        }

        /// <summary>
        /// Sets the value of this variable, checking const constraints
        /// </summary>
        public void SetValue(LuaValue newValue)
        {
            if (Attribute == FLua.Ast.Attribute.Const)
            {
                throw new LuaRuntimeException("attempt to change const variable");
            }

            if (IsClosed)
            {
                throw new LuaRuntimeException("attempt to use closed variable");
            }

            Value = newValue ?? throw new ArgumentNullException(nameof(newValue));
        }

        /// <summary>
        /// Gets the value of this variable, checking if it's closed
        /// </summary>
        public LuaValue GetValue()
        {
            if (IsClosed)
            {
                throw new LuaRuntimeException("attempt to use closed variable");
            }

            return Value;
        }

        /// <summary>
        /// Closes this variable if it has the Close attribute, calling __close metamethod
        /// </summary>
        public void Close()
        {
            if (Attribute == FLua.Ast.Attribute.Close && !IsClosed)
            {
                IsClosed = true;

                // Call __close metamethod if the value has one
                if (Value is LuaTable table && table.Metatable != null)
                {
                    var closeMethod = table.Metatable.RawGet(new LuaString("__close"));
                    if (closeMethod is LuaFunction closeFunc)
                    {
                        try
                        {
                            // Call __close(value, nil) - nil indicates normal close, not error
                            closeFunc.Call(new[] { Value, LuaNil.Instance });
                        }
                        catch (Exception ex)
                        {
                            // In Lua, errors in __close are typically ignored or logged
                            // For now, we'll just ignore them to prevent double-faults
                            Console.WriteLine($"Error in __close metamethod: {ex.Message}");
                        }
                    }
                }
                else if (Value is LuaFunction func)
                {
                    // For functions, we can call them as close handlers
                    try
                    {
                        func.Call(Array.Empty<LuaValue>());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in close function: {ex.Message}");
                    }
                }
            }
        }

        public override string ToString()
        {
            string attrStr;
            if (Attribute == FLua.Ast.Attribute.Const)
                attrStr = " <const>";
            else if (Attribute == FLua.Ast.Attribute.Close)
                attrStr = " <close>";
            else
                attrStr = "";
                
            return $"{Value}{attrStr}";
        }
    }
}
