print("Testing while loop with local variable")
local i = 0
print("i before loop:", i)

-- Test that we can read the local
if i == 0 then
    print("i is 0 - correct")
end

-- Test that we can update the local
i = i + 1
print("i after increment:", i)

-- Test comparison
if i < 2 then
    print("i < 2 is true - correct")
else
    print("i < 2 is false - wrong!")
end

-- Now test in a while loop
print("\nStarting while loop...")
while i < 3 do
    print("In loop, i =", i)
    local old_i = i
    i = i + 1
    print("  After increment: old_i =", old_i, "new i =", i)
    if i > 10 then
        print("  Safety break!")
        break
    end
end
print("After loop, i =", i)