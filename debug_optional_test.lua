-- Test optional capture group pattern matching
local text = "test"
local pattern = "te(st)?"

-- Test 1: Should match "test" and capture "st" 
local result = string.match(text, pattern)
print("Test 1:")
print("Text:", text)
print("Pattern:", pattern)
print("Result:", result)
print("Expected: st")
print()

-- Test 2: Test with "te" input
local text2 = "te"
local result2 = string.match(text2, pattern)
print("Test 2:")
print("Text:", text2)
print("Pattern:", pattern)
print("Result:", result2)
print("Expected: empty string")