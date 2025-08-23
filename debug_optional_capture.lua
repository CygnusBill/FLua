-- Debug script to test optional capture groups
local result = string.match("test", "te(st)?")
print("Result:", result)

local result2 = string.match("te", "te(st)?")  
print("Result2:", result2)