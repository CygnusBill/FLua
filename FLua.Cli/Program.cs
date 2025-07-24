using System;
using System.IO;
using FLua.Interpreter;

namespace FLua.Cli
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("FLua Compiler");
                Console.WriteLine("Usage: flua <filename> [options]");
                Console.WriteLine("Options:");
                Console.WriteLine("  -o <output>   Specify output file");
                Console.WriteLine("  -run          Run the script instead of compiling");
                Console.WriteLine("  -v            Verbose output");
                return 0;
            }

            string filename = args[0];
            bool runMode = Array.IndexOf(args, "-run") >= 0;
            bool verbose = Array.IndexOf(args, "-v") >= 0;

            if (!File.Exists(filename))
            {
                Console.Error.WriteLine($"Error: File '{filename}' not found");
                return 1;
            }

            try
            {
                string code = File.ReadAllText(filename);
                
                if (runMode)
                {
                    // Run the script using the interpreter
                    var interpreter = new LuaInterpreter();
                    var result = interpreter.ExecuteCode(code);
                    
                    if (result.Length > 0 && result[0] != LuaNil.Instance)
                    {
                        Console.WriteLine(result[0]);
                    }
                }
                else
                {
                    // For now, just print that compilation is not yet implemented
                    Console.WriteLine("Compilation to native code is not yet implemented");
                    
                    // In the future, this would compile the Lua code to IL/native code
                    // var compiler = new LuaCompiler();
                    // compiler.CompileToAssembly(code, outputPath);
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
    }
}
