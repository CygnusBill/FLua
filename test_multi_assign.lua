-- Test multiple assignment from function calls
local function multi()
    return 10, 20, 30
end

local a, b, c = multi()
print("a =", a)
print("b =", b) 
print("c =", c)

-- Test with fewer variables than returns
local x, y = multi()
print("x =", x)
print("y =", y)

-- Test with more variables than returns
local function dual()
    return 1, 2
end

local p, q, r = dual()
print("p =", p)
print("q =", q)
print("r =", r)  -- Should be nil