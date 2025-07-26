-- Test that the workaround with parentheses works

function returntable(t) 
  return function() return nil end, t
end

-- This should work with parentheses
for k in returntable({}) do
  break
end

print("Workaround with parentheses works!")

-- Also test pairs
for k,v in pairs({a=1, b=2}) do
  print(k, v)
end