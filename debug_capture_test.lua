-- Debug test for capture groups
local str = "hello world"
local pattern = "h(ell)o"

print("Testing string.match with capture group")
print("String: '" .. str .. "'")
print("Pattern: '" .. pattern .. "'")

local results = {string.match(str, pattern)}
print("Number of results: " .. #results)

for i, result in ipairs(results) do
    print("Result " .. i .. ": '" .. result .. "'")
end