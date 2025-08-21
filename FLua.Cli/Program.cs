using System;
using System.IO;
using System.Collections.Generic;
using CommandLine;
using FLua.Interpreter;
using FLua.Runtime;
using FLua.Compiler;
using FLua.Ast;

namespace FLua.Cli;

[Verb("run", HelpText = "Execute a Lua script file")]
public class RunOptions
{
    public RunOptions() { }
    
    [Value(0, MetaName = "file", HelpText = "Lua script file to execute", Required = true)]
    public string? File { get; set; }

    [Option('v', "verbose", HelpText = "Show verbose output")]
    public bool Verbose { get; set; }
}

[Verb("repl", HelpText = "Start interactive REPL mode")]
public class ReplOptions
{
    public ReplOptions() { }
}

[Verb("compile", HelpText = "Compile Lua script to executable")]
public class CompileOptions
{
    public CompileOptions() { }
    
    [Value(0, MetaName = "input", HelpText = "Input Lua script file", Required = true)]
    public string? InputFile { get; set; }

    [Option('o', "output", HelpText = "Output file path", Required = true)]
    public string? OutputFile { get; set; }

    [Option('t', "target", HelpText = "Compilation target (library, console, nativeaot)", Default = CompilationTarget.Library)]
    public CompilationTarget Target { get; set; }

    [Option("optimization", HelpText = "Optimization level (debug, release)", Default = OptimizationLevel.Release)]
    public OptimizationLevel Optimization { get; set; }

    [Option("debug", HelpText = "Include debug information")]
    public bool IncludeDebugInfo { get; set; }

    [Option("name", HelpText = "Assembly name")]
    public string? AssemblyName { get; set; }

    [Option('r', "reference", HelpText = "Additional assembly references")]
    public IEnumerable<string>? References { get; set; }

}

class Program
{
    static int Main(string[] args)
    {
        // Handle legacy behavior: no args = REPL, single file = run
        if (args.Length == 0)
        {
            return RunRepl();
        }

        // Check for piped input
        if (Console.IsInputRedirected && args.Length == 0)
        {
            return RunStdin();
        }

        // Handle single file execution (legacy mode)
        if (args.Length == 1 && !args[0].StartsWith('-') && File.Exists(args[0]))
        {
            return RunFile(args[0], verbose: false);
        }

        // Parse command line with verbs
        return CommandLine.Parser.Default.ParseArguments<RunOptions, ReplOptions, CompileOptions>(args)
            .MapResult(
                (RunOptions opts) => RunFile(opts.File!, opts.Verbose),
                (ReplOptions opts) => RunRepl(),
                (CompileOptions opts) => CompileFile(opts),
                errs => 1
            );
    }

    static int RunRepl()
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

    static int RunStdin()
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

    static int RunFile(string filename, bool verbose)
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

    static int CompileFile(CompileOptions options)
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