-- Test cases for function calls with table constructors (no parentheses)
print("Testing function calls with table constructors...")

-- Test 1: Basic statement context (should work)
print("1. Statement context:")
print{1,2,3}  -- This should work
print("  PASS: print{1,2,3} works")

-- Test 2: Assignment context (should work)  
print("\n2. Assignment context:")
local x = pairs{a=1, b=2}
print("  PASS: local x = pairs{a=1, b=2} works")

-- Test 3: For loop context (currently fails)
print("\n3. For loop context:")
local success = pcall(function()
  for k,v in pairs{a=1, b=2} do
    print("  ", k, v)
  end
end)
if success then
  print("  PASS: for k,v in pairs{a=1, b=2} works")
else
  print("  FAIL: for k,v in pairs{a=1, b=2} - parse error")
end

-- Test 4: If condition context (currently fails)
print("\n4. If condition context:")
function returntrue(t) return true end
success = pcall(function()
  if returntrue{} then
    print("  PASS: if f{} works")
  end
end)
if not success then
  print("  FAIL: if f{} - parse error")
end

-- Test 5: Workarounds with parentheses (should work)
print("\n5. Workarounds with parentheses:")
for k,v in pairs({a=1, b=2}) do
  -- This works
end
print("  PASS: for k,v in pairs({a=1, b=2}) works")

if returntrue({}) then
  -- This works  
end
print("  PASS: if f({}) works")

print("\nDone testing table constructor calls")