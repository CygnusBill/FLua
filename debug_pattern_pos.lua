-- Debug what's happening with captures
print("Testing simple nested pattern")
local text = "abcdef"
local pattern1 = "(a(bc)d)ef"
local r1, r2 = string.match(text, pattern1)
print("Pattern 1:", pattern1)
print("Capture 1:", r1, "Capture 2:", r2)

print("\nTesting the actual failing pattern")
local text2 = "abc123def" 
local pattern2 = "(a(bc)(123)d)ef"
local s1, s2, s3 = string.match(text2, pattern2)
print("Pattern 2:", pattern2)
print("Capture 1:", s1, "Capture 2:", s2, "Capture 3:", s3)