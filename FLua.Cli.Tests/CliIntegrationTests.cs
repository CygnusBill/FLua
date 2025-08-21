using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace FLua.Cli.Tests
{
    /// <summary>
    /// Integration tests for the FLua CLI application.
    /// Tests the various execution modes: REPL, file execution, stdin, and compilation.
    /// </summary>
    [TestClass]
    public class CliIntegrationTests
    {
        private const string CliExecutable = "flua";
        private const int TimeoutMs = 10000; // 10 seconds timeout

        private string _testDataDir = null!;

        [TestInitialize]
        public void Setup()
        {
            _testDataDir = Path.Combine(Path.GetTempPath(), "flua-cli-tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDataDir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDataDir))
            {
                Directory.Delete(_testDataDir, recursive: true);
            }
        }

        [TestMethod]
        public async Task Cli_SimpleFileExecution_WorksCorrectly()
        {
            // Create a simple Lua script
            var scriptPath = Path.Combine(_testDataDir, "simple.lua");
            await File.WriteAllTextAsync(scriptPath, "print('Hello from CLI!')");

            var result = await RunCliCommand($"run \"{scriptPath}\"");

            Assert.AreEqual(0, result.ExitCode, $"CLI should succeed. Output: {result.Output}, Error: {result.Error}");
            Assert.IsTrue(result.Output.Contains("Hello from CLI!"), 
                $"Output should contain greeting. Actual: {result.Output}");
        }

        [TestMethod]
        public async Task Cli_ArithmeticScript_ExecutesCorrectly()
        {
            // Test the arithmetic operations that were originally failing in REPL
            var scriptPath = Path.Combine(_testDataDir, "arithmetic.lua");
            await File.WriteAllTextAsync(scriptPath, @"
local a = 9
local b = 8
local result = a + b
print('Result:', result)
print('9 + 8 =', 9 + 8)
");

            var result = await RunCliCommand($"run \"{scriptPath}\"");

            Assert.AreEqual(0, result.ExitCode, $"CLI should succeed. Error: {result.Error}");
            Assert.IsTrue(result.Output.Contains("Result:\t17"), "Should output Result: 17");
            Assert.IsTrue(result.Output.Contains("9 + 8 =\t17"), "Should output 9 + 8 = 17");
        }

        [TestMethod]
        public async Task Cli_FileNotFound_ReturnsError()
        {
            var result = await RunCliCommand("run nonexistent.lua");

            Assert.AreNotEqual(0, result.ExitCode, "CLI should fail for non-existent file");
            Assert.IsTrue(result.Error.Contains("not found"), 
                $"Error should mention file not found. Actual: {result.Error}");
        }

        [TestMethod]
        public async Task Cli_VerboseMode_ShowsAdditionalOutput()
        {
            var scriptPath = Path.Combine(_testDataDir, "return_value.lua");
            await File.WriteAllTextAsync(scriptPath, "return 42");

            var result = await RunCliCommand($"run -v \"{scriptPath}\"");

            Assert.AreEqual(0, result.ExitCode, $"CLI should succeed. Error: {result.Error}");
            Assert.IsTrue(result.Output.Contains("Script returned: 42"), 
                $"Verbose mode should show return value. Actual: {result.Output}");
        }

        [TestMethod]
        public async Task Cli_StdinInput_WorksCorrectly()
        {
            var luaCode = "print('Hello from stdin!')";
            var result = await RunCliCommandWithInput("run -", luaCode);

            Assert.AreEqual(0, result.ExitCode, $"CLI should succeed. Error: {result.Error}");
            Assert.IsTrue(result.Output.Contains("Hello from stdin!"), 
                $"Should execute stdin input. Actual: {result.Output}");
        }

        [TestMethod]
        public async Task Cli_ComplexScript_ExecutesCorrectly()
        {
            var scriptPath = Path.Combine(_testDataDir, "complex.lua");
            await File.WriteAllTextAsync(scriptPath, @"
-- Test function definitions and calls
function factorial(n)
    if n <= 1 then
        return 1
    else
        return n * factorial(n - 1)
    end
end

-- Test table operations
local numbers = {1, 2, 3, 4, 5}
local sum = 0
for i, v in ipairs(numbers) do
    sum = sum + v
end

print('Factorial of 5:', factorial(5))
print('Sum of numbers:', sum)

-- Test string concatenation
local message = 'Hello' .. ' ' .. 'World'
print('Message:', message)
");

            var result = await RunCliCommand($"run \"{scriptPath}\"");

            Assert.AreEqual(0, result.ExitCode, $"CLI should succeed. Error: {result.Error}");
            Assert.IsTrue(result.Output.Contains("Factorial of 5:\t120"), "Should calculate factorial");
            Assert.IsTrue(result.Output.Contains("Sum of numbers:\t15"), "Should sum array");
            Assert.IsTrue(result.Output.Contains("Message:\tHello World"), "Should concatenate strings");
        }

        [TestMethod]
        public async Task Cli_LegacyFileExecution_WorksCorrectly()
        {
            // Test legacy mode: just filename as argument
            var scriptPath = Path.Combine(_testDataDir, "legacy.lua");
            await File.WriteAllTextAsync(scriptPath, "print('Legacy mode works!')");

            var result = await RunCliCommand($"\"{scriptPath}\"");

            Assert.AreEqual(0, result.ExitCode, $"CLI should succeed. Error: {result.Error}");
            Assert.IsTrue(result.Output.Contains("Legacy mode works!"), 
                $"Legacy mode should work. Actual: {result.Output}");
        }

        [TestMethod]
        public async Task Cli_SyntaxError_ReturnsError()
        {
            var scriptPath = Path.Combine(_testDataDir, "syntax_error.lua");
            await File.WriteAllTextAsync(scriptPath, "print('unclosed string");

            var result = await RunCliCommand($"run \"{scriptPath}\"");

            Assert.AreNotEqual(0, result.ExitCode, "CLI should fail for syntax errors");
            Assert.IsTrue(result.Error.Contains("Error"), 
                $"Should show error message. Actual: {result.Error}");
        }

        [TestMethod]
        public async Task Cli_RuntimeError_ReturnsError()
        {
            var scriptPath = Path.Combine(_testDataDir, "runtime_error.lua");
            await File.WriteAllTextAsync(scriptPath, @"
local function divide(a, b)
    if b == 0 then
        error('Division by zero!')
    end
    return a / b
end

print(divide(10, 0))
");

            var result = await RunCliCommand($"run \"{scriptPath}\"");

            Assert.AreNotEqual(0, result.ExitCode, "CLI should fail for runtime errors");
            Assert.IsTrue(result.Error.Contains("Error"), 
                $"Should show error message. Actual: {result.Error}");
        }

        [TestMethod]
        public async Task Cli_NoArguments_StartsRepl()
        {
            // This test would normally start REPL, but we'll test a quick exit
            var result = await RunCliCommandWithInput("", ".quit\n", timeoutMs: 3000);

            // Note: This might be challenging to test properly due to REPL's interactive nature
            // We expect it to start but then exit quickly
            Assert.IsTrue(result.ExitCode == 0 || result.Output.Contains("FLua Interactive REPL"), 
                $"Should start REPL or show REPL output. Exit: {result.ExitCode}, Output: {result.Output}");
        }

        [TestMethod]
        public async Task Cli_HelpCommand_ShowsUsage()
        {
            var result = await RunCliCommand("--help");

            // CommandLineParser library should show help
            Assert.IsTrue(result.Output.Contains("run") && result.Output.Contains("repl"), 
                $"Help should show available commands. Actual: {result.Output}");
        }

        /// <summary>
        /// Helper method to run CLI commands and capture output
        /// </summary>
        private async Task<(int ExitCode, string Output, string Error)> RunCliCommand(
            string arguments, 
            int timeoutMs = TimeoutMs)
        {
            return await RunCliCommandWithInput(arguments, null, timeoutMs);
        }

        /// <summary>
        /// Helper method to run CLI commands with input and capture output
        /// </summary>
        private async Task<(int ExitCode, string Output, string Error)> RunCliCommandWithInput(
            string arguments, 
            string? input = null, 
            int timeoutMs = TimeoutMs)
        {
            // Try to find published executable first, fall back to dotnet run
            var publishedExe = "/Users/bill/Repos/FLua/FLua.Cli/bin/Release/net8.0/osx-arm64/publish/flua";
            
            ProcessStartInfo startInfo;
            if (File.Exists(publishedExe))
            {
                // Use published executable directly
                startInfo = new ProcessStartInfo
                {
                    FileName = publishedExe,
                    Arguments = arguments,
                    WorkingDirectory = "/Users/bill/Repos/FLua",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }
            else
            {
                // Fall back to dotnet run
                startInfo = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"run --project FLua.Cli -- {arguments}",
                    WorkingDirectory = "/Users/bill/Repos/FLua",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }

            using var process = new Process { StartInfo = startInfo };
            
            var outputTask = Task<string>.Factory.StartNew(() => "");
            var errorTask = Task<string>.Factory.StartNew(() => "");

            try
            {
                process.Start();

                // Start reading output and error streams
                outputTask = process.StandardOutput.ReadToEndAsync();
                errorTask = process.StandardError.ReadToEndAsync();

                // Send input if provided
                if (!string.IsNullOrEmpty(input))
                {
                    await process.StandardInput.WriteAsync(input);
                    process.StandardInput.Close();
                }

                // Wait for completion with timeout
                var completed = await Task.Run(() => process.WaitForExit(timeoutMs));
                
                if (!completed)
                {
                    process.Kill();
                    return (-1, "Process timed out", "Process timed out");
                }

                var output = await outputTask;
                var error = await errorTask;

                return (process.ExitCode, output, error);
            }
            catch (Exception ex)
            {
                return (-1, "", $"Failed to run process: {ex.Message}");
            }
        }
    }
}