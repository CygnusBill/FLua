-- Test local function definitions
print("Testing local function definitions")

-- Simple function
local function add(a, b)
    return a + b
end

local result = add(10, 20)
print("add(10, 20) =", result)

-- Function with multiple returns
local function divmod(a, b)
    return a / b, a % b
end

local div, mod = divmod(17, 5)
print("divmod(17, 5) =", div, mod)

-- Function accessing outer variables
local x = 100
local function addX(y)
    return x + y
end

print("addX(50) =", addX(50))

-- Nested functions
local function outer(a)
    local function inner(b)
        return a + b
    end
    return inner(10)
end

print("outer(5) =", outer(5))

print("Local function test completed!")