using System;
using System.Collections.Generic;
using System.Linq;

namespace FLua.Common
{
    /// <summary>
    /// Represents the result of a hosting operation (execution, compilation, etc.)
    /// Extends CompilationResult with runtime execution context
    /// </summary>
    public readonly struct HostingResult<T>
    {
        private readonly bool _isSuccess;
        private readonly T? _value;
        private readonly List<HostingDiagnostic> _diagnostics;
        private readonly ExecutionContext? _executionContext;

        private HostingResult(T value, List<HostingDiagnostic> diagnostics, ExecutionContext? context = null)
        {
            _isSuccess = true;
            _value = value;
            _diagnostics = diagnostics ?? new List<HostingDiagnostic>();
            _executionContext = context;
        }

        private HostingResult(List<HostingDiagnostic> diagnostics, ExecutionContext? context = null)
        {
            _isSuccess = false;
            _value = default;
            _diagnostics = diagnostics ?? new List<HostingDiagnostic>();
            _executionContext = context;
        }

        /// <summary>
        /// Whether the operation succeeded
        /// </summary>
        public bool IsSuccess => _isSuccess;

        /// <summary>
        /// Whether the operation failed
        /// </summary>
        public bool IsFailure => !_isSuccess;

        /// <summary>
        /// The result value (only available if successful)
        /// </summary>
        public T Value => _isSuccess ? _value! : throw new InvalidOperationException($"Cannot access value of failed hosting result. Errors: {string.Join(", ", _diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.Message))}");

        /// <summary>
        /// All diagnostics from the operation
        /// </summary>
        public IReadOnlyList<HostingDiagnostic> Diagnostics => _diagnostics;

        /// <summary>
        /// Only error diagnostics
        /// </summary>
        public IEnumerable<HostingDiagnostic> Errors => _diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);

        /// <summary>
        /// Only warning diagnostics
        /// </summary>
        public IEnumerable<HostingDiagnostic> Warnings => _diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning);

        /// <summary>
        /// Information about execution context (timing, memory usage, etc.)
        /// </summary>
        public ExecutionContext? ExecutionContext => _executionContext;

        /// <summary>
        /// Whether there are any error diagnostics
        /// </summary>
        public bool HasErrors => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

        /// <summary>
        /// Whether there are any warning diagnostics
        /// </summary>
        public bool HasWarnings => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Warning);

        /// <summary>
        /// Create a successful hosting result
        /// </summary>
        public static HostingResult<T> Success(T value, IEnumerable<HostingDiagnostic>? diagnostics = null, ExecutionContext? context = null)
        {
            var diagnosticsList = diagnostics?.ToList() ?? new List<HostingDiagnostic>();
            
            if (diagnosticsList.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                throw new ArgumentException("Cannot create successful hosting result with error diagnostics");
            }
            
            return new HostingResult<T>(value, diagnosticsList, context);
        }

        /// <summary>
        /// Create a failed hosting result
        /// </summary>
        public static HostingResult<T> Failure(IEnumerable<HostingDiagnostic> diagnostics, ExecutionContext? context = null)
        {
            var diagnosticsList = diagnostics.ToList();
            
            if (!diagnosticsList.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                throw new ArgumentException("Failed hosting result must contain at least one error diagnostic");
            }
            
            return new HostingResult<T>(diagnosticsList, context);
        }

        /// <summary>
        /// Create a failed hosting result from a single error message
        /// </summary>
        public static HostingResult<T> Error(string message, HostingOperation operation = HostingOperation.Unknown, string? source = null, ExecutionContext? context = null)
        {
            var diagnostic = new HostingDiagnostic(DiagnosticSeverity.Error, message, operation, source);
            return new HostingResult<T>(new List<HostingDiagnostic> { diagnostic }, context);
        }

        /// <summary>
        /// Create a failed hosting result from an exception
        /// </summary>
        public static HostingResult<T> FromException(Exception exception, HostingOperation operation = HostingOperation.Unknown, string? source = null, ExecutionContext? context = null)
        {
            var diagnostic = new HostingDiagnostic(DiagnosticSeverity.Error, exception.Message, operation, source);
            return new HostingResult<T>(new List<HostingDiagnostic> { diagnostic }, context);
        }

        /// <summary>
        /// Transform the value if successful
        /// </summary>
        public HostingResult<TResult> Map<TResult>(Func<T, TResult> mapper)
        {
            if (!_isSuccess)
                return HostingResult<TResult>.Failure(_diagnostics, _executionContext);

            try
            {
                var newValue = mapper(_value!);
                return HostingResult<TResult>.Success(newValue, _diagnostics, _executionContext);
            }
            catch (Exception ex)
            {
                var errorDiagnostic = new HostingDiagnostic(DiagnosticSeverity.Error, ex.Message, HostingOperation.Transformation);
                var newDiagnostics = _diagnostics.Concat(new[] { errorDiagnostic }).ToList();
                return HostingResult<TResult>.Failure(newDiagnostics, _executionContext);
            }
        }

        /// <summary>
        /// Chain hosting operations
        /// </summary>
        public HostingResult<TResult> Bind<TResult>(Func<T, HostingResult<TResult>> binder)
        {
            if (!_isSuccess)
                return HostingResult<TResult>.Failure(_diagnostics, _executionContext);

            try
            {
                var result = binder(_value!);
                
                // Combine diagnostics
                var combinedDiagnostics = _diagnostics.Concat(result.Diagnostics).ToList();
                
                // Use the most recent execution context, or combine them
                var combinedContext = result.ExecutionContext ?? _executionContext;
                
                if (result.IsSuccess)
                    return HostingResult<TResult>.Success(result.Value, combinedDiagnostics, combinedContext);
                else
                    return HostingResult<TResult>.Failure(combinedDiagnostics, combinedContext);
            }
            catch (Exception ex)
            {
                var errorDiagnostic = new HostingDiagnostic(DiagnosticSeverity.Error, ex.Message, HostingOperation.Binding);
                var newDiagnostics = _diagnostics.Concat(new[] { errorDiagnostic }).ToList();
                return HostingResult<TResult>.Failure(newDiagnostics, _executionContext);
            }
        }

        /// <summary>
        /// Execute an action based on success or failure
        /// </summary>
        public void Match(Action<T, IReadOnlyList<HostingDiagnostic>, ExecutionContext?> onSuccess, Action<IReadOnlyList<HostingDiagnostic>, ExecutionContext?> onFailure)
        {
            if (_isSuccess)
                onSuccess(_value!, _diagnostics, _executionContext);
            else
                onFailure(_diagnostics, _executionContext);
        }

        /// <summary>
        /// Execute a function based on success or failure and return the result
        /// </summary>
        public TResult Match<TResult>(Func<T, IReadOnlyList<HostingDiagnostic>, ExecutionContext?, TResult> onSuccess, Func<IReadOnlyList<HostingDiagnostic>, ExecutionContext?, TResult> onFailure)
        {
            if (_isSuccess)
                return onSuccess(_value!, _diagnostics, _executionContext);
            else
                return onFailure(_diagnostics, _executionContext);
        }

        /// <summary>
        /// Convert to a basic Result, discarding hosting context
        /// </summary>
        public Result<T> ToResult()
        {
            if (_isSuccess)
                return Result<T>.Success(_value!);
            else
                return Result<T>.Failure(string.Join("; ", _diagnostics.Select(d => d.Message)));
        }

        /// <summary>
        /// Convert to a CompilationResult (for compiler operations)
        /// </summary>
        public CompilationResult<T> ToCompilationResult()
        {
            var compilerDiagnostics = _diagnostics.Select(d => new CompilerDiagnostic(
                d.Severity, d.Message, d.Source)).ToList();
                
            if (_isSuccess)
                return CompilationResult<T>.Success(_value!, compilerDiagnostics);
            else
                return CompilationResult<T>.Failure(compilerDiagnostics);
        }

        public override string ToString()
        {
            if (_isSuccess)
            {
                var contextInfo = _executionContext != null ? $", Context: {_executionContext}" : "";
                return $"Success: {_value} (Diagnostics: {_diagnostics.Count}{contextInfo})";
            }
            else
            {
                var contextInfo = _executionContext != null ? $", Context: {_executionContext}" : "";
                return $"Failure: {_diagnostics.Count} diagnostics ({Errors.Count()} errors, {Warnings.Count()} warnings{contextInfo})";
            }
        }
    }

    /// <summary>
    /// Represents a diagnostic specific to hosting operations
    /// </summary>
    public readonly struct HostingDiagnostic
    {
        public HostingDiagnostic(DiagnosticSeverity severity, string message, HostingOperation operation = HostingOperation.Unknown, string? source = null)
        {
            Severity = severity;
            Message = message;
            Operation = operation;
            Source = source;
            Timestamp = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Severity of the diagnostic
        /// </summary>
        public DiagnosticSeverity Severity { get; }

        /// <summary>
        /// The diagnostic message
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// The hosting operation that generated this diagnostic
        /// </summary>
        public HostingOperation Operation { get; }

        /// <summary>
        /// The source of the diagnostic (file, method, etc.)
        /// </summary>
        public string? Source { get; }

        /// <summary>
        /// When this diagnostic was created
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        public override string ToString()
        {
            var operationStr = Operation != HostingOperation.Unknown ? $"[{Operation}] " : "";
            var sourceStr = !string.IsNullOrEmpty(Source) ? $" ({Source})" : "";
            return $"{Severity.ToString().ToLower()}: {operationStr}{Message}{sourceStr}";
        }
    }

    /// <summary>
    /// Types of hosting operations that can generate diagnostics
    /// </summary>
    public enum HostingOperation
    {
        Unknown,
        Parsing,
        Compilation,
        Execution,
        Validation,
        ModuleResolution,
        EnvironmentCreation,
        SecurityCheck,
        Transformation,
        Binding,
        AssemblyGeneration,
        ExpressionTreeGeneration
    }

    /// <summary>
    /// Information about the execution context of a hosting operation
    /// </summary>
    public readonly struct ExecutionContext
    {
        public ExecutionContext(
            TimeSpan? executionTime = null,
            long? memoryUsed = null,
            int? instructionsExecuted = null,
            string? trustLevel = null,
            Dictionary<string, object>? metadata = null)
        {
            ExecutionTime = executionTime;
            MemoryUsed = memoryUsed;
            InstructionsExecuted = instructionsExecuted;
            TrustLevel = trustLevel;
            Metadata = metadata ?? new Dictionary<string, object>();
        }

        /// <summary>
        /// How long the operation took
        /// </summary>
        public TimeSpan? ExecutionTime { get; }

        /// <summary>
        /// Memory usage in bytes (if tracked)
        /// </summary>
        public long? MemoryUsed { get; }

        /// <summary>
        /// Number of instructions executed (if tracked)
        /// </summary>
        public int? InstructionsExecuted { get; }

        /// <summary>
        /// Trust level used for the operation
        /// </summary>
        public string? TrustLevel { get; }

        /// <summary>
        /// Additional metadata about the execution
        /// </summary>
        public Dictionary<string, object> Metadata { get; }

        public override string ToString()
        {
            var parts = new List<string>();
            if (ExecutionTime.HasValue) parts.Add($"Time: {ExecutionTime.Value.TotalMilliseconds:F2}ms");
            if (MemoryUsed.HasValue) parts.Add($"Memory: {MemoryUsed.Value} bytes");
            if (InstructionsExecuted.HasValue) parts.Add($"Instructions: {InstructionsExecuted.Value}");
            if (!string.IsNullOrEmpty(TrustLevel)) parts.Add($"Trust: {TrustLevel}");
            
            return string.Join(", ", parts);
        }
    }

    /// <summary>
    /// Static methods for working with hosting results
    /// </summary>
    public static class HostingResult
    {
        /// <summary>
        /// Combine multiple hosting results into one
        /// </summary>
        public static HostingResult<T[]> Combine<T>(params HostingResult<T>[] results)
        {
            return Combine((IEnumerable<HostingResult<T>>)results);
        }

        /// <summary>
        /// Combine multiple hosting results into one
        /// </summary>
        public static HostingResult<T[]> Combine<T>(IEnumerable<HostingResult<T>> results)
        {
            var resultsList = results.ToList();
            var allDiagnostics = resultsList.SelectMany(r => r.Diagnostics).ToList();
            var contexts = resultsList.Where(r => r.ExecutionContext.HasValue).Select(r => r.ExecutionContext!.Value).ToList();
            
            // Combine execution contexts
            ExecutionContext? combinedContext = null;
            if (contexts.Any())
            {
                var totalTime = contexts.Where(c => c.ExecutionTime.HasValue).Aggregate(TimeSpan.Zero, (sum, c) => sum + c.ExecutionTime!.Value);
                var totalMemory = contexts.Where(c => c.MemoryUsed.HasValue).Sum(c => c.MemoryUsed!.Value);
                var totalInstructions = contexts.Where(c => c.InstructionsExecuted.HasValue).Sum(c => c.InstructionsExecuted!.Value);
                var trustLevel = contexts.FirstOrDefault(c => !string.IsNullOrEmpty(c.TrustLevel)).TrustLevel;
                
                combinedContext = new ExecutionContext(
                    totalTime > TimeSpan.Zero ? totalTime : null,
                    totalMemory > 0 ? totalMemory : null,
                    totalInstructions > 0 ? totalInstructions : null,
                    trustLevel);
            }
            
            if (allDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                return HostingResult<T[]>.Failure(allDiagnostics, combinedContext);
            }
            
            var values = resultsList.Select(r => r.Value).ToArray();
            return HostingResult<T[]>.Success(values, allDiagnostics, combinedContext);
        }

        /// <summary>
        /// Create a successful result from a CompilationResult
        /// </summary>
        public static HostingResult<T> FromCompilationResult<T>(CompilationResult<T> compilationResult, ExecutionContext? context = null)
        {
            var hostingDiagnostics = compilationResult.Diagnostics.Select(d => new HostingDiagnostic(
                d.Severity, d.Message, HostingOperation.Compilation, d.File)).ToList();
                
            if (compilationResult.IsSuccess)
                return HostingResult<T>.Success(compilationResult.Value, hostingDiagnostics, context);
            else
                return HostingResult<T>.Failure(hostingDiagnostics, context);
        }
    }
}