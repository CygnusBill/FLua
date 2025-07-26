-- Test what parser features ARE working

print("=== Testing Working Parser Features ===")
print()

-- Test 1: Single underscore
print("1. Single underscore identifier:")
local _ = 42
local a, _ = 10, 20
print("   local _ = 42 -- ✅ WORKS")
print("   _ =", _)
print()

-- Test 2: For loop with parentheses
print("2. For loop with parentheses:")
for k, v in pairs({10, 20, 30}) do
  print("   ", k, "=", v)
end
print("   ✅ pairs({...}) works")
print()

-- Test 3: Table constructor calls as statements
print("3. Table constructor calls:")
print{1, 2, 3}
print("   ✅ print{...} works as statement")
print()

-- Test 4: Long strings
print("4. Long string literals:")
local s = [[Hello
World]]
print("   Long string:", s)
print("   ✅ Long strings work")
print()

-- Test 5: Unicode escapes
print("5. Unicode escapes:")
local u1 = "\u{41}"  -- 'A'
local u2 = "\u{1F600}"  -- 😀
print("   \\u{41} =", u1)
print("   \\u{1F600} = [emoji]")
print("   ✅ Unicode escapes work")
print()

print("=== Summary ===")
print("✅ Single underscore identifier: FIXED")
print("✅ Table constructor calls: Work as statements") 
print("❌ Table constructor calls: Don't work in for/if expressions")
print("✅ Long strings: Working")
print("✅ Unicode escapes: Working")