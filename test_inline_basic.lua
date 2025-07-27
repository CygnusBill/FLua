-- Test basic inline functions without closures
local add = function(a, b) return a + b end
print("5 + 3 =", add(5, 3))

local greet = function() return "Hello!" end
print(greet())

local ops = {
    multiply = function(x, y) return x * y end
}
print("10 * 5 =", ops.multiply(10, 5))