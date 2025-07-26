-- Test to-be-closed variables
print("=== Testing to-be-closed variables ===")

-- Create a simple object with __close metamethod
local function createResource(name)
    local resource = {name = name}
    local mt = {
        __close = function(self, err)
            print("Closing resource:", self.name)
            if err then
                print("Error during close:", err)
            end
        end
    }
    setmetatable(resource, mt)
    return resource
end

-- Test basic close variable
do
    local file <close> = createResource("file1")
    print("Using resource:", file.name)
    -- file should be closed when exiting this scope
end
print("After first block")

-- Test multiple close variables (should close in reverse order)
do
    local res1 <close> = createResource("resource1")
    local res2 <close> = createResource("resource2")
    local res3 <close> = createResource("resource3")
    print("Using all resources")
    -- Should close in order: resource3, resource2, resource1
end
print("After second block")

-- Test close variable with early return
local function testEarlyReturn()
    local early <close> = createResource("early")
    print("Before early return")
    return "returned early"
    -- early should still be closed
end

print("Return value:", testEarlyReturn())
print("After early return test")

print("to-be-closed variable tests completed successfully!")
