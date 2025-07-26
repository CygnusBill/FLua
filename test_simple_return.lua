-- Test simple return statements at top level
print("Testing simple return")

-- Early return
if true then
    print("This will print")
    return 42, "hello"
end

print("This should not print")