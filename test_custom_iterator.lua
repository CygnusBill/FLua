-- Test custom iterator
local function my_iter(state, key) 
    if key == nil then
        return 1, "one"
    elseif key == 1 then
        return 2, "two"
    else
        return nil
    end
end

local function my_iterator()
    return my_iter, "state", nil
end

for k, v in my_iterator() do
    print(k, v)
end