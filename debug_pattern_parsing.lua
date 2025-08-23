-- Let's see what happens if we use the standard Lua quantifier instead

-- Standard Lua uses - for minimal matching
print("Standard Lua quantifier:")
local result = string.match("te", "te(st)-")
print("te(st)- on 'te':", result)

result = string.match("test", "te(st)-")
print("te(st)- on 'test':", result)