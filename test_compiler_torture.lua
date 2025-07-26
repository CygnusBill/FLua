-- Simple torture test for compiler
-- Basic arithmetic and variables
local x = 42
local y = x + 8
print("x =", x)
print("y =", y)

-- Boolean operations
local a = true
local b = false
print("a =", a)
print("b =", b)
print("a and b =", a and b)
print("a or b =", a or b)

-- String operations
local str1 = "Hello"
local str2 = "World"
local combined = str1 .. " " .. str2
print("combined =", combined)

-- Numeric operations
local n1 = 10
local n2 = 3
print("n1 + n2 =", n1 + n2)
print("n1 - n2 =", n1 - n2)
print("n1 * n2 =", n1 * n2)
print("n1 / n2 =", n1 / n2)
print("n1 % n2 =", n1 % n2)

-- Comparison operations
print("n1 > n2 =", n1 > n2)
print("n1 < n2 =", n1 < n2)
print("n1 == n2 =", n1 == n2)
print("n1 ~= n2 =", n1 ~= n2)

print("Torture test completed successfully!")