namespace FLua.Common.Diagnostics;

/// <summary>
/// Interface for collecting diagnostic messages
/// </summary>
public interface IDiagnosticCollector
{
    /// <summary>
    /// Reports a diagnostic
    /// </summary>
    void Report(FLuaDiagnostic diagnostic);
    
    /// <summary>
    /// Gets all collected diagnostics
    /// </summary>
    IReadOnlyList<FLuaDiagnostic> GetDiagnostics();
    
    /// <summary>
    /// Gets whether any errors have been reported
    /// </summary>
    bool HasErrors { get; }
    
    /// <summary>
    /// Gets whether any warnings have been reported
    /// </summary>
    bool HasWarnings { get; }
    
    /// <summary>
    /// Clears all collected diagnostics
    /// </summary>
    void Clear();
    
    /// <summary>
    /// Gets the count of diagnostics by severity
    /// </summary>
    int GetCount(ErrorSeverity severity);
}