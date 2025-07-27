-- Test 1: Simple inline function
local add = function(a, b) return a + b end
print("Test 1: 5 + 3 =", add(5, 3))

-- Test 2: Function with no parameters
local greet = function() return "Hello from inline function!" end
print("Test 2:", greet())

-- Test 3: Function as table value
local ops = {
    multiply = function(x, y) return x * y end,
    divide = function(x, y) return x / y end
}
print("Test 3: 10 * 5 =", ops.multiply(10, 5))
print("Test 3: 20 / 4 =", ops.divide(20, 4))

-- Test 4: Function passed as argument
local function apply(f, a, b)
    return f(a, b)
end

local result = apply(function(x, y) return x - y end, 10, 3)
print("Test 4: 10 - 3 =", result)

-- Test 5: Nested functions
local outer = function(x)
    return function(y) return x + y end
end
local addFive = outer(5)
print("Test 5: 5 + 7 =", addFive(7))

print("All inline function tests completed!")