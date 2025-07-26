# FLua Test Suite Status Report

## Summary
This report documents the status of running the official Lua 5.4 test suite against FLua, identifying parser and runtime issues that need to be addressed.

## Fixed Issues

### 1. Decimal Numbers Starting with Dot
- **Issue**: Parser didn't support `.5` syntax (only `0.5`)
- **Fix**: Added `decFloatNoInt` parser variant
- **Status**: ✅ Fixed in commit 68bfb45

### 2. Long String Initial Newline Removal
- **Issue**: Long strings `[[...]]` didn't remove initial newline as per Lua spec
- **Fix**: Added `skipInitialNewline` logic to both long string parsers
- **Status**: ✅ Fixed in commit 68bfb45

### 3. Generic For Loop Multiple Return Values
- **Issue**: Generic for loop expected exactly 3 values, but `pairs()` returns expandable values
- **Fix**: Modified interpreter to expand last expression in iterator list
- **Status**: ✅ Fixed in commit 68bfb45

### 4. Scientific Notation Support
- **Issue**: Parser didn't support scientific notation (1E5, 1e-5, 3.14E+2)
- **Fix**: Added exponent parsing to decimal number parser
- **Status**: ✅ Fixed in commit 17d738b

### 5. Do Block Parsing After Integer Overflow
- **Issue**: Large hex literals (0x7fffffffffffffff) caused parser failures in subsequent code
- **Fix**: Root cause identified - integer overflow during parsing
- **Status**: ✅ Issue understood, workaround available

## Known Limitations

### 1. Function Call Syntax with Table Constructors
- **Issue**: `f{...}` syntax fails in generic for loops and if conditions
- **Examples**:
  ```lua
  for k in pairs{1,2,3} do end  -- Fails
  if f{} then end                -- Fails
  ```
- **Workaround**: Use parentheses: `pairs({1,2,3})`
- **Status**: 📝 Documented in PARSER_KNOWN_ISSUES.md

### 2. Underscore as Variable Name
- **Issue**: Single underscore `_` not recognized as valid identifier
- **Impact**: Common idiom `for _, v in pairs(t)` fails
- **Workaround**: Use different variable name
- **Status**: 🔧 Needs parser fix

## Pending Issues

### 1. Integer Overflow with Large Hex Literals
- **Issue**: `0x7fffffffffffffff` causes "Value was either too large or too small for an Int64"
- **Impact**: Prevents parsing of files with 64-bit integer constants
- **Files Affected**: strings.lua, likely others

### 2. Missing load() Function
- **Issue**: Dynamic code loading not implemented
- **Impact**: Many tests in literals.lua and other files rely on load()
- **Priority**: High - blocks many test cases

### 3. Bitwise Operation Limitations
- **Issue**: "Shift count too large" error for operations like `~(-1 << 64)`
- **Impact**: Bitwise tests fail

### 4. string.format() Implementation
- **Issue**: Format specifiers (%d, %x, %a, etc.) not implemented
- **Impact**: String formatting tests fail

### 5. Module System
- **Issue**: `require()` and module loading not implemented
- **Impact**: Cannot test files that depend on other modules (e.g., bitwise.lua needs bwcoercion)

## Test File Status

| Test File | Status | Main Issues |
|-----------|--------|-------------|
| literals.lua | ⚠️ Partial | Needs load(), some syntax issues fixed |
| strings.lua | ⚠️ Partial | Integer overflow, string.format() |
| math.lua | ⚠️ Partial | Scientific notation fixed, other issues remain |
| bitwise.lua | ❌ Failed | Missing module system |
| Other files | 🔍 Not tested | Pending investigation |

## Recommendations

1. **High Priority**:
   - Implement load() function for dynamic code evaluation
   - Fix integer overflow for large constants
   - Add underscore identifier support

2. **Medium Priority**:
   - Implement string.format() with format specifiers
   - Fix bitwise operation edge cases
   - Add basic module/require support

3. **Low Priority**:
   - Fix f{} syntax in all contexts (complex parser issue)

## Next Steps

Continue testing remaining Lua test files and update this report with findings. Focus on implementing high-priority missing features that block the most tests.