using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using FLua.Ast;
using FLua.Common;

namespace FLua.Compiler
{
    /// <summary>
    /// Result-based interface for Lua compilation backends with explicit error handling
    /// </summary>
    public interface IResultLuaCompiler
    {
        /// <summary>
        /// Compile a Lua AST to executable code with Result-based error handling
        /// </summary>
        /// <param name="ast">The Lua AST to compile</param>
        /// <param name="options">Compilation options</param>
        /// <returns>A compilation result with detailed diagnostics</returns>
        CompilationResult<CompilationOutput> CompileResult(IList<Statement> ast, CompilerOptions options);
        
        /// <summary>
        /// Get the supported compilation targets for this backend
        /// </summary>
        IEnumerable<CompilationTarget> SupportedTargets { get; }
        
        /// <summary>
        /// Get the backend name for logging/diagnostics
        /// </summary>
        string BackendName { get; }
        
        /// <summary>
        /// Validates compilation options against backend capabilities
        /// </summary>
        /// <param name="options">Options to validate</param>
        /// <returns>Validation result with any issues found</returns>
        CompilationResult<CompilerOptions> ValidateOptionsResult(CompilerOptions options);
    }

    /// <summary>
    /// Enhanced compilation output with detailed result information
    /// </summary>
    public record CompilationOutput(
        byte[]? Assembly = null,
        string? AssemblyPath = null,
        Delegate? CompiledDelegate = null,
        LambdaExpression? ExpressionTree = null,
        Type? GeneratedType = null,
        CompilationMetrics? Metrics = null
    );

    /// <summary>
    /// Metrics about the compilation process
    /// </summary>
    public record CompilationMetrics(
        TimeSpan CompilationTime,
        int GeneratedInstructionCount,
        int OptimizationPasses,
        long GeneratedCodeSize,
        Dictionary<string, object>? AdditionalMetrics = null
    );

    /// <summary>
    /// Result-based Roslyn compiler implementation
    /// </summary>
    public class ResultRoslynLuaCompiler : IResultLuaCompiler
    {
        private readonly RoslynLuaCompiler _innerCompiler;
        
        public ResultRoslynLuaCompiler()
        {
            _innerCompiler = new RoslynLuaCompiler();
        }
        
        public IEnumerable<CompilationTarget> SupportedTargets => _innerCompiler.SupportedTargets;
        
        public string BackendName => "Roslyn-based Lua Compiler (Result Pattern)";
        
        public CompilationResult<CompilerOptions> ValidateOptionsResult(CompilerOptions options)
        {
            var diagnostics = new List<CompilerDiagnostic>();
            
            try
            {
                // Validate output path
                if (string.IsNullOrWhiteSpace(options.OutputPath) && !options.GenerateInMemory)
                {
                    diagnostics.Add(new CompilerDiagnostic(
                        DiagnosticSeverity.Error,
                        "OutputPath must be specified when not generating in-memory"));
                }
                
                // Validate target compatibility
                if (!SupportedTargets.Contains(options.Target))
                {
                    diagnostics.Add(new CompilerDiagnostic(
                        DiagnosticSeverity.Error,
                        $"Compilation target {options.Target} is not supported by {BackendName}"));
                }
                
                // Validate target-specific options
                switch (options.Target)
                {
                    case CompilationTarget.Expression:
                        if (!options.GenerateExpressionTree)
                        {
                            var updatedOptions = options with { GenerateExpressionTree = true };
                            diagnostics.Add(new CompilerDiagnostic(
                                DiagnosticSeverity.Info,
                                "Enabled expression tree generation for Expression target"));
                            return CompilationResult<CompilerOptions>.Success(updatedOptions, diagnostics);
                        }
                        break;
                        
                    case CompilationTarget.Lambda:
                        if (!options.GenerateInMemory)
                        {
                            var updatedOptions = options with { GenerateInMemory = true };
                            diagnostics.Add(new CompilerDiagnostic(
                                DiagnosticSeverity.Info,
                                "Enabled in-memory generation for Lambda target"));
                            return CompilationResult<CompilerOptions>.Success(updatedOptions, diagnostics);
                        }
                        break;
                        
                    case CompilationTarget.NativeAot:
                        diagnostics.Add(new CompilerDiagnostic(
                            DiagnosticSeverity.Warning,
                            "Native AOT compilation is experimental and may have limitations"));
                        break;
                }
                
                // Check for conflicting options
                if (options.GenerateInMemory && !string.IsNullOrWhiteSpace(options.AssemblyPath))
                {
                    diagnostics.Add(new CompilerDiagnostic(
                        DiagnosticSeverity.Warning,
                        "AssemblyPath will be ignored when generating in-memory"));
                }
                
                return diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error)
                    ? CompilationResult<CompilerOptions>.Failure(diagnostics)
                    : CompilationResult<CompilerOptions>.Success(options, diagnostics);
            }
            catch (Exception ex)
            {
                diagnostics.Add(new CompilerDiagnostic(DiagnosticSeverity.Error, ex.Message));
                return CompilationResult<CompilerOptions>.Failure(diagnostics);
            }
        }
        
        public CompilationResult<CompilationOutput> CompileResult(IList<Statement> ast, CompilerOptions options)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var diagnostics = new List<CompilerDiagnostic>();
            
            try
            {
                // Validate options first
                var validationResult = ValidateOptionsResult(options);
                if (!validationResult.IsSuccess)
                    return CompilationResult<CompilationOutput>.Failure(validationResult.Diagnostics.ToList());
                
                var validatedOptions = validationResult.Value;
                diagnostics.AddRange(validationResult.Diagnostics);
                
                // Validate AST
                var astValidationResult = ValidateAst(ast);
                if (!astValidationResult.IsSuccess)
                {
                    diagnostics.AddRange(astValidationResult.Diagnostics);
                    return CompilationResult<CompilationOutput>.Failure(diagnostics);
                }
                
                diagnostics.AddRange(astValidationResult.Diagnostics);
                
                // Perform compilation using inner compiler
                var legacyResult = _innerCompiler.Compile(ast, validatedOptions);
                stopwatch.Stop();
                
                // Convert legacy result to new format
                var output = new CompilationOutput(
                    Assembly: legacyResult.Assembly,
                    AssemblyPath: legacyResult.AssemblyPath,
                    CompiledDelegate: legacyResult.CompiledDelegate,
                    ExpressionTree: legacyResult.ExpressionTree,
                    GeneratedType: legacyResult.GeneratedType,
                    Metrics: new CompilationMetrics(
                        CompilationTime: stopwatch.Elapsed,
                        GeneratedInstructionCount: EstimateInstructionCount(legacyResult),
                        OptimizationPasses: validatedOptions.Optimization == OptimizationLevel.Release ? 3 : 1,
                        GeneratedCodeSize: legacyResult.Assembly?.Length ?? 0
                    )
                );
                
                // Add any errors/warnings from legacy compiler
                if (legacyResult.Errors != null)
                {
                    foreach (var error in legacyResult.Errors)
                    {
                        diagnostics.Add(new CompilerDiagnostic(DiagnosticSeverity.Error, error));
                    }
                }
                
                if (legacyResult.Warnings != null)
                {
                    foreach (var warning in legacyResult.Warnings)
                    {
                        diagnostics.Add(new CompilerDiagnostic(DiagnosticSeverity.Warning, warning));
                    }
                }
                
                // Determine overall success
                var hasErrors = diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error) || !legacyResult.Success;
                
                if (hasErrors)
                    return CompilationResult<CompilationOutput>.Failure(diagnostics);
                else
                    return CompilationResult<CompilationOutput>.Success(output, diagnostics);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                diagnostics.Add(new CompilerDiagnostic(DiagnosticSeverity.Error, $"Compilation failed: {ex.Message}"));
                return CompilationResult<CompilationOutput>.Failure(diagnostics);
            }
        }
        
        private CompilationResult<IList<Statement>> ValidateAst(IList<Statement> ast)
        {
            var diagnostics = new List<CompilerDiagnostic>();
            
            try
            {
                if (ast == null || ast.Count == 0)
                {
                    diagnostics.Add(new CompilerDiagnostic(
                        DiagnosticSeverity.Error,
                        "AST is empty or null"));
                    return CompilationResult<IList<Statement>>.Failure(diagnostics);
                }
                
                // Perform basic AST validation
                int statementIndex = 0;
                foreach (var statement in ast)
                {
                    if (statement == null)
                    {
                        diagnostics.Add(new CompilerDiagnostic(
                            DiagnosticSeverity.Error,
                            $"Statement at index {statementIndex} is null"));
                    }
                    
                    statementIndex++;
                }
                
                // Check for commonly problematic patterns
                if (ast.Count > 1000)
                {
                    diagnostics.Add(new CompilerDiagnostic(
                        DiagnosticSeverity.Warning,
                        $"Large AST with {ast.Count} statements may impact compilation performance"));
                }
                
                return diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error)
                    ? CompilationResult<IList<Statement>>.Failure(diagnostics)
                    : CompilationResult<IList<Statement>>.Success(ast, diagnostics);
            }
            catch (Exception ex)
            {
                diagnostics.Add(new CompilerDiagnostic(DiagnosticSeverity.Error, $"AST validation failed: {ex.Message}"));
                return CompilationResult<IList<Statement>>.Failure(diagnostics);
            }
        }
        
        private int EstimateInstructionCount(CompilationResult result)
        {
            // Rough estimation based on available information
            if (result.Assembly != null)
                return result.Assembly.Length / 10; // Very rough estimate
            
            if (result.ExpressionTree != null)
                return EstimateExpressionComplexity(result.ExpressionTree);
            
            return 0;
        }
        
        private int EstimateExpressionComplexity(LambdaExpression expression)
        {
            // Simple complexity estimation for expression trees
            return CountExpressionNodes(expression.Body);
        }
        
        private int CountExpressionNodes(Expression expression)
        {
            int count = 1; // Count this node
            
            // Recursively count child nodes (simplified)
            if (expression is BinaryExpression binary)
            {
                count += CountExpressionNodes(binary.Left);
                count += CountExpressionNodes(binary.Right);
            }
            else if (expression is UnaryExpression unary && unary.Operand != null)
            {
                count += CountExpressionNodes(unary.Operand);
            }
            else if (expression is MethodCallExpression methodCall)
            {
                if (methodCall.Object != null)
                    count += CountExpressionNodes(methodCall.Object);
                
                foreach (var arg in methodCall.Arguments)
                    count += CountExpressionNodes(arg);
            }
            
            return count;
        }
    }

    /// <summary>
    /// Adapter to make existing ILuaCompiler compatible with Result pattern
    /// </summary>
    public class CompilerResultAdapter : IResultLuaCompiler
    {
        private readonly ILuaCompiler _innerCompiler;
        
        public CompilerResultAdapter(ILuaCompiler innerCompiler)
        {
            _innerCompiler = innerCompiler ?? throw new ArgumentNullException(nameof(innerCompiler));
        }
        
        public IEnumerable<CompilationTarget> SupportedTargets => _innerCompiler.SupportedTargets;
        
        public string BackendName => $"{_innerCompiler.BackendName} (Result Adapter)";
        
        public CompilationResult<CompilerOptions> ValidateOptionsResult(CompilerOptions options)
        {
            // Basic validation since the inner compiler might not expose validation
            var diagnostics = new List<CompilerDiagnostic>();
            
            if (!SupportedTargets.Contains(options.Target))
            {
                diagnostics.Add(new CompilerDiagnostic(
                    DiagnosticSeverity.Error,
                    $"Compilation target {options.Target} is not supported"));
                return CompilationResult<CompilerOptions>.Failure(diagnostics);
            }
            
            return CompilationResult<CompilerOptions>.Success(options);
        }
        
        public CompilationResult<CompilationOutput> CompileResult(IList<Statement> ast, CompilerOptions options)
        {
            try
            {
                var result = _innerCompiler.Compile(ast, options);
                
                var output = new CompilationOutput(
                    Assembly: result.Assembly,
                    AssemblyPath: result.AssemblyPath,
                    CompiledDelegate: result.CompiledDelegate,
                    ExpressionTree: result.ExpressionTree,
                    GeneratedType: result.GeneratedType
                );
                
                var diagnostics = new List<CompilerDiagnostic>();
                
                if (result.Errors != null)
                {
                    foreach (var error in result.Errors)
                    {
                        diagnostics.Add(new CompilerDiagnostic(DiagnosticSeverity.Error, error));
                    }
                }
                
                if (result.Warnings != null)
                {
                    foreach (var warning in result.Warnings)
                    {
                        diagnostics.Add(new CompilerDiagnostic(DiagnosticSeverity.Warning, warning));
                    }
                }
                
                if (result.Success)
                    return CompilationResult<CompilationOutput>.Success(output, diagnostics);
                else
                    return CompilationResult<CompilationOutput>.Failure(diagnostics);
            }
            catch (Exception ex)
            {
                var diagnostics = new List<CompilerDiagnostic>
                {
                    new CompilerDiagnostic(DiagnosticSeverity.Error, ex.Message)
                };
                return CompilationResult<CompilationOutput>.Failure(diagnostics);
            }
        }
    }
}