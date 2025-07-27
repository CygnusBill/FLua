# FLua Parser Known Issues

## Function Calls with Table Constructors in Specific Contexts

**Issue**: The parser fails to recognize function calls with table constructor syntax (no parentheses) in generic for loops and if conditions.

**Examples that fail**:
```lua
for k in pairs{1,2,3} do end    -- Fails in generic for
if f{} then end                  -- Fails in if condition
```

**Examples that work**:
```lua
print{1,2,3}                     -- Works as statement
local x = f{1,2,3}              -- Works in assignment
for k in pairs({1,2,3}) do end  -- Works with parentheses
if f({}) then end               -- Works with parentheses
```

**Root Cause**: The expression parser uses forward references that get captured too early when used in combinators like `sepBy1`. This affects parsing in specific syntactic contexts where the expression parser is composed with other parsers.

**Workaround**: Use parentheses around table constructors: `pairs({1,2,3})`

**Test Status**: Affects literals.lua line 250 and potentially other tests

**Priority**: Medium - Affects official Lua test suite compatibility

## Function Calls with Long Strings (No Parentheses)

**Issue**: The parser fails to recognize function calls with long string literals when there are no parentheses.

**Examples that fail**:
```lua
print[[hello]]        -- Parsed as variable "print" instead of function call
print[=[hello]=]      -- Same issue
```

**Examples that work**:
```lua
print([[hello]])      -- With parentheses works fine
print "hello"         -- Regular strings work
obj:method[[hello]]   -- Method calls work correctly
```

**Root Cause**: The parser's postfix operation handling for function calls with string literals may not be correctly recognizing long bracket strings (`[[...]]`) as valid postfix operations.

**Workaround**: Use parentheses around long string arguments: `print([[hello]])`

**Test Status**: Tests for this functionality are currently commented out in `FLua.Parser.Tests/Program.fs`

**Priority**: Low - This is an edge case and has an easy workaround

## Unicode Escapes Beyond Valid Range

**Issue**: Unicode escapes with values > U+10FFFF are accepted to maintain compatibility with Lua tests.

**Warning**: These produce invalid UTF-8 sequences and should not be used in real applications.

**Examples**:
```lua
"\u{10FFFF}"   -- Valid: Maximum valid Unicode
"\u{200000}"   -- Invalid: Beyond Unicode range (but accepted for tests)
"\u{3FFFFFF}"  -- Invalid: Way beyond Unicode (but accepted for tests)
```

**Note**: The parser includes a warning comment about this behavior, but doesn't emit runtime warnings.

## Single Underscore Identifier Issue (FIXED)

**Issue**: The parser was rejecting single underscore `_` as a valid identifier.

**Example that was failing**:
```lua
for _, n in pairs{"\n", "\r", "\n\r", "\r\n"} do
    -- The _ was not recognized as a valid identifier
end
```

**Root Cause**: The parser was using `many1Satisfy2L` which requires at least 2 characters for an identifier.

**Fix Applied**: Changed the identifier parser to use `many1SatisfyL` followed by `manySatisfy` to allow single-character identifiers.

**Status**: FIXED - Single underscore now works as an identifier.

## Function Calls with Long Strings (FIXED)

**Issue**: The parser was failing to recognize function calls with long string literals when there are no parentheses.

**Status**: FIXED - Long string function calls now work correctly.

## Shebang Support (FIXED)

**Issue**: The parser didn't support shebang lines (`#!/usr/bin/env lua`) at the start of files.

**Status**: FIXED - Shebang lines are now properly ignored when they appear as the first line of a file.

## Table Assignment at Statement Level

**Issue**: The parser fails to recognize table assignment statements like `t[1] = 100` at the statement level.

**Examples that fail**:
```lua
t[1] = 100           -- Fails: table indexing assignment
t.field = "value"    -- Fails: dot notation assignment
t["key"] = true      -- Fails: string key assignment
```

**Examples that work**:
```lua
local t = {}
t = {[1] = 100}      -- Works: table constructor with explicit keys
local x = t[1]       -- Works: table access in expressions
-- Workaround: use a function
local function set(t, k, v) t[k] = v end
set(t, 1, 100)       -- Works: assignment inside function
```

**Root Cause**: The statement parser doesn't include table access expressions in the list of valid assignment targets. It only accepts simple variables and lists of variables.

**Test Status**: One compiler test is failing due to this limitation (CompileAndExecute_TableAssignment_WorksCorrectly)

**Priority**: High - This is a fundamental Lua feature that should be supported

## Table Access in Binary Expressions

**Issue**: The parser has trouble with table access expressions when used directly in binary operations.

**Examples that fail**:
```lua
local sum = t[1] + t[2]    -- Fails: parser error after t[1]
local x = t.a + t.b        -- May fail in some contexts
```

**Examples that work**:
```lua
local a = t[1]
local b = t[2]
local sum = a + b          -- Works: separate variable assignments
```

**Root Cause**: Possible operator precedence or expression parsing issue when table access is followed by binary operators.

**Workaround**: Extract table values to local variables before using in expressions

**Test Status**: Affects some compiler tests that had to be rewritten to work around this

**Priority**: Medium - Has an easy workaround but affects code readability

## Other Notes

- All escape sequences are now working correctly (decimal, hex, unicode, line continuation)
- Method calls with long strings work properly  
- Shebang lines are supported (first line only)
- The literals.lua test from the official Lua test suite should now pass further
