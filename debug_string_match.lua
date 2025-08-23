-- Test optional capture group behavior

-- Test 1: Should capture "st" from "test" with pattern "te(st)?"
local result1 = string.match("test", "te(st)?")
print("Test 1:")
print("Pattern: te(st)?")
print("Text: test")
print("Result:", result1)
print("Expected: st")
print()

-- Test 2: Should return empty string for optional group that doesn't match
local result2 = string.match("te", "te(st)?")
print("Test 2:")
print("Pattern: te(st)?")
print("Text: te")
print("Result:", result2)
print("Expected: empty string")
print()

-- Test 3: Let's understand what happens with no capture groups
local result3 = string.match("test", "te.*")
print("Test 3:")
print("Pattern: te.*")
print("Text: test")
print("Result:", result3)
print("Expected: test")
print()

-- Test 4: What about a capturing group that matches?
local result4 = string.match("test", "(te.*)")
print("Test 4:")
print("Pattern: (te.*)")
print("Text: test")
print("Result:", result4)
print("Expected: test")
print()