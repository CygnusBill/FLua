local x = 42
load("test")  -- This should produce an error with line/column info
print(x)