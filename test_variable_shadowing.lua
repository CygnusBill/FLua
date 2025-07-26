-- Test variable shadowing in nested scopes
print("Testing variable shadowing")

-- Outer scope
local x = 10
print("outer x =", x)

-- Nested scope with shadowing
do
    local x = 20
    print("inner x =", x)
    
    -- Even deeper nesting
    do
        local x = 30
        print("inner inner x =", x)
    end
    
    print("back to inner x =", x)
end

print("back to outer x =", x)

-- Test with function scopes
local y = 100
local function test()
    local y = 200
    print("function y =", y)
    
    if true then
        local y = 300
        print("if block y =", y)
    end
    
    print("back to function y =", y)
end

test()
print("outer y =", y)

print("Variable shadowing test completed!")