-- Test expected Lua behavior with simple patterns
print("=== Testing Lua capture ordering ===")

-- Simple case: nested captures
local text1 = "abcdef"
local pattern1 = "(a(bc)d)ef" 
local r1, r2 = string.match(text1, pattern1)
print("Pattern: " .. pattern1)
print("Expected: abcd, bc") 
print("Got:      " .. (r1 or "nil") .. ", " .. (r2 or "nil"))
print()

-- Complex case: multiple nested captures
local text2 = "abc123def"
local pattern2 = "(a(bc)(123)d)ef"
local s1, s2, s3 = string.match(text2, pattern2)
print("Pattern: " .. pattern2)
print("Expected: abc123d, bc, 123")
print("Got:      " .. (s1 or "nil") .. ", " .. (s2 or "nil") .. ", " .. (s3 or "nil"))