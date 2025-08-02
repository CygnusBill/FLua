-- Simple varargs test
function test(...)
    local args = {...}
    print("Count:", #args)
    return ...
end

test(1, 2, 3)