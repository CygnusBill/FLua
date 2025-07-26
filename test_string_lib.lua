-- Simple test to check if string library is loaded

print("Testing string library presence...")
print("string type:", type(string))
print("string.packsize type:", type(string.packsize))

if string.packsize then
    print("packsize exists, testing...")
    local size = string.packsize("b")
    print("Size of 'b':", size)
else
    print("packsize not found!")
end