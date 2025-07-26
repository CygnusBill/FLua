-- Test console application

print("FLua Console Application Test")
print("=============================")

-- Do some calculations
local x = 42
local y = 8
local result = x + y

print("Calculation: " .. x .. " + " .. y .. " = " .. result)

-- Test local function
local function greet(name)
    return "Hello, " .. name .. "!"
end

local message = greet("World")
print(message)

print("Test completed successfully!")

-- Return success exit code
return 0