using System;
using System.Collections.Generic;
using System.Linq;

namespace FLua.Common
{
    /// <summary>
    /// Represents the result of a compilation operation
    /// </summary>
    public readonly struct CompilationResult<T>
    {
        private readonly bool _isSuccess;
        private readonly T? _value;
        private readonly List<CompilerDiagnostic> _diagnostics;

        private CompilationResult(T value, List<CompilerDiagnostic> diagnostics)
        {
            _isSuccess = true;
            _value = value;
            _diagnostics = diagnostics ?? new List<CompilerDiagnostic>();
        }

        private CompilationResult(List<CompilerDiagnostic> diagnostics)
        {
            _isSuccess = false;
            _value = default;
            _diagnostics = diagnostics ?? new List<CompilerDiagnostic>();
        }

        /// <summary>
        /// Whether the compilation succeeded
        /// </summary>
        public bool IsSuccess => _isSuccess;

        /// <summary>
        /// Whether the compilation failed
        /// </summary>
        public bool IsFailure => !_isSuccess;

        /// <summary>
        /// The compiled result (only available if successful)
        /// </summary>
        public T Value => _isSuccess ? _value! : throw new InvalidOperationException($"Cannot access value of failed compilation result. Errors: {string.Join(", ", _diagnostics.Select(d => d.Message))}");

        /// <summary>
        /// All diagnostics (errors, warnings, info) from the compilation
        /// </summary>
        public IReadOnlyList<CompilerDiagnostic> Diagnostics => _diagnostics;

        /// <summary>
        /// Only error diagnostics from the compilation
        /// </summary>
        public IEnumerable<CompilerDiagnostic> Errors => _diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error);

        /// <summary>
        /// Only warning diagnostics from the compilation
        /// </summary>
        public IEnumerable<CompilerDiagnostic> Warnings => _diagnostics.Where(d => d.Severity == DiagnosticSeverity.Warning);

        /// <summary>
        /// Whether there are any error diagnostics
        /// </summary>
        public bool HasErrors => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

        /// <summary>
        /// Whether there are any warning diagnostics
        /// </summary>
        public bool HasWarnings => _diagnostics.Any(d => d.Severity == DiagnosticSeverity.Warning);

        /// <summary>
        /// Create a successful compilation result
        /// </summary>
        public static CompilationResult<T> Success(T value, IEnumerable<CompilerDiagnostic>? diagnostics = null)
        {
            var diagnosticsList = diagnostics?.ToList() ?? new List<CompilerDiagnostic>();
            
            // Success means no errors, but warnings are allowed
            if (diagnosticsList.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                throw new ArgumentException("Cannot create successful compilation result with error diagnostics");
            }
            
            return new CompilationResult<T>(value, diagnosticsList);
        }

        /// <summary>
        /// Create a failed compilation result
        /// </summary>
        public static CompilationResult<T> Failure(IEnumerable<CompilerDiagnostic> diagnostics)
        {
            var diagnosticsList = diagnostics.ToList();
            
            // Failure should have at least one error
            if (!diagnosticsList.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                throw new ArgumentException("Failed compilation result must contain at least one error diagnostic");
            }
            
            return new CompilationResult<T>(diagnosticsList);
        }

        /// <summary>
        /// Create a failed compilation result from a single error message
        /// </summary>
        public static CompilationResult<T> Error(string message, string? file = null, int line = 0, int column = 0)
        {
            var diagnostic = new CompilerDiagnostic(DiagnosticSeverity.Error, message, file, line, column);
            return new CompilationResult<T>(new List<CompilerDiagnostic> { diagnostic });
        }

        /// <summary>
        /// Create a failed compilation result from an exception
        /// </summary>
        public static CompilationResult<T> FromException(Exception exception, string? file = null, int line = 0, int column = 0)
        {
            var diagnostic = new CompilerDiagnostic(DiagnosticSeverity.Error, exception.Message, file, line, column);
            return new CompilationResult<T>(new List<CompilerDiagnostic> { diagnostic });
        }

        /// <summary>
        /// Transform the value if successful
        /// </summary>
        public CompilationResult<TResult> Map<TResult>(Func<T, TResult> mapper)
        {
            if (!_isSuccess)
                return CompilationResult<TResult>.Failure(_diagnostics);

            try
            {
                var newValue = mapper(_value!);
                return CompilationResult<TResult>.Success(newValue, _diagnostics);
            }
            catch (Exception ex)
            {
                var errorDiagnostic = new CompilerDiagnostic(DiagnosticSeverity.Error, ex.Message);
                var newDiagnostics = _diagnostics.Concat(new[] { errorDiagnostic }).ToList();
                return CompilationResult<TResult>.Failure(newDiagnostics);
            }
        }

        /// <summary>
        /// Chain compilation operations
        /// </summary>
        public CompilationResult<TResult> Bind<TResult>(Func<T, CompilationResult<TResult>> binder)
        {
            if (!_isSuccess)
                return CompilationResult<TResult>.Failure(_diagnostics);

            try
            {
                var result = binder(_value!);
                
                // Combine diagnostics
                var combinedDiagnostics = _diagnostics.Concat(result.Diagnostics).ToList();
                
                if (result.IsSuccess)
                    return CompilationResult<TResult>.Success(result.Value, combinedDiagnostics);
                else
                    return CompilationResult<TResult>.Failure(combinedDiagnostics);
            }
            catch (Exception ex)
            {
                var errorDiagnostic = new CompilerDiagnostic(DiagnosticSeverity.Error, ex.Message);
                var newDiagnostics = _diagnostics.Concat(new[] { errorDiagnostic }).ToList();
                return CompilationResult<TResult>.Failure(newDiagnostics);
            }
        }

        /// <summary>
        /// Execute an action based on success or failure
        /// </summary>
        public void Match(Action<T, IReadOnlyList<CompilerDiagnostic>> onSuccess, Action<IReadOnlyList<CompilerDiagnostic>> onFailure)
        {
            if (_isSuccess)
                onSuccess(_value!, _diagnostics);
            else
                onFailure(_diagnostics);
        }

        /// <summary>
        /// Execute a function based on success or failure and return the result
        /// </summary>
        public TResult Match<TResult>(Func<T, IReadOnlyList<CompilerDiagnostic>, TResult> onSuccess, Func<IReadOnlyList<CompilerDiagnostic>, TResult> onFailure)
        {
            if (_isSuccess)
                return onSuccess(_value!, _diagnostics);
            else
                return onFailure(_diagnostics);
        }

        /// <summary>
        /// Convert to a basic Result, discarding diagnostics
        /// </summary>
        public Result<T> ToResult()
        {
            if (_isSuccess)
                return Result<T>.Success(_value!);
            else
                return Result<T>.Failure(string.Join("; ", _diagnostics.Select(d => d.Message)));
        }

        public override string ToString()
        {
            if (_isSuccess)
                return $"Success: {_value} (Diagnostics: {_diagnostics.Count})";
            else
                return $"Failure: {_diagnostics.Count} diagnostics ({Errors.Count()} errors, {Warnings.Count()} warnings)";
        }
    }

    /// <summary>
    /// Represents a compiler diagnostic (error, warning, or info message)
    /// </summary>
    public readonly struct CompilerDiagnostic
    {
        public CompilerDiagnostic(DiagnosticSeverity severity, string message, string? file = null, int line = 0, int column = 0)
        {
            Severity = severity;
            Message = message;
            File = file;
            Line = line;
            Column = column;
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
        /// The file where the diagnostic occurred (if known)
        /// </summary>
        public string? File { get; }

        /// <summary>
        /// The line number where the diagnostic occurred (1-based, 0 if unknown)
        /// </summary>
        public int Line { get; }

        /// <summary>
        /// The column number where the diagnostic occurred (1-based, 0 if unknown)
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// Location string for display purposes
        /// </summary>
        public string Location
        {
            get
            {
                if (string.IsNullOrEmpty(File))
                    return Line > 0 ? $"line {Line}" : "unknown location";
                
                if (Line > 0 && Column > 0)
                    return $"{File}({Line},{Column})";
                else if (Line > 0)
                    return $"{File}({Line})";
                else
                    return File;
            }
        }

        public override string ToString()
        {
            return $"{Severity.ToString().ToLower()}: {Message} [{Location}]";
        }
    }

    /// <summary>
    /// Diagnostic severity levels
    /// </summary>
    public enum DiagnosticSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Static methods for working with compilation results
    /// </summary>
    public static class CompilationResult
    {
        /// <summary>
        /// Combine multiple compilation results into one
        /// </summary>
        public static CompilationResult<T[]> Combine<T>(params CompilationResult<T>[] results)
        {
            return Combine((IEnumerable<CompilationResult<T>>)results);
        }

        /// <summary>
        /// Combine multiple compilation results into one
        /// </summary>
        public static CompilationResult<T[]> Combine<T>(IEnumerable<CompilationResult<T>> results)
        {
            var resultsList = results.ToList();
            var allDiagnostics = resultsList.SelectMany(r => r.Diagnostics).ToList();
            
            if (allDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                return CompilationResult<T[]>.Failure(allDiagnostics);
            }
            
            var values = resultsList.Select(r => r.Value).ToArray();
            return CompilationResult<T[]>.Success(values, allDiagnostics);
        }
    }
}