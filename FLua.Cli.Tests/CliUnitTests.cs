using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace FLua.Cli.Tests
{
    /// <summary>
    /// Unit tests for the FLua CLI application focused on individual command functionality.
    /// These tests verify that CLI commands work correctly using the built executable.
    /// </summary>
    [TestClass]
    public class CliUnitTests
    {
        private const int TimeoutMs = 5000; // 5 seconds timeout for most operations
        private string _testDataDir = null!;

        [TestInitialize]
        public void Setup()
        {
            _testDataDir = Path.Combine(Path.GetTempPath(), "flua-cli-unit-tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDataDir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testDataDir))
            {
                try
                {
                    Directory.Delete(_testDataDir, recursive: true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [TestMethod]
        public async Task Cli_HelpCommand_ShowsUsage()
        {
            var result = await RunCliAsync("--help");
            
            // Help output typically goes to stderr for CommandLineParser library
            var output = result.StdErr + result.StdOut;
            Assert.IsTrue(output.Contains("run") || output.Contains("Execute"), 
                $"Help should show available commands. Actual: {output}");
        }

        [TestMethod]
        public async Task Cli_VersionCommand_ShowsVersion()
        {
            var result = await RunCliAsync("--version");
            
            var output = result.StdErr + result.StdOut;
            Assert.IsTrue(output.Contains("flua") || output.Contains("1.0"), 
                $"Version should show program info. Actual: {output}");
        }

        [TestMethod]
        public async Task Cli_SimpleScript_ExecutesCorrectly()
        {
            // Create a simple test script
            var scriptPath = Path.Combine(_testDataDir, "test.lua");
            await File.WriteAllTextAsync(scriptPath, "print('Hello CLI Test!')");

            var result = await RunCliAsync($"run \"{scriptPath}\"");

            Assert.AreEqual(0, result.ExitCode, $"CLI should succeed. Error: {result.StdErr}");
            Assert.IsTrue(result.StdOut.Contains("Hello CLI Test!"), 
                $"Should execute script. Output: {result.StdOut}");
        }

        [TestMethod]
        public async Task Cli_ArithmeticScript_WorksCorrectly()
        {
            // Test the original arithmetic operations that were failing in REPL
            var scriptPath = Path.Combine(_testDataDir, "arithmetic.lua");
            await File.WriteAllTextAsync(scriptPath, @"
print('Testing arithmetic:')
print('9 + 8 =', 9 + 8)
print('10 - 3 =', 10 - 3)
print('2 * 5 =', 2 * 5)
");

            var result = await RunCliAsync($"run \"{scriptPath}\"");

            Assert.AreEqual(0, result.ExitCode, $"CLI should succeed. Error: {result.StdErr}");
            Assert.IsTrue(result.StdOut.Contains("9 + 8 =\t17"), "Should compute 9 + 8");
            Assert.IsTrue(result.StdOut.Contains("10 - 3 =\t7"), "Should compute 10 - 3");
            Assert.IsTrue(result.StdOut.Contains("2 * 5 =\t10"), "Should compute 2 * 5");
        }

        [TestMethod]
        public async Task Cli_NonExistentFile_ReturnsError()
        {
            var result = await RunCliAsync("run nonexistent.lua");

            Assert.AreNotEqual(0, result.ExitCode, "CLI should fail for non-existent file");
            Assert.IsTrue(result.StdErr.Contains("not found") || result.StdErr.Contains("Error"), 
                $"Should show error message. Error: {result.StdErr}");
        }

        [TestMethod]
        public async Task Cli_SyntaxErrorScript_ReturnsError()
        {
            var scriptPath = Path.Combine(_testDataDir, "syntax_error.lua");
            await File.WriteAllTextAsync(scriptPath, "print('unclosed string");

            var result = await RunCliAsync($"run \"{scriptPath}\"");

            Assert.AreNotEqual(0, result.ExitCode, "CLI should fail for syntax errors");
            Assert.IsTrue(result.StdErr.Contains("Error"), 
                $"Should show syntax error. Error: {result.StdErr}");
        }

        [TestMethod]
        public async Task Cli_VerboseMode_ShowsReturnValue()
        {
            var scriptPath = Path.Combine(_testDataDir, "return_test.lua");
            await File.WriteAllTextAsync(scriptPath, "return 42");

            var result = await RunCliAsync($"run -v \"{scriptPath}\"");

            Assert.AreEqual(0, result.ExitCode, $"CLI should succeed. Error: {result.StdErr}");
            Assert.IsTrue(result.StdOut.Contains("returned: 42"), 
                $"Verbose mode should show return value. Output: {result.StdOut}");
        }

        [TestMethod]
        public async Task Cli_LocalVariables_WorkCorrectly()
        {
            var scriptPath = Path.Combine(_testDataDir, "locals.lua");
            await File.WriteAllTextAsync(scriptPath, @"
local x = 10
local y = 20
local sum = x + y
print('Sum:', sum)
");

            var result = await RunCliAsync($"run \"{scriptPath}\"");

            Assert.AreEqual(0, result.ExitCode, $"CLI should succeed. Error: {result.StdErr}");
            Assert.IsTrue(result.StdOut.Contains("Sum:\t30"), 
                $"Should calculate local variables. Output: {result.StdOut}");
        }

        [TestMethod]
        public async Task Cli_FunctionDefinition_WorksCorrectly()
        {
            var scriptPath = Path.Combine(_testDataDir, "function.lua");
            await File.WriteAllTextAsync(scriptPath, @"
function greet(name)
    return 'Hello, ' .. name .. '!'
end

print(greet('World'))
");

            var result = await RunCliAsync($"run \"{scriptPath}\"");

            Assert.AreEqual(0, result.ExitCode, $"CLI should succeed. Error: {result.StdErr}");
            Assert.IsTrue(result.StdOut.Contains("Hello, World!"), 
                $"Should define and call function. Output: {result.StdOut}");
        }

        [TestMethod]
        public async Task Cli_StringOperations_WorkCorrectly()
        {
            var scriptPath = Path.Combine(_testDataDir, "strings.lua");
            await File.WriteAllTextAsync(scriptPath, @"
local str1 = 'Hello'
local str2 = 'World'
local combined = str1 .. ' ' .. str2
print('Combined:', combined)
print('Length:', #combined)
");

            var result = await RunCliAsync($"run \"{scriptPath}\"");

            Assert.AreEqual(0, result.ExitCode, $"CLI should succeed. Error: {result.StdErr}");
            Assert.IsTrue(result.StdOut.Contains("Combined:\tHello World"), "Should concatenate strings");
            Assert.IsTrue(result.StdOut.Contains("Length:\t11"), "Should calculate string length");
        }

        [TestMethod]
        public async Task Cli_TableOperations_WorkCorrectly()
        {
            var scriptPath = Path.Combine(_testDataDir, "tables.lua");
            await File.WriteAllTextAsync(scriptPath, @"
local t = {1, 2, 3, 4, 5}
local sum = 0
for i = 1, #t do
    sum = sum + t[i]
end
print('Array sum:', sum)

local obj = {name = 'test', value = 100}
print('Object name:', obj.name)
print('Object value:', obj.value)
");

            var result = await RunCliAsync($"run \"{scriptPath}\"");

            Assert.AreEqual(0, result.ExitCode, $"CLI should succeed. Error: {result.StdErr}");
            Assert.IsTrue(result.StdOut.Contains("Array sum:\t15"), "Should sum array elements");
            Assert.IsTrue(result.StdOut.Contains("Object name:\ttest"), "Should access object property");
            Assert.IsTrue(result.StdOut.Contains("Object value:\t100"), "Should access object value");
        }

        /// <summary>
        /// Run CLI command and capture results
        /// </summary>
        private async Task<(int ExitCode, string StdOut, string StdErr)> RunCliAsync(string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project FLua.Cli --no-build -- {arguments}",
                WorkingDirectory = "/Users/bill/Repos/FLua",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            
            try
            {
                process.Start();

                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                var completed = await Task.Run(() => process.WaitForExit(TimeoutMs));
                
                if (!completed)
                {
                    process.Kill();
                    return (-1, "TIMEOUT", "Process timed out after " + TimeoutMs + "ms");
                }

                var output = await outputTask;
                var error = await errorTask;

                return (process.ExitCode, output, error);
            }
            catch (Exception ex)
            {
                return (-1, "", $"Failed to run CLI: {ex.Message}");
            }
        }
    }
}