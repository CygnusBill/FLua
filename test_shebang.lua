#!/usr/bin/env lua
-- Test shebang support

print("If this prints, shebang lines are supported!")

-- The shebang should be ignored by the parser
-- It's only meaningful to the shell

local x = 42
print("x =", x)

-- Make sure regular comments still work
-- And that the shebang is only valid on line 1