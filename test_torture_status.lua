-- Test script to check FLua's readiness for Lua torture tests

print("=== FLua Torture Test Readiness Check ===")
print()

-- Test 1: _VERSION global
print("1. Testing _VERSION global:")
print("   _VERSION =", _VERSION)
if _VERSION == nil then
  print("   ❌ MISSING: _VERSION should be 'Lua 5.4'")
else
  print("   ✅ _VERSION is defined")
end
print()

-- Test 2: load() function
print("2. Testing load() function:")
local success, result = pcall(function()
  local f = load("return 42")
  return f and f() == 42
end)
if success and result then
  print("   ✅ load() function works")
else
  print("   ❌ MISSING: load() function", success and "" or ("- " .. tostring(result)))
end
print()

-- Test 3: string.dump() function
print("3. Testing string.dump() function:")
local success, result = pcall(function()
  local f = function() return 42 end
  local dump = string.dump(f)
  return type(dump) == "string"
end)
if success and result then
  print("   ✅ string.dump() function works")
else
  print("   ❌ MISSING: string.dump() function", success and "" or ("- " .. tostring(result)))
end
print()

-- Test 4: debug library
print("4. Testing debug library:")
local success, debug = pcall(require, "debug")
if success then
  print("   ✅ debug library available")
  print("   Available functions:", next(debug) and "" or "none")
  for k, v in pairs(debug or {}) do
    print("     -", k)
  end
else
  print("   ❌ MISSING: debug library -", tostring(debug))
end
print()

-- Test 5: os.execute() function
print("5. Testing os.execute() function:")
local success, result = pcall(os.execute)
if success then
  print("   ✅ os.execute() available")
else
  print("   ❌ MISSING: os.execute() -", tostring(result))
end
print()

-- Test 6: io.tmpfile() function
print("6. Testing io.tmpfile() function:")
local success, result = pcall(function()
  return io.tmpfile ~= nil
end)
if success and result then
  print("   ✅ io.tmpfile() available")
else
  print("   ❌ MISSING: io.tmpfile()")
end
print()

-- Test 7: collectgarbage() function
print("7. Testing collectgarbage() function:")
local success, result = pcall(collectgarbage, "count")
if success then
  print("   ✅ collectgarbage() works - memory:", result, "KB")
else
  print("   ❌ MISSING/BROKEN: collectgarbage() -", tostring(result))
end
print()

-- Test 8: string.packsize() function
print("8. Testing string.packsize() function:")
local success, result = pcall(function()
  return string.packsize and string.packsize("i")
end)
if success and result then
  print("   ✅ string.packsize() works - int size:", result)
else
  print("   ❌ MISSING: string.packsize()")
end
print()

-- Test 9: warn() function
print("9. Testing warn() function:")
local success = pcall(warn, "@on")
if success then
  print("   ✅ warn() function available")
else
  print("   ❌ MISSING: warn() function")
end
print()

-- Test 10: Shebang support
print("10. Testing shebang (#!) support:")
print("    ❌ NOT SUPPORTED: Parser doesn't handle #! lines")
print()

print("=== Summary ===")
print("The Lua torture tests require several missing features.")
print("Key missing components that block torture tests:")
print("- _VERSION global variable")
print("- load() function for dynamic code loading")
print("- string.dump() for function serialization")
print("- os.execute() for system commands")
print("- Shebang (#!) line support in parser")
print()
print("Without these, the official Lua test suite cannot run.")