using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FLua.Compiler;

namespace FLua.Cli;

public abstract class CommandOptions
{
    public abstract string CommandName { get; }
    public abstract string HelpText { get; }
}

public class RunOptions : CommandOptions
{
    public override string CommandName => "run";
    public override string HelpText => "Execute a Lua script file";
    
    public string? File { get; set; }
    public bool Verbose { get; set; }
}

public class ReplOptions : CommandOptions
{
    public override string CommandName => "repl";
    public override string HelpText => "Start interactive REPL mode";
}

public class CompileOptions : CommandOptions
{
    public override string CommandName => "compile";
    public override string HelpText => "Compile Lua script to executable";
    
    public string? InputFile { get; set; }
    public string? OutputFile { get; set; }
    public CompilationTarget Target { get; set; } = CompilationTarget.Library;
    public OptimizationLevel Optimization { get; set; } = OptimizationLevel.Release;
    public bool IncludeDebugInfo { get; set; }
    public string? AssemblyName { get; set; }
    public List<string> References { get; set; } = new();
}

public class ParseResult<T> where T : CommandOptions
{
    public bool Success { get; init; }
    public T? Options { get; init; }
    public string? ErrorMessage { get; init; }
    public bool ShowHelp { get; init; }
    
    public static ParseResult<T> FromSuccess(T options) => new() { Success = true, Options = options };
    public static ParseResult<T> FromError(string error) => new() { Success = false, ErrorMessage = error };
    public static ParseResult<T> FromHelp() => new() { Success = false, ShowHelp = true };
}

public static class SimpleCommandLineParser
{
    public static ParseResult<T> Parse<T>(string[] args) where T : CommandOptions, new()
    {
        var options = new T();
        
        // Simple approach: just parse the args array directly
        var positionalArgs = new List<string>();
        
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            
            // Skip command name if it matches
            if (i == 0 && arg == options.CommandName)
                continue;
                
            // Handle help flags
            if (arg == "--help" || arg == "-h")
            {
                return ParseResult<T>.FromHelp();
            }
            
            // Handle options for each command type
            switch (options)
            {
                case RunOptions runOpts:
                    if (arg == "-v" || arg == "--verbose")
                    {
                        runOpts.Verbose = true;
                    }
                    else if (arg.StartsWith('-'))
                    {
                        return ParseResult<T>.FromError($"Unknown option: {arg}");
                    }
                    else
                    {
                        positionalArgs.Add(arg);
                    }
                    break;
                    
                case CompileOptions compileOpts:
                    if (arg == "-o" || arg == "--output")
                    {
                        if (i + 1 >= args.Length)
                            return ParseResult<T>.FromError($"Option {arg} requires a value");
                        compileOpts.OutputFile = args[++i];
                    }
                    else if (arg == "-t" || arg == "--target")
                    {
                        if (i + 1 >= args.Length)
                            return ParseResult<T>.FromError($"Option {arg} requires a value");
                        if (!Enum.TryParse<CompilationTarget>(args[++i], true, out var target))
                            return ParseResult<T>.FromError($"Invalid target: {args[i]}");
                        compileOpts.Target = target;
                    }
                    else if (arg == "--optimization")
                    {
                        if (i + 1 >= args.Length)
                            return ParseResult<T>.FromError($"Option {arg} requires a value");
                        if (!Enum.TryParse<OptimizationLevel>(args[++i], true, out var optimization))
                            return ParseResult<T>.FromError($"Invalid optimization level: {args[i]}");
                        compileOpts.Optimization = optimization;
                    }
                    else if (arg == "--debug")
                    {
                        compileOpts.IncludeDebugInfo = true;
                    }
                    else if (arg == "--name")
                    {
                        if (i + 1 >= args.Length)
                            return ParseResult<T>.FromError($"Option {arg} requires a value");
                        compileOpts.AssemblyName = args[++i];
                    }
                    else if (arg == "-r" || arg == "--reference")
                    {
                        if (i + 1 >= args.Length)
                            return ParseResult<T>.FromError($"Option {arg} requires a value");
                        compileOpts.References.Add(args[++i]);
                    }
                    else if (arg.StartsWith('-'))
                    {
                        return ParseResult<T>.FromError($"Unknown option: {arg}");
                    }
                    else
                    {
                        positionalArgs.Add(arg);
                    }
                    break;
                    
                case ReplOptions:
                    if (arg.StartsWith('-'))
                    {
                        return ParseResult<T>.FromError($"Unknown option: {arg}");
                    }
                    else
                    {
                        positionalArgs.Add(arg);
                    }
                    break;
            }
        }
        
        // Validate and set positional arguments
        var validationResult = ValidateAndSetRequiredValues(options, positionalArgs);
        if (!validationResult.Success)
        {
            return validationResult;
        }
        
        return ParseResult<T>.FromSuccess(options);
    }
    
    private static ParseResult<T> ValidateAndSetRequiredValues<T>(T options, List<string> values) where T : CommandOptions
    {
        switch (options)
        {
            case RunOptions runOpts:
                if (values.Count == 0)
                {
                    return ParseResult<T>.FromError("File argument is required");
                }
                if (values.Count > 1)
                {
                    return ParseResult<T>.FromError($"Too many arguments. Expected file path, got: {string.Join(", ", values)}");
                }
                runOpts.File = values[0];
                break;
                
            case CompileOptions compileOpts:
                if (values.Count == 0)
                {
                    return ParseResult<T>.FromError("Input file argument is required");
                }
                if (values.Count > 1)
                {
                    return ParseResult<T>.FromError($"Too many arguments. Expected input file, got: {string.Join(", ", values)}");
                }
                compileOpts.InputFile = values[0];
                
                // Validate required options
                if (string.IsNullOrEmpty(compileOpts.OutputFile))
                {
                    return ParseResult<T>.FromError("Output file (-o/--output) is required");
                }
                break;
                
            case ReplOptions:
                if (values.Count > 0)
                {
                    return ParseResult<T>.FromError($"REPL command does not accept arguments: {string.Join(", ", values)}");
                }
                break;
        }
        
        return ParseResult<T>.FromSuccess(options);
    }
    
    public static void ShowHelp(string programName, Type? commandType = null)
    {
        if (commandType == null)
        {
            // Show general help
            Console.WriteLine($"{programName} 1.0.0+531e68f3926245f542f08d3289cd1e610ed90d8d");
            Console.WriteLine("Copyright (C) 2025 flua");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine($"  {programName} <command> [options]");
            Console.WriteLine($"  {programName} <file>                 # Legacy: run file directly");
            Console.WriteLine($"  {programName}                       # Legacy: start REPL");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  run        Execute a Lua script file");
            Console.WriteLine("  repl       Start interactive REPL mode");
            Console.WriteLine("  compile    Compile Lua script to executable");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h, --help    Show help information");
            Console.WriteLine("  --version     Show version information");
        }
        else if (commandType == typeof(RunOptions))
        {
            Console.WriteLine("Usage: run [options] <file>");
            Console.WriteLine();
            Console.WriteLine("Execute a Lua script file");
            Console.WriteLine();
            Console.WriteLine("Arguments:");
            Console.WriteLine("  <file>        Lua script file to execute (use '-' for stdin)");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -v, --verbose Show verbose output including return values");
            Console.WriteLine("  -h, --help    Show this help");
        }
        else if (commandType == typeof(ReplOptions))
        {
            Console.WriteLine("Usage: repl");
            Console.WriteLine();
            Console.WriteLine("Start interactive REPL mode");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h, --help    Show this help");
        }
        else if (commandType == typeof(CompileOptions))
        {
            Console.WriteLine("Usage: compile [options] <input-file>");
            Console.WriteLine();
            Console.WriteLine("Compile Lua script to executable");
            Console.WriteLine();
            Console.WriteLine("Arguments:");
            Console.WriteLine("  <input-file>           Input Lua script file");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -o, --output <file>    Output file path (required)");
            Console.WriteLine("  -t, --target <target>  Compilation target (library, console, nativeaot) [default: library]");
            Console.WriteLine("  --optimization <level> Optimization level (debug, release) [default: release]");
            Console.WriteLine("  --debug                Include debug information");
            Console.WriteLine("  --name <name>          Assembly name");
            Console.WriteLine("  -r, --reference <ref>  Additional assembly reference (can be used multiple times)");
            Console.WriteLine("  -h, --help             Show this help");
        }
    }
    
    public static void ShowVersion()
    {
        Console.WriteLine("flua 1.0.0+531e68f3926245f542f08d3289cd1e610ed90d8d");
    }
}

public static class CommandLineDispatcher
{
    public static int Execute(string[] args)
    {
        // Handle empty args = REPL
        if (args.Length == 0)
        {
            return ExecuteRepl();
        }
        
        // Handle version flag
        if (args.Length == 1 && (args[0] == "--version" || args[0] == "-V"))
        {
            SimpleCommandLineParser.ShowVersion();
            return 0;
        }
        
        // Handle global help
        if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h"))
        {
            SimpleCommandLineParser.ShowHelp("flua");
            return 0;
        }
        
        // Handle piped input
        if (Console.IsInputRedirected && args.Length == 0)
        {
            return ExecuteStdin();
        }
        
        // Handle legacy single file execution
        if (args.Length == 1 && !args[0].StartsWith('-') && File.Exists(args[0]))
        {
            return ExecuteRun(new RunOptions { File = args[0], Verbose = false });
        }
        
        // Parse commands
        var command = args.Length > 0 ? args[0] : "";
        
        return command switch
        {
            "run" => HandleRunCommand(args),
            "repl" => HandleReplCommand(args),
            "compile" => HandleCompileCommand(args),
            _ => HandleUnknownCommand(command)
        };
    }
    
    private static int HandleRunCommand(string[] args)
    {
        var result = SimpleCommandLineParser.Parse<RunOptions>(args);
        
        if (result.ShowHelp)
        {
            SimpleCommandLineParser.ShowHelp("flua run", typeof(RunOptions));
            return 0;
        }
        
        if (!result.Success)
        {
            Console.Error.WriteLine($"Error: {result.ErrorMessage}");
            Console.Error.WriteLine("Use 'flua run --help' for usage information.");
            return 1;
        }
        
        return ExecuteRun(result.Options!);
    }
    
    private static int HandleReplCommand(string[] args)
    {
        var result = SimpleCommandLineParser.Parse<ReplOptions>(args);
        
        if (result.ShowHelp)
        {
            SimpleCommandLineParser.ShowHelp("flua repl", typeof(ReplOptions));
            return 0;
        }
        
        if (!result.Success)
        {
            Console.Error.WriteLine($"Error: {result.ErrorMessage}");
            Console.Error.WriteLine("Use 'flua repl --help' for usage information.");
            return 1;
        }
        
        return ExecuteRepl();
    }
    
    private static int HandleCompileCommand(string[] args)
    {
        var result = SimpleCommandLineParser.Parse<CompileOptions>(args);
        
        if (result.ShowHelp)
        {
            SimpleCommandLineParser.ShowHelp("flua compile", typeof(CompileOptions));
            return 0;
        }
        
        if (!result.Success)
        {
            Console.Error.WriteLine($"Error: {result.ErrorMessage}");
            Console.Error.WriteLine("Use 'flua compile --help' for usage information.");
            return 1;
        }
        
        return ExecuteCompile(result.Options!);
    }
    
    private static int HandleUnknownCommand(string command)
    {
        if (string.IsNullOrEmpty(command))
        {
            Console.Error.WriteLine("Error: No command specified.");
        }
        else
        {
            Console.Error.WriteLine($"Error: Unknown command '{command}'.");
        }
        
        Console.Error.WriteLine("Use 'flua --help' to see available commands.");
        return 1;
    }
    
    // These methods will be implemented to call the existing logic
    private static int ExecuteRun(RunOptions options) => Program.RunFile(options.File!, options.Verbose);
    private static int ExecuteRepl() => Program.RunRepl();
    private static int ExecuteStdin() => Program.RunStdin();
    private static int ExecuteCompile(CompileOptions options) => Program.CompileFile(options);
}