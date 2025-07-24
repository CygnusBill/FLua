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
        private string? _incompleteInput;
        
        public LuaRepl()
        {
            _interpreter = new LuaInterpreter();
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
                Console.Write(prompt);
                string? line = Console.ReadLine();
                
                // Handle end of input (null when piped input ends or Ctrl+D)
                if (line == null)
                {
                    if (_incompleteInput != null)
                    {
                        Console.WriteLine();
                        Console.WriteLine("❌ Incomplete input at end of stream");
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
            
            Console.WriteLine("Goodbye! 👋");
        }
        
        private void PrintBanner()
        {
            Console.WriteLine();
            Console.WriteLine("🚀 FLua Interactive REPL v2.0 (C#)");
            Console.WriteLine("====================================");
            Console.WriteLine("C# implementation of Lua interpreter");
            Console.WriteLine();
            Console.WriteLine("Commands:");
            Console.WriteLine("  .help     - Show help");
            Console.WriteLine("  .quit     - Exit the REPL");
            Console.WriteLine("  .clear    - Clear screen");
            Console.WriteLine();
            Console.WriteLine("Enter Lua expressions or statements:");
            Console.WriteLine();
        }
        
        private bool HandleReplCommands(string input)
        {
            switch (input.Trim().ToLowerInvariant())
            {
                case ".quit":
                case ".exit":
                    Console.WriteLine("Goodbye! 👋");
                    Environment.Exit(0);
                    return true; // Never reached
                
                case ".help":
                    PrintHelp();
                    return true;
                
                case ".clear":
                    Console.Clear();
                    PrintBanner();
                    return true;
                
                default:
                    return false; // Not a REPL command
            }
        }
        
        private void PrintHelp()
        {
            Console.WriteLine();
            Console.WriteLine("FLua REPL Help:");
            Console.WriteLine("===============");
            Console.WriteLine();
            Console.WriteLine("🔹 Expressions (return values):");
            Console.WriteLine("   > 1 + 2 * 3");
            Console.WriteLine("   > \"hello\" .. \" world\"");
            Console.WriteLine();
            Console.WriteLine("🔹 Statements (no return):");
            Console.WriteLine("   > local x = 42");
            Console.WriteLine("   > print(\"Hello, World!\")");
            Console.WriteLine("   > x = x + 1");
            Console.WriteLine();
            Console.WriteLine("🔹 Multi-line (automatic detection):");
            Console.WriteLine("   > local function factorial(n)");
            Console.WriteLine("   >>   if n <= 1 then return 1");
            Console.WriteLine("   >>   else return n * factorial(n-1) end");
            Console.WriteLine("   >> end");
            Console.WriteLine("   >>");
            Console.WriteLine();
            Console.WriteLine("🔹 Built-in functions:");
            Console.WriteLine("   print(), type(), tostring(), tonumber(), pairs(), ipairs()");
            Console.WriteLine();
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
                
                if (canParseAsStatements && statements != null)
                {
                    // Execute the statements
                    var results = _interpreter.ExecuteCode(input);
                    
                    if (results.Length > 0)
                    {
                        // Statement returned explicit values (like return statement)
                        foreach (var result in results)
                        {
                            Console.WriteLine($"=> {result}");
                        }
                    }
                    else
                    {
                        // Analyze the AST to determine output behavior
                        var outputBehavior = AnalyzeStatementOutputBehavior(statements);
                        
                        switch (outputBehavior)
                        {
                            case OutputBehavior.ShowNil:
                                Console.WriteLine("=> nil");
                                break;
                            case OutputBehavior.EvaluateAsExpression:
                                try
                                {
                                    var expressionResult = _interpreter.EvaluateExpression(input);
                                    Console.WriteLine($"= {expressionResult}");
                                }
                                catch
                                {
                                    // Failed to evaluate as expression, no output
                                }
                                break;
                            case OutputBehavior.EvaluateAsFunctionCall:
                                try
                                {
                                    var expressionResult = _interpreter.EvaluateExpression(input);
                                    Console.WriteLine($"=> {expressionResult}");
                                }
                                catch
                                {
                                    Console.WriteLine("=> nil");
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
                    Console.WriteLine($"= {result}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
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
            
            // Multiple statements - no additional output
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