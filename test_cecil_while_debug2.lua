local i = 0
local iterations = 0

while i < 3 do
    iterations = iterations + 1
    if iterations > 5 then
        print("Breaking due to runaway after", iterations, "iterations")
        break
    end
    
    print("Before increment: i =", i)
    i = i + 1
    print("After increment: i =", i)
end

print("Loop ended. i =", i, "iterations =", iterations)