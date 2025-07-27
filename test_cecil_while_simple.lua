i = 0  -- Use global instead of local
print("Before loop, i =", i)

while i < 3 do
    print("In loop, i =", i)
    i = i + 1
end

print("After loop, i =", i)