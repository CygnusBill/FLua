-- Test to diagnose the issue
local i = 0
print("Initial i =", i)

-- First comparison
local result1 = i < 3
print("i < 3 =", result1)

-- Increment
i = i + 1
print("After increment, i =", i)

-- Second comparison
local result2 = i < 3
print("i < 3 =", result2)

-- Direct comparison without variables
print("1 < 3 =", 1 < 3)
print("3 < 3 =", 3 < 3)
print("4 < 3 =", 4 < 3)