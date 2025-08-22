using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FLua.Common;
using FLua.Runtime;
using FLua.Hosting.Security;

namespace FLua.Hosting
{
    /// <summary>
    /// Backward compatibility adapter that wraps IResultLuaHost to provide the original ILuaHost interface.
    /// This allows gradual migration of client code while maintaining existing API contracts.
    /// </summary>
    public class LuaHostAdapter : ILuaHost
    {
        private readonly IResultLuaHost _resultHost;
        
        public LuaHostAdapter(IResultLuaHost resultHost)
        {
            _resultHost = resultHost ?? throw new ArgumentNullException(nameof(resultHost));
        }
        
        public LuaHostOptions DefaultOptions 
        { 
            get => _resultHost.DefaultOptions; 
            set => _resultHost.DefaultOptions = value; 
        }
        
        public ILuaSecurityPolicy SecurityPolicy 
        { 
            get => _resultHost.SecurityPolicy; 
            set => _resultHost.SecurityPolicy = value; 
        }
        
        public IModuleResolver ModuleResolver 
        { 
            get => _resultHost.ModuleResolver; 
            set => _resultHost.ModuleResolver = value; 
        }
        
        public Func<T> CompileToFunction<T>(string luaCode, LuaHostOptions? options = null)
        {
            var result = _resultHost.CompileToFunctionResult<T>(luaCode, options);
            
            if (result.IsSuccess)
                return result.Value;
            
            ThrowHostingException(result.Diagnostics, result.ExecutionContext, "Compilation failed");
            return null!; // Never reached
        }
        
        public Delegate CompileToDelegate(string luaCode, Type delegateType, string[]? parameterNames = null, LuaHostOptions? options = null)
        {
            var result = _resultHost.CompileToDelegateResult(luaCode, delegateType, parameterNames, options);
            
            if (result.IsSuccess)
                return result.Value;
            
            ThrowHostingException(result.Diagnostics, result.ExecutionContext, "Delegate compilation failed");
            return null!; // Never reached
        }
        
        public Expression<Func<T>> CompileToExpression<T>(string luaCode, LuaHostOptions? options = null)
        {
            var result = _resultHost.CompileToExpressionResult<T>(luaCode, options);
            
            if (result.IsSuccess)
                return result.Value;
            
            ThrowHostingException(result.Diagnostics, result.ExecutionContext, "Expression compilation failed");
            return null!; // Never reached
        }
        
        public Assembly CompileToAssembly(string luaCode, LuaHostOptions? options = null)
        {
            var result = _resultHost.CompileToAssemblyResult(luaCode, options);
            
            if (result.IsSuccess)
                return result.Value;
            
            ThrowHostingException(result.Diagnostics, result.ExecutionContext, "Assembly compilation failed");
            return null!; // Never reached
        }
        
        public byte[] CompileToBytes(string luaCode, LuaHostOptions? options = null)
        {
            var result = _resultHost.CompileToBytesResult(luaCode, options);
            
            if (result.IsSuccess)
                return result.Value;
            
            ThrowHostingException(result.Diagnostics, result.ExecutionContext, "Byte compilation failed");
            return null!; // Never reached
        }
        
        public LuaValue Execute(string luaCode, LuaHostOptions? options = null)
        {
            var result = _resultHost.ExecuteResult(luaCode, options);
            
            if (result.IsSuccess)
                return result.Value;
            
            ThrowHostingException(result.Diagnostics, result.ExecutionContext, "Execution failed");
            return default!; // Never reached
        }
        
        public async Task<LuaValue> ExecuteAsync(string luaCode, LuaHostOptions? options = null, CancellationToken cancellationToken = default)
        {
            var result = await _resultHost.ExecuteResultAsync(luaCode, options, cancellationToken);
            
            if (result.IsSuccess)
                return result.Value;
            
            ThrowHostingException(result.Diagnostics, result.ExecutionContext, "Async execution failed");
            return default!; // Never reached
        }
        
        public LuaEnvironment CreateFilteredEnvironment(TrustLevel trustLevel, LuaHostOptions? options = null)
        {
            var result = _resultHost.CreateFilteredEnvironmentResult(trustLevel, options);
            
            if (result.IsSuccess)
                return result.Value;
            
            ThrowHostingException(result.Diagnostics, result.ExecutionContext, "Environment creation failed");
            return null!; // Never reached
        }
        
        public ValidationResult ValidateCode(string luaCode)
        {
            var result = _resultHost.ValidateCodeResult(luaCode);
            
            if (result.IsSuccess)
            {
                var validationInfo = result.Value;
                
                // Convert enhanced ValidationInfo to legacy ValidationResult
                if (validationInfo.IsValid)
                {
                    var warnings = validationInfo.SemanticWarnings.Select(w => w.Message)
                        .Concat(validationInfo.PerformanceHints.Select(h => h.Message))
                        .Concat(validationInfo.SecurityViolations.Where(v => false).Select(v => v.Message)) // No security violations in valid case
                        .ToList();
                    
                    return new ValidationResult 
                    { 
                        IsValid = true,
                        Warnings = warnings
                    };
                }
                else
                {
                    var errors = validationInfo.SyntaxErrors.Select(e => e.Message).ToList();
                    var warnings = validationInfo.SecurityViolations.Select(v => v.Message).ToList();
                    
                    return new ValidationResult 
                    { 
                        IsValid = false,
                        Errors = errors,
                        Warnings = warnings
                    };
                }
            }
            
            // Convert diagnostics to legacy format
            var resultErrors = result.Errors.Select(d => d.Message).ToList();
            var resultWarnings = result.Warnings.Select(d => d.Message).ToList();
            
            return new ValidationResult
            {
                IsValid = false,
                Errors = resultErrors,
                Warnings = resultWarnings
            };
        }
        
        /// <summary>
        /// Helper method to convert hosting diagnostics to appropriate exceptions
        /// </summary>
        private static void ThrowHostingException(IEnumerable<HostingDiagnostic> diagnostics, ExecutionContext? context, string operation)
        {
            var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            var warnings = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning).ToList();
            
            if (!errors.Any())
            {
                throw new InvalidOperationException($"{operation}: Unknown error occurred");
            }
            
            var primaryError = errors.First();
            var errorMessage = $"{operation}: {primaryError.Message}";
            
            if (errors.Count > 1)
            {
                var additionalErrors = string.Join("; ", errors.Skip(1).Select(e => e.Message));
                errorMessage += $" (Additional errors: {additionalErrors})";
            }
            
            if (warnings.Any())
            {
                var warningMessages = string.Join("; ", warnings.Select(w => w.Message));
                errorMessage += $" (Warnings: {warningMessages})";
            }
            
            if (context != null)
            {
                errorMessage += $" (Context: {context})";
            }
            
            // Choose appropriate exception type based on operation
            switch (primaryError.Operation)
            {
                case HostingOperation.Parsing:
                case HostingOperation.Validation:
                    throw new ArgumentException(errorMessage);
                    
                case HostingOperation.SecurityCheck:
                    throw new UnauthorizedAccessException(errorMessage);
                    
                case HostingOperation.Execution:
                    if (errorMessage.Contains("timeout") || errorMessage.Contains("cancelled"))
                        throw new TimeoutException(errorMessage);
                    else
                        throw new LuaRuntimeException(errorMessage);
                    
                case HostingOperation.Compilation:
                case HostingOperation.AssemblyGeneration:
                case HostingOperation.ExpressionTreeGeneration:
                    throw new InvalidOperationException(errorMessage);
                    
                case HostingOperation.ModuleResolution:
                    throw new System.IO.FileNotFoundException(errorMessage);
                    
                case HostingOperation.EnvironmentCreation:
                    throw new InvalidOperationException(errorMessage);
                    
                default:
                    throw new Exception(errorMessage);
            }
        }
    }

    /// <summary>
    /// Reverse adapter that wraps legacy ILuaHost to provide IResultLuaHost interface
    /// </summary>
    [Obsolete("This adapter is provided for migration purposes. Use ResultLuaHost directly for new code.")]
    public class ResultLuaHostAdapter : IResultLuaHost
    {
        private readonly ILuaHost _legacyHost;
        
        public ResultLuaHostAdapter(ILuaHost legacyHost)
        {
            _legacyHost = legacyHost ?? throw new ArgumentNullException(nameof(legacyHost));
        }
        
        public LuaHostOptions DefaultOptions 
        { 
            get => _legacyHost.DefaultOptions; 
            set => _legacyHost.DefaultOptions = value; 
        }
        
        public ILuaSecurityPolicy SecurityPolicy 
        { 
            get => _legacyHost.SecurityPolicy; 
            set => _legacyHost.SecurityPolicy = value; 
        }
        
        public IModuleResolver ModuleResolver 
        { 
            get => _legacyHost.ModuleResolver; 
            set => _legacyHost.ModuleResolver = value; 
        }
        
        public HostingResult<Func<T>> CompileToFunctionResult<T>(string luaCode, LuaHostOptions? options = null)
        {
            try
            {
                var result = _legacyHost.CompileToFunction<T>(luaCode, options);
                return HostingResult<Func<T>>.Success(result);
            }
            catch (Exception ex)
            {
                return HostingResult<Func<T>>.FromException(ex, HostingOperation.Compilation);
            }
        }
        
        public HostingResult<Delegate> CompileToDelegateResult(string luaCode, Type delegateType, string[]? parameterNames = null, LuaHostOptions? options = null)
        {
            try
            {
                var result = _legacyHost.CompileToDelegate(luaCode, delegateType, parameterNames, options);
                return HostingResult<Delegate>.Success(result);
            }
            catch (Exception ex)
            {
                return HostingResult<Delegate>.FromException(ex, HostingOperation.Compilation);
            }
        }
        
        public HostingResult<Expression<Func<T>>> CompileToExpressionResult<T>(string luaCode, LuaHostOptions? options = null)
        {
            try
            {
                var result = _legacyHost.CompileToExpression<T>(luaCode, options);
                return HostingResult<Expression<Func<T>>>.Success(result);
            }
            catch (Exception ex)
            {
                return HostingResult<Expression<Func<T>>>.FromException(ex, HostingOperation.ExpressionTreeGeneration);
            }
        }
        
        public HostingResult<Assembly> CompileToAssemblyResult(string luaCode, LuaHostOptions? options = null)
        {
            try
            {
                var result = _legacyHost.CompileToAssembly(luaCode, options);
                return HostingResult<Assembly>.Success(result);
            }
            catch (Exception ex)
            {
                return HostingResult<Assembly>.FromException(ex, HostingOperation.AssemblyGeneration);
            }
        }
        
        public HostingResult<byte[]> CompileToBytesResult(string luaCode, LuaHostOptions? options = null)
        {
            try
            {
                var result = _legacyHost.CompileToBytes(luaCode, options);
                return HostingResult<byte[]>.Success(result);
            }
            catch (Exception ex)
            {
                return HostingResult<byte[]>.FromException(ex, HostingOperation.AssemblyGeneration);
            }
        }
        
        public HostingResult<LuaValue> ExecuteResult(string luaCode, LuaHostOptions? options = null)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var result = _legacyHost.Execute(luaCode, options);
                stopwatch.Stop();
                
                var context = new ExecutionContext(stopwatch.Elapsed, trustLevel: options?.TrustLevel.ToString());
                return HostingResult<LuaValue>.Success(result, context: context);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var context = new ExecutionContext(stopwatch.Elapsed);
                return HostingResult<LuaValue>.FromException(ex, HostingOperation.Execution, context: context);
            }
        }
        
        public async Task<HostingResult<LuaValue>> ExecuteResultAsync(string luaCode, LuaHostOptions? options = null, CancellationToken cancellationToken = default)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var result = await _legacyHost.ExecuteAsync(luaCode, options, cancellationToken);
                stopwatch.Stop();
                
                var context = new ExecutionContext(stopwatch.Elapsed, trustLevel: options?.TrustLevel.ToString());
                return HostingResult<LuaValue>.Success(result, context: context);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                var context = new ExecutionContext(stopwatch.Elapsed);
                return HostingResult<LuaValue>.FromException(ex, HostingOperation.Execution, context: context);
            }
        }
        
        public HostingResult<LuaEnvironment> CreateFilteredEnvironmentResult(TrustLevel trustLevel, LuaHostOptions? options = null)
        {
            try
            {
                var result = _legacyHost.CreateFilteredEnvironment(trustLevel, options);
                return HostingResult<LuaEnvironment>.Success(result);
            }
            catch (Exception ex)
            {
                return HostingResult<LuaEnvironment>.FromException(ex, HostingOperation.EnvironmentCreation);
            }
        }
        
        public HostingResult<ValidationInfo> ValidateCodeResult(string luaCode)
        {
            try
            {
                var legacyResult = _legacyHost.ValidateCode(luaCode);
                
                // Convert legacy ValidationResult to enhanced ValidationInfo
                if (legacyResult.IsValid)
                {
                    var warnings = legacyResult.Warnings.Select(w => new SemanticWarning(w, "Legacy", null, null)).ToList();
                    var validationInfo = ValidationInfo.Valid(warnings: warnings);
                    return HostingResult<ValidationInfo>.Success(validationInfo);
                }
                else
                {
                    var syntaxErrors = legacyResult.Errors.Select(e => new SyntaxError(e, 0, 0)).ToList();
                    var validationInfo = ValidationInfo.Invalid(syntaxErrors);
                    return HostingResult<ValidationInfo>.Success(validationInfo);
                }
            }
            catch (Exception ex)
            {
                return HostingResult<ValidationInfo>.FromException(ex, HostingOperation.Validation);
            }
        }
    }

    /// <summary>
    /// Static factory methods for creating hosting instances
    /// </summary>
    public static class LuaHostFactory
    {
        /// <summary>
        /// Creates a new Result-based Lua host with optional dependencies
        /// </summary>
        public static IResultLuaHost CreateResultHost(
            Environment.IResultEnvironmentProvider? environmentProvider = null,
            IResultLuaCompiler? compiler = null,
            ILuaSecurityPolicy? securityPolicy = null)
        {
            return new ResultLuaHost(
                environmentProvider != null ? new EnvironmentProviderAdapter(environmentProvider) : null,
                compiler != null ? new CompilerAdapter(compiler) : null,
                securityPolicy);
        }
        
        /// <summary>
        /// Creates a legacy-compatible Lua host that uses Result pattern internally
        /// </summary>
        public static ILuaHost CreateCompatibleHost(
            Environment.IResultEnvironmentProvider? environmentProvider = null,
            IResultLuaCompiler? compiler = null,
            ILuaSecurityPolicy? securityPolicy = null)
        {
            var resultHost = CreateResultHost(environmentProvider, compiler, securityPolicy);
            return new LuaHostAdapter(resultHost);
        }
        
        /// <summary>
        /// Wraps an existing legacy host to provide Result-based interface (for migration)
        /// </summary>
        [Obsolete("Use CreateResultHost for new code. This method is provided for migration purposes only.")]
        public static IResultLuaHost WrapLegacyHost(ILuaHost legacyHost)
        {
            return new ResultLuaHostAdapter(legacyHost);
        }
    }

    // Internal adapters to bridge interfaces
    internal class EnvironmentProviderAdapter : Environment.IEnvironmentProvider
    {
        private readonly Environment.IResultEnvironmentProvider _resultProvider;
        
        public EnvironmentProviderAdapter(Environment.IResultEnvironmentProvider resultProvider)
        {
            _resultProvider = resultProvider;
        }
        
        public LuaEnvironment CreateEnvironment(TrustLevel trustLevel, LuaHostOptions? options = null)
        {
            var result = _resultProvider.CreateEnvironmentResult(trustLevel, options);
            
            if (result.IsSuccess)
                return result.Value;
            
            var errors = string.Join("; ", result.Errors.Select(e => e.Message));
            throw new InvalidOperationException($"Environment creation failed: {errors}");
        }
        
        public void ConfigureModuleSystem(LuaEnvironment environment, IModuleResolver? moduleResolver, TrustLevel trustLevel)
        {
            var result = _resultProvider.ConfigureModuleSystemResult(environment, moduleResolver, trustLevel);
            
            if (!result.IsSuccess)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Message));
                throw new InvalidOperationException($"Module system configuration failed: {errors}");
            }
        }
        
        public void AddHostFunctions(LuaEnvironment environment, Dictionary<string, Func<LuaValue[], LuaValue>> hostFunctions)
        {
            var result = _resultProvider.AddHostFunctionsResult(environment, hostFunctions);
            
            if (!result.IsSuccess)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Message));
                throw new InvalidOperationException($"Host functions addition failed: {errors}");
            }
        }
        
        public void InjectHostContext(LuaEnvironment environment, Dictionary<string, object> hostContext)
        {
            var result = _resultProvider.InjectHostContextResult(environment, hostContext);
            
            if (!result.IsSuccess)
            {
                var errors = string.Join("; ", result.Errors.Select(e => e.Message));
                throw new InvalidOperationException($"Host context injection failed: {errors}");
            }
        }
    }

    internal class CompilerAdapter : Compiler.ILuaCompiler
    {
        private readonly IResultLuaCompiler _resultCompiler;
        
        public CompilerAdapter(IResultLuaCompiler resultCompiler)
        {
            _resultCompiler = resultCompiler;
        }
        
        public IEnumerable<Compiler.CompilationTarget> SupportedTargets => _resultCompiler.SupportedTargets;
        public string BackendName => _resultCompiler.BackendName;
        
        public Compiler.CompilationResult Compile(IList<Ast.Statement> ast, Compiler.CompilerOptions options)
        {
            var result = _resultCompiler.CompileResult(ast, options);
            
            if (result.IsSuccess)
            {
                var output = result.Value;
                return new Compiler.CompilationResult(
                    Success: true,
                    Assembly: output.Assembly,
                    AssemblyPath: output.AssemblyPath,
                    CompiledDelegate: output.CompiledDelegate,
                    ExpressionTree: output.ExpressionTree,
                    GeneratedType: output.GeneratedType,
                    Warnings: result.Warnings.Select(w => w.Message)
                );
            }
            else
            {
                return new Compiler.CompilationResult(
                    Success: false,
                    Errors: result.Errors.Select(e => e.Message),
                    Warnings: result.Warnings.Select(w => w.Message)
                );
            }
        }
    }
}