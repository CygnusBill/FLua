local i = 0
local count = 0
while i < 1 do
    count = count + 1
    print("in loop, count =", count)
    if count > 3 then
        print("Breaking due to runaway")
        break
    end
    i = 1
end
print("done, i =", i)