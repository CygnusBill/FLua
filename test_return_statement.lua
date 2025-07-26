-- Test return statements
print("Testing return statements")

-- Function with single return
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

-- Function with no return
local function doNothing()
    print("This function returns nothing")
end

local nothing = doNothing()
print("doNothing() returned:", nothing)

-- Function with conditional return
local function abs(x)
    if x < 0 then
        return -x
    else
        return x
    end
end

print("abs(-5) =", abs(-5))
print("abs(5) =", abs(5))

print("Return statement test completed!")