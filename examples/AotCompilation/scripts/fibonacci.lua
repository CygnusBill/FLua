
-- Fibonacci calculator as a standalone program
local function fibonacci(n)
    if n <= 1 then
        return n
    end
    local a, b = 0, 1
    for i = 2, n do
        a, b = b, a + b
    end
    return b
end

-- Get command line argument or use default
local n = tonumber(arg and arg[1]) or 10

print(string.format('Fibonacci sequence up to position %d:', n))
for i = 0, n do
    print(string.format('F(%d) = %d', i, fibonacci(i)))
end

print('\nDone!')
return 0  -- Exit code
