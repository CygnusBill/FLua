-- Test const variables
print("=== Testing const variables ===")

-- Basic const variable declaration
local x <const> = 42
print("x =", x)

-- This should work - reading const variable
print("x * 2 =", x * 2)

-- This should fail - trying to modify const variable
-- x = 50  -- Uncomment to test error

-- Multiple const variables
local a <const>, b <const> = 10, 20
print("a =", a, "b =", b)

-- Mixed const and regular variables
local c <const>, d = 30, 40
print("c =", c, "d =", d)

-- This should work - modifying regular variable
d = 50
print("d after modification =", d)

-- This should fail - trying to modify const variable
-- c = 60  -- Uncomment to test error

print("const variable tests completed successfully!")
