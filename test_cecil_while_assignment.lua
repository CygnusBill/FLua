-- Test to isolate the assignment issue
local x = 10
print("Before loop: x =", x)

local counter = 0
while counter < 2 do
    counter = counter + 1
    if counter > 5 then
        print("Breaking due to runaway at counter =", counter)
        break
    end
    print("Loop iteration", counter)
    print("  Before assignment: x =", x)
    x = 20
    print("  After assignment: x =", x)
end

print("After loop: x =", x)
print("After loop: counter =", counter)