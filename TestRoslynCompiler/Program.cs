using System;
using System.IO;
using System.Linq;
using FLua.Compiler;
using FLua.Parser;
using FLua.Ast;
using Microsoft.FSharp.Collections;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.WriteLine("Usage: TestRoslynCompiler <lua-file>");
            return;
        }

        try
        {
            var luaFile = args[0];
            var isConsole = args.Length > 1 && args[1] == "--console";
            var outputFile = Path.ChangeExtension(luaFile, ".dll"); // Always .dll on .NET Core
            
            // Read Lua source
            var source = File.ReadAllText(luaFile);
            Console.WriteLine($"Compiling {luaFile} as {(isConsole ? "console app" : "library")} with Roslyn code generator...");
            
            // Parse
            var ast = ParserHelper.ParseString(source);
            var astList = ListModule.ToArray(ast).ToList();
            
            // Determine target based on command line arg
            var target = args.Length > 1 && args[1] == "--console" 
                ? CompilationTarget.ConsoleApp 
                : CompilationTarget.Library;
            
            // Compile with Roslyn backend
            var compiler = new RoslynLuaCompiler();
            var options = new CompilerOptions(
                OutputPath: outputFile,
                Target: target,
                Optimization: OptimizationLevel.Debug,
                IncludeDebugInfo: true,
                AssemblyName: Path.GetFileNameWithoutExtension(outputFile)
            );
            
            var result = compiler.Compile(astList, options);
            
            if (result.Success)
            {
                Console.WriteLine($"Compilation successful! Output: {outputFile}");
                var csFile = Path.ChangeExtension(outputFile, ".cs");
                if (File.Exists(csFile))
                {
                    Console.WriteLine($"Generated C# code saved to: {csFile}");
                    Console.WriteLine("\nGenerated code:");
                    Console.WriteLine("================");
                    Console.WriteLine(File.ReadAllText(csFile));
                    Console.WriteLine("================");
                }
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