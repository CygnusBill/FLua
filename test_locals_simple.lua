-- Testing local variables (simplified from locals.lua)
print('testing local variables')

-- Simple local variable test
local function f(x) 
    x = nil
    return x 
end

local result1 = f(10)
print("f(10) result:", result1)

-- Local variable scoping
do
  local i = 10
  print("outer i:", i)
  do 
    local i = 100
    print("inner i:", i)
  end
  print("outer i again:", i)
end

-- Multiple local assignments
local a, b = 1, 2
print("a =", a, "b =", b)

local x, y, z = 10, 20
print("x =", x, "y =", y, "z =", z)

print("Local variables test completed!")