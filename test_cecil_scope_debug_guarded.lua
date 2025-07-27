local outer = 0
print("Before loop: outer =", outer)

local n = 0
local guard = 0
while n < 1 do
    guard = guard + 1
    if guard > 3 then
        print("Breaking due to runaway")
        break
    end
    print("In loop, before assignment: outer =", outer)
    outer = 99
    print("In loop, after assignment: outer =", outer)
    n = n + 1
end

print("After loop: outer =", outer)
print("After loop: n =", n)