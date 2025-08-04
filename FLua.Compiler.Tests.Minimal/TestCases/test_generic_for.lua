-- Test generic for loops

-- Test 1: pairs
local t = {a = 1, b = 2, c = 3}
print("Test 1: pairs")
for k, v in pairs(t) do
    print(k, v)
end

-- Test 2: ipairs 
local arr = {10, 20, 30}
print("\nTest 2: ipairs")
for i, v in ipairs(arr) do
    print(i, v)
end

-- Test 3: Custom iterator (skipped - requires anonymous functions)
print("\nTest 3: custom iterator - skipped (requires anonymous functions)")

-- Test 4: Break in generic for
print("\nTest 4: break")
for k, v in pairs({x=10, y=20, z=30}) do
    print(k, v)
    if k == "y" then
        break
    end
end

print("\nDone!")