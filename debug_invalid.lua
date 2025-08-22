-- Test invalid pattern
print("=== Testing invalid pattern ===")

-- Test unmatched bracket
local success, result = pcall(function()
    return string.match("hello", "[invalid")
end)

print("Pattern: [invalid, Text: hello")
print("Expected: error")
print("Success:", success)
print("Result:", result)