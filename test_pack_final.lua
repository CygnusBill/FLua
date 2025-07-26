-- Test string.pack, string.unpack, and string.packsize

print("Testing string.packsize...")

-- Basic format sizes
local size = string.packsize("b")
print("packsize('b'):", size, size == 1 and "PASS" or "FAIL")

size = string.packsize("B") 
print("packsize('B'):", size, size == 1 and "PASS" or "FAIL")

size = string.packsize("h")
print("packsize('h'):", size, size == 2 and "PASS" or "FAIL")

size = string.packsize("H")
print("packsize('H'):", size, size == 2 and "PASS" or "FAIL")

size = string.packsize("f")
print("packsize('f'):", size, size == 4 and "PASS" or "FAIL")

size = string.packsize("d")
print("packsize('d'):", size, size == 8 and "PASS" or "FAIL")

-- Variable-length integers
size = string.packsize("i1")
print("packsize('i1'):", size, size == 1 and "PASS" or "FAIL")

size = string.packsize("i4")
print("packsize('i4'):", size, size == 4 and "PASS" or "FAIL")

-- Multiple formats
size = string.packsize("bBhH")
print("packsize('bBhH'):", size, size == 6 and "PASS" or "FAIL")

-- Test variable-length error
local ok = pcall(string.packsize, "s")
print("packsize('s') error:", not ok and "PASS" or "FAIL")

print("\nTesting string.pack/unpack...")

-- Basic integer
local packed = string.pack("b", 127)
local v, pos = string.unpack("b", packed)
print("pack/unpack byte:", v, v == 127 and "PASS" or "FAIL")

-- Negative byte
packed = string.pack("b", -128)
v = string.unpack("b", packed)
print("pack/unpack negative:", v, v == -128 and "PASS" or "FAIL")

-- Unsigned byte
packed = string.pack("B", 255)
v = string.unpack("B", packed)
print("pack/unpack unsigned:", v, v == 255 and "PASS" or "FAIL")

-- Float
packed = string.pack("f", 3.14)
v = string.unpack("f", packed)
local diff = math.abs(v - 3.14)
print("pack/unpack float:", v, diff < 0.01 and "PASS" or "FAIL")

-- String
packed = string.pack("c5", "hello")
v = string.unpack("c5", packed)
print("pack/unpack string:", v, v == "hello" and "PASS" or "FAIL")

-- Zero-terminated string
packed = string.pack("z", "test")
v = string.unpack("z", packed)
print("pack/unpack z-string:", v, v == "test" and "PASS" or "FAIL")

-- Multiple values
packed = string.pack("bH", 42, 1000)
local v1, v2 = string.unpack("bH", packed)
print("pack/unpack multiple:", v1, v2, (v1 == 42 and v2 == 1000) and "PASS" or "FAIL")

-- With padding
packed = string.pack("bxH", 10, 500)
v1, v2 = string.unpack("bxH", packed)
print("pack/unpack padding:", v1, v2, (v1 == 10 and v2 == 500) and "PASS" or "FAIL")

print("\nAll tests completed!")