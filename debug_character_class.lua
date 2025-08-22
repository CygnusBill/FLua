-- Test character classes 
print("=== Testing character classes ===")

-- Test character class with +
local text1 = "Contact: user@domain.com for help"
local pattern1 = "[%w%.]+@[%w%.]+"
local r1 = string.match(text1, pattern1)
print("Pattern: " .. pattern1)
print("Expected: user@domain.com")
print("Got:      " .. (r1 or "nil"))
print()

-- Test digit pattern
local text2 = "Temperature: 25.5 degrees"
local pattern2 = "([%d%.]+)"
local r2 = string.match(text2, pattern2)
print("Pattern: " .. pattern2)
print("Expected: 25.5")
print("Got:      " .. (r2 or "nil"))