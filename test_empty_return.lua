-- Test empty return
local function test()
    print("Before return")
    return
    print("After return - should not print")
end

test()
print("Done")