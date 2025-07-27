-- Simple Lua script to test AOT compilation
print("Hello from AOT-compiled Lua!")
print("2 + 2 =", 2 + 2)

local function greet(name)
    return "Hello, " .. name .. "!"
end

print(greet("World"))