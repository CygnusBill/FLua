using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using FLua.Parser;
using FLua.Runtime;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Collections;

namespace FLua.Interpreter
{
    public enum OutputBehavior
    {
        NoOutput,              // Assignments, declarations, etc.
        ShowNil,               // Side-effect functions like print
        EvaluateAsExpression,  // Simple expressions
        EvaluateAsFunctionCall // Pure function calls
    }

    /// <summary>
    /// A sophisticated REPL (Read-Eval-Print Loop) for the Lua interpreter with multi-line support
    /// </summary>
    public class LuaRepl
    {
        private readonly LuaInterpreter _interpreter;
        private readonly TextReader _input;
        private readonly TextWriter _output;
        private readonly bool _exitOnQuit;
        private string? _incompleteInput;
        
        public LuaRepl() : this(Console.In, Console.Out, exitOnQuit: true)
        {
        }
        
        public LuaRepl(TextReader input, TextWriter output, bool exitOnQuit = false)
        {
            _interpreter = new LuaInterpreter();
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _exitOnQuit = exitOnQuit;
            _incompleteInput = null;
        }
        
        /// <summary>
        /// Starts the REPL
        /// </summary>
        public void Run()
        {
            PrintBanner();
            
            while (true)
            {
                // Show appropriate prompt based on whether we're continuing input
                string prompt = _incompleteInput != null ? "  >> " : "lua> ";
                _output.Write(prompt);
                string? line = _input.ReadLine();
                
                // Handle end of input (null when piped input ends or Ctrl+D)
                if (line == null)
                {
                    if (_incompleteInput != null)
                    {
                        _output.WriteLine();
                        _output.WriteLine("❌ Incomplete input at end of stream");
                    }
                    break;
                }
                
                // Handle empty lines
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (_incompleteInput == null)
                    {
                        continue;
                    }
                    // For incomplete input, empty line might signal completion
                    // Let's try to parse what we have so far
                }
                
                // Handle REPL commands
                if (_incompleteInput == null && HandleReplCommands(line))
                {
                    // Check if it was a quit command and we're not supposed to exit
                    if (!_exitOnQuit && (line.Trim().ToLowerInvariant() == ".quit" || line.Trim().ToLowerInvariant() == ".exit"))
                    {
                        break;
                    }
                    continue;
                }
                
                // Handle explicit multi-line continuation with backslash
                if (line.TrimEnd().EndsWith("\\"))
                {
                    string lineWithoutBackslash = line.TrimEnd().TrimEnd('\\');
                    _incompleteInput = _incompleteInput != null 
                        ? _incompleteInput + "\n" + lineWithoutBackslash
                        : lineWithoutBackslash;
                    continue;
                }
                
                // Build the complete input
                string inputToProcess = _incompleteInput != null 
                    ? _incompleteInput + "\n" + line
                    : line;
                
                // Check if this input needs continuation
                if (NeedsContinuation(inputToProcess))
                {
                    _incompleteInput = inputToProcess;
                    continue;
                }
                else
                {
                    // Input is complete, evaluate it
                    _incompleteInput = null;
                    EvaluateInput(inputToProcess);
                }
            }
            
            _output.WriteLine("Goodbye! 👋");
        }
        
        private void PrintBanner()
        {
            _output.WriteLine();
            _output.WriteLine("🚀 FLua Interactive REPL v2.1 (C#)");
            _output.WriteLine("====================================");
            _output.WriteLine("C# implementation of Lua interpreter");
            _output.WriteLine("✨ Now with full module system support!");
            _output.WriteLine();
            _output.WriteLine("Commands:");
            _output.WriteLine("  .help     - Show help");
            _output.WriteLine("  .quit     - Exit the REPL");
            _output.WriteLine("  .clear    - Clear screen");
            _output.WriteLine();
            _output.WriteLine("Enter Lua expressions or statements:");
            _output.WriteLine();
        }
        
        private bool HandleReplCommands(string input)
        {
            switch (input.Trim().ToLowerInvariant())
            {
                case ".quit":
                case ".exit":
                    _output.WriteLine("Goodbye! 👋");
                    if (_exitOnQuit)
                    {
                        Environment.Exit(0);
                    }
                    return true;
                
                case ".help":
                    PrintHelp();
                    return true;
                
                case ".clear":
                    // Only clear if we're using the actual Console, not for testing
                    if (_output == Console.Out)
                    {
                        Console.Clear();
                    }
                    PrintBanner();
                    return true;
                
                default:
                    return false; // Not a REPL command
            }
        }
        
        private void PrintHelp()
        {
            _output.WriteLine();
            _output.WriteLine("FLua REPL Help:");
            _output.WriteLine("===============");
            _output.WriteLine();
            _output.WriteLine("🔹 Expressions (return values):");
            _output.WriteLine("   > 1 + 2 * 3");
            _output.WriteLine("   > \"hello\" .. \" world\"");
            _output.WriteLine();
            _output.WriteLine("🔹 Statements (no return):");
            _output.WriteLine("   > local x = 42");
            _output.WriteLine("   > print(\"Hello, World!\")");
            _output.WriteLine("   > x = x + 1");
            _output.WriteLine();
            _output.WriteLine("🔹 Multi-line (automatic detection):");
            _output.WriteLine("   > local function factorial(n)");
            _output.WriteLine("   >>   if n <= 1 then return 1");
            _output.WriteLine("   >>   else return n * factorial(n-1) end");
            _output.WriteLine("   >> end");
            _output.WriteLine("   >>");
            _output.WriteLine();
            _output.WriteLine("🔹 Built-in functions:");
            _output.WriteLine("   print(), type(), tostring(), tonumber(), pairs(), ipairs()");
            _output.WriteLine("   pcall(), error(), setmetatable(), getmetatable()");
            _output.WriteLine();
            _output.WriteLine("🔹 Module System (✨ NEW):");
            _output.WriteLine("   📦 require()    - Load modules: require('math'), require('string')");
            _output.WriteLine("   📚 package.*   - loaded, path, searchers, searchpath()");
            _output.WriteLine("   📋 Examples:   local math = require('math')");
            _output.WriteLine("                  local str = require('string')");
            _output.WriteLine();
            _output.WriteLine("🔹 Standard Libraries:");
            _output.WriteLine("   📐 math.*     - sin(), cos(), tan(), sqrt(), floor(), ceil()");
            _output.WriteLine("                   abs(), max(), min(), pi, huge, random()");
            _output.WriteLine("   📝 string.*   - len(), sub(), upper(), lower(), find()");
            _output.WriteLine("                   gsub(), format(), char(), byte()");
            _output.WriteLine("   📋 table.*    - insert(), remove(), concat(), sort()");
            _output.WriteLine("                   pack(), unpack(), move()");
            _output.WriteLine("   📁 io.*       - open(), close(), read(), write(), flush()");
            _output.WriteLine("                   input(), output(), lines()");
            _output.WriteLine("   🕐 os.*       - time(), date(), clock(), getenv()");
            _output.WriteLine("                   exit(), tmpname(), difftime()");
            _output.WriteLine("   🔤 utf8.*     - len(), char(), codepoint(), offset()");
            _output.WriteLine("   ⚡ coroutine.* - create(), resume(), yield(), status()");
            _output.WriteLine("                   running(), wrap(), isyieldable(), close()");
            _output.WriteLine();
        }
        
        /// <summary>
        /// Check if input needs continuation by examining parser state and result
        /// </summary>
        private bool NeedsContinuation(string input)
        {
            string trimmed = input.Trim();
            string[] lines = trimmed.Split('\n')
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
            
            // Try to parse the input and examine the result
            try
            {
                var parseResult = ParserHelper.ParseString(input);
                
                // Even if parsing succeeded, check for patterns that suggest incomplete input
                string lastLine = lines.Length > 0 ? lines[lines.Length - 1] : "";
                string allText = string.Join(" ", lines);
                
                // Check for incomplete patterns
                return CheckIncompletePatterns(allText, lastLine);
            }
            catch (Exception ex)
            {
                // Statement parsing failed - check if it can be parsed as an expression
                try
                {
                    ParserHelper.ParseExpression(input);
                    // If expression parsing succeeds, the input is complete
                    return false;
                }
                catch
                {
                    // Both statement and expression parsing failed - analyze the error
                    string errorMsg = ex.Message;
                    string lastLine = lines.Length > 0 ? lines[lines.Length - 1] : "";
                    string allText = string.Join(" ", lines);
                    
                    // Check for specific error patterns that indicate incomplete input
                    return errorMsg.Contains("Expecting 'end'") ||
                           errorMsg.Contains("Expecting 'until'") ||
                           errorMsg.Contains("Expecting ')'") ||
                           errorMsg.Contains("Expecting '}'") ||
                           errorMsg.Contains("Expecting ']'") ||
                           errorMsg.Contains("end of input") ||
                           errorMsg.Contains("Unexpected end of input") ||
                           CheckIncompletePatterns(allText, lastLine);
                }
            }
        }
        
        private bool CheckIncompletePatterns(string allText, string lastLine)
        {
            string trimmed = allText.Trim();
            
            // Check for incomplete function definitions
            if ((allText.Contains("local") && allText.Contains("function") && !allText.Contains("end")) ||
                (allText.StartsWith("function") && !allText.Contains("end")))
            {
                return true;
            }
            
            // Check for incomplete control structures
            if ((allText.Contains("if") && allText.Contains("then") && !allText.Contains("end")) ||
                (allText.Contains("while") && allText.Contains("do") && !allText.Contains("end")) ||
                (allText.Contains("for") && allText.Contains("do") && !allText.Contains("end")) ||
                (allText.Contains("repeat") && !allText.Contains("until")))
            {
                return true;
            }
            
            // Check for incomplete tokens
            if (lastLine == "local" || lastLine == "function" || lastLine == "if" ||
                lastLine == "while" || lastLine == "for" || lastLine == "repeat" ||
                lastLine == "then" || lastLine == "do" || lastLine == "else" ||
                lastLine == "elseif")
            {
                return true;
            }
            
            // Check for lines ending with operators or punctuation suggesting continuation
            if (lastLine.EndsWith("(") || lastLine.EndsWith("{") || lastLine.EndsWith("[") ||
                lastLine.EndsWith(",") || lastLine.EndsWith("="))
            {
                return true;
            }
            
            // Check for unmatched parentheses/braces
            int openParens = allText.Count(c => c == '(');
            int closeParens = allText.Count(c => c == ')');
            int openBraces = allText.Count(c => c == '{');
            int closeBraces = allText.Count(c => c == '}');
            int openBrackets = allText.Count(c => c == '[');
            int closeBrackets = allText.Count(c => c == ']');
            
            if (openParens > closeParens || openBraces > closeBraces || openBrackets > closeBrackets)
            {
                return true;
            }
            
            // Additional heuristics for common incomplete patterns
            if ((trimmed.StartsWith("local function") && !trimmed.Contains("end")) ||
                (trimmed.StartsWith("function") && !trimmed.Contains("end")) ||
                (trimmed.StartsWith("if ") && !trimmed.Contains("end")) ||
                (trimmed.StartsWith("while ") && !trimmed.Contains("end")) ||
                (trimmed.StartsWith("for ") && !trimmed.Contains("end")) ||
                (trimmed.StartsWith("repeat") && !trimmed.Contains("until")))
            {
                return true;
            }
            
            if (trimmed.EndsWith("then") || trimmed.EndsWith("do") || trimmed.EndsWith("{") ||
                trimmed.EndsWith("=") || trimmed.EndsWith("local") || trimmed.EndsWith("function") ||
                trimmed.EndsWith("if") || trimmed.EndsWith("while") || trimmed.EndsWith("for") ||
                trimmed.EndsWith("repeat"))
            {
                return true;
            }
            
            return false;
        }
        
        private void EvaluateInput(string input)
        {
            try
            {
                // Handle explicit expression evaluation with '=' prefix
                if (input.Trim().StartsWith("="))
                {
                    var expressionText = input.Trim().Substring(1).Trim();
                    if (!string.IsNullOrEmpty(expressionText))
                    {
                        try
                        {
                            var expressionResult = _interpreter.EvaluateExpression(expressionText);
                            _output.WriteLine($"= {expressionResult}");
                        }
                        catch (Exception ex)
                        {
                            _output.WriteLine($"Error: {ex.Message}");
                        }
                        return;
                    }
                }

                // Parse the input to understand its structure
                Microsoft.FSharp.Collections.FSharpList<FLua.Ast.Statement>? statements = null;
                bool canParseAsStatements = false;
                
                try
                {
                    statements = ParserHelper.ParseString(input);
                    canParseAsStatements = true;
                }
                catch
                {
                    // Cannot parse as statements, will try as expression later
                }

                // If parsing as statements succeeded, execute as statements
                // Don't try expression evaluation for things that parse as statements

                // Special case: if input is a simple variable name and can be parsed as a statement,
                // try expression evaluation first (variables should show their values)
                string trimmedInput = input.Trim();
                bool isSimpleVar = System.Text.RegularExpressions.Regex.IsMatch(trimmedInput, @"^[a-zA-Z_][a-zA-Z0-9_]*$");

                if (canParseAsStatements && statements != null && statements.Length == 1 && isSimpleVar)
                {
                    try
                    {
                        var expressionResult = _interpreter.EvaluateExpression(input);
                        _output.WriteLine($"= {expressionResult}");
                        return;
                    }
                    catch
                    {
                        // Fall back to statement execution
                    }
                }

                if (canParseAsStatements && statements != null)
                {
                    // Special handling for REPL: if this is a single function call that returns a value,
                    // treat it as an expression to show the result
                    if (statements.Length == 1 &&
                        statements[0].Tag == FLua.Ast.Statement.Tags.FunctionCall &&
                        !IsSideEffectFunctionCall(statements[0]))
                    {
                        // This is a pure function call - evaluate it as an expression to show the result
                        try
                        {
                            var expressionResult = _interpreter.EvaluateExpression(input);
                            _output.WriteLine($"= {expressionResult}");
                            return;
                        }
                        catch
                        {
                            // Fallback: execute as statement
                        }
                    }

                    // Execute the statements
                    var results = _interpreter.ExecuteCode(input);

                    if (results.Length > 0)
                    {
                        // Statement returned explicit values (like return statement)
                        foreach (var result in results)
                        {
                            _output.WriteLine($"=> {result}");
                        }
                    }
                    else
                    {
                        // Analyze the AST to determine output behavior
                        var outputBehavior = AnalyzeStatementOutputBehavior(statements);

                        switch (outputBehavior)
                        {
                            case OutputBehavior.ShowNil:
                                _output.WriteLine("=> nil");
                                break;
                            case OutputBehavior.EvaluateAsExpression:
                                try
                                {
                                    // For multiple statements, extract and evaluate the last statement
                                    if (statements.Length > 1)
                                    {
                                        // Find the last statement by getting the last non-REPL-command, non-empty line
                                        var lines = input.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                                        var lastNonCommandLine = lines.LastOrDefault(line => 
                                            !string.IsNullOrWhiteSpace(line) && 
                                            !line.Trim().StartsWith("."))?.Trim();
                                        
                                        if (!string.IsNullOrEmpty(lastNonCommandLine))
                                        {
                                            var expressionResult = _interpreter.EvaluateExpression(lastNonCommandLine);
                                            _output.WriteLine($"= {expressionResult}");
                                        }
                                    }
                                    else
                                    {
                                        var expressionResult = _interpreter.EvaluateExpression(input);
                                        _output.WriteLine($"= {expressionResult}");
                                    }
                                }
                                catch
                                {
                                    // Failed to evaluate as expression, no output
                                }
                                break;
                            case OutputBehavior.EvaluateAsFunctionCall:
                                try
                                {
                                    // For multiple statements, extract and evaluate the last statement as function call
                                    if (statements.Length > 1)
                                    {
                                        // Find the last statement by getting the last non-REPL-command, non-empty line
                                        var lines = input.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                                        var lastNonCommandLine = lines.LastOrDefault(line => 
                                            !string.IsNullOrWhiteSpace(line) && 
                                            !line.Trim().StartsWith("."))?.Trim();
                                        
                                        if (!string.IsNullOrEmpty(lastNonCommandLine))
                                        {
                                            var expressionResult = _interpreter.EvaluateExpression(lastNonCommandLine);
                                            _output.WriteLine($"=> {expressionResult}");
                                        }
                                        else
                                        {
                                            _output.WriteLine("=> nil");
                                        }
                                    }
                                    else
                                    {
                                        var expressionResult = _interpreter.EvaluateExpression(input);
                                        _output.WriteLine($"=> {expressionResult}");
                                    }
                                }
                                catch
                                {
                                    _output.WriteLine("=> nil");
                                }
                                break;
                            case OutputBehavior.NoOutput:
                            default:
                                // No additional output needed
                                break;
                        }
                    }
                }
                else
                {
                    // Cannot parse as statements, try as expression
                    var result = _interpreter.EvaluateExpression(input);
                    _output.WriteLine($"= {result}");
                }
            }
            catch (Exception ex)
            {
                _output.WriteLine($"❌ Error: {ex.Message}");
            }
        }
        
        private bool IsLikelyFunctionCall(string input)
        {
            string trimmed = input.Trim();
            
            // Simple heuristics for function calls
            return trimmed.Contains("(") && trimmed.Contains(")") && 
                   (trimmed.Contains("print") || trimmed.Contains("tostring") || 
                    trimmed.Contains("tonumber") || trimmed.Contains("type") ||
                    trimmed.Contains("pairs") || trimmed.Contains("ipairs") ||
                    trimmed.Contains("math.") || trimmed.Contains("string.") ||
                    // Generic pattern: identifier followed by parentheses
                    System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"[a-zA-Z_][a-zA-Z0-9_]*\s*\("));
        }
        
        private bool HasSideEffects(string input)
        {
            string trimmed = input.Trim();
            
            // Functions that have side effects (print to console, modify state, etc.)
            // Use regex to handle various spacing patterns
            return System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"\bprint\s*\(") ||
                   System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"\berror\s*\(") ||
                   System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"\bsetmetatable\s*\(") ||
                   // Add more side-effect functions as needed
                   false;
        }
        
        private bool IsAssignmentOrDeclaration(string input)
        {
            string trimmed = input.Trim();
            
            // Check for various assignment and declaration patterns
            return trimmed.StartsWith("local ") ||
                   trimmed.StartsWith("function ") ||
                   trimmed.StartsWith("local function ") ||
                   // Simple assignment pattern: identifier = value
                   System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^[a-zA-Z_][a-zA-Z0-9_]*\s*=\s*") ||
                   // Table field assignment: table.field = value or table[key] = value
                   System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^[a-zA-Z_][a-zA-Z0-9_.[\]]*\s*=\s*");
        }

        private OutputBehavior AnalyzeStatementOutputBehavior(Microsoft.FSharp.Collections.FSharpList<FLua.Ast.Statement> statements)
        {
            // Handle single statement case (most common)
            if (statements.Length == 1)
            {
                var statement = statements.Head;

                switch (statement.Tag)
                {
                    case FLua.Ast.Statement.Tags.Assignment:
                    case FLua.Ast.Statement.Tags.LocalAssignment:
                    case FLua.Ast.Statement.Tags.FunctionDef:
                    case FLua.Ast.Statement.Tags.LocalFunctionDef:
                    case FLua.Ast.Statement.Tags.DoBlock:
                    case FLua.Ast.Statement.Tags.While:
                    case FLua.Ast.Statement.Tags.Repeat:
                    case FLua.Ast.Statement.Tags.If:
                    case FLua.Ast.Statement.Tags.NumericFor:
                    case FLua.Ast.Statement.Tags.GenericFor:
                    case FLua.Ast.Statement.Tags.Label:
                    case FLua.Ast.Statement.Tags.Goto:
                    case FLua.Ast.Statement.Tags.Break:
                    case FLua.Ast.Statement.Tags.Empty:
                        return OutputBehavior.NoOutput;
                    
                    case FLua.Ast.Statement.Tags.FunctionCall:
                        // Check if this is a side-effect function like print
                        if (IsSideEffectFunctionCall(statement))
                        {
                            return OutputBehavior.ShowNil;
                        }
                        else if (IsSimpleExpressionWrappedAsStatement(statement))
                        {
                            // Simple expressions wrapped as statements should be treated as expressions
                            return OutputBehavior.EvaluateAsExpression;
                        }
                        else
                        {
                            return OutputBehavior.EvaluateAsFunctionCall;
                        }
                    
                    case FLua.Ast.Statement.Tags.Return:
                        // Return statements are handled by ExecuteCode returning values
                        return OutputBehavior.NoOutput;
                    
                    default:
                        return OutputBehavior.EvaluateAsExpression;
                }
            }
            
            // Multiple statements - check the last statement for output behavior
            if (statements.Length > 1)
            {
                // Find the last statement by iterating through the list
                FLua.Ast.Statement? lastStatement = null;
                foreach (var stmt in statements)
                {
                    lastStatement = stmt;
                }
                
                if (lastStatement != null)
                {
                    switch (lastStatement.Tag)
                    {
                        case FLua.Ast.Statement.Tags.FunctionCall:
                            // Check if this is a side-effect function like print
                            if (IsSideEffectFunctionCall(lastStatement))
                            {
                                return OutputBehavior.ShowNil;
                            }
                            else if (IsSimpleExpressionWrappedAsStatement(lastStatement))
                            {
                                // Simple expressions wrapped as statements should be treated as expressions
                                return OutputBehavior.EvaluateAsExpression;
                            }
                            else
                            {
                                return OutputBehavior.EvaluateAsFunctionCall;
                            }
                        
                        case FLua.Ast.Statement.Tags.Assignment:
                        case FLua.Ast.Statement.Tags.LocalAssignment:
                        case FLua.Ast.Statement.Tags.FunctionDef:
                        case FLua.Ast.Statement.Tags.LocalFunctionDef:
                        case FLua.Ast.Statement.Tags.DoBlock:
                        case FLua.Ast.Statement.Tags.While:
                        case FLua.Ast.Statement.Tags.Repeat:
                        case FLua.Ast.Statement.Tags.If:
                        case FLua.Ast.Statement.Tags.NumericFor:
                        case FLua.Ast.Statement.Tags.GenericFor:
                        case FLua.Ast.Statement.Tags.Label:
                        case FLua.Ast.Statement.Tags.Goto:
                        case FLua.Ast.Statement.Tags.Break:
                        case FLua.Ast.Statement.Tags.Empty:
                        case FLua.Ast.Statement.Tags.Return:
                            return OutputBehavior.NoOutput;
                        
                        default:
                            return OutputBehavior.EvaluateAsExpression;
                    }
                }
            }
            
            return OutputBehavior.NoOutput;
        }

        private bool IsSideEffectFunctionCall(FLua.Ast.Statement statement)
        {
            if (statement.Tag != FLua.Ast.Statement.Tags.FunctionCall)
                return false;

            // Extract the function call statement
            var functionCallStatement = statement as FLua.Ast.Statement.FunctionCall;
            if (functionCallStatement?.Item == null)
                return false;

            var expr = functionCallStatement.Item;

            // Check if it's a call to a known side-effect function
            if (expr.Tag == FLua.Ast.Expr.Tags.FunctionCall)
            {
                var funcCall = expr as FLua.Ast.Expr.FunctionCall;
                if (funcCall?.Item1?.Tag == FLua.Ast.Expr.Tags.Var)
                {
                    var variable = funcCall.Item1 as FLua.Ast.Expr.Var;
                    if (variable?.Item != null)
                    {
                        string functionName = variable.Item;
                        return functionName == "print" || functionName == "error";
                    }
                }
            }

            return false;
        }

        private bool LooksLikeExpression(string input)
        {
            var trimmed = input.Trim();

            // First check if this looks like a statement that should not be treated as expression
            if (HasSideEffects(trimmed))
            {
                return false; // Side-effect statements should be executed as statements
            }

            // Check for expression-like patterns
            if (trimmed.Contains("+") || trimmed.Contains("-") || trimmed.Contains("*") || trimmed.Contains("/")) return true; // Arithmetic
            if (trimmed.Contains("==") || trimmed.Contains("~=") || trimmed.Contains("<") || trimmed.Contains(">") || trimmed.Contains("<=") || trimmed.Contains(">=")) return true; // Comparisons
            if (trimmed.Contains("and") || trimmed.Contains("or") || trimmed.Contains("not")) return true; // Logical operators
            if (trimmed.Contains(".") && !trimmed.Contains(" = ")) return true; // Table access (but not assignments)
            if (trimmed.Contains("[") && trimmed.Contains("]")) return true; // Array access
            if (trimmed.StartsWith("#")) return true; // Length operator

            // Function calls that don't have side effects
            if (trimmed.Contains("(") && trimmed.Contains(")"))
            {
                // Check if it's a side-effect function call - if not, it might be an expression
                if (!HasSideEffects(trimmed))
                {
                    return true;
                }
            }

            // Variables and literals that aren't statements
            if (!trimmed.Contains(" = ") && !trimmed.Contains("local ") && !trimmed.StartsWith("if ") &&
                !trimmed.StartsWith("while ") && !trimmed.StartsWith("for ") && !trimmed.StartsWith("function ") &&
                !trimmed.StartsWith("return ") && !trimmed.StartsWith("break") && !trimmed.StartsWith("do"))
            {
                // If it looks like a variable name or number or string, it's probably an expression
                return System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^[a-zA-Z_][a-zA-Z0-9_]*$") || // Variable
                       System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^[0-9]+(\.[0-9]+)?$") || // Number
                       (trimmed.StartsWith("\"") && trimmed.EndsWith("\"")) || // String
                       (trimmed.StartsWith("'") && trimmed.EndsWith("'")) || // String
                       trimmed == "true" || trimmed == "false" || trimmed == "nil"; // Literals
            }

            return false;
        }

        private bool IsSimpleExpressionWrappedAsStatement(FLua.Ast.Statement statement)
        {
            if (statement.Tag != FLua.Ast.Statement.Tags.FunctionCall)
                return false;
            
            // Extract the function call statement
            var functionCallStatement = statement as FLua.Ast.Statement.FunctionCall;
            if (functionCallStatement?.Item == null)
                return false;
            
            var expr = functionCallStatement.Item;
            
            // Check if this is actually a simple expression (not a function call)
            return expr.Tag != FLua.Ast.Expr.Tags.FunctionCall && 
                   expr.Tag != FLua.Ast.Expr.Tags.MethodCall;
        }
        

    }
} 