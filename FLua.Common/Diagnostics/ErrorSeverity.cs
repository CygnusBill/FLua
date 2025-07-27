namespace FLua.Common.Diagnostics;

/// <summary>
/// Represents the severity level of a diagnostic message
/// </summary>
public enum ErrorSeverity
{
    /// <summary>
    /// An error that prevents execution or compilation
    /// </summary>
    Error,
    
    /// <summary>
    /// A warning about potential issues
    /// </summary>
    Warning,
    
    /// <summary>
    /// Informational message
    /// </summary>
    Info,
    
    /// <summary>
    /// A suggestion for improvement
    /// </summary>
    Hint
}