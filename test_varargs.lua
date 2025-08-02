-- Test basic varargs functionality
function test_varargs(a, b, ...)
    print("a =", a)
    print("b =", b)
    
    -- Access varargs
    local args = {...}
    print("varargs count =", #args)
    
    for i = 1, #args do
        print("vararg", i, "=", args[i])
    end
    
    return a, b, ...
end

-- Test with different numbers of arguments
print("--- Test 1: More args than params ---")
local r1, r2, r3, r4 = test_varargs(1, 2, 3, 4, 5)
print("returns:", r1, r2, r3, r4)

print("\n--- Test 2: Exact number of args ---")
local r1, r2 = test_varargs(10, 20)
print("returns:", r1, r2)

print("\n--- Test 3: Direct varargs use ---")
function print_all(...)
    print("Got", #{...}, "arguments:", ...)
end

print_all(1, 2, 3)
print_all("a", "b", "c", "d", "e")

print("\n--- Test 4: Varargs in expressions ---")
function sum_all(...)
    local args = {...}
    local sum = 0
    for i = 1, #args do
        sum = sum + args[i]
    end
    return sum
end

print("Sum of 1,2,3,4,5 =", sum_all(1, 2, 3, 4, 5))