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

## Table Assignment at Statement Level (FIXED)

**Issue**: The parser was failing to recognize table assignment statements like `t[1] = 100` at the statement level.

**Status**: FIXED - Table assignments now work correctly.

**Fix Applied**: 
- Created a specialized `pLvalue` parser that parses assignable expressions (variables and table access)
- Modified the parser to stop at `=` or `,` to avoid consuming too much input
- Properly handled whitespace with `notFollowedBy` checks
- Combined assignment and function call parsing to resolve ambiguity

**Examples that now work**:
```lua
t[1] = 100              -- Works: table indexing assignment
t.field = "value"       -- Works: dot notation assignment
t["key"] = true         -- Works: string key assignment
t.a.b[1] = 42          -- Works: nested table assignment
a[1], b[2] = 10, 20    -- Works: multiple table assignment
```

## Table Access in Binary Expressions (FIXED)

**Issue**: The parser had trouble with table access expressions when used directly in binary operations.

**Status**: FIXED - Table access now works correctly in binary expressions.

**Fix Applied**: 
- Added whitespace consumption after closing bracket in bracket access parser
- This ensures consistency with other postfix operators (dot access, function calls)
- The fix was a simple addition of `.>> ws` after `pstring "]"` in the bracket access parser

**Examples that now work**:
```lua
local sum = t[1] + t[2]      -- Works: bracket access in binary expression
local x = t.a + t.b          -- Works: dot notation (already worked)
local calc = t[1] * 2 + t[2] -- Works: complex expressions
t[1]  +  t[2]                -- Works: with various whitespace
```

## Other Notes

- All escape sequences are now working correctly (decimal, hex, unicode, line continuation)
- Method calls with long strings work properly  
- Shebang lines are supported (first line only)
- The literals.lua test from the official Lua test suite should now pass further
