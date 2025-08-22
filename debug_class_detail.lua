-- Debug character class matching
print("=== Debugging character class details ===")

-- Test just the %.
local r1 = string.match(".", "%.")
print("Pattern: %., Text: .")
print("Expected: .")
print("Got:      " .. (r1 or "nil"))
print()

-- Test [%.] 
local r2 = string.match(".", "[%.]")
print("Pattern: [%.], Text: .")
print("Expected: .")
print("Got:      " .. (r2 or "nil"))
print()

-- Test [%w%.]
local r3 = string.match(".", "[%w%.]")
print("Pattern: [%w%.], Text: .")
print("Expected: .")
print("Got:      " .. (r3 or "nil"))
print()

-- Test [%d%.]
local r4 = string.match(".", "[%d%.]")
print("Pattern: [%d%.], Text: .")
print("Expected: .")
print("Got:      " .. (r4 or "nil"))
print()

-- Test the full pattern on components
local r5 = string.match("domain.com", "[%w%.]+")
print("Pattern: [%w%.]+, Text: domain.com")
print("Expected: domain.com")
print("Got:      " .. (r5 or "nil"))
print()

local r6 = string.match("25.5", "[%d%.]+")
print("Pattern: [%d%.]+, Text: 25.5")  
print("Expected: 25.5")
print("Got:      " .. (r6 or "nil"))