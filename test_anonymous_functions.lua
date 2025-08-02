-- Test anonymous functions (NOT YET IMPLEMENTED IN COMPILER)
-- This file documents test cases for when anonymous functions are implemented

-- Test 1: Basic anonymous function
local add = function(a, b)
    return a + b
end
print("Test 1: Basic anonymous function")
print(add(5, 3))  -- Should print 8

-- Test 2: Anonymous function as argument
local function apply(func, x, y)
    return func(x, y)
end
print("\nTest 2: Anonymous function as argument")
print(apply(function(a, b) return a * b end, 4, 5))  -- Should print 20

-- Test 3: Anonymous function in table
local ops = {
    add = function(a, b) return a + b end,
    sub = function(a, b) return a - b end,
    mul = function(a, b) return a * b end
}
print("\nTest 3: Anonymous function in table")
print(ops.add(10, 5))  -- Should print 15
print(ops.sub(10, 5))  -- Should print 5
print(ops.mul(10, 5))  -- Should print 50

-- Test 4: Anonymous function returning multiple values
local multi = function()
    return 1, 2, 3
end
print("\nTest 4: Multiple returns")
local a, b, c = multi()
print(a, b, c)  -- Should print 1 2 3

-- Test 5: Anonymous function with closures
local function makeCounter()
    local count = 0
    return function()
        count = count + 1
        return count
    end
end
print("\nTest 5: Closures")
local counter = makeCounter()
print(counter())  -- Should print 1
print(counter())  -- Should print 2
print(counter())  -- Should print 3

-- Test 6: Anonymous function in generic for iterator
local function makeIterator()
    local i = 0
    return function()
        i = i + 1
        if i <= 3 then
            return i, i * i
        end
    end
end
print("\nTest 6: Iterator with anonymous function")
for i, square in makeIterator() do
    print(i, square)  -- Should print 1 1, 2 4, 3 9
end