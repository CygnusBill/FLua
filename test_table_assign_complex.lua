local t = {}
t[1] = 100
t.field = "hello"
t["key"] = true
print(t[1], t.field, t["key"])

-- Multiple assignment
local a, b = {}, {}
a[1], b[1] = 10, 20
print(a[1], b[1])

-- Nested table assignment
local nested = {inner = {}}
nested.inner.value = 42
print(nested.inner.value)