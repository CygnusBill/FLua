-- Test top-level varargs (should work with interpreter, needs fix for lambda compilation)
local x, y, z = ...
print("Got arguments:", x, y, z)
return x + y + z