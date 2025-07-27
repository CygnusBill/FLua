-- Test inline function expressions
local add = function(a, b) return a + b end
print(add(5, 3))  -- Should print 8

-- Function as argument
local function apply(f, x, y)
    return f(x, y)
end

local result = apply(function(a, b) return a * b end, 4, 5)
print(result)  -- Should print 20

-- Function in table
local ops = {
    add = function(a, b) return a + b end,
    mul = function(a, b) return a * b end
}
print(ops.add(10, 20))  -- Should print 30
print(ops.mul(10, 20))  -- Should print 200

-- Function returning function
local function makeAdder(n)
    return function(x) return x + n end
end
local add5 = makeAdder(5)
print(add5(10))  -- Should print 15