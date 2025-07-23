/// FLua REPL - Interactive Lua Interpreter
/// 
/// A Read-Eval-Print Loop for the FLua interpreter.
/// Supports both expressions and statements with proper error handling.
///
module FLua.Repl

open System
open FParsec
open FLua.Parser
open FLua.Parser.Parser
open FLua.Interpreter.Values
open FLua.Interpreter.Interpreter

/// REPL state to maintain environment and parser state across evaluations
type ReplState = {
    Environment: LuaEnvironment
    IncompleteInput: string option  // Store incomplete input for continuation
}

/// Create initial REPL state with standard environment
let createInitialState () =
    let env = FLua.Interpreter.Environment.GlobalEnvironment.createStandard ()
    FLua.Interpreter.Environment.GlobalEnvironment.addMathLibrary env.Globals
    { Environment = env; IncompleteInput = None }

/// Print the FLua banner
let printBanner () =
    printfn ""
    printfn "🚀 FLua Interactive REPL v1.0"
    printfn "================================"
    printfn "F# implementation of Lua 5.4"
    printfn ""
    printfn "Commands:"
    printfn "  .help     - Show this help"
    printfn "  .quit     - Exit the REPL"
    printfn "  .clear    - Clear screen"
    printfn "  .env      - Show environment variables"
    printfn ""
    printfn "Enter Lua expressions or statements:"
    printfn ""

/// Print help information
let printHelp () =
    printfn ""
    printfn "FLua REPL Help:"
    printfn "==============="
    printfn ""
    printfn "🔹 Expressions (return values):"
    printfn "   > 1 + 2 * 3"
    printfn "   > \"hello\" .. \" world\""
    printfn "   > math.max(10, 20)"
    printfn ""
    printfn "🔹 Statements (no return):"
    printfn "   > local x = 42"
    printfn "   > print(\"Hello, World!\")"
    printfn "   > x = x + 1"
    printfn ""
    printfn "🔹 Multi-line (automatic detection):"
    printfn "   > local function factorial(n)"
    printfn "   >>   if n <= 1 then return 1"
    printfn "   >>   else return n * factorial(n-1) end"
    printfn "   >> end"
    printfn "   >>"
    printfn ""
    printfn "🔹 Built-in functions:"
    printfn "   print(), type(), tostring(), tonumber()"
    printfn "   math.abs(), math.max(), math.min(), math.floor(), math.ceil()"
    printfn ""

/// Show environment variables
let printEnvironment (state: ReplState) =
    printfn ""
    printfn "Environment Variables:"
    printfn "====================="
    printfn ""
    printfn "🔹 Locals:"
    for kvp in state.Environment.Locals do
        let valueStr = LuaValue.toString kvp.Value
        let typeStr = LuaValue.typeName kvp.Value
        printfn "   %s = %s (%s)" kvp.Key valueStr typeStr
    
    printfn ""
    printfn "🔹 Globals (user-defined):"
    let userGlobals = 
        state.Environment.Globals 
        |> Seq.filter (fun kvp -> 
            not (kvp.Key.StartsWith("_") || 
                 ["print"; "type"; "tostring"; "tonumber"; "next"; "pairs"; "ipairs"; "error"; "assert"; "math"] 
                 |> List.contains kvp.Key))
        |> Seq.toList
    
    if userGlobals.IsEmpty then
        printfn "   (none)"
    else
        for kvp in userGlobals do
            let valueStr = LuaValue.toString kvp.Value
            let typeStr = LuaValue.typeName kvp.Value
            printfn "   %s = %s (%s)" kvp.Key valueStr typeStr
    printfn ""

/// Check if input needs continuation by examining parser state and result
let needsContinuation (input: string) =
    let trimmed = input.Trim()
    let lines = trimmed.Split('\n') |> Array.map (fun s -> s.Trim()) |> Array.filter (fun s -> s <> "")
    
    // Try to parse the input and examine the result for continuation hints
    match FParsec.CharParsers.run luaFile input with
    | FParsec.CharParsers.Success (statements, _, _) ->
        // Even if parsing succeeded, check if it looks like incomplete input
        // that was incorrectly parsed as separate statements
        match statements with
        | LocalAssignment ([("function", _)], None) :: FunctionCallStmt _ :: _ ->
            // This pattern indicates "local function name(...)" was parsed incorrectly
            // as multiple statements, which means it's actually incomplete
            true
        | _ -> 
            // Check for other incomplete patterns that parse as individual statements
            let lastLine = if lines.Length > 0 then lines.[lines.Length - 1] else ""
            let allText = String.concat " " lines
            
            // Detect incomplete function definition patterns
            (allText.Contains("local") && allText.Contains("function") && not (allText.Contains("end"))) ||
            (allText.StartsWith("function") && not (allText.Contains("end"))) ||
            // Detect incomplete control structures
            (allText.Contains("if") && allText.Contains("then") && not (allText.Contains("end"))) ||
            (allText.Contains("while") && allText.Contains("do") && not (allText.Contains("end"))) ||
            (allText.Contains("for") && allText.Contains("do") && not (allText.Contains("end"))) ||
            (allText.Contains("repeat") && not (allText.Contains("until"))) ||
            // Detect incomplete tokens that suggest more input needed
            lastLine = "local" ||
            lastLine = "function" ||
            lastLine = "if" ||
            lastLine = "while" ||
            lastLine = "for" ||
            lastLine = "repeat" ||
            lastLine = "then" ||
            lastLine = "do" ||
            lastLine = "else" ||
            lastLine = "elseif" ||
            lastLine.EndsWith("(") ||
            lastLine.EndsWith("{") ||
            lastLine.EndsWith(",") ||
            lastLine.EndsWith("=") ||
            // Check for unmatched parentheses/braces
            (allText.Split('(').Length > allText.Split(')').Length) ||
            (allText.Split('{').Length > allText.Split('}').Length) ||
            (allText.Split('[').Length > allText.Split(']').Length)
        
    | FParsec.CharParsers.Failure (errorMsg, error, userState) ->
        // Analyze the error to determine if it's due to incomplete input
        let errorString = errorMsg.ToString()
        let lastLine = if lines.Length > 0 then lines.[lines.Length - 1] else ""
        let allText = String.concat " " lines
        
        // Check for specific patterns that indicate incomplete input rather than syntax errors
        errorString.Contains("Expecting 'end'") ||
        errorString.Contains("Expecting 'until'") ||
        errorString.Contains("Expecting ')'") ||
        errorString.Contains("Expecting '}'") ||
        errorString.Contains("Expecting ']'") ||
        errorString.Contains("end of input") ||
        errorString.Contains("Unexpected end of input") ||
        errorString.Contains("Note: The error occurred at the end of the input stream") ||
        // Check position - if error is at end of input, likely incomplete
        (error.Position.Index >= int64 (input.Length - 1)) ||
        // Check for incomplete function definition contexts
        (allText.Contains("local") && allText.Contains("function") && not (allText.Contains("end"))) ||
        (allText.StartsWith("function") && not (allText.Contains("end"))) ||
        // Check for incomplete control structures
        (allText.Contains("if") && allText.Contains("then") && not (allText.Contains("end"))) ||
        (allText.Contains("while") && allText.Contains("do") && not (allText.Contains("end"))) ||
        (allText.Contains("for") && allText.Contains("do") && not (allText.Contains("end"))) ||
        (allText.Contains("repeat") && not (allText.Contains("until"))) ||
        // Check for incomplete tokens
        lastLine = "local" ||
        lastLine = "function" ||
        lastLine = "if" ||
        lastLine = "while" ||
        lastLine = "for" ||
        lastLine = "repeat" ||
        lastLine = "then" ||
        lastLine = "do" ||
        lastLine = "else" ||
        lastLine = "elseif" ||
        lastLine.EndsWith("(") ||
        lastLine.EndsWith("{") ||
        lastLine.EndsWith("[") ||
        lastLine.EndsWith(",") ||
        lastLine.EndsWith("=") ||
        // Check for unmatched parentheses/braces
        (allText.Split('(').Length > allText.Split(')').Length) ||
        (allText.Split('{').Length > allText.Split('}').Length) ||
        (allText.Split('[').Length > allText.Split(']').Length) ||
        // Additional heuristics for common incomplete patterns
        (trimmed.StartsWith("local function") && not (trimmed.Contains("end"))) ||
        (trimmed.StartsWith("function") && not (trimmed.Contains("end"))) ||
        (trimmed.StartsWith("if ") && not (trimmed.Contains("end"))) ||
        (trimmed.StartsWith("while ") && not (trimmed.Contains("end"))) ||
        (trimmed.StartsWith("for ") && not (trimmed.Contains("end"))) ||
        (trimmed.StartsWith("repeat") && not (trimmed.Contains("until"))) ||
        trimmed.EndsWith("then") ||
        trimmed.EndsWith("do") ||
        trimmed.EndsWith("{") ||
        trimmed.EndsWith("=") ||
        trimmed.EndsWith("local") ||
        trimmed.EndsWith("function") ||
        trimmed.EndsWith("if") ||
        trimmed.EndsWith("while") ||
        trimmed.EndsWith("for") ||
        trimmed.EndsWith("repeat")

/// Try to parse and evaluate input as statement first, then as expression  
let evaluateInput (state: ReplState) (input: string) =
    // First try as statement(s) - this handles function definitions, assignments, etc.
    match FParsec.CharParsers.run luaFile input with
    | FParsec.CharParsers.Success (stmtAst, _, _) ->
        let interpreterState = {
            Environment = state.Environment
            ReturnValues = None
            BreakFlag = false
        }
        
        try
            let resultState = InterpreterCore.execBlock interpreterState stmtAst
            
            // Check if this is a single function call statement
            let isFunctionCallStmt = 
                match stmtAst with
                | [FunctionCallStmt _] -> true
                | _ -> false
            
            match resultState.ReturnValues with
            | Some values when not values.IsEmpty ->
                let valueStrings = values |> List.map LuaValue.toString
                printfn "=> %s" (String.Join(", ", valueStrings))
            | _ when isFunctionCallStmt ->
                // For function call statements, get all return values directly
                match stmtAst with
                | [FunctionCallStmt funcCall] ->
                    try
                        match funcCall with
                        | FunctionCall (funcExpr, argExprs) ->
                            let funcVal = InterpreterCore.evalExpr interpreterState funcExpr
                            let argVals = argExprs |> List.map (InterpreterCore.evalExpr interpreterState)
                            let results = InterpreterCore.callFunction interpreterState funcVal argVals
                            match results with
                            | [] -> printfn "=> nil"
                            | values ->
                                let valueStrings = values |> List.map LuaValue.toString
                                printfn "=> %s" (String.Join(", ", valueStrings))
                        | MethodCall (objExpr, methodName, argExprs) ->
                            let objVal = InterpreterCore.evalExpr interpreterState objExpr
                            let argVals = objVal :: (argExprs |> List.map (InterpreterCore.evalExpr interpreterState))
                            match objVal with
                            | LuaTable table ->
                                let methodVal = LuaTable.get table (LuaString methodName)
                                let results = InterpreterCore.callFunction interpreterState methodVal argVals
                                match results with
                                | [] -> printfn "=> nil"
                                | values ->
                                    let valueStrings = values |> List.map LuaValue.toString
                                    printfn "=> %s" (String.Join(", ", valueStrings))
                            | _ -> printfn "=> nil"  // Method call on non-table
                        | _ -> ()  // Other function call types
                    with
                    | _ -> ()  // If evaluation fails, don't show return value
                | _ -> ()
            | _ -> ()  // No output for other statements without return
            
            // Update REPL state with any changes to the environment
            { state with Environment = resultState.Environment }
        with
        | FLua.Interpreter.Environment.LuaRuntimeError msg ->
            printfn "❌ Runtime error: %s" msg
            state
        | ex ->
            printfn "❌ Error: %s" ex.Message
            state
    
    | FParsec.CharParsers.Failure _ ->
        // Try as expression - for simple expressions like "1 + 2"
        match FParsec.CharParsers.run expr input with
        | FParsec.CharParsers.Success (exprAst, _, _) ->
            // Create interpreter state from REPL state
            let interpreterState = {
                Environment = state.Environment
                ReturnValues = None
                BreakFlag = false
            }
            
            try
                let result = InterpreterCore.evalExpr interpreterState exprAst
                printfn "= %s" (LuaValue.toString result)
                state
            with
            | FLua.Interpreter.Environment.LuaRuntimeError msg ->
                printfn "❌ Runtime error: %s" msg
                state
            | ex ->
                printfn "❌ Error: %s" ex.Message
                state
        
        | FParsec.CharParsers.Failure (msg, _, _) ->
            printfn "❌ Parse error: %s" msg
            state



/// Main REPL loop
let rec replLoop (state: ReplState) =
    // Show appropriate prompt based on whether we're continuing input
    let prompt = if state.IncompleteInput.IsSome then "  >> " else "lua> "
    printf "%s" prompt
    let input = Console.ReadLine()
    
    match input with
    | null -> ()  // EOF (Ctrl+D)
    | ".quit" | ".exit" -> 
        printfn "Goodbye! 👋"
    | ".help" -> 
        printHelp ()
        replLoop state
    | ".clear" ->
        Console.Clear()
        printBanner ()
        replLoop state
    | ".env" ->
        printEnvironment state
        replLoop state
    | line when String.IsNullOrWhiteSpace(line) ->
        replLoop state
    | line when line.TrimEnd().EndsWith("\\") ->
        // Explicit multi-line input with backslash continuation
        let lineWithoutBackslash = line.TrimEnd().TrimEnd('\\')
        let newState = { state with IncompleteInput = Some lineWithoutBackslash }
        replLoop newState
    | line ->
        // Handle continuation from previous incomplete input
        let inputToProcess = 
            match state.IncompleteInput with
            | Some previousInput -> previousInput + "\n" + line
            | None -> line
        
        // Check if this input needs continuation using parser state
        if needsContinuation inputToProcess then
            // Input is incomplete, store it and continue
            let newState = { state with IncompleteInput = Some inputToProcess }
            replLoop newState
        else
            // Input is complete (or has syntax error), evaluate it
            let newState = evaluateInput { state with IncompleteInput = None } inputToProcess
            replLoop newState

/// Entry point
[<EntryPoint>]
let main argv =
    try
        Console.Clear()
        printBanner ()
        
        let initialState = createInitialState ()
        replLoop initialState
        0
    with
    | ex ->
        printfn "❌ Fatal error: %s" ex.Message
        1 