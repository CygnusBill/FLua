using System.Diagnostics;
using FLua.Compiler;
using FLua.Hosting;

// Example: AOT (Ahead-of-Time) Compilation
// This example shows how to compile Lua scripts into standalone native executables.
// AOT compilation produces single-file executables that run without .NET installed.

Console.WriteLine("=== FLua AOT Compilation Example ===\n");

// Create a sample Lua script
var scriptDir = Path.Combine(Directory.GetCurrentDirectory(), "scripts");
Directory.CreateDirectory(scriptDir);

var scriptPath = Path.Combine(scriptDir, "fibonacci.lua");
var luaScript = @"
-- Fibonacci calculator as a standalone program
local function fibonacci(n)
    if n <= 1 then
        return n
    end
    local a, b = 0, 1
    for i = 2, n do
        a, b = b, a + b
    end
    return b
end

-- Get command line argument or use default
local n = tonumber(arg and arg[1]) or 10

print(string.format('Fibonacci sequence up to position %d:', n))
for i = 0, n do
    print(string.format('F(%d) = %d', i, fibonacci(i)))
end

print('\nDone!')
return 0  -- Exit code
";

File.WriteAllText(scriptPath, luaScript);

Console.WriteLine("Step 1: Creating Lua Script");
Console.WriteLine("---------------------------");
Console.WriteLine($"Script saved to: {scriptPath}");
Console.WriteLine($"Script size: {new FileInfo(scriptPath).Length} bytes\n");

// Compile to native executable using CLI
Console.WriteLine("Step 2: Compiling to Native Executable");
Console.WriteLine("--------------------------------------");

var outputPath = Path.Combine(scriptDir, "fibonacci");
if (OperatingSystem.IsWindows())
    outputPath += ".exe";

// Use the FLua CLI to compile
var compileProcess = new Process
{
    StartInfo = new ProcessStartInfo
    {
        FileName = "dotnet",
        Arguments = $"run --project ../../FLua.Cli -- compile \"{scriptPath}\" -t NativeAot -o \"{outputPath}\"",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        WorkingDirectory = Directory.GetCurrentDirectory()
    }
};

Console.WriteLine("Compiling (this may take 30-60 seconds for AOT)...");
var sw = Stopwatch.StartNew();

compileProcess.Start();
var output = compileProcess.StandardOutput.ReadToEnd();
var error = compileProcess.StandardError.ReadToEnd();
compileProcess.WaitForExit();

sw.Stop();

if (compileProcess.ExitCode == 0 && File.Exists(outputPath))
{
    Console.WriteLine($"✓ Compilation successful in {sw.Elapsed.TotalSeconds:F1} seconds");
    var exeInfo = new FileInfo(outputPath);
    Console.WriteLine($"✓ Executable size: {exeInfo.Length / 1024.0 / 1024.0:F1} MB");
    Console.WriteLine($"✓ Output: {outputPath}\n");
}
else
{
    Console.WriteLine($"✗ Compilation failed:");
    Console.WriteLine(output);
    Console.WriteLine(error);
    return;
}

// Run the native executable
Console.WriteLine("Step 3: Running Native Executable");
Console.WriteLine("---------------------------------");

var runProcess = new Process
{
    StartInfo = new ProcessStartInfo
    {
        FileName = outputPath,
        Arguments = "15",  // Calculate up to F(15)
        UseShellExecute = false,
        RedirectStandardOutput = true
    }
};

sw.Restart();
runProcess.Start();
var runOutput = runProcess.StandardOutput.ReadToEnd();
runProcess.WaitForExit();
sw.Stop();

Console.WriteLine($"Execution time: {sw.ElapsedMilliseconds}ms");
Console.WriteLine("\nOutput:");
Console.WriteLine(runOutput);

// Demonstrate the difference from .NET dependency
Console.WriteLine("\nStep 4: Native Executable Properties");
Console.WriteLine("------------------------------------");
Console.WriteLine("✓ No .NET runtime required");
Console.WriteLine("✓ Single file deployment");
Console.WriteLine("✓ Fast startup time");
Console.WriteLine("✓ Can be distributed standalone");
Console.WriteLine("✓ Platform-specific (need to compile for each OS/arch)");

// Show other compilation options
Console.WriteLine("\nOther AOT Compilation Examples:");
Console.WriteLine("-------------------------------");
Console.WriteLine(@"
# Compile a game script:
dotnet run --project FLua.Cli -- compile game.lua -t NativeAot -o game

# Compile a utility script:
dotnet run --project FLua.Cli -- compile utils.lua -t NativeAot -o utils

# Compile with optimization:
dotnet run --project FLua.Cli -- compile script.lua -t NativeAot -O Release

Benefits of AOT:
- Instant startup (no JIT compilation)
- Smaller memory footprint
- Better for CLI tools and utilities
- Improved security (harder to reverse engineer)
- Predictable performance

Limitations:
- Larger file size than JIT-compiled
- Platform-specific binaries
- Longer compilation time
- No runtime code generation
");

// Cleanup option
Console.WriteLine("\nPress 'C' to clean up generated files, any other key to keep them...");
var key = Console.ReadKey();
if (key.KeyChar == 'c' || key.KeyChar == 'C')
{
    File.Delete(scriptPath);
    File.Delete(outputPath);
    Directory.Delete(scriptDir, recursive: true);
    Console.WriteLine("\nFiles cleaned up.");
}
else
{
    Console.WriteLine($"\nFiles kept at: {scriptDir}");
}