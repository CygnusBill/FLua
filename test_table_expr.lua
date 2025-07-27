-- Test table access in expressions
local t = {10, 20, 30}
local a = {x = 5}
local b = {x = 15}

-- This should work but currently fails
local sum = t[1] + t[2]
print("Sum of t[1] + t[2]:", sum)

-- Also test with dot notation
local result = a.x + b.x
print("Sum of a.x + b.x:", result)

-- Test more complex expressions
local complex = t[1] * 2 + t[2] / 2
print("Complex expression:", complex)

-- Test nested table access
local nested = {inner = {value = 100}}
local calc = nested.inner.value + 50
print("Nested calculation:", calc)