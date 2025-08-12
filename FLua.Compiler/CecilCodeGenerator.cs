using System;
using System.Collections.Generic;
using System.Linq;
using BindingFlags = System.Reflection.BindingFlags;
using System.Runtime.InteropServices;
using FLua.Ast;
using FLua.Runtime;
using FLua.Common.Diagnostics;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace FLua.Compiler;

/// <summary>
/// Cecil-based code generator for direct IL generation
/// Generates efficient IL code with significantly smaller output size than Roslyn
/// </summary>
public class CecilCodeGenerator
{
    private readonly IDiagnosticCollector? _diagnostics;
    // Locals are now tracked in Scope.Locals
    private readonly Stack<Instruction> _breakTargets = new();
    
    // Cecil generation context
    private AssemblyDefinition? _assembly;
    private ModuleDefinition? _module;
    private TypeDefinition? _typeDefinition;
    private MethodDefinition? _currentMethod;
    private ILProcessor? _il;
    
    // Scope tracking for variable name mangling and IL locals
    private class Scope
    {
        public Dictionary<string, string> Variables { get; } = new Dictionary<string, string>();
        public Dictionary<string, SourceLocation> VariableLocations { get; } = new Dictionary<string, SourceLocation>();
        public Dictionary<string, VariableDefinition> Locals { get; } = new Dictionary<string, VariableDefinition>();
        public Scope? Parent { get; set; }
    }
    
    private Scope _currentScope = new Scope();
    
    // Cached type references
    private TypeReference? _luaValueType;
    private TypeReference? _luaEnvironmentType;
    private TypeReference? _luaValueArrayType;
    private TypeReference? _voidType;
    private TypeReference? _intType;
    private TypeReference? _stringType;
    private TypeReference? _boolType;
    
    public CecilCodeGenerator(IDiagnosticCollector? diagnostics = null)
    {
        _diagnostics = diagnostics;
    }
    
    /// <summary>
    /// Push a new scope for variable tracking
    /// </summary>
    private void PushScope()
    {
        var newScope = new Scope { Parent = _currentScope };
        _currentScope = newScope;
    }
    
    /// <summary>
    /// Pop the current scope
    /// </summary>
    private void PopScope()
    {
        if (_currentScope.Parent != null)
        {
            _currentScope = _currentScope.Parent;
        }
    }
    
    /// <summary>
    /// Find a local variable in the scope chain
    /// </summary>
    private VariableDefinition? FindLocal(string name)
    {
        var scope = _currentScope;
        while (scope != null)
        {
            if (scope.Locals.TryGetValue(name, out var local))
            {
                // Found the local variable
                return local;
            }
            scope = scope.Parent;
        }
        // Local not found - this might indicate a bug
        return null;
    }
    
    /// <summary>
    /// Generate Cecil assembly from Lua AST
    /// </summary>
    public AssemblyDefinition GenerateAssembly(IList<Statement> statements, CompilerOptions options)
    {
        var assemblyName = options.AssemblyName ?? "CompiledLuaScript";
        _assembly = AssemblyDefinition.CreateAssembly(
            new AssemblyNameDefinition(assemblyName, new Version(1, 0, 0, 0)),
            assemblyName,
            ModuleKind.Dll);
        
        _module = _assembly.MainModule;
        
        // Add reference to FLua.Runtime assembly
        var runtimeAssemblyPath = typeof(LuaValue).Assembly.Location;
        var runtimeAssembly = AssemblyDefinition.ReadAssembly(runtimeAssemblyPath);
        var runtimeReference = AssemblyNameReference.Parse(runtimeAssembly.FullName);
        _module.AssemblyReferences.Add(runtimeReference);
        
        InitializeTypeReferences();
        
        if (options.Target == CompilationTarget.ConsoleApp)
        {
            GenerateConsoleApplication(statements, options);
        }
        else
        {
            GenerateLibrary(statements, options);
        }
        
        return _assembly;
    }
    
    /// <summary>
    /// Initialize commonly used type references
    /// </summary>
    private void InitializeTypeReferences()
    {
        // Import runtime types
        _luaValueType = _module!.ImportReference(typeof(LuaValue));
        _luaEnvironmentType = _module.ImportReference(typeof(LuaEnvironment));
        _luaValueArrayType = _module.ImportReference(typeof(LuaValue[]));
        _voidType = _module.ImportReference(typeof(void));
        _intType = _module.ImportReference(typeof(int));
        _stringType = _module.ImportReference(typeof(string));
        _boolType = _module.ImportReference(typeof(bool));
    }
    
    /// <summary>
    /// Generate console application with Main method
    /// </summary>
    private void GenerateConsoleApplication(IList<Statement> statements, CompilerOptions options)
    {
        // Create Program class
        _typeDefinition = new TypeDefinition("", "Program",
            TypeAttributes.Public | TypeAttributes.Class,
            _module!.ImportReference(typeof(object)));
        _module.Types.Add(_typeDefinition);
        
        // Create Main method
        var mainMethod = new MethodDefinition("Main",
            MethodAttributes.Public | MethodAttributes.Static,
            _intType);
        
        // Add string[] args parameter
        mainMethod.Parameters.Add(new ParameterDefinition("args", 
            ParameterAttributes.None,
            _module.ImportReference(typeof(string[]))));
        
        _typeDefinition.Methods.Add(mainMethod);
        _currentMethod = mainMethod;
        _il = mainMethod.Body.GetILProcessor();
        
        // Initialize Lua environment
        var createStandardEnv = _module.ImportReference(
            typeof(LuaEnvironment).GetMethod("CreateStandardEnvironment"));
        _il.Emit(OpCodes.Call, createStandardEnv);
        
        var environmentLocal = new VariableDefinition(_luaEnvironmentType);
        mainMethod.Body.Variables.Add(environmentLocal);
        _il.Emit(OpCodes.Stloc, environmentLocal);
        
        // Generate statements
        foreach (var statement in statements)
        {
            GenerateStatement(statement, environmentLocal);
        }
        
        // Return 0
        _il.Emit(OpCodes.Ldc_I4_0);
        _il.Emit(OpCodes.Ret);
        
        // Set entry point
        if (_assembly is not null)
        {
            _assembly.EntryPoint = mainMethod;
        }
    }
    
    /// <summary>
    /// Generate library with public Execute method
    /// </summary>
    private void GenerateLibrary(IList<Statement> statements, CompilerOptions options)
    {
        // Create CompiledLuaScript.LuaScript class (nested class structure)
        var outerClass = new TypeDefinition("", "CompiledLuaScript",
            TypeAttributes.Public | TypeAttributes.Class,
            _module!.ImportReference(typeof(object)));
        _module.Types.Add(outerClass);
        
        _typeDefinition = new TypeDefinition("", "LuaScript",
            TypeAttributes.NestedPublic | TypeAttributes.Class,
            _module!.ImportReference(typeof(object)));
        outerClass.NestedTypes.Add(_typeDefinition);
        
        // Create Execute method
        var executeMethod = new MethodDefinition("Execute",
            MethodAttributes.Public | MethodAttributes.Static,
            _luaValueArrayType);
        
        // Add LuaEnvironment parameter
        executeMethod.Parameters.Add(new ParameterDefinition("env",
            ParameterAttributes.None,
            _luaEnvironmentType));
        
        _typeDefinition.Methods.Add(executeMethod);
        _currentMethod = executeMethod;
        _il = executeMethod.Body.GetILProcessor();
        
        var environmentLocal = new VariableDefinition(_luaEnvironmentType);
        executeMethod.Body.Variables.Add(environmentLocal);
        _il.Emit(OpCodes.Ldarg_0);
        _il.Emit(OpCodes.Stloc, environmentLocal);
        
        // Generate statements
        foreach (var statement in statements)
        {
            GenerateStatement(statement, environmentLocal);
        }
        
        // Return empty array
        _il.Emit(OpCodes.Ldc_I4_0);
        _il.Emit(OpCodes.Newarr, _luaValueType);
        _il.Emit(OpCodes.Ret);
    }
    
    /// <summary>
    /// Generate IL for a statement
    /// </summary>
    private void GenerateStatement(Statement statement, VariableDefinition environment)
    {
        switch (statement)
        {
            case Statement.FunctionCall funcCall:
                GenerateExpression(funcCall.Item, environment);
                _il!.Emit(OpCodes.Pop); // Discard return value for statement context
                break;
                
            case Statement.LocalAssignment localAssign:
                GenerateLocalAssignment(localAssign, environment);
                break;
                
            case Statement.Assignment assign:
                GenerateAssignment(assign, environment);
                break;
                
            case Statement.Return returnStmt:
                GenerateReturn(returnStmt, environment);
                break;
                
            case Statement.DoBlock doBlock:
                var blockStatements = FSharpListHelpers.ToList(doBlock.Item);
                foreach (var stmt in blockStatements)
                {
                    GenerateStatement(stmt, environment);
                }
                break;
                
            case Statement.If ifStmt:
                GenerateIfStatement(ifStmt, environment);
                break;
                
            case Statement.While whileStmt:
                GenerateWhileStatement(whileStmt, environment);
                break;
                
            case Statement.Repeat repeatStmt:
                GenerateRepeatStatement(repeatStmt, environment);
                break;
                
            case var stmt when stmt.IsBreak:
                GenerateBreakStatement();
                break;
                
            case Statement.NumericFor numFor:
                GenerateNumericForStatement(numFor, environment);
                break;
                
            default:
                ReportError($"Statement type {statement.GetType().Name} not yet implemented in Cecil backend", null);
                break;
        }
    }
    
    /// <summary>
    /// Generate IL for an expression
    /// </summary>
    private void GenerateExpression(Expr expression, VariableDefinition environment)
    {
        switch (expression)
        {
            case Expr.Literal literal:
                GenerateLiteral(literal.Item);
                break;
                
            case Expr.Var variable:
                GenerateVariable(variable.Item, environment);
                break;
                
            case Expr.VarPos variablePos:
                var (name, location) = (variablePos.Item1, variablePos.Item2);
                if (name == "load" || name == "loadfile" || name == "dofile")
                {
                    var diagnostic = DiagnosticBuilder.DynamicFeatureError(name, location);
                    _diagnostics?.Report(diagnostic);
                }
                GenerateVariable(name, environment);
                break;
                
            case Expr.FunctionCall funcCall:
                GenerateFunctionCall(funcCall.Item1, funcCall.Item2, environment);
                break;
                
            case Expr.FunctionCallPos funcCallPos:
                GenerateFunctionCall(funcCallPos.Item1, funcCallPos.Item2, environment);
                break;
                
            case Expr.Binary binary:
                GenerateBinaryOperation(binary.Item1, binary.Item2, binary.Item3, environment);
                break;
                
            case Expr.TableConstructor tableConstructor:
                GenerateTableConstructor(tableConstructor.Item, environment);
                break;
                
            case Expr.TableAccess tableAccess:
                GenerateTableAccess(tableAccess.Item1, tableAccess.Item2, environment);
                break;
                
            default:
                ReportError($"Expression type {expression.GetType().Name} not yet implemented in Cecil backend", null);
                // Push nil as fallback
                var nilGetter = _module!.ImportReference(
                    typeof(LuaValue).GetField("Nil"));
                _il!.Emit(OpCodes.Ldsfld, nilGetter);
                break;
        }
    }
    
    /// <summary>
    /// Generate IL for literal values
    /// </summary>
    private void GenerateLiteral(Literal literal)
    {
        if (literal.IsNil)
        {
            var nilGetter = _module!.ImportReference(
                typeof(LuaValue).GetField("Nil"));
            _il!.Emit(OpCodes.Ldsfld, nilGetter);
        }
        else if (literal.IsBoolean)
        {
            var boolLit = (Literal.Boolean)literal;
            _il!.Emit(boolLit.Item ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
            var boolMethod = _module!.ImportReference(
                typeof(LuaValue).GetMethod("Boolean", [typeof(bool)]));
            _il.Emit(OpCodes.Call, boolMethod);
        }
        else if (literal.IsInteger)
        {
            var intLit = (Literal.Integer)literal;
            _il!.Emit(OpCodes.Ldc_I8, (long)intLit.Item);
            var intMethod = _module!.ImportReference(
                typeof(LuaValue).GetMethod("Integer", [typeof(long)]));
            _il.Emit(OpCodes.Call, intMethod);
        }
        else if (literal.IsFloat)
        {
            var floatLit = (Literal.Float)literal;
            _il!.Emit(OpCodes.Ldc_R8, floatLit.Item);
            var floatMethod = _module!.ImportReference(
                typeof(LuaValue).GetMethod("Float", [typeof(double)]));
            _il.Emit(OpCodes.Call, floatMethod);
        }
        else if (literal.IsString)
        {
            var stringLit = (Literal.String)literal;
            _il!.Emit(OpCodes.Ldstr, stringLit.Item);
            var stringMethod = _module!.ImportReference(
                typeof(LuaValue).GetMethod("String", [typeof(string)]));
            _il.Emit(OpCodes.Call, stringMethod);
        }
    }
    
    /// <summary>
    /// Generate IL for variable access
    /// </summary>
    private void GenerateVariable(string name, VariableDefinition environment)
    {
        var local = FindLocal(name);
        if (local != null)
        {
            _il!.Emit(OpCodes.Ldloc, local);
        }
        else
        {
            // Load from environment
            _il!.Emit(OpCodes.Ldloc, environment);
            _il.Emit(OpCodes.Ldstr, name);
            var getVariable = _module!.ImportReference(
                typeof(LuaEnvironment).GetMethod("GetVariable"));
            _il.Emit(OpCodes.Call, getVariable);
        }
    }
    
    /// <summary>
    /// Generate IL for function calls
    /// </summary>
    private void GenerateFunctionCall(Expr func, FSharpList<Expr> args, VariableDefinition environment)
    {
        // Store function in a local first
        var funcLocal = new VariableDefinition(_luaValueType);
        _currentMethod!.Body.Variables.Add(funcLocal);
        
        // Generate function expression and store it
        GenerateExpression(func, environment);
        _il!.Emit(OpCodes.Stloc, funcLocal);
        
        // Convert F# list to array
        var argsList = FSharpListHelpers.ToList(args);
        
        // Generate arguments array
        _il.Emit(OpCodes.Ldc_I4, argsList.Count);
        _il.Emit(OpCodes.Newarr, _luaValueType);
        
        for (int i = 0; i < argsList.Count; i++)
        {
            _il.Emit(OpCodes.Dup);
            _il.Emit(OpCodes.Ldc_I4, i);
            GenerateExpression(argsList[i], environment);
            _il.Emit(OpCodes.Stelem_Any, _luaValueType);
        }
        
        // Store args array
        var argsLocal = new VariableDefinition(_luaValueArrayType);
        _currentMethod.Body.Variables.Add(argsLocal);
        _il.Emit(OpCodes.Stloc, argsLocal);
        
        // Check if it's a function
        _il.Emit(OpCodes.Ldloc, funcLocal);
        var isFunctionProperty = _module!.ImportReference(
            typeof(LuaValue).GetProperty("IsFunction")!.GetGetMethod());
        _il.Emit(OpCodes.Call, isFunctionProperty);
        
        var isFunction = _il.Create(OpCodes.Nop);
        _il.Emit(OpCodes.Brtrue, isFunction);
        
        // If not a function, throw error
        _il.Emit(OpCodes.Ldstr, "Attempt to call a non-function value");
        var exceptionCtor = _module.ImportReference(
            typeof(LuaRuntimeException).GetConstructor([typeof(string)]));
        _il.Emit(OpCodes.Newobj, exceptionCtor);
        _il.Emit(OpCodes.Throw);
        
        // If it is a function, extract and call it
        _il.Append(isFunction);
        _il.Emit(OpCodes.Ldloc, funcLocal);
        
        // Use the non-generic AsFunction() method
        var asFunctionMethods = typeof(LuaValue).GetMethods()
            .Where(m => m.Name == "AsFunction")
            .ToArray();
        
        var nonGenericAsFunction = asFunctionMethods.FirstOrDefault(m => !m.IsGenericMethodDefinition);
        if (nonGenericAsFunction == null)
        {
            throw new InvalidOperationException("Could not find non-generic AsFunction method on LuaValue");
        }
        
        var asFunctionMethod = _module.ImportReference(nonGenericAsFunction);
        _il.Emit(OpCodes.Call, asFunctionMethod);
        _il.Emit(OpCodes.Ldloc, argsLocal);
        var callMethod = _module.ImportReference(
            typeof(LuaFunction).GetMethod("Call"));
        _il.Emit(OpCodes.Callvirt, callMethod);
        
        // Get first result or nil
        var tempResults = new VariableDefinition(_luaValueArrayType);
        _currentMethod.Body.Variables.Add(tempResults);
        _il.Emit(OpCodes.Stloc, tempResults);
        
        var nilResult = _il.Create(OpCodes.Nop);
        var done = _il.Create(OpCodes.Nop);
        
        _il.Emit(OpCodes.Ldloc, tempResults);
        _il.Emit(OpCodes.Ldlen);
        _il.Emit(OpCodes.Conv_I4);
        _il.Emit(OpCodes.Ldc_I4_0);
        _il.Emit(OpCodes.Ble, nilResult);
        
        // Return first element
        _il.Emit(OpCodes.Ldloc, tempResults);
        _il.Emit(OpCodes.Ldc_I4_0);
        _il.Emit(OpCodes.Ldelem_Any, _luaValueType);
        _il.Emit(OpCodes.Br, done);
        
        // Return nil if no results
        _il.Append(nilResult);
        var nilGetter = _module.ImportReference(
            typeof(LuaValue).GetField("Nil"));
        _il.Emit(OpCodes.Ldsfld, nilGetter);
        
        _il.Append(done);
    }
    
    /// <summary>
    /// Generate IL for binary operations
    /// </summary>
    private void GenerateBinaryOperation(Expr left, BinaryOp op, Expr right, VariableDefinition environment)
    {
        GenerateExpression(left, environment);
        GenerateExpression(right, environment);
        
        string methodName = GetBinaryOpMethodName(op);
        
        var opMethod = _module!.ImportReference(
            typeof(LuaOperations).GetMethod(methodName));
        _il!.Emit(OpCodes.Call, opMethod);
    }
    
    /// <summary>
    /// Get the LuaOperations method name for a binary operator
    /// </summary>
    private string GetBinaryOpMethodName(BinaryOp op)
    {
        if (op.IsAdd) return "Add";
        if (op.IsSubtract) return "Subtract";
        if (op.IsMultiply) return "Multiply";
        if (op.IsFloatDiv) return "FloatDivide";
        if (op.IsEqual) return "Equal";
        if (op.IsLess) return "Less";
        if (op.IsLessEqual) return "LessEqual";
        if (op.IsGreater) return "Greater";
        if (op.IsGreaterEqual) return "GreaterEqual";
        if (op.IsNotEqual) return "NotEqual";
        if (op.IsConcat) return "Concat";
        if (op.IsAnd) return "And";
        if (op.IsOr) return "Or";
        
        throw new NotImplementedException($"Binary operator {op.GetType().Name} not implemented");
    }
    
    /// <summary>
    /// Generate IL for local variable assignment
    /// </summary>
    private void GenerateLocalAssignment(Statement.LocalAssignment localAssign, VariableDefinition environment)
    {
        var variables = FSharpListHelpers.ToList(localAssign.Item1);
        var expressions = localAssign.Item2.HasValue() ? 
            FSharpListHelpers.ToList(localAssign.Item2.Value) : [];
        
        for (int i = 0; i < variables.Count; i++)
        {
            var (varName, _) = variables[i];
            
            if (i < expressions.Count)
            {
                GenerateExpression(expressions[i], environment);
            }
            else
            {
                var nilGetter = _module!.ImportReference(
                    typeof(LuaValue).GetField("Nil"));
                _il!.Emit(OpCodes.Ldsfld, nilGetter);
            }
            
            var local = new VariableDefinition(_luaValueType);
            _currentMethod!.Body.Variables.Add(local);
            _il!.Emit(OpCodes.Stloc, local);
            _currentScope.Locals[varName] = local;
            
            // DEBUG: Verify the local was stored
            if (!_currentScope.Locals.ContainsKey(varName))
            {
                throw new InvalidOperationException($"Failed to store local variable '{varName}'");
            }
        }
    }
    
    /// <summary>
    /// Generate IL for variable assignment
    /// </summary>
    private void GenerateAssignment(Statement.Assignment assign, VariableDefinition environment)
    {
        var variables = FSharpListHelpers.ToList(assign.Item1);
        var expressions = FSharpListHelpers.ToList(assign.Item2);
        
        for (int i = 0; i < variables.Count; i++)
        {
            if (i < expressions.Count)
            {
                GenerateExpression(expressions[i], environment);
            }
            else
            {
                var nilGetter = _module!.ImportReference(
                    typeof(LuaValue).GetField("Nil"));
                _il!.Emit(OpCodes.Ldsfld, nilGetter);
            }
            
            // Store to variable
            if (variables[i] is Expr.Var variable)
            {
                var varName = variable.Item;
                var local = FindLocal(varName);
                if (local != null)
                {
                    // Debug: Add a comment to the IL
                    _il!.Emit(OpCodes.Nop); // Debug marker for found local
                    _il!.Emit(OpCodes.Stloc, local);
                }
                else
                {
                    // DEBUG: This path should not be taken for local variables
                    if (_currentScope.Locals.Count > 0)
                    {
                        var localVars = string.Join(", ", _currentScope.Locals.Keys);
                        ReportError($"Variable '{varName}' not found in locals. Available locals: {localVars}", null);
                    }
                    // Store value in temp
                    var tempLocal = new VariableDefinition(_luaValueType);
                    _currentMethod!.Body.Variables.Add(tempLocal);
                    _il!.Emit(OpCodes.Stloc, tempLocal);
                    
                    // Set in environment
                    _il.Emit(OpCodes.Ldloc, environment);
                    _il.Emit(OpCodes.Ldstr, varName);
                    _il.Emit(OpCodes.Ldloc, tempLocal);
                    var setVariable = _module!.ImportReference(
                        typeof(LuaEnvironment).GetMethod("SetVariable"));
                    _il.Emit(OpCodes.Call, setVariable);
                }
            }
            else if (variables[i] is Expr.TableAccess tableAccess)
            {
                // Table assignment: t[k] = v or t.k = v
                // Value is already on stack from GenerateExpression above
                
                // Store value in temp
                var valueLocal = new VariableDefinition(_luaValueType);
                _currentMethod!.Body.Variables.Add(valueLocal);
                _il!.Emit(OpCodes.Stloc, valueLocal);
                
                // Generate table expression
                GenerateExpression(tableAccess.Item1, environment);
                
                // Check if it's a table
                _il.Emit(OpCodes.Dup);
                var isTableProp = _module!.ImportReference(
                    typeof(LuaValue).GetProperty("IsTable")?.GetMethod!);
                _il.Emit(OpCodes.Call, isTableProp);
                
                var isTable = _il.Create(OpCodes.Nop);
                _il.Emit(OpCodes.Brtrue, isTable);
                
                // Not a table, throw error
                _il.Emit(OpCodes.Pop);
                _il.Emit(OpCodes.Ldstr, "Attempt to index a non-table value");
                var exceptionCtor = _module.ImportReference(
                    typeof(LuaRuntimeException).GetConstructor([typeof(string)]));
                _il.Emit(OpCodes.Newobj, exceptionCtor);
                _il.Emit(OpCodes.Throw);
                
                _il.Append(isTable);
                
                // Extract LuaTable from LuaValue
                var asTableMethod = _module.ImportReference(
                    typeof(LuaValue).GetMethod("AsTable")!.MakeGenericMethod(typeof(LuaTable)));
                _il.Emit(OpCodes.Call, asTableMethod);
                
                // Generate key expression
                GenerateExpression(tableAccess.Item2, environment);
                
                // Load value
                _il.Emit(OpCodes.Ldloc, valueLocal);
                
                // Call table.Set(key, value)
                var setMethod = _module.ImportReference(
                    typeof(LuaTable).GetMethod("Set"));
                _il.Emit(OpCodes.Callvirt, setMethod);
            }
            else
            {
                ReportError($"Assignment target type {variables[i].GetType().Name} not yet implemented", null);
            }
        }
    }
    
    /// <summary>
    /// Generate IL for return statement
    /// </summary>
    private void GenerateReturn(Statement.Return returnStmt, VariableDefinition environment)
    {
        if (returnStmt.Item.HasValue() && returnStmt.Item.Value.Count() > 0)
        {
            var exprs = FSharpListHelpers.ToList(returnStmt.Item.Value);
            GenerateExpression(exprs[0], environment);
        }
        else
        {
            var nilGetter = _module!.ImportReference(
                typeof(LuaValue).GetField("Nil"));
            _il!.Emit(OpCodes.Ldsfld, nilGetter);
        }
        _il!.Emit(OpCodes.Ret);
    }
    
    /// <summary>
    /// Generate IL for if statement
    /// </summary>
    private void GenerateIfStatement(Statement.If ifStmt, VariableDefinition environment)
    {
        var conditions = FSharpListHelpers.ToList(ifStmt.Item1);
        var elseBlock = ifStmt.Item2.HasValue() ? 
            FSharpListHelpers.ToList(ifStmt.Item2.Value) : 
            null;
        
        var endLabel = _il!.Create(OpCodes.Nop);
        var nextLabel = _il.Create(OpCodes.Nop);
        
        foreach (var (condition, block) in conditions)
        {
            GenerateExpression(condition, environment);
            var isTruthy = _module!.ImportReference(
                typeof(LuaValue).GetMethod("IsTruthy"));
            _il.Emit(OpCodes.Call, isTruthy);
            _il.Emit(OpCodes.Brfalse, nextLabel);
            
            var blockStatements = FSharpListHelpers.ToList(block);
            foreach (var stmt in blockStatements)
            {
                GenerateStatement(stmt, environment);
            }
            _il.Emit(OpCodes.Br, endLabel);
            
            _il.Append(nextLabel);
            nextLabel = _il.Create(OpCodes.Nop);
        }
        
        if (elseBlock != null)
        {
            foreach (var stmt in elseBlock)
            {
                GenerateStatement(stmt, environment);
            }
        }
        
        _il.Append(endLabel);
    }
    
    /// <summary>
    /// Generate IL for while loop
    /// </summary>
    private void GenerateWhileStatement(Statement.While whileStmt, VariableDefinition environment)
    {
        var (condition, block) = (whileStmt.Item1, whileStmt.Item2);
        
        var loopStart = _il!.Create(OpCodes.Nop);
        var loopEnd = _il.Create(OpCodes.Nop);
        
        _breakTargets.Push(loopEnd);
        
        _il.Append(loopStart);
        GenerateExpression(condition, environment);
        var isTruthy = _module!.ImportReference(
            typeof(LuaValue).GetMethod("IsTruthy"));
        _il.Emit(OpCodes.Call, isTruthy);
        _il.Emit(OpCodes.Brfalse, loopEnd);
        
        var blockStatements = FSharpListHelpers.ToList(block);
        foreach (var stmt in blockStatements)
        {
            GenerateStatement(stmt, environment);
        }
        
        _il.Emit(OpCodes.Br, loopStart);
        _il.Append(loopEnd);
        
        _breakTargets.Pop();
    }
    
    /// <summary>
    /// Generate IL for repeat/until loop
    /// </summary>
    private void GenerateRepeatStatement(Statement.Repeat repeatStmt, VariableDefinition environment)
    {
        var (block, condition) = (repeatStmt.Item1, repeatStmt.Item2);
        
        var loopStart = _il!.Create(OpCodes.Nop);
        var loopEnd = _il.Create(OpCodes.Nop);
        
        _breakTargets.Push(loopEnd);
        
        _il.Append(loopStart);
        
        // Execute block statements
        var blockStatements = FSharpListHelpers.ToList(block);
        foreach (var stmt in blockStatements)
        {
            GenerateStatement(stmt, environment);
        }
        
        // Evaluate condition
        GenerateExpression(condition, environment);
        var isTruthy = _module!.ImportReference(
            typeof(LuaValue).GetMethod("IsTruthy"));
        _il.Emit(OpCodes.Call, isTruthy);
        
        // Loop if condition is false (until means loop while false)
        _il.Emit(OpCodes.Brfalse, loopStart);
        
        _il.Append(loopEnd);
        _breakTargets.Pop();
    }
    
    /// <summary>
    /// Generate IL for break statement
    /// </summary>
    private void GenerateBreakStatement()
    {
        if (_breakTargets.Count == 0)
        {
            ReportError("Break statement outside of loop", null);
            return;
        }
        
        _il!.Emit(OpCodes.Br, _breakTargets.Peek());
    }
    
    /// <summary>
    /// Generate IL for numeric for loop
    /// </summary>
    private void GenerateNumericForStatement(Statement.NumericFor numFor, VariableDefinition environment)
    {
        var (varName, start, end, step) = (numFor.Item1, numFor.Item2, numFor.Item3, numFor.Item4);
        var block = numFor.Item5;
        
        // Create locals for loop variable, limit, and step
        var loopVar = new VariableDefinition(_luaValueType);
        var limitVar = new VariableDefinition(_luaValueType);
        var stepVar = new VariableDefinition(_luaValueType);
        
        _currentMethod!.Body.Variables.Add(loopVar);
        _currentMethod.Body.Variables.Add(limitVar);
        _currentMethod.Body.Variables.Add(stepVar);
        
        // Initialize loop variable
        GenerateExpression(start, environment);
        _il!.Emit(OpCodes.Stloc, loopVar);
        _currentScope.Locals[varName] = loopVar;
        
        // Initialize limit
        GenerateExpression(end, environment);
        _il.Emit(OpCodes.Stloc, limitVar);
        
        // Initialize step (default to 1 if not provided)
        if (step.HasValue())
        {
            GenerateExpression(step.Value, environment);
        }
        else
        {
            _il.Emit(OpCodes.Ldc_I8, 1L);
            var intMethod = _module!.ImportReference(
                typeof(LuaValue).GetMethod("Integer", [typeof(long)]));
            _il.Emit(OpCodes.Call, intMethod);
        }
        _il.Emit(OpCodes.Stloc, stepVar);
        
        var loopStart = _il.Create(OpCodes.Nop);
        var loopEnd = _il.Create(OpCodes.Nop);
        
        _breakTargets.Push(loopEnd);
        
        _il.Append(loopStart);
        
        // Check if we should continue looping
        // For positive step: loopVar <= limit
        // For negative step: loopVar >= limit
        _il.Emit(OpCodes.Ldloc, stepVar);
        _il.Emit(OpCodes.Ldc_I8, 0L);
        var zeroMethod = _module!.ImportReference(
            typeof(LuaValue).GetMethod("Integer", [typeof(long)]));
        _il.Emit(OpCodes.Call, zeroMethod);
        var greater = _module.ImportReference(
            typeof(LuaOperations).GetMethod("Greater"));
        _il.Emit(OpCodes.Call, greater);
        var isTruthy = _module.ImportReference(
            typeof(LuaValue).GetMethod("IsTruthy"));
        _il.Emit(OpCodes.Call, isTruthy);
        
        var checkNegativeStep = _il.Create(OpCodes.Nop);
        var executeBlock = _il.Create(OpCodes.Nop);
        
        _il.Emit(OpCodes.Brfalse, checkNegativeStep);
        
        // Positive step: check if loopVar <= limit
        _il.Emit(OpCodes.Ldloc, loopVar);
        _il.Emit(OpCodes.Ldloc, limitVar);
        var lessEqual = _module.ImportReference(
            typeof(LuaOperations).GetMethod("LessEqual"));
        _il.Emit(OpCodes.Call, lessEqual);
        _il.Emit(OpCodes.Call, isTruthy);
        _il.Emit(OpCodes.Brtrue, executeBlock);
        _il.Emit(OpCodes.Br, loopEnd);
        
        // Negative step: check if loopVar >= limit
        _il.Append(checkNegativeStep);
        _il.Emit(OpCodes.Ldloc, loopVar);
        _il.Emit(OpCodes.Ldloc, limitVar);
        var greaterEqual = _module.ImportReference(
            typeof(LuaOperations).GetMethod("GreaterEqual"));
        _il.Emit(OpCodes.Call, greaterEqual);
        _il.Emit(OpCodes.Call, isTruthy);
        _il.Emit(OpCodes.Brfalse, loopEnd);
        
        // Execute block
        _il.Append(executeBlock);
        var blockStatements = FSharpListHelpers.ToList(block);
        foreach (var stmt in blockStatements)
        {
            GenerateStatement(stmt, environment);
        }
        
        // Increment loop variable
        _il.Emit(OpCodes.Ldloc, loopVar);
        _il.Emit(OpCodes.Ldloc, stepVar);
        var add = _module.ImportReference(
            typeof(LuaOperations).GetMethod("Add"));
        _il.Emit(OpCodes.Call, add);
        _il.Emit(OpCodes.Stloc, loopVar);
        
        _il.Emit(OpCodes.Br, loopStart);
        _il.Append(loopEnd);
        
        _breakTargets.Pop();
        _currentScope.Locals.Remove(varName); // Remove loop variable from scope
    }
    
    /// <summary>
    /// Generate IL for table constructor
    /// </summary>
    private void GenerateTableConstructor(FSharpList<TableField> fields, VariableDefinition environment)
    {
        // Create new table
        var tableCtor = _module!.ImportReference(
            typeof(LuaTable).GetConstructor(Type.EmptyTypes));
        _il!.Emit(OpCodes.Newobj, tableCtor);
        
        // Convert to LuaValue and store in local for repeated access
        var tableMethod = _module.ImportReference(
            typeof(LuaValue).GetMethod("Table", [typeof(object)]));
        _il.Emit(OpCodes.Call, tableMethod);
        
        var tableLocal = new VariableDefinition(_luaValueType);
        _currentMethod!.Body.Variables.Add(tableLocal);
        _il.Emit(OpCodes.Stloc, tableLocal);
        
        var fieldsList = FSharpListHelpers.ToList(fields);
        int arrayIndex = 1; // Lua arrays start at 1
        
        foreach (var field in fieldsList)
        {
            _il.Emit(OpCodes.Ldloc, tableLocal); // Load LuaValue containing table
            
            // Extract the LuaTable from LuaValue
            var asTableGeneric = typeof(LuaValue).GetMethod("AsTable");
            var asTableMethod = _module.ImportReference(
                asTableGeneric!.MakeGenericMethod(typeof(LuaTable)));
            _il.Emit(OpCodes.Call, asTableMethod);
            
            if (field.IsExprField)
            {
                // Array-style field: {value}
                var exprField = (TableField.ExprField)field;
                
                // Push index
                _il.Emit(OpCodes.Ldc_I8, (long)arrayIndex);
                var intMethod = _module.ImportReference(
                    typeof(LuaValue).GetMethod("Integer", [typeof(long)]));
                _il.Emit(OpCodes.Call, intMethod);
                
                // Push value
                GenerateExpression(exprField.Item, environment);
                
                arrayIndex++;
            }
            else if (field.IsNamedField)
            {
                // Named field: {name = value}
                var namedField = (TableField.NamedField)field;
                
                // Push key (string)
                _il.Emit(OpCodes.Ldstr, namedField.Item1);
                var stringMethod = _module.ImportReference(
                    typeof(LuaValue).GetMethod("String", [typeof(string)]));
                _il.Emit(OpCodes.Call, stringMethod);
                
                // Push value
                GenerateExpression(namedField.Item2, environment);
            }
            else if (field.IsKeyField)
            {
                // Key field: {[expr] = value}
                var keyField = (TableField.KeyField)field;
                
                // Push key
                GenerateExpression(keyField.Item1, environment);
                
                // Push value
                GenerateExpression(keyField.Item2, environment);
            }
            
            // Call table.Set(key, value)
            var setMethod = _module.ImportReference(
                typeof(LuaTable).GetMethod("Set"));
            _il.Emit(OpCodes.Callvirt, setMethod);
        }
        
        // Leave table on stack
        _il.Emit(OpCodes.Ldloc, tableLocal);
    }
    
    /// <summary>
    /// Generate IL for table access
    /// </summary>
    private void GenerateTableAccess(Expr table, Expr key, VariableDefinition environment)
    {
        // Generate table expression (returns LuaValue)
        GenerateExpression(table, environment);
        
        // Store table value in local
        var tableValueLocal = new VariableDefinition(_luaValueType);
        _currentMethod!.Body.Variables.Add(tableValueLocal);
        _il!.Emit(OpCodes.Stloc, tableValueLocal);
        
        // Check if it's a table
        _il.Emit(OpCodes.Ldloc, tableValueLocal);
        var isTableProp = _module!.ImportReference(
            typeof(LuaValue).GetProperty("IsTable")?.GetMethod!);
        _il.Emit(OpCodes.Call, isTableProp);
        
        var isTable = _il.Create(OpCodes.Nop);
        _il.Emit(OpCodes.Brtrue, isTable);
        
        // Not a table, throw error
        _il.Emit(OpCodes.Ldstr, "Attempt to index a non-table value");
        var exceptionCtor = _module.ImportReference(
            typeof(LuaRuntimeException).GetConstructor([typeof(string)]));
        _il.Emit(OpCodes.Newobj, exceptionCtor);
        _il.Emit(OpCodes.Throw);
        
        _il.Append(isTable);
        
        // Extract LuaTable from LuaValue
        _il.Emit(OpCodes.Ldloc, tableValueLocal);
        var asTableMethod = _module.ImportReference(
            typeof(LuaValue).GetMethod("AsTable")?.MakeGenericMethod(typeof(LuaTable))!);
        _il.Emit(OpCodes.Call, asTableMethod);
        
        // Generate key expression
        GenerateExpression(key, environment);
        
        // Call table.Get(key)
        var getMethod = _module.ImportReference(
            typeof(LuaTable).GetMethod("Get"));
        _il.Emit(OpCodes.Callvirt, getMethod);
    }
    
    /// <summary>
    /// Report compilation error using diagnostic system
    /// </summary>
    private void ReportError(string message, SourceLocation? location)
    {
        var diagnostic = new FLuaDiagnostic(
            ErrorCodes.CodeGenerationFailure,
            ErrorSeverity.Error,
            message,
            location
        );
        _diagnostics?.Report(diagnostic);
    }
}