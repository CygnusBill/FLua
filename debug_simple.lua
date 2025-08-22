-- Simple nested capture test
local text = "abc123def"
local pattern = "(a(bc)(123)d)ef"

print("Pattern:", pattern)
print("Text:", text)

-- This should capture: abc123d, bc, 123
local result1, result2, result3 = string.match(text, pattern)
print("Capture 1:", result1)
print("Capture 2:", result2)
print("Capture 3:", result3)