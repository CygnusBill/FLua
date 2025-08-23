using System;
using FLua.Runtime;

// Quick debug test for pattern matching with captures
class Program {
    static void Main() {
        var str = "hello world";
        var pattern = "h(ell)o";
        
        Console.WriteLine($"Testing pattern: '{pattern}' against string: '{str}'");
        
        var match = LuaPatterns.Find(str, pattern, 1, false);
        
        if (match != null) {
            Console.WriteLine($"Match found: Start={match.Start}, End={match.End}");
            Console.WriteLine($"Number of captures: {match.Captures.Count}");
            
            for (int i = 0; i < match.Captures.Count; i++) {
                Console.WriteLine($"Capture {i}: '{match.Captures[i]}'");
            }
        } else {
            Console.WriteLine("No match found");
        }
    }
}