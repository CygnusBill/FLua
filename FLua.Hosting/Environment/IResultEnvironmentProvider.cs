using System;
using System.Collections.Generic;
using FLua.Common;
using FLua.Runtime;
using FLua.Hosting.Security;

namespace FLua.Hosting.Environment
{
    /// <summary>
    /// Result-based interface for providing custom Lua environments for hosted code execution.
    /// Provides explicit error handling through Result patterns instead of exceptions.
    /// </summary>
    public interface IResultEnvironmentProvider
    {
        /// <summary>
        /// Creates a Lua environment configured for the specified trust level with Result-based error handling.
        /// </summary>
        /// <param name="trustLevel">The trust level for security restrictions</param>
        /// <param name="options">Host options for additional configuration</param>
        /// <returns>A hosting result containing the configured environment or error diagnostics</returns>
        HostingResult<LuaEnvironment> CreateEnvironmentResult(TrustLevel trustLevel, LuaHostOptions? options = null);
        
        /// <summary>
        /// Configures the module loading system for the environment with Result-based error handling.
        /// </summary>
        /// <param name="environment">The environment to configure</param>
        /// <param name="moduleResolver">The module resolver to use</param>
        /// <param name="trustLevel">Current trust level</param>
        /// <returns>A hosting result indicating success or failure with diagnostics</returns>
        HostingResult<LuaEnvironment> ConfigureModuleSystemResult(LuaEnvironment environment, IModuleResolver? moduleResolver, TrustLevel trustLevel);
        
        /// <summary>
        /// Adds host-provided functions to the environment with Result-based error handling.
        /// </summary>
        /// <param name="environment">The environment to configure</param>
        /// <param name="hostFunctions">Dictionary of function name to implementation</param>
        /// <returns>A hosting result indicating success or failure with diagnostics</returns>
        HostingResult<LuaEnvironment> AddHostFunctionsResult(LuaEnvironment environment, Dictionary<string, Func<LuaValue[], LuaValue>> hostFunctions);
        
        /// <summary>
        /// Injects host context variables into the environment with Result-based error handling.
        /// </summary>
        /// <param name="environment">The environment to configure</param>
        /// <param name="hostContext">Context variables to inject</param>
        /// <returns>A hosting result indicating success or failure with diagnostics</returns>
        HostingResult<LuaEnvironment> InjectHostContextResult(LuaEnvironment environment, Dictionary<string, object> hostContext);
    }

    /// <summary>
    /// Result-based implementation of FilteredEnvironmentProvider
    /// </summary>
    public class ResultFilteredEnvironmentProvider : IResultEnvironmentProvider
    {
        private readonly ILuaSecurityPolicy _securityPolicy;
        
        public ResultFilteredEnvironmentProvider(ILuaSecurityPolicy securityPolicy)
        {
            _securityPolicy = securityPolicy ?? throw new ArgumentNullException(nameof(securityPolicy));
        }
        
        public HostingResult<LuaEnvironment> CreateEnvironmentResult(TrustLevel trustLevel, LuaHostOptions? options = null)
        {
            try
            {
                options ??= new LuaHostOptions();
                
                var environment = new LuaEnvironment();
                var diagnostics = new List<HostingDiagnostic>();
                
                // Apply security filtering based on trust level
                var securityResult = ApplySecurityFiltering(environment, trustLevel);
                if (!securityResult.IsSuccess)
                    return securityResult;
                
                diagnostics.AddRange(securityResult.Diagnostics);
                
                // Configure standard libraries based on trust level
                var librariesResult = ConfigureStandardLibraries(environment, trustLevel);
                if (!librariesResult.IsSuccess)
                    return librariesResult;
                
                diagnostics.AddRange(librariesResult.Diagnostics);
                
                // Add host functions if provided
                if (options.HostFunctions != null && options.HostFunctions.Any())
                {
                    var hostFunctionsResult = AddHostFunctionsResult(environment, options.HostFunctions);
                    if (!hostFunctionsResult.IsSuccess)
                        return hostFunctionsResult;
                    
                    diagnostics.AddRange(hostFunctionsResult.Diagnostics);
                }
                
                // Inject host context if provided
                if (options.HostContext != null && options.HostContext.Any())
                {
                    var contextResult = InjectHostContextResult(environment, options.HostContext);
                    if (!contextResult.IsSuccess)
                        return contextResult;
                    
                    diagnostics.AddRange(contextResult.Diagnostics);
                }
                
                return HostingResult<LuaEnvironment>.Success(environment, diagnostics);
            }
            catch (Exception ex)
            {
                return HostingResult<LuaEnvironment>.FromException(ex, HostingOperation.EnvironmentCreation);
            }
        }
        
        public HostingResult<LuaEnvironment> ConfigureModuleSystemResult(LuaEnvironment environment, IModuleResolver? moduleResolver, TrustLevel trustLevel)
        {
            try
            {
                var diagnostics = new List<HostingDiagnostic>();
                
                if (moduleResolver == null)
                {
                    // Use default module resolver based on trust level
                    var defaultResolver = CreateDefaultModuleResolver(trustLevel);
                    var resolverResult = ConfigureModuleResolver(environment, defaultResolver, trustLevel);
                    if (!resolverResult.IsSuccess)
                        return resolverResult;
                    
                    diagnostics.AddRange(resolverResult.Diagnostics);
                }
                else
                {
                    // Validate the provided resolver against security policy
                    var validationResult = ValidateModuleResolver(moduleResolver, trustLevel);
                    if (!validationResult.IsSuccess)
                        return HostingResult<LuaEnvironment>.Failure(validationResult.Diagnostics.Select(d =>
                            new HostingDiagnostic(d.Severity, d.Message, HostingOperation.ModuleResolution)).ToList());
                    
                    var resolverResult = ConfigureModuleResolver(environment, moduleResolver, trustLevel);
                    if (!resolverResult.IsSuccess)
                        return resolverResult;
                    
                    diagnostics.AddRange(resolverResult.Diagnostics);
                }
                
                return HostingResult<LuaEnvironment>.Success(environment, diagnostics);
            }
            catch (Exception ex)
            {
                return HostingResult<LuaEnvironment>.FromException(ex, HostingOperation.ModuleResolution);
            }
        }
        
        public HostingResult<LuaEnvironment> AddHostFunctionsResult(LuaEnvironment environment, Dictionary<string, Func<LuaValue[], LuaValue>> hostFunctions)
        {
            try
            {
                var diagnostics = new List<HostingDiagnostic>();
                
                foreach (var (name, function) in hostFunctions)
                {
                    // Validate function name against security policy
                    var nameValidationResult = ValidateFunctionName(name);
                    if (!nameValidationResult.IsSuccess)
                    {
                        diagnostics.Add(new HostingDiagnostic(
                            DiagnosticSeverity.Warning, 
                            $"Host function '{name}' has invalid name: {nameValidationResult.Error}",
                            HostingOperation.EnvironmentCreation));
                        continue;
                    }
                    
                    // Wrap function for error handling
                    var wrappedFunction = WrapHostFunction(function, name);
                    
                    try
                    {
                        environment.SetGlobal(name, new LuaFunction(wrappedFunction));
                        diagnostics.Add(new HostingDiagnostic(
                            DiagnosticSeverity.Info,
                            $"Added host function '{name}'",
                            HostingOperation.EnvironmentCreation));
                    }
                    catch (Exception ex)
                    {
                        diagnostics.Add(new HostingDiagnostic(
                            DiagnosticSeverity.Error,
                            $"Failed to add host function '{name}': {ex.Message}",
                            HostingOperation.EnvironmentCreation));
                    }
                }
                
                return HostingResult<LuaEnvironment>.Success(environment, diagnostics);
            }
            catch (Exception ex)
            {
                return HostingResult<LuaEnvironment>.FromException(ex, HostingOperation.EnvironmentCreation);
            }
        }
        
        public HostingResult<LuaEnvironment> InjectHostContextResult(LuaEnvironment environment, Dictionary<string, object> hostContext)
        {
            try
            {
                var diagnostics = new List<HostingDiagnostic>();
                
                foreach (var (name, value) in hostContext)
                {
                    // Validate variable name
                    var nameValidationResult = ValidateVariableName(name);
                    if (!nameValidationResult.IsSuccess)
                    {
                        diagnostics.Add(new HostingDiagnostic(
                            DiagnosticSeverity.Warning, 
                            $"Host context variable '{name}' has invalid name: {nameValidationResult.Error}",
                            HostingOperation.EnvironmentCreation));
                        continue;
                    }
                    
                    // Convert .NET object to LuaValue
                    var conversionResult = ConvertToLuaValue(value);
                    if (!conversionResult.IsSuccess)
                    {
                        diagnostics.Add(new HostingDiagnostic(
                            DiagnosticSeverity.Warning,
                            $"Failed to convert host context variable '{name}': {conversionResult.Error}",
                            HostingOperation.EnvironmentCreation));
                        continue;
                    }
                    
                    try
                    {
                        environment.SetGlobal(name, conversionResult.Value);
                        diagnostics.Add(new HostingDiagnostic(
                            DiagnosticSeverity.Info,
                            $"Injected host context variable '{name}' ({value?.GetType().Name ?? "null"})",
                            HostingOperation.EnvironmentCreation));
                    }
                    catch (Exception ex)
                    {
                        diagnostics.Add(new HostingDiagnostic(
                            DiagnosticSeverity.Error,
                            $"Failed to inject host context variable '{name}': {ex.Message}",
                            HostingOperation.EnvironmentCreation));
                    }
                }
                
                return HostingResult<LuaEnvironment>.Success(environment, diagnostics);
            }
            catch (Exception ex)
            {
                return HostingResult<LuaEnvironment>.FromException(ex, HostingOperation.EnvironmentCreation);
            }
        }
        
        #region Private Helper Methods
        
        private HostingResult<LuaEnvironment> ApplySecurityFiltering(LuaEnvironment environment, TrustLevel trustLevel)
        {
            try
            {
                var diagnostics = new List<HostingDiagnostic>();
                
                // Apply security policy restrictions
                var restrictions = _securityPolicy.GetRestrictionsForTrustLevel(trustLevel);
                
                foreach (var restriction in restrictions.ForbiddenGlobals)
                {
                    if (environment.HasGlobal(restriction))
                    {
                        environment.RemoveGlobal(restriction);
                        diagnostics.Add(new HostingDiagnostic(
                            DiagnosticSeverity.Info,
                            $"Removed forbidden global '{restriction}' for trust level {trustLevel}",
                            HostingOperation.SecurityCheck));
                    }
                }
                
                return HostingResult<LuaEnvironment>.Success(environment, diagnostics);
            }
            catch (Exception ex)
            {
                return HostingResult<LuaEnvironment>.FromException(ex, HostingOperation.SecurityCheck);
            }
        }
        
        private HostingResult<LuaEnvironment> ConfigureStandardLibraries(LuaEnvironment environment, TrustLevel trustLevel)
        {
            try
            {
                var diagnostics = new List<HostingDiagnostic>();
                
                // Configure libraries based on trust level
                switch (trustLevel)
                {
                    case TrustLevel.FullTrust:
                        // All libraries available
                        LoadAllStandardLibraries(environment);
                        diagnostics.Add(new HostingDiagnostic(DiagnosticSeverity.Info, 
                            "Loaded all standard libraries for FullTrust", HostingOperation.EnvironmentCreation));
                        break;
                        
                    case TrustLevel.Trusted:
                        // Most libraries, excluding dangerous ones
                        LoadTrustedStandardLibraries(environment);
                        diagnostics.Add(new HostingDiagnostic(DiagnosticSeverity.Info, 
                            "Loaded trusted standard libraries", HostingOperation.EnvironmentCreation));
                        break;
                        
                    case TrustLevel.Restricted:
                        // Limited library set
                        LoadRestrictedStandardLibraries(environment);
                        diagnostics.Add(new HostingDiagnostic(DiagnosticSeverity.Info, 
                            "Loaded restricted standard libraries", HostingOperation.EnvironmentCreation));
                        break;
                        
                    case TrustLevel.Sandbox:
                        // Very limited library set
                        LoadSandboxStandardLibraries(environment);
                        diagnostics.Add(new HostingDiagnostic(DiagnosticSeverity.Info, 
                            "Loaded sandbox standard libraries", HostingOperation.EnvironmentCreation));
                        break;
                        
                    case TrustLevel.Untrusted:
                        // Minimal library set
                        LoadUntrustedStandardLibraries(environment);
                        diagnostics.Add(new HostingDiagnostic(DiagnosticSeverity.Info, 
                            "Loaded minimal standard libraries for untrusted code", HostingOperation.EnvironmentCreation));
                        break;
                }
                
                return HostingResult<LuaEnvironment>.Success(environment, diagnostics);
            }
            catch (Exception ex)
            {
                return HostingResult<LuaEnvironment>.FromException(ex, HostingOperation.EnvironmentCreation);
            }
        }
        
        private IModuleResolver CreateDefaultModuleResolver(TrustLevel trustLevel)
        {
            // Create appropriate module resolver based on trust level
            return trustLevel switch
            {
                TrustLevel.FullTrust => new FileSystemModuleResolver(),
                TrustLevel.Trusted => new RestrictedFileSystemModuleResolver(),
                TrustLevel.Restricted => new SandboxModuleResolver(),
                TrustLevel.Sandbox => new SandboxModuleResolver(),
                TrustLevel.Untrusted => new NullModuleResolver(),
                _ => new NullModuleResolver()
            };
        }
        
        private HostingResult<IModuleResolver> ConfigureModuleResolver(LuaEnvironment environment, IModuleResolver resolver, TrustLevel trustLevel)
        {
            try
            {
                // Configure the environment's require function to use the resolver
                var requireFunction = new LuaFunction(args =>
                {
                    if (args.Length == 0 || !args[0].IsString)
                        return new[] { LuaValue.Nil };
                    
                    var moduleName = args[0].AsString();
                    var moduleResult = resolver.ResolveModule(moduleName);
                    
                    // Note: This would need to be updated to handle Result-based module resolution
                    return new[] { moduleResult ?? LuaValue.Nil };
                });
                
                environment.SetGlobal("require", requireFunction);
                
                return HostingResult<IModuleResolver>.Success(resolver);
            }
            catch (Exception ex)
            {
                return HostingResult<IModuleResolver>.FromException(ex, HostingOperation.ModuleResolution);
            }
        }
        
        private Result<IModuleResolver> ValidateModuleResolver(IModuleResolver resolver, TrustLevel trustLevel)
        {
            // Validate that the resolver is appropriate for the trust level
            // This would contain security checks
            return Result<IModuleResolver>.Success(resolver);
        }
        
        private Result<string> ValidateFunctionName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result<string>.Failure("Function name cannot be empty");
                
            if (name.StartsWith("_"))
                return Result<string>.Failure("Function names starting with underscore are reserved");
                
            return Result<string>.Success(name);
        }
        
        private Result<string> ValidateVariableName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Result<string>.Failure("Variable name cannot be empty");
                
            if (name.StartsWith("_"))
                return Result<string>.Failure("Variable names starting with underscore are reserved");
                
            return Result<string>.Success(name);
        }
        
        private Func<LuaValue[], LuaValue> WrapHostFunction(Func<LuaValue[], LuaValue> function, string name)
        {
            return args =>
            {
                try
                {
                    return function(args);
                }
                catch (Exception ex)
                {
                    // Log the error and return nil instead of propagating exception
                    // In a real implementation, this could use a logging framework
                    return LuaValue.Nil;
                }
            };
        }
        
        private Result<LuaValue> ConvertToLuaValue(object? value)
        {
            try
            {
                return value switch
                {
                    null => Result<LuaValue>.Success(LuaValue.Nil),
                    bool b => Result<LuaValue>.Success(LuaValue.Boolean(b)),
                    string s => Result<LuaValue>.Success(LuaValue.String(s)),
                    int i => Result<LuaValue>.Success(LuaValue.Integer(i)),
                    long l => Result<LuaValue>.Success(LuaValue.Integer(l)),
                    float f => Result<LuaValue>.Success(LuaValue.Number(f)),
                    double d => Result<LuaValue>.Success(LuaValue.Number(d)),
                    LuaValue lv => Result<LuaValue>.Success(lv),
                    _ => Result<LuaValue>.Failure($"Cannot convert type {value.GetType().Name} to LuaValue")
                };
            }
            catch (Exception ex)
            {
                return Result<LuaValue>.Failure($"Conversion error: {ex.Message}");
            }
        }
        
        // Placeholder library loading methods
        private void LoadAllStandardLibraries(LuaEnvironment environment) { }
        private void LoadTrustedStandardLibraries(LuaEnvironment environment) { }
        private void LoadRestrictedStandardLibraries(LuaEnvironment environment) { }
        private void LoadSandboxStandardLibraries(LuaEnvironment environment) { }
        private void LoadUntrustedStandardLibraries(LuaEnvironment environment) { }
        
        #endregion
    }

    // Note: FileSystemModuleResolver is implemented in FileSystemModuleResolver.cs
    // Placeholder implementations for specialized resolvers would go here
}