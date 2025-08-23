-- Test what our pattern system actually supports

-- Test basic capture
print("Basic capture:")
local result = string.match("test", "(st)")  
print("(st) on 'test':", result)

-- Test capture with context
print("Capture with context:")
result = string.match("test", "te(st)")
print("te(st) on 'test':", result)

-- Test optional character
print("Optional character:")
result = string.match("test", "tes?t") 
print("tes?t on 'test':", result)

-- Test optional character that doesn't match
result = string.match("tet", "tes?t")
print("tes?t on 'tet':", result)

-- Test the failing case: optional capture group
print("Optional capture group (should fail):")
result = string.match("test", "te(st)?")
print("te(st)? on 'test':", result)

result = string.match("te", "te(st)?")
print("te(st)? on 'te':", result)