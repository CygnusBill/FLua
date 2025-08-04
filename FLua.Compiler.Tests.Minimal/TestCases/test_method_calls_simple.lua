-- Simple method call test
local obj = {
    value = 10,
    getValue = function(self)
        return self.value
    end
}

print("Test: Simple method call")
print("obj:getValue() =", obj:getValue())