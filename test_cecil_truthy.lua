local t = true
local f = false

print("t =", t)
print("f =", f)

if t then
    print("t is truthy")
else
    print("t is falsy")
end

if f then
    print("f is truthy")
else
    print("f is falsy")
end

local result = 1 > 5
print("1 > 5 =", result)

if result then
    print("result is truthy")
else
    print("result is falsy")
end