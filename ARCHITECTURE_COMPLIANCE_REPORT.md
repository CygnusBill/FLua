# FLua Architecture Compliance Report

## Executive Summary
This report analyzes the FLua codebase for compliance with the architectural principle: "All runtime functionality must be in FLua.Runtime project to avoid duplication between interpreter and future compiler."

### Key Finding
**CRITICAL VIOLATION**: The FLua.Interpreter project contains substantial runtime logic that should be in FLua.Runtime, creating significant technical debt for future compiler implementation.

## Detailed Findings

### 1. Arithmetic and Mathematical Operations (HIGH PRIORITY)
**Location**: `/Users/bill/Repos/FLua/FLua.Interpreter/LuaInterpreter.cs` (lines 1103-1383)

The entire `EvaluateBinaryOp` method implements all arithmetic operations directly in the interpreter:
- Addition, subtraction, multiplication, division
- Modulo, power, floor division
- Bitwise operations (AND, OR, XOR, shifts)
- String concatenation
- Comparison operations (less than, greater than, equals, etc.)
- Logical operations (AND, OR)

**Impact**: Any future compiler will need to duplicate this entire implementation.

**Recommendation**: Move to a new `LuaOperations` class in FLua.Runtime.

### 2. Unary Operations (HIGH PRIORITY)
**Location**: `/Users/bill/Repos/FLua/FLua.Interpreter/LuaInterpreter.cs` (lines 1470-1528)

The `EvaluateUnaryOp` method implements:
- Negation
- Logical NOT
- Length operator
- Bitwise NOT

**Impact**: Duplicated implementation required in compiler.

**Recommendation**: Move to `LuaOperations` class in FLua.Runtime.

### 3. Type Conversions and Coercions (MEDIUM PRIORITY)
**Location**: Throughout `/Users/bill/Repos/FLua/FLua.Interpreter/LuaInterpreter.cs`

Direct usage of type conversion properties:
- `AsNumber`, `AsInteger`, `AsString` properties used extensively
- String concatenation with implicit conversions (line 1202)
- Equality comparisons using `ToString()` (lines 1207, 1212)

**Impact**: Type conversion logic is scattered and will need duplication.

**Recommendation**: Create centralized type conversion methods in FLua.Runtime.

### 4. Metamethod Handling (MEDIUM PRIORITY)
**Location**: `/Users/bill/Repos/FLua/FLua.Interpreter/LuaInterpreter.cs` (lines 1388-1544)

Methods for metamethod resolution:
- `GetMetamethodForBinaryOp`
- `GetMetamethodForUnaryOp`
- `TryInvokeMetamethod`

**Impact**: Metamethod behavior will need reimplementation in compiler.

**Recommendation**: Move metamethod resolution to FLua.Runtime.

### 5. Table Operations (LOW PRIORITY)
**Location**: Various locations in `/Users/bill/Repos/FLua/FLua.Interpreter/LuaInterpreter.cs`

Direct table manipulations:
- `table.Set()` calls
- `table.Get()` calls
- Metatable access

**Current State**: These are already using FLua.Runtime's LuaTable class, which is correct.

**Recommendation**: No action needed - properly using runtime types.

## Proposed Architecture

### New Classes in FLua.Runtime

1. **LuaOperations.cs**
   ```csharp
   public static class LuaOperations
   {
       public static LuaValue Add(LuaValue left, LuaValue right);
       public static LuaValue Subtract(LuaValue left, LuaValue right);
       public static LuaValue Multiply(LuaValue left, LuaValue right);
       // ... other binary operations
       
       public static LuaValue Negate(LuaValue value);
       public static LuaValue Not(LuaValue value);
       // ... other unary operations
   }
   ```

2. **LuaTypeConversion.cs**
   ```csharp
   public static class LuaTypeConversion
   {
       public static double? ToNumber(LuaValue value);
       public static long? ToInteger(LuaValue value);
       public static string ToString(LuaValue value);
       public static bool ToBoolean(LuaValue value);
   }
   ```

3. **LuaMetamethods.cs**
   ```csharp
   public static class LuaMetamethods
   {
       public static LuaValue InvokeBinaryMetamethod(LuaValue left, LuaValue right, string method);
       public static LuaValue InvokeUnaryMetamethod(LuaValue value, string method);
       public static string? GetMetamethodName(BinaryOp op);
       public static string? GetMetamethodName(UnaryOp op);
   }
   ```

## Migration Strategy

1. **Phase 1**: Create new classes in FLua.Runtime with the proposed functionality
2. **Phase 2**: Update FLua.Interpreter to use the new runtime classes
3. **Phase 3**: Add comprehensive unit tests for all moved functionality
4. **Phase 4**: Verify no regression in existing Lua tests

## Benefits of Compliance

1. **Code Reuse**: Future compiler can use the same runtime operations
2. **Consistency**: Guaranteed identical behavior between interpreter and compiler
3. **Maintainability**: Single source of truth for runtime behavior
4. **Testing**: Centralized location for runtime operation tests
5. **Performance**: Potential for optimization in one place benefits all consumers

## Risks of Non-Compliance

1. **Duplication**: Compiler will need to reimplement all arithmetic operations
2. **Inconsistency**: Risk of behavioral differences between interpreter and compiler
3. **Maintenance Burden**: Bug fixes and features must be implemented twice
4. **Testing Overhead**: Duplicate test suites needed
5. **Technical Debt**: Increases exponentially as more features are added

## Conclusion

The current architecture has a significant violation of the separation principle. The interpreter contains approximately 400+ lines of runtime logic that belongs in FLua.Runtime. This represents a critical technical debt that should be addressed before compiler development begins.

**Recommendation**: HIGH PRIORITY - Refactor runtime operations into FLua.Runtime before proceeding with compiler implementation.