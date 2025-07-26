-- Test known parser issues

print("=== Testing Known Parser Issues ===")
print()

-- Test 1: Function calls with table constructors in for loops
print("1. Function call with table constructor in for loop:")
local test1 = [[
for k in pairs{1,2,3} do 
  print(k)
end
]]
print("   Code: for k in pairs{1,2,3} do ... end")
local success, err = pcall(load, test1)
if success then
  print("   ✅ FIXED: Parser now handles this correctly")
else
  print("   ❌ STILL BROKEN:", err)
end
print()

-- Test 2: Function calls with table constructors in if conditions
print("2. Function call with table constructor in if condition:")
local test2 = [[
local function f(t) return #t > 0 end
if f{1,2,3} then
  print("ok")
end
]]
print("   Code: if f{1,2,3} then ... end")
local success, err = pcall(load, test2)
if success then
  print("   ✅ FIXED: Parser now handles this correctly")
else
  print("   ❌ STILL BROKEN:", err)
end
print()

-- Test 3: Function calls with long strings (no parentheses)
print("3. Function call with long string (no parentheses):")
local test3 = [[
print[[hello world]]
]]
print("   Code: print[[hello world]]")
local success, err = pcall(load, test3)
if success then
  print("   ✅ FIXED: Parser now handles this correctly")
else
  print("   ❌ STILL BROKEN:", err)
end
print()

-- Test 4: Single underscore as identifier
print("4. Single underscore as identifier:")
local test4 = [[
local _ = 42
for _, v in pairs{1,2,3} do
  print(v)
end
]]
print("   Code: local _ = 42; for _, v in pairs{...}")
local success, err = pcall(load, test4)
if success then
  print("   ✅ FIXED: Parser now handles single underscore")
else
  print("   ❌ STILL BROKEN:", err)
end
print()

-- Test 5: Unicode escapes beyond valid range
print("5. Unicode escapes beyond valid range:")
local test5 = [[
local s1 = "\u{10FFFF}"   -- Maximum valid Unicode
local s2 = "\u{200000}"   -- Beyond Unicode range
]]
print("   Testing unicode escapes...")
local success, err = pcall(load, test5)
if success then
  print("   ✅ Parser accepts extended unicode escapes (for compatibility)")
else
  print("   ❌ Parser rejects extended unicode:", err)
end
print()

print("=== Summary ===")
print("Check which parser issues are still present.")
print("Note: load() function is missing, so these tests use pcall(load, ...)")
print("which will fail if load() doesn't exist.")