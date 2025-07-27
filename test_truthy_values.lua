-- Test IsTruthy property behavior
local values = {
    true,
    false,
    0,
    1,
    nil,
    "hello",
    ""
}

for i, v in ipairs(values) do
    print("Value:", v, "Type:", type(v))
end