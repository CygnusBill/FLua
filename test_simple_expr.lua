-- Even simpler test to isolate the issue
x = returntable{}  -- This should work
print("Expression in assignment works")

-- Now test in for loop
for k in returntable{} do break end
print("For loop with table constructor works!")