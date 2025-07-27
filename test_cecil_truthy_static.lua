-- Test truthiness with different values
local values = {true, false, 0, 1, nil}
local names = {"true", "false", "0", "1", "nil"}

for i = 1, #values do
    local v = values[i]
    print("Testing", names[i])
    if v then
        print("  -> is truthy")
    else
        print("  -> is falsy")
    end
end