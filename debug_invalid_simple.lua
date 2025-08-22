-- Test invalid pattern directly  
print("=== Testing invalid pattern directly ===")

local result = string.match("hello", "[invalid")
print("Result:", result)