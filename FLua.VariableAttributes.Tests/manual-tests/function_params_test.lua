-- Test function parameters with attributes
print("=== Testing function parameters with attributes ===")

-- Function with const parameter
local function testConstParam(x <const>)
    print("Received const parameter:", x)
    -- This should fail if we try to modify x
    -- x = x + 1  -- Uncomment to test error
    return x * 2
end

print("testConstParam(5) =", testConstParam(5))

-- Function with close parameter
local function createCloseableResource(name)
    local resource = {name = name}
    local mt = {
        __close = function(self, err)
            print("Resource closed:", self.name)
        end
    }
    setmetatable(resource, mt)
    return resource
end

local function testCloseParam(resource <close>)
    print("Using resource in function:", resource.name)
    -- resource should be closed when function exits
end

local myResource = createCloseableResource("myResource")
testCloseParam(myResource)
print("After function call")

-- Function with mixed parameter attributes
local function mixedParams(a <const>, b, c <close>)
    print("const a:", a)
    print("regular b:", b)
    print("close c:", c.name)
    
    -- Can modify b
    b = b + 10
    print("modified b:", b)
    
    -- Cannot modify a (uncomment to test)
    -- a = a + 1
    
    return b
end

local resource2 = createCloseableResource("resource2")
print("mixedParams result:", mixedParams(100, 200, resource2))
print("After mixed params test")

print("function parameter attribute tests completed successfully!")
