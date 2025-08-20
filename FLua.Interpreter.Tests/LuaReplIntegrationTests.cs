using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Interpreter;
using System;
using System.IO;

namespace FLua.Interpreter.Tests
{
    /// <summary>
    /// Integration tests for the LuaRepl to ensure arithmetic and basic operations work correctly.
    /// These tests specifically target the REPL execution path that was not covered by other tests.
    /// </summary>
    [TestClass]
    public class LuaReplIntegrationTests
    {
        private LuaRepl _repl = null!;
        private StringWriter _output = null!;
        private StringReader _input = null!;
        private TextWriter _originalOut = null!;
        private TextReader _originalIn = null!;

        [TestInitialize]
        public void Setup()
        {
            _repl = new LuaRepl();
            _output = new StringWriter();
            _originalOut = Console.Out;
            _originalIn = Console.In;
            Console.SetOut(_output);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Console.SetOut(_originalOut);
            Console.SetIn(_originalIn);
            _output?.Dispose();
            _input?.Dispose();
        }

        [TestMethod]
        public void Repl_SimpleArithmetic_EvaluatesCorrectly()
        {
            // Test the exact scenario that was failing: raw arithmetic expressions
            var testInput = "9+8\n.quit\n";
            _input = new StringReader(testInput);
            Console.SetIn(_input);

            _repl.Run();

            var output = _output.ToString();
            Assert.IsTrue(output.Contains("= 17"), 
                $"Expected '= 17' in output, but got: {output}");
        }

        [TestMethod]
        public void Repl_ArithmeticAssignment_WorksCorrectly()
        {
            // Test the assignment scenario that was also failing
            var testInput = "x = 9\ny = 5\nz = x + y\nz\n.quit\n";
            _input = new StringReader(testInput);
            Console.SetIn(_input);

            _repl.Run();

            var output = _output.ToString();
            Assert.IsTrue(output.Contains("= 14"), 
                $"Expected '= 14' in output, but got: {output}");
        }

        [TestMethod]
        public void Repl_MultipleArithmeticOperations_AllWork()
        {
            // Test various arithmetic operations
            var testInput = "10 - 3\n12 * 2\n15 / 3\n2 ^ 3\n.quit\n";
            _input = new StringReader(testInput);
            Console.SetIn(_input);

            _repl.Run();

            var output = _output.ToString();
            Assert.IsTrue(output.Contains("= 7"), "Subtraction failed");
            Assert.IsTrue(output.Contains("= 24"), "Multiplication failed");
            Assert.IsTrue(output.Contains("= 5"), "Division failed");
            Assert.IsTrue(output.Contains("= 8"), "Exponentiation failed");
        }

        [TestMethod]
        public void Repl_StringConcatenation_WorksCorrectly()
        {
            // Test string operations that also use the expression tree generator
            var testInput = "'Hello' .. ' ' .. 'World'\n.quit\n";
            _input = new StringReader(testInput);
            Console.SetIn(_input);

            _repl.Run();

            var output = _output.ToString();
            Assert.IsTrue(output.Contains("= Hello World"), 
                $"Expected '= Hello World' in output, but got: {output}");
        }

        [TestMethod]
        public void Repl_BooleanExpressions_EvaluateCorrectly()
        {
            // Test boolean operations
            var testInput = "5 > 3\n2 == 2\n4 ~= 5\n.quit\n";
            _input = new StringReader(testInput);
            Console.SetIn(_input);

            _repl.Run();

            var output = _output.ToString();
            Assert.IsTrue(output.Contains("= true"), "Boolean expressions should evaluate to true");
        }

        [TestMethod]
        public void Repl_MixedStatementsAndExpressions_HandleCorrectly()
        {
            // Test the complex scenario: mix of assignments and expressions
            var testInput = "local a = 10\nlocal b = 20\na + b\nprint('Result:', a + b)\na * 2\n.quit\n";
            _input = new StringReader(testInput);
            Console.SetIn(_input);

            _repl.Run();

            var output = _output.ToString();
            Assert.IsTrue(output.Contains("= 30"), "Should show a + b = 30");
            Assert.IsTrue(output.Contains("= 20"), "Should show a * 2 = 20");
            Assert.IsTrue(output.Contains("Result:\t30"), "Print should work");
        }

        [TestMethod]
        public void Repl_ErrorHandling_ShowsAppropriateMessages()
        {
            // Test that errors don't crash the REPL
            var testInput = "undefined_variable\n5 + 5\n.quit\n";
            _input = new StringReader(testInput);
            Console.SetIn(_input);

            _repl.Run();

            var output = _output.ToString();
            // Should show error for undefined variable but still work for 5 + 5
            Assert.IsTrue(output.Contains("❌ Error") || output.Contains("Error"), "Should show error message");
            Assert.IsTrue(output.Contains("= 10"), "Should still evaluate 5 + 5 correctly");
        }
    }
}