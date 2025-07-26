-- Minimal test to isolate the issue
print("Test 1:")
assert(string.packsize("b") == 1, "test 1")
print("passed")

print("\nTest 2:")
assert(string.packsize("B") == 1, "test 2")
print("passed")

print("\nAll tests passed!")