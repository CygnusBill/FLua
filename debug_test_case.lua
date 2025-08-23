-- Exactly replicate the failing test case
print("Testing exactly what the unit test does:")

-- Test case 1: te(st)? on "test" should return "st"
local result = string.match("test", "te(st)?")
print("test, te(st)?:", result, "(expected: st)")

-- Test case 2: te(st)? on "te" should return ""
local result2 = string.match("te", "te(st)?")
print("te, te(st)?:", result2, "(expected: empty string)")