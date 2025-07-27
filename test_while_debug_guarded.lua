local i = 0
while i < 3 do
    print("i =", i)
    if i > 5 then
        print("Safety break!")
        break
    end
    i = i + 1
end
print("Done, i =", i)