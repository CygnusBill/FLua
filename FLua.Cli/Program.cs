using System;
using System.IO;
using FLua.Interpreter;
using FLua.Runtime;

namespace FLua.Cli
{
    class Program
    {
        static int Main(string[] args)
        {
            // If no arguments provided, enter REPL mode
            if (args.Length == 0)
            {
                var repl = new LuaRepl();
                repl.Run();
                return 0;
            }

            // Handle help flag
            if (args[0] == "--help" || args[0] == "-h")
            {
                ShowHelp();
                return 0;
            }

            // Handle version flag
            if (args[0] == "--version" || args[0] == "-v")
            {
                Console.WriteLine("FLua - A Lua implementation in F# and C#");
                Console.WriteLine("Version: 1.0.0");
                return 0;
            }

            // File execution mode
            string filename = args[0];
            bool verbose = Array.IndexOf(args, "--verbose") >= 0;

            if (!File.Exists(filename))
            {
                Console.Error.WriteLine($"Error: File '{filename}' not found");
                return 1;
            }

            try
            {
                string code = File.ReadAllText(filename);
                
                // Execute the script using the interpreter
                var interpreter = new LuaInterpreter();
                var result = interpreter.ExecuteCode(code);
                
                // Only output if there's a return value and verbose mode is on
                if (verbose && result.Length > 0 && result[0] != LuaNil.Instance)
                {
                    Console.WriteLine($"Script returned: {result[0]}");
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                if (verbose)
                {
                    Console.Error.WriteLine(ex.StackTrace);
                }
                return 1;
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("FLua - A Lua implementation in F# and C#");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  flua                    Enter interactive REPL mode");
            Console.WriteLine("  flua <script.lua>       Execute a Lua script file");
            Console.WriteLine("  flua [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h, --help             Show this help message");
            Console.WriteLine("  -v, --version          Show version information");
            Console.WriteLine("      --verbose          Show verbose output when executing files");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  flua                   # Start interactive REPL");
            Console.WriteLine("  flua script.lua        # Run script.lua");
            Console.WriteLine("  flua --verbose test.lua # Run test.lua with verbose output");
            Console.WriteLine();
            Console.WriteLine("In REPL mode, use .help for REPL-specific commands.");
        }
    }
}
