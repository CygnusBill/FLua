-- Test method calls with colon syntax

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

-- Test 2: String methods (skipped - interpreter doesn't support string methods yet)
print("\nTest 2: String methods - skipped (interpreter limitation)")

-- Test 3: Method calls in expressions
print("\nTest 3: Method calls in expressions")
local result = obj:getValue() + obj:add(10)
print("getValue() + add(10):", result)  -- Should print 50 (20 + 30)

-- Test 4: Chained method calls
local chainObj = {
    x = 5,
    double = function(self)
        self.x = self.x * 2
        return self
    end,
    add = function(self, n)
        self.x = self.x + n
        return self
    end,
    get = function(self)
        return self.x
    end
}

print("\nTest 4: Chained method calls")
local chained = chainObj:double():add(3):get()
print("double():add(3):get():", chained)  -- Should print 13 (5*2 + 3)

print("\nDone!")