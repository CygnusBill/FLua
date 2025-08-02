-- Test generic for loops with anonymous functions
-- This requires both generic for AND anonymous functions to be implemented

-- Test 1: Custom iterator with anonymous function
local function multi_iter()
    return function(state, key)
        if key == nil then
            return 1, "one"
        elseif key == 1 then
            return 2, "two"
        else
            return nil
        end
    end, "state", nil
end

print("Test 1: custom iterator with anonymous function")
for k, v in multi_iter() do
    print(k, v)
end

-- Test 2: Simple stateless iterator
local function count_to(n)
    return function(max, current)
        if current < max then
            return current + 1
        end
    end, n, 0
end

print("\nTest 2: count iterator")
for i in count_to(5) do
    print(i)  -- Should print 1, 2, 3, 4, 5
end

-- Test 3: Stateful iterator with closure
local function fibonacci(n)
    local a, b = 0, 1
    local count = 0
    return function()
        if count < n then
            count = count + 1
            a, b = b, a + b
            return count, a
        end
    end
end

print("\nTest 3: fibonacci iterator")
for i, fib in fibonacci(7) do
    print(i, fib)  -- Should print fibonacci numbers
end