-- Simplified pack/unpack test

print("Testing string.packsize...")

-- Direct test without assert
local size = string.packsize("b")
print("Size of 'b':", size)
if size == 1 then
    print("PASS: signed byte is 1 byte")
else
    print("FAIL: signed byte should be 1 byte, got", size)
end

-- Test multiple formats
size = string.packsize("bBhH")
print("Size of 'bBhH':", size)
if size == 6 then  -- 1+1+2+2
    print("PASS: multiple formats add up correctly")
else
    print("FAIL: expected 6, got", size)
end

-- Test variable-length error
print("\nTesting variable-length error...")
local ok, err = pcall(string.packsize, "s")
if not ok then
    print("PASS: variable string format correctly errors:", err)
else
    print("FAIL: variable string format should error")
end

print("\nTesting string.pack and string.unpack...")

-- Test basic packing
local packed = string.pack("b", 127)
print("Packed byte length:", #packed)

local v, pos = string.unpack("b", packed)
print("Unpacked value:", v, "position:", pos)
if v == 127 and pos == 2 then
    print("PASS: byte pack/unpack")
else
    print("FAIL: expected 127 at position 2")
end

-- Test multiple values
packed = string.pack("bH", 42, 1000)
print("\nPacked bH length:", #packed)

local v1, v2, pos = string.unpack("bH", packed)
print("Unpacked values:", v1, v2, "position:", pos)
if v1 == 42 and v2 == 1000 then
    print("PASS: multiple value pack/unpack")
else
    print("FAIL: expected 42, 1000")
end

print("\nBasic tests completed!")