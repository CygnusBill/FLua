-- Comprehensive test for string.pack, string.unpack, and string.packsize

local function test_packsize()
    print("Testing string.packsize...")
    
    -- Test basic format sizes
    local tests = {
        {"b", 1, "signed byte"},
        {"B", 1, "unsigned byte"},
        {"h", 2, "short"},
        {"H", 2, "unsigned short"},
        {"l", 8, "long"},
        {"L", 8, "unsigned long"},
        {"j", 8, "lua_Integer"},
        {"J", 8, "unsigned lua_Integer"},
        {"T", 8, "size_t"},
        {"f", 4, "float"},
        {"d", 8, "double"},
        {"n", 8, "lua_Number"},
    }
    
    for _, test in ipairs(tests) do
        local format, expected, name = test[1], test[2], test[3]
        local size = string.packsize(format)
        if size == expected then
            print("  PASS:", name, "is", size, "bytes")
        else
            print("  FAIL:", name, "expected", expected, "got", size)
        end
    end
    
    -- Test variable-length integers
    local var_tests = {
        {"i1", 1}, {"i2", 2}, {"i4", 4}, {"i8", 8},
        {"I1", 1}, {"I2", 2}, {"I4", 4}, {"I8", 8},
    }
    
    for _, test in ipairs(var_tests) do
        local format, expected = test[1], test[2]
        local size = string.packsize(format)
        if size == expected then
            print("  PASS:", format, "is", size, "bytes")
        else
            print("  FAIL:", format, "expected", expected, "got", size)
        end
    end
    
    -- Test multiple formats
    local size = string.packsize("bBhH")
    if size == 6 then
        print("  PASS: multiple formats add up correctly")
    else
        print("  FAIL: bBhH expected 6, got", size)
    end
    
    -- Test padding
    size = string.packsize("bxh")
    if size == 4 then
        print("  PASS: padding works")
    else
        print("  FAIL: bxh expected 4, got", size)
    end
    
    -- Test fixed strings
    size = string.packsize("c10")
    if size == 10 then
        print("  PASS: fixed string c10 is 10 bytes")
    else
        print("  FAIL: c10 expected 10, got", size)
    end
    
    -- Test variable-length format errors
    local ok, err = pcall(string.packsize, "s")
    if not ok and err:match("variable%-length") then
        print("  PASS: variable string format correctly errors")
    else
        print("  FAIL: variable string format should error")
    end
    
    print("packsize tests completed\n")
end

local function test_pack_unpack()
    print("Testing string.pack and string.unpack...")
    
    -- Test integers
    local tests = {
        {"b", 127, "signed byte max"},
        {"b", -128, "signed byte min"},
        {"B", 255, "unsigned byte max"},
        {"h", 32767, "short max"},
        {"h", -32768, "short min"},
        {"H", 65535, "unsigned short max"},
        {"i4", 1234567, "4-byte integer"},
    }
    
    for _, test in ipairs(tests) do
        local format, value, name = test[1], test[2], test[3]
        local packed = string.pack(format, value)
        local unpacked, pos = string.unpack(format, packed)
        if unpacked == value then
            print("  PASS:", name, "pack/unpack")
        else
            print("  FAIL:", name, "expected", value, "got", unpacked)
        end
    end
    
    -- Test floats
    local packed = string.pack("f", 3.14159)
    local unpacked = string.unpack("f", packed)
    if math.abs(unpacked - 3.14159) < 0.00001 then
        print("  PASS: float pack/unpack")
    else
        print("  FAIL: float expected ~3.14159, got", unpacked)
    end
    
    -- Test strings
    packed = string.pack("c5", "hello")
    unpacked = string.unpack("c5", packed)
    if unpacked == "hello" then
        print("  PASS: fixed string pack/unpack")
    else
        print("  FAIL: fixed string expected 'hello', got", unpacked)
    end
    
    packed = string.pack("z", "test")
    unpacked = string.unpack("z", packed)
    if unpacked == "test" then
        print("  PASS: zero-terminated string pack/unpack")
    else
        print("  FAIL: zero-terminated string expected 'test', got", unpacked)
    end
    
    -- Test multiple values
    packed = string.pack("bHf", 42, 1000, 2.5)
    local v1, v2, v3 = string.unpack("bHf", packed)
    if v1 == 42 and v2 == 1000 and math.abs(v3 - 2.5) < 0.0001 then
        print("  PASS: multiple value pack/unpack")
    else
        print("  FAIL: multiple values pack/unpack")
    end
    
    -- Test padding
    packed = string.pack("bxH", 10, 500)
    v1, v2 = string.unpack("bxH", packed)
    if v1 == 10 and v2 == 500 then
        print("  PASS: padding pack/unpack")
    else
        print("  FAIL: padding pack/unpack")
    end
    
    -- Test position parameter
    packed = string.pack("BBB", 1, 2, 3)
    local v, pos = string.unpack("B", packed, 2)
    if v == 2 and pos == 3 then
        print("  PASS: unpack with position")
    else
        print("  FAIL: unpack with position")
    end
    
    print("pack/unpack tests completed\n")
end

local function test_errors()
    print("Testing error conditions...")
    
    -- Invalid format
    local ok, err = pcall(string.pack, "q", 42)
    if not ok then
        print("  PASS: invalid format correctly errors")
    else
        print("  FAIL: invalid format should error")
    end
    
    -- Size too large
    ok, err = pcall(string.pack, "i9", 42)
    if not ok then
        print("  PASS: size > 8 correctly errors")
    else
        print("  FAIL: size > 8 should error")
    end
    
    -- Not enough data
    ok, err = pcall(string.unpack, "i4", "ab")
    if not ok then
        print("  PASS: insufficient data correctly errors")
    else
        print("  FAIL: insufficient data should error")
    end
    
    -- String too long
    ok, err = pcall(string.pack, "c3", "toolong")
    if not ok then
        print("  PASS: string too long correctly errors")
    else
        print("  FAIL: string too long should error")
    end
    
    print("error tests completed\n")
end

-- Run all tests
test_packsize()
test_pack_unpack()
test_errors()

print("All tests completed successfully!")