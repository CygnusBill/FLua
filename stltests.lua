-- Lua Standard Library Test Script

-- Basic Library Tests
print("--- Basic Library Tests ---")
assert(type(10) == "number", "type() should return 'number' for numbers")
assert(type("hello") == "string", "type() should return 'string' for strings")
assert(type(true) == "boolean", "type() should return 'boolean' for booleans")

local success, message = pcall(function() error("Test Error") end)
assert(not success and message == "Test Error", "pcall() should catch errors")

-- Table Library Tests
print("\n--- Table Library Tests ---")
local my_table = {}
table.insert(my_table, "apple")
table.insert(my_table, "banana")
assert(my_table[1] == "apple", "table.insert() failed (element 1)")
assert(my_table[2] == "banana", "table.insert() failed (element 2)")

table.remove(my_table, 1)
assert(my_table[1] == "banana", "table.remove() failed")

local sorted_table = {3, 1, 2}
table.sort(sorted_table)
assert(sorted_table[1] == 1 and sorted_table[2] == 2 and sorted_table[3] == 3, "table.sort() failed")

-- String Library Tests
print("\n--- String Library Tests ---")
local text = "Hello Lua"
local found_start, found_end = string.find(text, "Lua")
assert(found_start == 7 and found_end == 9, "string.find() failed")

local replaced_text = string.gsub(text, "Lua", "World")
assert(replaced_text == "Hello World", "string.gsub() failed")

-- Math Library Tests
print("\n--- Math Library Tests ---")
assert(math.floor(3.14) == 3, "math.floor() failed")
assert(math.ceil(3.14) == 4, "math.ceil() failed")

local rand_num = math.random()
assert(rand_num >= 0 and rand_num < 1, "math.random() failed (range)")

-- I/O Library Tests (simplified for demonstration, as file I/O requires a file system)
print("\n--- I/O Library Tests (simplified) ---")
-- In a real scenario, you'd create and read from temporary files
local f = io.open("test.txt", "w")
if f then
    f:write("Test content\n")
    f:close()
    f = io.open("test.txt", "r")
    local content = f:read("*l")
    assert(content == "Test content", "io.write() or io.read() failed")
    f:close()
    os.remove("test.txt") -- Clean up the test file
else
    print("WARNING: Could not open test.txt for I/O tests. Skipping.")
end

-- OS Library Tests (simplified)
print("\n--- OS Library Tests (simplified) ---")
local current_time = os.time()
assert(type(current_time) == "number", "os.time() should return a number")

-- Coroutine Library Tests
print("\n--- Coroutine Library Tests ---")
local co = coroutine.create(function()
    print("Coroutine started")
    coroutine.yield("Yielded value")
    print("Coroutine resumed")
end)

assert(coroutine.status(co) == "suspended", "coroutine.create() failed (status)")
local success, value = coroutine.resume(co)
assert(success and value == "Yielded value", "coroutine.resume() failed (yielded value)")
assert(coroutine.status(co) == "suspended", "coroutine.resume() failed (status after yield)")
success = coroutine.resume(co)
assert(success and coroutine.status(co) == "dead", "coroutine.resume() failed (status after completion)")

-- Debug Library Tests (simplified)
print("\n--- Debug Library Tests (simplified) ---")
local function test_debug_info()
    local info = debug.getinfo(1, "nS")
    assert(info.name == "test_debug_info", "debug.getinfo() failed (function name)")
    assert(info.what == "Lua", "debug.getinfo() failed (source type)")
end
test_debug_info()

print("\nAll standard library tests passed (simplified)!")

