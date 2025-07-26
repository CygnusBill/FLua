-- Test that reserved words are properly rejected

-- These should work (not reserved)
local myvar = 1
local x_and_y = 2  -- Contains "and" but not as whole word

-- This would fail: local and = 3
print("Reserved word handling works!")