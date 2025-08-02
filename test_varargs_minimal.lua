-- Minimal varargs test
function test(a, ...)
    print("a =", a)
    return a
end

test(1, 2, 3)