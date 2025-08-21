using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using FLua.Interpreter;
using FLua.Runtime;
using FLua.Compiler;
using FLua.Ast;

namespace FLua.Cli;

public class Program
{
    public static int Main(string[] args)
    {
        return CommandLineDispatcher.Execute(args);
    }

    public static int RunRepl()
    {
        try
        {
            var repl = new LuaRepl();
            repl.Run();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"REPL Error: {ex.Message}");
            return 1;
        }
    }

    public static int RunStdin()
    {
        try
        {
            string code = Console.In.ReadToEnd();
            if (!string.IsNullOrWhiteSpace(code))
            {
                var interpreter = new LuaInterpreter();
                interpreter.ExecuteCode(code);
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 1;
        }
    }

    public static int RunFile(string filename, bool verbose)
    {
        try
        {
            string code;
            
            // Support "-" as stdin
            if (filename == "-")
            {
                code = Console.In.ReadToEnd();
            }
            else
            {
                if (!File.Exists(filename))
                {
                    Console.Error.WriteLine($"Error: File '{filename}' not found");
                    return 1;
                }
                code = File.ReadAllText(filename);
            }
            
            // Execute the script using the interpreter
            var interpreter = new LuaInterpreter();
            var result = interpreter.ExecuteCode(code);
            
            // Only output if there's a return value and verbose mode is on
            if (verbose && result.Length > 0 && result[0] != LuaValue.Nil)
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

    public static int CompileFile(CompileOptions options)
    {
        try
        {
            if (!File.Exists(options.InputFile))
            {
                Console.Error.WriteLine($"Error: Input file '{options.InputFile}' not found");
                return 1;
            }

            // Parse Lua code
            var code = File.ReadAllText(options.InputFile!);
            IList<Statement> ast;
            
            try
            {
                var fsharpList = FLua.Parser.ParserHelper.ParseStringWithFileName(code, options.InputFile!);
                ast = Microsoft.FSharp.Collections.ListModule.ToArray(fsharpList);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Parse Error: {ex.Message}");
                return 1;
            }

            // Use Roslyn backend for better debugging
            var compiler = new RoslynLuaCompiler();
            var compilerOptions = new CompilerOptions(
                OutputPath: options.OutputFile!,
                Target: options.Target,
                Optimization: options.Optimization,
                IncludeDebugInfo: options.IncludeDebugInfo,
                AssemblyName: options.AssemblyName,
                References: options.References
            );

            var result = compiler.Compile(ast, compilerOptions);

            if (!result.Success)
            {
                Console.Error.WriteLine("Compilation failed:");
                if (result.Errors != null)
                {
                    foreach (var error in result.Errors)
                    {
                        Console.Error.WriteLine($"  {error}");
                    }
                }
                return 1;
            }

            Console.WriteLine($"Successfully compiled '{options.InputFile}' to '{options.OutputFile}'");
            Console.WriteLine($"Target: {options.Target}");
            Console.WriteLine($"Backend: {compiler.BackendName}");
            
            // Display warnings if any
            if (result.Warnings != null && result.Warnings.Any())
            {
                Console.WriteLine();
                Console.WriteLine("Warnings:");
                foreach (var warning in result.Warnings)
                {
                    Console.WriteLine($"  {warning}");
                }
            }
            
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Compilation Error: {ex.Message}");
            return 1;
        }
    }
}