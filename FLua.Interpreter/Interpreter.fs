/// FLua.Interpreter.Interpreter - Main Lua Interpreter
/// 
/// This module provides the core interpreter functionality.
/// It evaluates expressions and executes statements using the AST from the parser.
///
module FLua.Interpreter.Interpreter

open System
open System.Collections.Generic
open FLua.Parser
open FLua.Interpreter.Values
open FLua.Interpreter.Environment

/// Result of executing a Lua program
type ExecutionResult =
    | Success of LuaValue list  // Return values
    | Error of string           // Error message

/// The main interpreter state
type InterpreterState = {
    Environment: LuaEnvironment
    ReturnValues: LuaValue list option  // Set when return statement is executed
    BreakFlag: bool                     // Set when break statement is executed
}

/// Main interpreter module with mutually recursive evaluation functions
module InterpreterCore =
    
    /// Evaluate a literal expression
    let evalLiteral = function
        | Literal.Nil -> LuaNil
        | Literal.Boolean b -> LuaBool b
        | Literal.Integer i -> LuaInteger (int64 i)
        | Literal.Float f -> LuaNumber f
        | Literal.String s -> LuaString s
    
    /// Evaluate a binary operation
    let evalBinaryOp left op right =
        match op with
        // Arithmetic operators
        | Add ->
            match LuaValue.tryToNumber left, LuaValue.tryToNumber right with
            | Some l, Some r -> LuaNumber (l + r)
            | _ -> raise (LuaRuntimeError "attempt to add non-numbers")
        
        | Subtract ->
            match LuaValue.tryToNumber left, LuaValue.tryToNumber right with
            | Some l, Some r -> LuaNumber (l - r)
            | _ -> raise (LuaRuntimeError "attempt to subtract non-numbers")
        
        | Multiply ->
            match LuaValue.tryToNumber left, LuaValue.tryToNumber right with
            | Some l, Some r -> LuaNumber (l * r)
            | _ -> raise (LuaRuntimeError "attempt to multiply non-numbers")
        
        | FloatDiv ->
            match LuaValue.tryToNumber left, LuaValue.tryToNumber right with
            | Some l, Some r when r <> 0.0 -> LuaNumber (l / r)
            | Some _, Some _ -> raise (LuaRuntimeError "division by zero")
            | _ -> raise (LuaRuntimeError "attempt to divide non-numbers")
        
        | Power ->
            match LuaValue.tryToNumber left, LuaValue.tryToNumber right with
            | Some l, Some r -> LuaNumber (Math.Pow(l, r))
            | _ -> raise (LuaRuntimeError "attempt to exponentiate non-numbers")
        
        | Modulo ->
            match LuaValue.tryToNumber left, LuaValue.tryToNumber right with
            | Some l, Some r when r <> 0.0 -> LuaNumber (l % r)
            | Some _, Some _ -> raise (LuaRuntimeError "division by zero")
            | _ -> raise (LuaRuntimeError "attempt to perform modulo on non-numbers")
        
        | FloorDiv ->
            match LuaValue.tryToNumber left, LuaValue.tryToNumber right with
            | Some l, Some r when r <> 0.0 -> LuaNumber (Math.Floor(l / r))
            | Some _, Some _ -> raise (LuaRuntimeError "division by zero")
            | _ -> raise (LuaRuntimeError "attempt to perform floor division on non-numbers")
        
        // Comparison operators
        | Less ->
            match LuaValue.tryToNumber left, LuaValue.tryToNumber right with
            | Some l, Some r -> LuaBool (l < r)
            | _ -> 
                match left, right with
                | LuaString l, LuaString r -> LuaBool (l < r)
                | _ -> raise (LuaRuntimeError "attempt to compare incompatible types")
        
        | Greater ->
            match LuaValue.tryToNumber left, LuaValue.tryToNumber right with
            | Some l, Some r -> LuaBool (l > r)
            | _ -> 
                match left, right with
                | LuaString l, LuaString r -> LuaBool (l > r)
                | _ -> raise (LuaRuntimeError "attempt to compare incompatible types")
        
        | LessEqual ->
            match LuaValue.tryToNumber left, LuaValue.tryToNumber right with
            | Some l, Some r -> LuaBool (l <= r)
            | _ -> 
                match left, right with
                | LuaString l, LuaString r -> LuaBool (l <= r)
                | _ -> raise (LuaRuntimeError "attempt to compare incompatible types")
        
        | GreaterEqual ->
            match LuaValue.tryToNumber left, LuaValue.tryToNumber right with
            | Some l, Some r -> LuaBool (l >= r)
            | _ -> 
                match left, right with
                | LuaString l, LuaString r -> LuaBool (l >= r)
                | _ -> raise (LuaRuntimeError "attempt to compare incompatible types")
        
        | Equal -> LuaBool (left = right)
        | NotEqual -> LuaBool (left <> right)
        
        // Logical operators (short-circuit evaluation handled at higher level)
        | And -> if LuaValue.isTruthy left then right else left
        | Or -> if LuaValue.isTruthy left then left else right
        
        // String concatenation
        | Concat -> LuaString (LuaValue.toString left + LuaValue.toString right)
        
        // TODO: Add more operators (bitwise, etc.)
        | _ -> raise (LuaRuntimeError $"binary operator {op} not yet implemented")
    
    /// Evaluate a unary operation
    let evalUnaryOp op value =
        match op with
        | Not -> LuaBool (not (LuaValue.isTruthy value))
        | Negate ->
            match LuaValue.tryToNumber value with
            | Some n -> LuaNumber (-n)
            | None -> raise (LuaRuntimeError "attempt to negate non-number")
        | Length ->
            match value with
            | LuaString s -> LuaInteger (int64 s.Length)
            | LuaTable table -> LuaInteger (int64 table.Array.Count)  // Simplified
            | _ -> raise (LuaRuntimeError "attempt to get length of non-string/table")
        | _ -> raise (LuaRuntimeError $"unary operator {op} not yet implemented")
    
    /// Execute a block of statements
    let rec execBlock (state: InterpreterState) (statements: Statement list) =
        let mutable currentState = state
        
        for stmt in statements do
            if currentState.ReturnValues.IsNone && not currentState.BreakFlag then
                currentState <- execStmt currentState stmt
        
        currentState
    
    /// Execute a single statement  
    and execStmt (state: InterpreterState) = function
        | Empty -> state
        
        | Assignment (varExprs, valueExprs) ->
            let values = valueExprs |> List.map (evalExpr state)
            let mutable valueIndex = 0
            
            for varExpr in varExprs do
                let value = if valueIndex < values.Length then values.[valueIndex] else LuaNil
                valueIndex <- valueIndex + 1
                
                match varExpr with
                | Var name ->
                    // Use proper scoping to find where the variable should be updated
                    LuaEnvironment.setVariable state.Environment name value
                
                | TableAccess (tableExpr, keyExpr) ->
                    let tableVal = evalExpr state tableExpr
                    let keyVal = evalExpr state keyExpr
                    match tableVal with
                    | LuaTable table -> LuaTable.set table keyVal value
                    | _ -> raise (LuaRuntimeError "attempt to index non-table")
                
                | _ -> raise (LuaRuntimeError "invalid assignment target")
            
            state
        
        | LocalAssignment (parameters, valueExprsOpt) ->
            let values = 
                valueExprsOpt 
                |> Option.map (List.map (evalExpr state))
                |> Option.defaultValue []
            
            let mutable valueIndex = 0
            for (name, attr) in parameters do
                let value = if valueIndex < values.Length then values.[valueIndex] else LuaNil
                LuaEnvironment.setLocal state.Environment name value
                valueIndex <- valueIndex + 1
            
            state
        
        | FunctionCallStmt expr ->
            evalExpr state expr |> ignore  // Discard return values
            state
        
        | Return exprsOpt ->
            let returnValues = 
                exprsOpt 
                |> Option.map (List.map (evalExpr state))
                |> Option.defaultValue [LuaNil]
            { state with ReturnValues = Some returnValues }
        
        | Break ->
            { state with BreakFlag = true }
            
        | LocalFunctionDef (name, funcDef) ->
            // Create a closure for the local function
            let closure = {
                Parameters = funcDef.Parameters
                IsVararg = funcDef.IsVararg
                Body = funcDef.Body
                Environment = state.Environment
            }
            let funcValue = LuaFunction (LuaClosure closure)
            
            // Bind the function as a local variable
            LuaEnvironment.setLocal state.Environment name funcValue
            state
            
        | If (conditionBlocks, elseBlockOpt) ->
            // Try each condition in order
            let rec tryConditions conditions =
                match conditions with
                | [] ->
                    // No condition matched, execute else block if present
                    match elseBlockOpt with
                    | Some elseBlock -> execBlock state elseBlock
                    | None -> state
                | (condition, block) :: rest ->
                    let conditionValue = evalExpr state condition
                    if LuaValue.isTruthy conditionValue then
                        execBlock state block
                    else
                        tryConditions rest
            
            tryConditions conditionBlocks
            
        | While (condition, body) ->
            let rec loop currentState =
                if currentState.ReturnValues.IsSome || currentState.BreakFlag then
                    currentState
                else
                    let conditionValue = evalExpr currentState condition
                    if LuaValue.isTruthy conditionValue then
                        let newState = execBlock currentState body
                        if newState.BreakFlag then
                            { newState with BreakFlag = false }  // Break out of while loop
                        else
                            loop newState
                    else
                        currentState
            loop state
            
        | NumericFor (varName, startExpr, endExpr, stepExprOpt, body) ->
            let startVal = evalExpr state startExpr
            let endVal = evalExpr state endExpr
            let stepVal = 
                stepExprOpt 
                |> Option.map (evalExpr state) 
                |> Option.defaultValue (LuaNumber 1.0)
            
            match LuaValue.tryToNumber startVal, LuaValue.tryToNumber endVal, LuaValue.tryToNumber stepVal with
            | Some start, Some endValue, Some step when step <> 0.0 ->
                // Create new environment for the loop variable
                let loopEnv = LuaEnvironment.createChild state.Environment
                let loopState = { state with Environment = loopEnv }
                
                let rec loop currentVal currentState =
                    if currentState.ReturnValues.IsSome || currentState.BreakFlag then
                        currentState
                    else
                        let shouldContinue = 
                            if step > 0.0 then currentVal <= endValue
                            else currentVal >= endValue
                        
                        if shouldContinue then
                            // Set loop variable
                            LuaEnvironment.setLocal currentState.Environment varName (LuaNumber currentVal)
                            
                            // Execute body
                            let newState = execBlock currentState body
                            if newState.BreakFlag then
                                { newState with BreakFlag = false }  // Break out of for loop
                            else
                                loop (currentVal + step) newState
                        else
                            currentState
                
                let result = loop start loopState
                { result with Environment = state.Environment }  // Restore original environment
                
            | _ -> raise (LuaRuntimeError "for loop limits and step must be numbers")
            
        | DoBlock body ->
            // Create new environment for the do block
            let blockEnv = LuaEnvironment.createChild state.Environment
            let blockState = { state with Environment = blockEnv }
            let result = execBlock blockState body
            { result with Environment = state.Environment }  // Restore original environment
        
        | _ -> raise (LuaRuntimeError "statement type not yet implemented")
    
    /// Call a function with arguments
    and callFunction (state: InterpreterState) func args =
        match func with
        | LuaFunction (LuaBuiltin builtin) ->
            builtin.Function args
        
        | LuaFunction (LuaClosure closure) ->
            // Create new environment for function execution
            let funcEnv = LuaEnvironment.createChild closure.Environment
            
            // Bind parameters
            let mutable argIndex = 0
            for param in closure.Parameters do
                match param with
                | Param (name, attr) when argIndex < args.Length ->
                    LuaEnvironment.setLocal funcEnv name args.[argIndex]
                    argIndex <- argIndex + 1
                | Param (name, attr) ->
                    LuaEnvironment.setLocal funcEnv name LuaNil
                | VarargParam ->
                    // TODO: Handle varargs properly
                    ()
            
            // Execute function body
            let funcState = { state with Environment = funcEnv; ReturnValues = None }
            let resultState = execBlock funcState closure.Body
            
            // Return values or nil
            resultState.ReturnValues |> Option.defaultValue [LuaNil]
        
        | _ -> raise (LuaRuntimeError "attempt to call non-function")
    
    /// Evaluate an expression
    and evalExpr (state: InterpreterState) = function
        | Literal lit -> evalLiteral lit
        
        | Var name -> LuaEnvironment.getValue state.Environment name
        
        | Binary (left, op, right) ->
            let leftVal = evalExpr state left
            // Handle short-circuit evaluation for logical operators
            match op with
            | And when not (LuaValue.isTruthy leftVal) -> leftVal
            | Or when LuaValue.isTruthy leftVal -> leftVal
            | _ -> 
                let rightVal = evalExpr state right
                evalBinaryOp leftVal op rightVal
        
        | Unary (op, expr) ->
            let value = evalExpr state expr
            evalUnaryOp op value
        
        | TableAccess (tableExpr, keyExpr) ->
            let tableVal = evalExpr state tableExpr
            let keyVal = evalExpr state keyExpr
            match tableVal with
            | LuaTable table -> LuaTable.get table keyVal
            | _ -> raise (LuaRuntimeError "attempt to index non-table")
        
        | TableConstructor fields ->
            let table = LuaTable.empty ()
            let mutable arrayIndex = 1L
            
            for field in fields do
                match field with
                | ExprField expr ->
                    let value = evalExpr state expr
                    LuaTable.set table (LuaInteger arrayIndex) value
                    arrayIndex <- arrayIndex + 1L
                
                | NamedField (name, expr) ->
                    let value = evalExpr state expr
                    LuaTable.set table (LuaString name) value
                
                | KeyField (keyExpr, valueExpr) ->
                    let key = evalExpr state keyExpr
                    let value = evalExpr state valueExpr
                    LuaTable.set table key value
            
            LuaTable table
        
        | FunctionCall (funcExpr, argExprs) ->
            let funcVal = evalExpr state funcExpr
            let argVals = argExprs |> List.map (evalExpr state)
            let results = callFunction state funcVal argVals
            match results with
            | [] -> LuaNil  // If no return values, return nil
            | head :: _ -> head  // Return first value
        
        | MethodCall (objExpr, methodName, argExprs) ->
            let objVal = evalExpr state objExpr
            let argVals = objVal :: (argExprs |> List.map (evalExpr state))  // Add self as first arg
            match objVal with
            | LuaTable table ->
                let methodVal = LuaTable.get table (LuaString methodName)
                let results = callFunction state methodVal argVals
                match results with
                | [] -> LuaNil  // If no return values, return nil
                | head :: _ -> head  // Return first value
            | _ -> raise (LuaRuntimeError "attempt to call method on non-table")
        
        | FunctionDef funcDef ->
            // Create a closure capturing the current environment
            LuaFunction (LuaClosure {
                Parameters = funcDef.Parameters
                IsVararg = funcDef.IsVararg
                Body = funcDef.Body
                Environment = state.Environment
            })
        
        | _ -> raise (LuaRuntimeError "expression type not yet implemented")

/// Main interpreter interface
module Interpreter =
    
    /// Execute a Lua program (block of statements)
    let execute (program: Block) =
        try
            let env = GlobalEnvironment.createStandard ()
            GlobalEnvironment.addMathLibrary env.Globals
            
            let initialState = {
                Environment = env
                ReturnValues = None
                BreakFlag = false
            }
            
            let finalState = InterpreterCore.execBlock initialState program
            Success (finalState.ReturnValues |> Option.defaultValue [])
        
        with
        | LuaRuntimeError msg -> Error msg
        | ex -> Error $"Runtime error: {ex.Message}"
    
    /// Evaluate a single expression in a standard environment
    let evaluateExpression (expr: Expr) =
        try
            let env = GlobalEnvironment.createStandard ()
            GlobalEnvironment.addMathLibrary env.Globals
            
            let state = {
                Environment = env
                ReturnValues = None
                BreakFlag = false
            }
            
            let result = InterpreterCore.evalExpr state expr
            Success [result]
        
        with
        | LuaRuntimeError msg -> Error msg
        | ex -> Error $"Runtime error: {ex.Message}" 