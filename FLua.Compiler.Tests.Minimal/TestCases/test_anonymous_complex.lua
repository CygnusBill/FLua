-- Complex anonymous function tests

-- Test 1: Anonymous function in table
print("Test 1: Anonymous function in table")
local obj = {
    value = 10,
    getValue = function(self)
        return self.value
    end,
    setValue = function(self, v)
        self.value = v
    end
}
print("obj:getValue() =", obj:getValue())
obj:setValue(20)
print("After setValue(20):", obj:getValue())

-- Test 2: Anonymous function as argument
print("\nTest 2: Anonymous function as argument")
local function apply(func, x, y)
    return func(x, y)
end
local result = apply(function(a, b) return a * b end, 4, 5)
print("apply(multiply, 4, 5) =", result)

-- Test 3: Multiple anonymous functions
print("\nTest 3: Multiple anonymous functions")
local ops = {
    add = function(a, b) return a + b end,
    sub = function(a, b) return a - b end,
    mul = function(a, b) return a * b end
}
print("ops.add(10, 5) =", ops.add(10, 5))
print("ops.sub(10, 5) =", ops.sub(10, 5))
print("ops.mul(10, 5) =", ops.mul(10, 5))

-- Test 4: Returning anonymous function
print("\nTest 4: Returning anonymous function")
local function makeAdder(n)
    return function(x)
        return x + n
    end
end
local add5 = makeAdder(5)
print("add5(10) =", add5(10))

print("\nDone!")