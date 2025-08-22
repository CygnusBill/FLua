-- Debug quantifier behavior in detail
print("=== Testing quantifier behavior ===")

-- Test ? on regular character
local r1 = string.match("te", "tes?")
print("Pattern: tes?, Text: te")
print("Expected: te")
print("Got:      " .. (r1 or "nil"))
print()

-- Test ? on regular character (with optional present)
local r2 = string.match("test", "tes?t")  
print("Pattern: tes?t, Text: test")
print("Expected: test")
print("Got:      " .. (r2 or "nil"))
print()

-- Test ? on capture group
local r3 = string.match("te", "te(s)?")
print("Pattern: te(s)?, Text: te")
print("Expected match with empty capture")
print("Got:      " .. (r3 or "nil"))
print()

-- Test ? on capture group (with optional present)
local r4 = string.match("tes", "te(s)?")
print("Pattern: te(s)?, Text: tes")
print("Expected: s")
print("Got:      " .. (r4 or "nil"))