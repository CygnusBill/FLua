local outer = 0
print("Before loop: outer =", outer)

local n = 0
while n < 1 do
    print("In loop, before assignment: outer =", outer)
    outer = 99
    print("In loop, after assignment: outer =", outer)
    n = n + 1
end

print("After loop: outer =", outer)