-- Test custom error messages for table constructor calls

-- This should give a helpful error message for for loops
for k in pairs{1,2,3} do
  print(k)
end