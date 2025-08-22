-- Test optional capture groups
print("=== Testing optional capture groups ===")

-- Test 1: Optional group present
local text1 = "test"
local pattern1 = "te(st)?"
local r1 = string.match(text1, pattern1)
print("Pattern: " .. pattern1 .. ", Text: " .. text1)
print("Expected: st")
print("Got:      " .. (r1 or "nil"))
print()

-- Test 2: Optional group absent
local text2 = "te"
local pattern2 = "te(st)?"
local r2 = string.match(text2, pattern2)
print("Pattern: " .. pattern2 .. ", Text: " .. text2)
print("Expected: (empty string or nil)")
print("Got:      " .. (r2 or "nil"))
print()

-- Test 3: Check for multiple returns
print("=== Checking multi-return behavior ===")
local a1, a2, a3 = string.match("test", "te(st)?")
print("Multiple returns:", a1, a2, a3)