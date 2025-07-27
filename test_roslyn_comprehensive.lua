-- Comprehensive test for Roslyn code generator

-- Test 1: Local variables and assignments
local x = 10
local y = 20
local z = x + y
print("x + y =", z)

-- Test 2: Binary operations
local a = 5
local b = 3
print("5 + 3 =", a + b)
print("5 - 3 =", a - b)
print("5 * 3 =", a * b)
print("5 / 3 =", a / b)
print("5 % 3 =", a % b)

-- Test 3: String operations
local str1 = "Hello"
local str2 = "World"
local combined = str1 .. " " .. str2
print("Combined string:", combined)

-- Test 4: Boolean operations
local t = true
local f = false
print("true and false =", t and f)
print("true or false =", t or f)

-- Test 5: Comparison operations
print("10 < 20 =", 10 < 20)
print("10 > 20 =", 10 > 20)
print("10 == 10 =", 10 == 10)
print("10 ~= 20 =", 10 ~= 20)

-- Test 6: Local function definition
local function add(a, b)
    return a + b
end

local result = add(100, 200)
print("add(100, 200) =", result)

-- Test 7: Variable shadowing
local shadow = "outer"
do
    local shadow = "inner"
    print("Inside block, shadow =", shadow)
end
print("Outside block, shadow =", shadow)

-- Test 8: Return statement
return 42, "success"