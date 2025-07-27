namespace FLua.Common.Diagnostics;

/// <summary>
/// Default implementation of IDiagnosticCollector
/// </summary>
public class DiagnosticCollector : IDiagnosticCollector
{
    private readonly List<FLuaDiagnostic> _diagnostics = new();
    private readonly object _lock = new();
    
    public bool HasErrors => GetCount(ErrorSeverity.Error) > 0;
    
    public bool HasWarnings => GetCount(ErrorSeverity.Warning) > 0;
    
    public void Report(FLuaDiagnostic diagnostic)
    {
        if (diagnostic == null)
            throw new ArgumentNullException(nameof(diagnostic));
        
        lock (_lock)
        {
            _diagnostics.Add(diagnostic);
        }
    }
    
    public IReadOnlyList<FLuaDiagnostic> GetDiagnostics()
    {
        lock (_lock)
        {
            return _diagnostics.ToList().AsReadOnly();
        }
    }
    
    public void Clear()
    {
        lock (_lock)
        {
            _diagnostics.Clear();
        }
    }
    
    public int GetCount(ErrorSeverity severity)
    {
        lock (_lock)
        {
            return _diagnostics.Count(d => d.Severity == severity);
        }
    }
    
    /// <summary>
    /// Reports an error with the given code and message
    /// </summary>
    public void ReportError(string code, string message, SourceLocation? location = null)
    {
        Report(FLuaDiagnostic.Error(code, message, location));
    }
    
    /// <summary>
    /// Reports a warning with the given code and message
    /// </summary>
    public void ReportWarning(string code, string message, SourceLocation? location = null)
    {
        Report(FLuaDiagnostic.Warning(code, message, location));
    }
}