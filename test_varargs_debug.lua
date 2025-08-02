-- Debug varargs handling
function test(...)
    print("In test function")
    print("... =", ...)
    
    local args = {...}
    print("args table created")
    print("#args =", #args)
    
    for i = 1, 10 do
        if args[i] then
            print("args[" .. i .. "] =", args[i])
        end
    end
    
    return args
end

print("Calling test(10, 20, 30)")
local result = test(10, 20, 30)
print("result =", result)
print("#result =", #result)