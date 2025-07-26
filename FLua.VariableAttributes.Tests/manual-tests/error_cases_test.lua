-- Test error cases for const and close variables
print("=== Testing error cases ===")

-- Test 1: Basic const modification error
print("Test 1: Const modification error")
local function testConstError()
    local x <const> = 42
    print("x =", x)
    
    -- This should throw an error
    local success, err = pcall(function()
        x = 50  -- This should fail
    end)
    
    if not success then
        print("Expected error caught:", err)
    else
        print("ERROR: Should have thrown an error!")
    end
end

testConstError()

-- Test 2: Const in assignment chain
print("\nTest 2: Const in multiple assignment")
local function testConstInChain()
    local a <const>, b = 10, 20
    print("a =", a, "b =", b)
    
    -- This should work
    b = 30
    print("b after modification =", b)
    
    -- This should fail
    local success, err = pcall(function()
        a = 15  -- This should fail
    end)
    
    if not success then
        print("Expected error caught:", err)
    else
        print("ERROR: Should have thrown an error!")
    end
end

testConstInChain()

-- Test 3: Close variable cleanup on error
print("\nTest 3: Close variable cleanup on error")
local function createTrackingResource(name)
    local resource = {name = name}
    local mt = {
        __close = function(self, err)
            print("Cleaning up resource:", self.name)
            if err then
                print("Cleanup due to error:", tostring(err))
            end
        end
    }
    setmetatable(resource, mt)
    return resource
end

local function testCloseOnError()
    local resource <close> = createTrackingResource("errorTest")
    print("Created resource:", resource.name)
    
    -- Cause an error
    error("Intentional error for testing")
end

local success, err = pcall(testCloseOnError)
if not success then
    print("Error occurred:", err)
end

print("\nError case tests completed!")
