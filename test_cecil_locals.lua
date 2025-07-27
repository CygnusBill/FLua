-- Test local variable scoping
local x = 1
print("x after declaration:", x)

-- This should update the existing x, not create a new one
x = 2  
print("x after assignment:", x)

-- Test in a block
do
    print("x in block before local:", x)
    local x = 3  -- This SHOULD create a new x that shadows
    print("x in block after local:", x)
end

print("x after block:", x)  -- Should be 2, not 3