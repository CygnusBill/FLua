-- Debug pack test
print("Starting debug test...")

print("1. Testing direct call")
local result = string.packsize("b")
print("Result:", result)

print("\n2. Testing comparison")
local equal = (result == 1)
print("Equal:", equal)

print("\n3. Testing assert with comparison")
assert(result == 1)
print("Assert passed")

print("\n4. Testing original assert")
assert(string.packsize("b") == 1, "signed byte should be 1 byte")
print("Original assert passed!")