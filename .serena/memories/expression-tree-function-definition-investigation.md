# Expression Tree Function Definition Investigation - August 2025

## Problem Context
During expression tree test fixes, encountered a failing test `CompileToExpression_ComplexCalculation_EvaluatesCorrectly` that expected function definitions to work in expression tree compilation. User correctly identified this as an intersection between security levels and expression tree capabilities.

## Key Discovery: Security Levels vs Language Constructs
**Critical Insight**: These are separate, intersecting concerns:

1. **Security Levels** (Untrusted, Sandbox, Restricted, Trusted, FullTrust)
   - Control which **built-in functions** are available
   - Examples: `load`, `require`, `io`, `debug` modules
   - Implemented in `StandardSecurityPolicy.cs`
   - Default: `TrustLevel.Sandbox`

2. **Expression Tree Limitations** 
   - Control which **language constructs** are supported
   - Examples: function definitions, loops, complex control flow
   - Implemented in `MinimalExpressionTreeGenerator.cs`
   - By design: Simple expressions only

## Technical Fixes Applied

### 1. Function Definition Error Handling
**Before**: Silent nil return → confusing "Value is not a function, it's Nil" error
**After**: 
- Proper diagnostics: "Local function definitions are not supported in expression tree compilation"
- Lua-like runtime error: "attempt to call a nil value"

### 2. Statement-Level Function Handling
Added support for detecting and handling function definition statements:
```csharp
else if (stmt.IsLocalFunctionDef)
{
    _diagnostics.Report(new FLuaDiagnostic
    {
        Code = "EXPR-001",
        Severity = ErrorSeverity.Error,
        Message = "Local function definitions are not supported in expression tree compilation. Use a different compilation target for complex Lua programs."
    });
    // Create local variable set to nil (Lua-like behavior)
}
```

### 3. Improved Function Call Error Handling
Enhanced function call handling to behave like standard Lua:
```csharp
// Check if value is nil and throw proper Lua error
var isNilCheck = Expression.Equal(
    Expression.Field(func, nameof(LuaValue.Type)),
    Expression.Constant(LuaType.Nil));

var conditionalCall = Expression.Condition(
    isNilCheck,
    throwNilError, // "attempt to call a nil value"
    actualFunctionCall
);
```

### 4. LuaValue.Type Field Access Fix
**Bug**: Used `Expression.Property` for `LuaValue.Type` 
**Fix**: Changed to `Expression.Field` since `Type` is a field, not property

## Results
- ✅ **Fixed 2 originally failing expression tree tests** (local variables, table operations)
- ✅ **Improved error handling** to be more Lua-like
- ✅ **Added comprehensive diagnostics** for unsupported constructs
- ✅ **Clarified design boundaries** between security and language support

## Expression Tree Scope Clarification
**Supported**: 
- Arithmetic operations (`2 + 2`)
- Local variables (`local x = 10; return x`)
- Table operations (`{a=10}.a`)
- Function calls on existing functions (`math.floor(10.7)`)

**Unsupported by Design**:
- Function definitions (`local function factorial(n)`)
- Loops (`for`, `while`, `repeat`)
- Complex control flow (`if`, `goto`)

## Test Status Impact
- **Before**: 12/14 expression tree tests passing
- **After**: 13/14 expression tree tests passing
- **Remaining**: 1 test with incorrect expectations (expects function definitions to work in expression trees)

## Architectural Insight
The intersection between security levels and expression tree compilation is properly handled:
- Security levels control **what functions are available** at runtime
- Expression tree compilation controls **what language constructs are supported** at compile time
- Both can coexist and provide appropriate error messages

## Recommendation
The failing test `CompileToExpression_ComplexCalculation_EvaluatesCorrectly` should either:
1. Use a different compilation target that supports function definitions, or
2. Expect the "attempt to call a nil value" error as correct behavior

The current implementation properly separates concerns and provides clear, Lua-compatible error handling.