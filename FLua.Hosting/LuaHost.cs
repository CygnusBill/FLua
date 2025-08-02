using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FLua.Ast;
using FLua.Compiler;
using FLua.Hosting.Environment;
using FLua.Hosting.Security;
using FLua.Interpreter;
using FLua.Parser;
using FLua.Runtime;
using FLua.Common.Diagnostics;
using Microsoft.FSharp.Collections;

namespace FLua.Hosting;

/// <summary>
/// Main implementation of the FLua hosting infrastructure.
/// Provides secure execution, compilation, and management of Lua scripts.
/// </summary>
public class LuaHost : ILuaHost
{
    private readonly IEnvironmentProvider _environmentProvider;
    private readonly ILuaCompiler _compiler;
    private readonly ILuaSecurityPolicy _securityPolicy;
    private readonly LuaInterpreter _interpreter;
    
    public LuaHostOptions DefaultOptions { get; set; } = new LuaHostOptions();
    public ILuaSecurityPolicy SecurityPolicy { get; set; }
    public IModuleResolver ModuleResolver { get; set; } = null!;
    
    public LuaHost(
        IEnvironmentProvider? environmentProvider = null,
        ILuaCompiler? compiler = null,
        ILuaSecurityPolicy? securityPolicy = null)
    {
        _securityPolicy = securityPolicy ?? new StandardSecurityPolicy();
        SecurityPolicy = _securityPolicy;
        _environmentProvider = environmentProvider ?? new FilteredEnvironmentProvider(_securityPolicy);
        _compiler = compiler ?? new RoslynLuaCompiler();
        _interpreter = new LuaInterpreter();
    }
    
    public LuaValue Execute(string luaCode, LuaHostOptions? options = null)
    {
        options ??= new LuaHostOptions();
        var validation = ValidateCode(luaCode);
        
        if (!validation.IsValid)
        {
            throw new LuaRuntimeException($"Invalid Lua code: {string.Join("; ", validation.Errors)}");
        }
        
        // Create filtered environment based on trust level
        var env = _environmentProvider.CreateEnvironment(options.TrustLevel, options);
        
        // Parse the code
        FSharpList<Statement> statements;
        try
        {
            statements = ParserHelper.ParseString(luaCode);
        }
        catch (Exception ex)
        {
            throw new LuaRuntimeException($"Parse error: {ex.Message}");
        }
        
        // Execute with timeout if specified
        if (options.ExecutionTimeout.HasValue)
        {
            using var cts = new CancellationTokenSource(options.ExecutionTimeout.Value);
            var task = Task.Run(() => ExecuteInternal(statements, env), cts.Token);
            
            try
            {
                task.Wait(cts.Token);
                return task.Result;
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException($"Script execution exceeded timeout of {options.ExecutionTimeout.Value}");
            }
        }
        
        return ExecuteInternal(statements, env);
    }
    
    private LuaValue ExecuteInternal(FSharpList<Statement> statements, LuaEnvironment env)
    {
        var results = _interpreter.ExecuteStatements(statements);
        return results.Length > 0 ? results[0] : LuaValue.Nil;
    }
    
    public async Task<LuaValue> ExecuteAsync(string luaCode, LuaHostOptions? options = null, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => Execute(luaCode, options), cancellationToken);
    }
    
    public Func<T> CompileToFunction<T>(string luaCode, LuaHostOptions? options = null)
    {
        var del = CompileToDelegate(luaCode, typeof(Func<T>), null, options);
        return (Func<T>)del;
    }
    
    public Delegate CompileToDelegate(string luaCode, Type delegateType, string[]? parameterNames = null, LuaHostOptions? options = null)
    {
        options ??= new LuaHostOptions();
        
        // Validate the code first
        var validation = ValidateCode(luaCode);
        if (!validation.IsValid)
        {
            throw new LuaRuntimeException($"Invalid Lua code: {string.Join("; ", validation.Errors)}");
        }
        
        // Parse the code
        FSharpList<Statement> statements;
        try
        {
            statements = ParserHelper.ParseString(luaCode);
        }
        catch (Exception ex)
        {
            throw new LuaRuntimeException($"Parse error: {ex.Message}");
        }
        
        // Configure compiler options for lambda generation
        var compilerOptions = options.CompilerOptions ?? new CompilerOptions(
            OutputPath: null!,
            Target: CompilationTarget.Lambda,
            AssemblyName: $"LuaLambda_{Guid.NewGuid():N}",
            GenerateInMemory: true
        );
        
        // Parse the code first
        var stmts = ParserHelper.ParseString(luaCode);
        
        // Compile to assembly
        var result = _compiler.Compile(System.Linq.Enumerable.ToList(stmts), compilerOptions);
        
        if (!result.Success)
        {
            throw new LuaRuntimeException($"Compilation failed: {string.Join("; ", result.Errors ?? Enumerable.Empty<string>())}");
        }
        
        // For now, return a placeholder delegate
        // TODO: Extract the actual delegate from the compiled assembly
        if (result.Assembly != null)
        {
            // Need to extract the delegate from the assembly
            throw new NotImplementedException("Lambda extraction from assembly not yet implemented");
        }
        
        throw new NotImplementedException("Lambda generation not yet implemented in compiler");
    }
    
    public Expression<Func<T>> CompileToExpression<T>(string luaCode, LuaHostOptions? options = null)
    {
        options ??= new LuaHostOptions();
        
        // Validate the code first
        var validation = ValidateCode(luaCode);
        if (!validation.IsValid)
        {
            throw new LuaRuntimeException($"Invalid Lua code: {string.Join("; ", validation.Errors)}");
        }
        
        // Configure compiler options for expression tree generation
        var compilerOptions = options.CompilerOptions ?? new CompilerOptions(
            OutputPath: null!,
            Target: CompilationTarget.Expression,
            AssemblyName: $"LuaExpression_{Guid.NewGuid():N}",
            GenerateExpressionTree: true,
            GenerateInMemory: true
        );
        
        // Parse the code first
        var stmts = ParserHelper.ParseString(luaCode);
        
        // Compile to expression
        var result = _compiler.Compile(System.Linq.Enumerable.ToList(stmts), compilerOptions);
        
        if (!result.Success)
        {
            throw new LuaRuntimeException($"Compilation failed: {string.Join("; ", result.Errors ?? Enumerable.Empty<string>())}");
        }
        
        // For now, return a placeholder expression
        // TODO: Extract the actual expression from the compiled result
        if (result.Assembly != null)
        {
            // Need to extract the expression from the assembly
            throw new NotImplementedException("Expression extraction from assembly not yet implemented");
        }
        
        throw new NotImplementedException("Expression tree generation not yet implemented in compiler");
    }
    
    public Assembly CompileToAssembly(string luaCode, LuaHostOptions? options = null)
    {
        options ??= new LuaHostOptions();
        
        // Validate the code first
        var validation = ValidateCode(luaCode);
        if (!validation.IsValid)
        {
            throw new LuaRuntimeException($"Invalid Lua code: {string.Join("; ", validation.Errors)}");
        }
        
        // Use provided compiler options or create defaults
        var compilerOptions = options.CompilerOptions ?? new CompilerOptions(
            OutputPath: System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"LuaAssembly_{Guid.NewGuid():N}.dll"),
            Target: CompilationTarget.Library,
            AssemblyName: $"LuaAssembly_{Guid.NewGuid():N}"
        );
        
        // Parse the code first
        var stmts = ParserHelper.ParseString(luaCode);
        
        // Compile to assembly
        var result = _compiler.Compile(System.Linq.Enumerable.ToList(stmts), compilerOptions);
        
        if (!result.Success)
        {
            throw new LuaRuntimeException($"Compilation failed: {string.Join("; ", result.Errors ?? Enumerable.Empty<string>())}");
        }
        
        if (result.Assembly != null)
        {
            // Load assembly from bytes
            return Assembly.Load(result.Assembly);
        }
        
        // Load the assembly from the output path
        if (!string.IsNullOrEmpty(compilerOptions.OutputPath) && System.IO.File.Exists(compilerOptions.OutputPath))
        {
            return Assembly.LoadFrom(compilerOptions.OutputPath);
        }
        
        throw new InvalidOperationException("Failed to compile or load assembly");
    }
    
    public byte[] CompileToBytes(string luaCode, LuaHostOptions? options = null)
    {
        options ??= new LuaHostOptions();
        
        // Compile to assembly first
        var tempPath = System.IO.Path.GetTempFileName();
        try
        {
            var compilerOptions = new CompilerOptions(
                OutputPath: tempPath,
                Target: CompilationTarget.Library,
                AssemblyName: $"LuaAssembly_{Guid.NewGuid():N}"
            );
            
            options = options with { CompilerOptions = compilerOptions };
            var assembly = CompileToAssembly(luaCode, options);
            
            // Read the assembly bytes
            if (System.IO.File.Exists(tempPath))
            {
                return System.IO.File.ReadAllBytes(tempPath);
            }
            
            throw new InvalidOperationException("Failed to read compiled assembly bytes");
        }
        finally
        {
            // Clean up temp file
            if (System.IO.File.Exists(tempPath))
            {
                System.IO.File.Delete(tempPath);
            }
        }
    }
    
    public LuaEnvironment CreateFilteredEnvironment(TrustLevel trustLevel, LuaHostOptions? options = null)
    {
        options ??= new LuaHostOptions { TrustLevel = trustLevel };
        return _environmentProvider.CreateEnvironment(trustLevel, options);
    }
    
    public ValidationResult ValidateCode(string luaCode)
    {
        try
        {
            // Try to parse the code
            var statements = ParserHelper.ParseString(luaCode);
            
            // If we got here, the code is valid
            return new ValidationResult { IsValid = true, Errors = new List<string>() };
        }
        catch (Exception ex)
        {
            return new ValidationResult 
            { 
                IsValid = false, 
                Errors = new List<string> { $"Validation error: {ex.Message}" }
            };
        }
    }
}