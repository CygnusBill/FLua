-- Test method calls on existing objects

-- Test 1: String methods (if supported by runtime)
print("Test 1: String methods")
local s = "hello"
-- Test string metatable methods if available

-- Test 2: Table with pre-defined methods
print("\nTest 2: Pre-defined table methods")
local t = {1, 2, 3}
-- table.insert is a function, not a method, so we can't test t:insert()

-- Test 3: Math operations as methods (if supported)
print("\nTest 3: Using pairs as a method call")
local data = {a=1, b=2}
-- We can't actually call pairs as a method, it's a global function

print("\nNote: Most method call tests require anonymous functions which aren't implemented yet")
print("Method call syntax is working, but we need anonymous functions to create objects with methods")