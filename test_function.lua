-- Test global function definition
function add(a, b)
    return a + b
end

print("add(2, 3) =", add(2, 3))

-- Test table function definition
t = {}
function t.multiply(a, b)
    return a * b
end

print("t.multiply(4, 5) =", t.multiply(4, 5)) 