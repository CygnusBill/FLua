-- Test global function definition
function add(a, b)
    return a + b
end

-- Test table function definition
math.custom = {}
function math.custom.multiply(a, b)
    return a * b
end

-- Test method definition with colon syntax
table = {}
function table:concat(a, b)
    return a .. b
end

-- Test the functions
print("add(2, 3) =", add(2, 3))
print("math.custom.multiply(4, 5) =", math.custom.multiply(4, 5))
print("table:concat('hello', 'world') =", table:concat('hello', 'world')) 