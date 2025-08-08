# Closure and Variable Capture in Compiled Lua

## Overview

One of the most significant limitations when compiling Lua to static .NET code is the handling of closures and variable capture. This document explains why this limitation exists and what patterns work or don't work in compiled mode.

## The Challenge

Lua is a dynamic language with first-class functions and lexical scoping. Functions can capture variables from their enclosing scope, creating closures:

```lua
local counter = 0
local function increment()
    counter = counter + 1  -- Captures 'counter' from outer scope
    return counter
end
```

When this code runs in the interpreter, the function `increment` maintains a reference to the variable `counter`, which persists even after the scope that created it has exited.

## Why Compilation is Difficult

### 1. Static vs Dynamic Binding

In compiled .NET code, variable locations are determined at compile time:
- Local variables become stack slots or registers
- They have fixed lifetimes tied to their scope
- Once a method returns, its local variables are gone

In Lua's interpreter:
- Variables can be "closed over" and promoted to heap storage
- Closures maintain references to these heap-allocated variables
- Multiple closures can share the same captured variable

### 2. The Upvalue Problem

```lua
local function makeCounter()
    local count = 0
    return function()
        count = count + 1  -- 'count' is an upvalue
        return count
    end
end

local c1 = makeCounter()
local c2 = makeCounter()
print(c1())  -- 1
print(c1())  -- 2
print(c2())  -- 1 (different closure, different upvalue)
```

Each closure needs its own copy of the captured environment. In compiled code, this would require:
- Heap-allocating closure environments
- Generating wrapper classes for each closure
- Complex lifetime management

### 3. Shared Mutable State

```lua
local shared = 0
local functions = {}
for i = 1, 3 do
    functions[i] = function()
        shared = shared + i  -- All closures share 'shared'
        return shared
    end
end
```

All three functions share the same `shared` variable. In compiled code, this requires:
- Reference semantics for captured variables
- Potentially boxing primitive types
- Synchronization for thread safety

## Current Compilation Behavior

### What Works

1. **Functions without captures** compile correctly:
```lua
local function add(a, b)
    return a + b
end
```

2. **Read-only access to globals** works:
```lua
function calculate()
    return math.pi * 2
end
```

3. **Local variables within function scope** work:
```lua
function process()
    local temp = 10
    temp = temp * 2
    return temp
end
```

### What Doesn't Work

1. **Capturing local variables** fails or produces incorrect results:
```lua
local multiplier = 2
function scale(x)
    return x * multiplier  -- Captures 'multiplier'
end
```

2. **Nested function definitions** with captures:
```lua
function outer(x)
    local function inner(y)
        return x + y  -- Captures 'x' from outer
    end
    return inner
end
```

3. **Module-level closures**:
```lua
-- In a module
local privateCounter = 0
function M.increment()
    privateCounter = privateCounter + 1  -- Captures module-local variable
    return privateCounter
end
```

## The Module Compilation Issue

This is particularly problematic for modules because Lua modules often use the module pattern with private state:

```lua
-- counter.lua
local counter = 0
local M = {}

function M.increment()
    counter = counter + 1  -- This closure captures 'counter'
    return counter
end

function M.reset()
    counter = 0  -- Also captures 'counter'
end

return M
```

When compiled:
- The local variable `counter` exists only during module initialization
- The compiled functions lose their reference to `counter`
- Calls to `M.increment()` fail because `counter` is nil or inaccessible

## Workarounds

### 1. Use Global or Module State

Instead of local variables, use table fields:

```lua
local M = {
    counter = 0  -- Store in the module table
}

function M.increment()
    M.counter = M.counter + 1  -- Access via table
    return M.counter
end
```

### 2. Pass State Explicitly

Avoid closures by passing state as parameters:

```lua
function increment(state)
    state.counter = state.counter + 1
    return state.counter
end

local myState = {counter = 0}
print(increment(myState))
```

### 3. Use the Interpreter for Closure-Heavy Code

Some code is better suited for interpretation:
- Configuration DSLs
- Event handlers with captured state
- Factory functions that create closures

### 4. Compile Only Pure Functions

Focus compilation on performance-critical, pure functions:
- Mathematical calculations
- Data transformations
- Stateless business logic

## Future Possibilities

### Potential Solutions

1. **Closure Classes**: Generate a class for each closure with fields for captured variables
2. **Environment Objects**: Pass an environment object containing all captured variables
3. **Hybrid Execution**: Compile simple functions, interpret complex closures
4. **Static Analysis**: Detect and reject non-compilable patterns at compile time

### Why Not Implemented Yet

- **Complexity**: Proper closure compilation is complex to implement correctly
- **Performance**: The overhead might negate compilation benefits
- **Size**: Generated code for closures can be much larger
- **Debugging**: Makes debugging compiled code much harder

## Best Practices

1. **Know Your Code**: Understand which parts use closures
2. **Design for Compilation**: Structure code to minimize closure use in hot paths
3. **Profile First**: Measure whether compilation actually helps
4. **Document Limitations**: Make it clear which modules can be compiled
5. **Test Both Modes**: Ensure code works both interpreted and compiled

## Examples

### Example 1: Factory Pattern (Doesn't Compile)

```lua
-- This won't work when compiled
function makeMultiplier(factor)
    return function(x)
        return x * factor  -- Captures 'factor'
    end
end
```

### Example 2: Refactored for Compilation

```lua
-- This works when compiled
local Multiplier = {}
Multiplier.__index = Multiplier

function Multiplier:new(factor)
    return setmetatable({factor = factor}, self)
end

function Multiplier:multiply(x)
    return x * self.factor  -- Accesses via self
end
```

### Example 3: Module Pattern (Problematic)

```lua
-- Original (won't compile correctly)
local count = 0
local M = {}
function M.get() return count end
function M.inc() count = count + 1 end
return M
```

### Example 4: Module Pattern (Compilable)

```lua
-- Refactored for compilation
local M = {_count = 0}
function M.get() return M._count end
function M.inc() M._count = M._count + 1 end
return M
```

## Conclusion

Closure compilation is one of the most challenging aspects of compiling dynamic languages to static code. While FLua's compiler can handle many Lua patterns, code that relies heavily on closures and variable capture should either:

1. Be refactored to avoid closures
2. Use the interpreter for correct execution
3. Wait for future compiler improvements

Understanding these limitations helps developers write Lua code that can benefit from compilation while maintaining correctness.