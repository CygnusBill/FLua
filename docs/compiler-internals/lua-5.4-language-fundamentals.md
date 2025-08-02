# Lua 5.4 Language Fundamentals

## Overview
This document captures key concepts from Lua 5.4 that are essential for implementing the FLua compiler.

## Core Language Concepts

### 1. Dynamic Typing
- **Variables don't have types; values do**
- All values carry their own type information
- No type definitions in the language itself
- Types: nil, boolean, number, string, function, userdata, thread, table

### 2. Values and Types

#### Basic Types
- **nil**: Represents absence of value, distinct from all others
- **boolean**: true/false (only nil and false are falsy)
- **number**: Double-precision floating point by default, also integers
- **string**: Immutable byte sequences, can contain any 8-bit value including embedded zeros
- **function**: First-class values (C functions or Lua functions)
- **userdata**: Arbitrary C data in Lua
- **thread**: Independent execution threads (coroutines)
- **table**: Associative arrays, only data structuring mechanism

#### Type Checking (C API)
```c
lua_isboolean(L, index)    // Check if boolean
lua_isfunction(L, index)   // Check if function (C or Lua)
lua_isinteger(L, index)    // Check if integer representation
lua_isnil(L, index)        // Check if nil
lua_isnumber(L, index)     // Check if number or convertible
lua_isstring(L, index)     // Check if string or convertible
lua_istable(L, index)      // Check if table
lua_type(L, index)         // Get type code
```

### 3. Metatables and Metamethods

Metatables define behavior of tables through metamethods:
- Control arithmetic operations
- Define comparison behavior
- Handle table access/modification
- Customize string representation
- Manage garbage collection

Key Functions:
- `setmetatable(table, metatable)` - Set metatable
- `getmetatable(object)` - Get metatable
- `rawget/rawset` - Bypass metamethods

### 4. Environments and Scoping

#### Lexical Scoping
```lua
x = 10                -- global variable
do                    -- new block
  local x = x         -- new 'x', with value 10
  print(x)            --> 10
  x = x+1
  do                  -- another block
    local x = x+1     -- another 'x'
    print(x)          --> 12
  end
  print(x)            --> 11
end
print(x)              --> 10  (the global one)
```

#### Environment Chain
- Each function has an environment
- Local variables shadow outer scopes
- Global environment accessed through _G

### 5. Functions and Closures

#### Closures and Upvalues
```lua
local x = 20
for i = 1, 10 do
  local y = 0
  a[i] = function () y = y + 1; return x + y end
end
```
- Each closure gets its own 'y' (loop variable)
- All share 'x' from outer scope
- Upvalues persist beyond scope lifetime

#### C Closures
- Associate values (upvalues) with C functions
- Access via `lua_upvalueindex(n)`
- Create with `lua_pushcclosure(L, f, n)`

### 6. Control Structures

#### Syntax Forms
```lua
-- if statement
if exp then block {elseif exp then block} [else block] end

-- while loop  
while exp do block end

-- repeat-until
repeat block until exp

-- numeric for
for Name = exp, exp [, exp] do block end

-- generic for
for namelist in explist do block end

-- break statement
break  -- terminates innermost loop
```

### 7. Multiple Returns and Assignment

- Functions can return multiple values
- Multiple assignment: `local a, b, c = f()`
- Extra values discarded, missing values become nil
- Parentheses force single result: `return (f())`

### 8. Tables

- Only data structuring mechanism
- Associative arrays with any value as key (except nil)
- Array part optimized for integer keys
- Table constructors: `{1, 2, 3}` or `{x=1, y=2}`
- Method syntax: `table:method()` equals `table.method(table)`

## Compiler Implementation Notes

### Key Considerations

1. **Type System**: Since Lua is dynamically typed, the compiler must:
   - Generate type checks at runtime
   - Use tagged unions or similar for value representation
   - Handle automatic conversions (string/number coercion)

2. **Closures**: Must implement:
   - Upvalue capture and storage
   - Environment chain management
   - Proper lifetime management

3. **Tables**: Need efficient implementation for:
   - Mixed array/hash storage
   - Metamethod dispatch
   - Weak references (when implemented)

4. **Control Flow**: 
   - Break only exits loops (not blocks)
   - Return can appear anywhere in block
   - Goto support (Lua 5.4)

5. **Multiple Values**:
   - Function calls can produce variable results
   - Assignment adjusts value count
   - Special handling for varargs

### Error Handling

- Runtime errors propagate up call stack
- Protected calls with pcall/xpcall
- Error objects can be any Lua value
- Stack traceback available through debug library

### Standard Libraries

Core libraries that must be available:
- Basic functions (print, type, tostring, etc.)
- String manipulation with pattern matching
- Table operations
- Math functions
- I/O operations
- OS interface
- Debug facilities