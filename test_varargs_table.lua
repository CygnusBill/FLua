-- Test {...} table constructor
function test(...)
    local t = {...}
    return t
end

local result = test(10, 20, 30)
print(result[1], result[2], result[3])