using System;

namespace FLua.Runtime
{
    /// <summary>
    /// Represents a variable in Lua with its value and attributes
    /// </summary>
    public class LuaVariable
    {
        public LuaValue Value { get; private set; }
        public LuaAttribute Attribute { get; }
        public bool IsClosed { get; private set; }
        public string? Name { get; set; }

        public LuaVariable(LuaValue value) : this(value, LuaAttribute.NoAttribute)
        {
        }

        public LuaVariable(LuaValue value, LuaAttribute attribute, string? name = null)
        {
            if (value.Type == LuaType.Nil)
                Value = LuaValue.Nil;
            else
                Value = value;
            Attribute = attribute;
            IsClosed = false;
            Name = name;
        }

        /// <summary>
        /// Sets the value of this variable, checking const constraints
        /// </summary>
        public void SetValue(LuaValue newValue)
        {
            if (Attribute == LuaAttribute.Const)
            {
                throw LuaRuntimeException.ConstAssignment(Name ?? "variable");
            }

            if (IsClosed)
            {
                throw LuaRuntimeException.ClosedVariableAccess(Name ?? "variable");
            }

            if (newValue.Type == LuaType.Nil)
                Value = LuaValue.Nil;
            else
                Value = newValue;
        }

        /// <summary>
        /// Gets the value of this variable, checking if it's closed
        /// </summary>
        public LuaValue GetValue()
        {
            if (IsClosed)
            {
                throw LuaRuntimeException.ClosedVariableAccess(Name ?? "variable");
            }

            return Value;
        }

        /// <summary>
        /// Closes this variable if it has the Close attribute, calling __close metamethod
        /// </summary>
        public void Close()
        {
            if (Attribute == LuaAttribute.Close && !IsClosed)
            {
                IsClosed = true;

                // Call __close metamethod if the value has one
                if (Value.IsTable)
                {
                    var table = Value.AsTable<LuaTable>();
                    if (table.Metatable != null)
                    {
                        var closeMethod = table.Metatable.RawGet(LuaValue.String("__close"));
                        if (closeMethod.IsFunction)
                        {
                            try
                            {
                                // Call __close(value, nil) - nil indicates normal close, not error
                                var closeFunc = closeMethod.AsFunction<LuaFunction>();
                                closeFunc.Call(new[] { Value, LuaValue.Nil });
                            }
                            catch (Exception ex)
                            {
                                // In Lua, errors in __close are typically ignored or logged
                                // For now, we'll just ignore them to prevent double-faults
                                Console.WriteLine($"Error in __close metamethod: {ex.Message}");
                            }
                        }
                    }
                }
                else if (Value.IsFunction)
                {
                    // For functions, we can call them as close handlers
                    try
                    {
                        var func = Value.AsFunction<LuaFunction>();
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
            if (Attribute == LuaAttribute.Const)
                attrStr = " <const>";
            else if (Attribute == LuaAttribute.Close)
                attrStr = " <close>";
            else
                attrStr = "";
                
            return $"{Value}{attrStr}";
        }
    }
}
