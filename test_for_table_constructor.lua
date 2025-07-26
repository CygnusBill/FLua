-- Minimal test for the for loop table constructor issue

-- This should work but currently fails:
-- for k in pairs{1,2,3} do end

-- Let's test with a simpler case
function returntable(t) 
  return function() return nil end, t
end

-- Try the simplest case
for k in returntable{} do
  break  -- Just to have something in the body
end

print("If you see this, the parser issue is fixed!")