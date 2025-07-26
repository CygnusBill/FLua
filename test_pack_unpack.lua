-- Test string.pack, string.unpack, and string.packsize functions

print("Testing string.packsize...")
print("About to test basic format sizes...")

-- Test basic format sizes
print("Testing packsize('b')...")
assert(string.packsize("b") == 1, "signed byte should be 1 byte")
print("First assert passed")
assert(string.packsize("B") == 1, "unsigned byte should be 1 byte")
assert(string.packsize("h") == 2, "short should be 2 bytes")
assert(string.packsize("H") == 2, "unsigned short should be 2 bytes")
assert(string.packsize("l") == 8, "long should be 8 bytes")
assert(string.packsize("L") == 8, "unsigned long should be 8 bytes")
assert(string.packsize("j") == 8, "lua_Integer should be 8 bytes")
assert(string.packsize("J") == 8, "unsigned lua_Integer should be 8 bytes")
assert(string.packsize("T") == 8, "size_t should be 8 bytes")
assert(string.packsize("f") == 4, "float should be 4 bytes")
assert(string.packsize("d") == 8, "double should be 8 bytes")
assert(string.packsize("n") == 8, "lua_Number should be 8 bytes")

-- Test variable-length integers
assert(string.packsize("i1") == 1, "i1 should be 1 byte")
assert(string.packsize("i2") == 2, "i2 should be 2 bytes")
assert(string.packsize("i4") == 4, "i4 should be 4 bytes")
assert(string.packsize("i8") == 8, "i8 should be 8 bytes")
assert(string.packsize("I1") == 1, "I1 should be 1 byte")
assert(string.packsize("I2") == 2, "I2 should be 2 bytes")
assert(string.packsize("I4") == 4, "I4 should be 4 bytes")
assert(string.packsize("I8") == 8, "I8 should be 8 bytes")

-- Test multiple formats
assert(string.packsize("bBhH") == 1+1+2+2, "multiple formats should add up")
assert(string.packsize("i1i2i4i8") == 1+2+4+8, "multiple variable ints should add up")

-- Test padding
assert(string.packsize("bxh") == 1+1+2, "padding should be 1 byte")
assert(string.packsize("bxxxh") == 1+3+2, "multiple padding should work")

-- Test fixed-length strings
assert(string.packsize("c10") == 10, "c10 should be 10 bytes")
assert(string.packsize("c1c2c3") == 1+2+3, "multiple fixed strings should add up")

-- Test that variable-length formats throw errors
local ok, err = pcall(string.packsize, "s")
assert(not ok and err:match("variable%-length"), "variable string should error")

ok, err = pcall(string.packsize, "z")
assert(not ok and err:match("variable%-length"), "zero-terminated string should error")

print("string.packsize tests passed!")

print("\nTesting string.pack and string.unpack...")

-- Test basic integer packing
local packed = string.pack("b", 127)
local v, pos = string.unpack("b", packed)
assert(v == 127 and pos == 2, "signed byte pack/unpack")

packed = string.pack("b", -128)
v, pos = string.unpack("b", packed)
assert(v == -128 and pos == 2, "negative signed byte pack/unpack")

packed = string.pack("B", 255)
v, pos = string.unpack("B", packed)
assert(v == 255 and pos == 2, "unsigned byte pack/unpack")

-- Test short integers
packed = string.pack("h", 32767)
v, pos = string.unpack("h", packed)
assert(v == 32767 and pos == 3, "short pack/unpack")

packed = string.pack("h", -32768)
v, pos = string.unpack("h", packed)
assert(v == -32768 and pos == 3, "negative short pack/unpack")

packed = string.pack("H", 65535)
v, pos = string.unpack("H", packed)
assert(v == 65535 and pos == 3, "unsigned short pack/unpack")

-- Test variable-length integers
packed = string.pack("i1", 100)
v, pos = string.unpack("i1", packed)
assert(v == 100 and pos == 2, "i1 pack/unpack")

packed = string.pack("i4", 1234567)
v, pos = string.unpack("i4", packed)
assert(v == 1234567 and pos == 5, "i4 pack/unpack")

-- Test floating point
packed = string.pack("f", 3.14159)
v, pos = string.unpack("f", packed)
assert(math.abs(v - 3.14159) < 0.00001 and pos == 5, "float pack/unpack")

packed = string.pack("d", 3.141592653589793)
v, pos = string.unpack("d", packed)
assert(math.abs(v - 3.141592653589793) < 0.000000000001 and pos == 9, "double pack/unpack")

-- Test strings
packed = string.pack("c5", "hello")
v, pos = string.unpack("c5", packed)
assert(v == "hello" and pos == 6, "fixed string pack/unpack")

packed = string.pack("z", "hello")
v, pos = string.unpack("z", packed)
assert(v == "hello" and pos == 7, "zero-terminated string pack/unpack")

packed = string.pack("s1", "hi")
v, pos = string.unpack("s1", packed)
assert(v == "hi" and pos == 4, "string with 1-byte length pack/unpack")

-- Test multiple values
packed = string.pack("bHf", 42, 1000, 2.5)
local v1, v2, v3, pos = string.unpack("bHf", packed)
assert(v1 == 42 and v2 == 1000 and math.abs(v3 - 2.5) < 0.0001, "multiple value pack/unpack")

-- Test padding
packed = string.pack("bxH", 10, 500)
v1, v2, pos = string.unpack("bxH", packed)
assert(v1 == 10 and v2 == 500, "padding pack/unpack")

-- Test position parameter
packed = string.pack("BBB", 1, 2, 3)
v, pos = string.unpack("B", packed, 2)
assert(v == 2 and pos == 3, "unpack with position")

v, pos = string.unpack("B", packed, 3)
assert(v == 3 and pos == 4, "unpack with position 3")

-- Test empty format
packed = string.pack("")
assert(#packed == 0, "empty format should produce empty string")

-- Test complex format
packed = string.pack("c3xi2Bz", "abc", 1000, 255, "test")
local s1, i1, b1, s2, pos = string.unpack("c3xi2Bz", packed)
assert(s1 == "abc" and i1 == 1000 and b1 == 255 and s2 == "test", "complex format pack/unpack")

print("string.pack and string.unpack tests passed!")

-- Test error conditions
print("\nTesting error conditions...")

-- Invalid format specifier
ok, err = pcall(string.pack, "q", 42)
assert(not ok, "invalid format should error")

-- Size too large
ok, err = pcall(string.pack, "i9", 42)
assert(not ok, "size > 8 should error")

-- Not enough data to unpack
ok, err = pcall(string.unpack, "i4", "ab")
assert(not ok, "insufficient data should error")

-- String too long for format
ok, err = pcall(string.pack, "c3", "toolong")
assert(not ok, "string too long should error")

print("Error condition tests passed!")
print("\nAll pack/unpack tests completed successfully!")