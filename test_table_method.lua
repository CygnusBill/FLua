-- Test custom table method
local obj = {
    value = 42,
    getValue = function(self)
        return self.value
    end
}
local result = obj:getValue()

-- Test method returning multiple values
local multi = {
    getTwo = function(self)
        return 10, 20
    end
}
local x, y = multi:getTwo()