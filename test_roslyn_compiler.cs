using System;
using System.IO;
using FLua.Compiler;
using FLua.Parser;
using FLua.Ast;

class TestRoslynCompiler
{
    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: test_roslyn_compiler <lua-file>");
            return;
        }

        try
        {
            var luaFile = args[0];
            var outputFile = Path.ChangeExtension(luaFile, ".dll");
            
            // Read Lua source
            var source = File.ReadAllText(luaFile);
            Console.WriteLine($"Compiling {luaFile}...");
            
            // Parse
            var parseResult = Parser.parseScript(source);
            if (!parseResult.Item1)
            {
                Console.WriteLine($"Parse error: {parseResult.Item3}");
                return;
            }
            
            // Compile with Roslyn backend
            var compiler = new RoslynLuaCompiler();
            var options = new CompilerOptions
            {
                Target = CompilationTarget.Library,
                OutputPath = outputFile,
                IncludeDebugInfo = true,
                AssemblyName = Path.GetFileNameWithoutExtension(outputFile)
            };
            
            var result = compiler.Compile(parseResult.Item2, options);
            
            if (result.Success)
            {
                Console.WriteLine($"Compilation successful! Output: {outputFile}");
                Console.WriteLine($"Generated C# code saved to: {Path.ChangeExtension(outputFile, ".cs")}");
            }
            else
            {
                Console.WriteLine("Compilation failed:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"  {error}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}