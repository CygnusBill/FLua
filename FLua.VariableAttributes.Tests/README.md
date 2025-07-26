# FLua Variable Attributes Tests

This test project provides comprehensive testing for the `const` and `<close>` variable attributes implementation in FLua.

## Project Structure

```
FLua.VariableAttributes.Tests/
├── VariableAttributeTests.cs          # Main MSTest test suite
├── FLua.VariableAttributes.Tests.csproj # Project file
├── README.md                           # This file
├── manual-tests/                       # Individual Lua test files for manual testing
│   ├── const_variables_test.lua
│   ├── close_variables_test.lua
│   ├── function_params_test.lua
│   └── error_cases_test.lua
└── cleanup/                            # Old/backup files (not used)
```

## Test Categories

### 1. ConstVariableTests
- Basic const variable declaration and access
- Const modification prevention and error handling
- Multiple const variables in single declaration
- Mixed const and regular variables
- Const variables in block scopes
- Clear error messages for const violations

### 2. CloseVariableTests
- Basic close variable with `__close` metamethod
- Close variables in function scopes
- Multiple close variables (LIFO cleanup order)
- Close variables with early returns
- Automatic cleanup on scope exit

### 3. FunctionParameterAttributeTests
- Function parameters with const attributes
- Function parameters with close attributes
- Mixed parameter attributes
- Parameter modification prevention

### 4. ParserAttributeTests
- Parsing const local variable declarations
- Parsing close local variable declarations
- Parsing multiple attributed variables
- Parsing function parameters with attributes

### 5. RuntimeVariableTests
- Direct LuaVariable const behavior testing
- Direct LuaVariable close behavior testing
- LuaEnvironment attribute support
- To-be-closed variable tracking

## Running the Tests

```bash
cd /Users/bill/Repos/FLua
dotnet test FLua.VariableAttributes.Tests
```

## Test Framework

Uses MSTest following the established pattern in the FLua solution, providing integration testing across Parser, Runtime, and Interpreter components.

## Coverage

- ✅ Parser attribute syntax support
- ✅ Runtime const checking
- ✅ Runtime close variable tracking
- ✅ Interpreter scope management
- ✅ Error handling and messages
- ✅ Integration across all components
