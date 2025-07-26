-- Test function calls with long strings (no parentheses)

-- Test cases that should work but currently fail
print[[hello world]]      -- Long string without parentheses
print[=[another test]=]   -- Different bracket level

-- These should work (with parentheses)
print([[hello with parens]])
print([=[with parens too]=])

-- Regular strings work
print "regular string"
print 'single quotes'

-- Method calls should work
local obj = {
  method = function(self, str) 
    print("Method called with:", str)
  end
}

obj:method[[method test]]  -- Method with long string

print("If you see this, long string calls are fixed!")