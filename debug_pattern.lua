-- Test pattern matching
local text = "abc123def"
local pattern = "(a(bc)(123)d)ef"
local result = string.match(text, pattern)
print("Result:", result)

-- Get all captures
local captures = {string.match(text, pattern)}
for i, capture in ipairs(captures) do
    print("Capture " .. i .. ": '" .. tostring(capture) .. "'")
end