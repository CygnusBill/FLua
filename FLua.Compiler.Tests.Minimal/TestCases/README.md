# FLua Compiler Test Cases

This directory contains Lua test scripts used to validate various compiler features and edge cases.

## Test Categories

### Anonymous Functions
- `test_anonymous_functions.lua` - Comprehensive anonymous function tests (pending implementation)
- `test_anonymous_simple.lua` - Basic anonymous function assignment
- `test_anonymous_complex.lua` - Complex anonymous function scenarios with closures
- `test_anonymous_no_closure.lua` - Anonymous functions without closure capture
- `test_anon_debug.lua` - Debug scenarios for anonymous functions

### Control Flow
- `test_generic_for.lua` - Generic for loop with pairs/ipairs
- `test_generic_for_simple.lua` - Simple for loop test
- `test_generic_for_with_anon.lua` - For loops with anonymous iterators
- `test_custom_iterator.lua` - Custom iterator implementation

### Functions
- `test_simple_func.lua` - Basic function definition and call
- `test_no_params.lua` - Functions without parameters
- `test_empty_return.lua` - Functions with empty return statements
- `test_varargs.lua` - Variable argument functions
- `test_varargs_simple.lua` - Simple varargs scenarios
- `test_varargs_minimal.lua` - Minimal varargs test case
- `test_varargs_table.lua` - Varargs with table operations
- `test_varargs_debug.lua` - Debug scenarios for varargs

### Method Calls
- `test_method_calls.lua` - Method call syntax (object:method())
- `test_method_calls_simple.lua` - Basic method calls
- `test_method_calls_existing.lua` - Method calls on existing objects
- `test_method_calls_no_chain.lua` - Non-chained method calls
- `test_string_method.lua` - String method calls

## Usage

These test files are used for:

1. **Compiler Development** - Testing new compiler features as they're implemented
2. **Regression Testing** - Ensuring compiler changes don't break existing functionality
3. **Feature Documentation** - Documenting expected behavior for various Lua constructs

## Running Tests

To use these test files with the compiler:

```csharp
// Example usage in tests
string luaCode = File.ReadAllText("TestCases/test_simple_func.lua");
var ast = FLua.Parser.ParserHelper.ParseString(luaCode);
var result = compiler.Compile(ast, options);
```

## Test Status

| Test File | Feature | Status |
|-----------|---------|--------|
| `test_simple_func.lua` | Basic functions | ✅ Implemented |
| `test_generic_for.lua` | For loops | ✅ Implemented |
| `test_method_calls.lua` | Method syntax | ✅ Implemented |
| `test_varargs.lua` | Variable arguments | ✅ Implemented |
| `test_anonymous_functions.lua` | Anonymous functions | ⏳ Pending |
| `test_custom_iterator.lua` | Custom iterators | ⏳ Pending |

## Adding New Tests

When adding new test cases:

1. Use descriptive names: `test_<feature>_<variant>.lua`
2. Include comments explaining what's being tested
3. Add expected output in comments
4. Update this README with the new test

## Notes

- Some tests document features not yet implemented in the compiler
- Tests should be simple and focused on one feature
- Complex integration tests belong in the main test suite