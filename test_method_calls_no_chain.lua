-- Test method calls without chaining

-- Test 1: Table method calls
local obj = {
    value = 10,
    getValue = function(self)
        return self.value
    end,
    setValue = function(self, v)
        self.value = v
    end,
    add = function(self, x)
        return self.value + x
    end
}

print("Test 1: Table methods")
print("getValue:", obj:getValue())  -- Should print 10
obj:setValue(20)
print("After setValue(20):", obj:getValue())  -- Should print 20
print("add(5):", obj:add(5))  -- Should print 25

-- Test 2: Method calls in expressions
print("\nTest 2: Method calls in expressions")
local result = obj:getValue() + obj:add(10)
print("getValue() + add(10):", result)  -- Should print 50 (20 + 30)

print("\nDone!")