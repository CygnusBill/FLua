-- Simple variable shadowing test
print("Testing simple variable shadowing")

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

-- Test with if blocks
local y = 100
if true then
    local y = 200
    print("if block y =", y)
end
print("outer y after if =", y)

print("Simple shadowing test completed!")