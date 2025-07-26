-- Test assert function
print("Testing assert...")

-- Simple assert
assert(true, "This should pass")
print("Simple assert passed")

-- Assert with expression
assert(1 + 1 == 2, "Math is broken")
print("Expression assert passed")

-- Assert with function call
assert(string.packsize("b") == 1, "packsize failed")
print("Function call assert passed")

print("All asserts passed!")