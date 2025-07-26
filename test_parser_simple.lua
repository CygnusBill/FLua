-- Simple parser tests without using load()

print("=== Testing Parser Issues Directly ===")
print()

-- Test 1: Single underscore
print("1. Testing single underscore:")
local _ = 42
print("   local _ = 42 -- Success!")
print("   _ =", _)
print()

-- Test 2: For loop with underscore
print("2. Testing for loop with underscore:")
for _, v in pairs{10, 20, 30} do
  print("   Value:", v)
end
print("   ✅ Single underscore works in for loops")
print()

-- Test 3: Function call with table constructor (workaround)
print("3. Testing function call with table (using workaround):")
for k in pairs({1,2,3}) do
  print("   Key:", k)
end
print("   ✅ Works with parentheses")
print()

-- Test 4: Direct table constructor call
print("4. Testing direct call without load:")
local function test()
  return "ok"
end
-- Can't test f{} syntax without load() to parse it
print("   ⚠️  Cannot test f{} syntax without load() function")
print()

print("=== Direct Execution Results ===")
print("- Single underscore identifier: ✅ WORKING")
print("- For loops with pairs: ✅ WORKING (with parentheses)")
print("- Cannot test parser-only issues without load() function")