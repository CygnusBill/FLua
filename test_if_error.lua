-- Test custom error for if statements

function test() return true end

if test{} then
  print("hello")
end