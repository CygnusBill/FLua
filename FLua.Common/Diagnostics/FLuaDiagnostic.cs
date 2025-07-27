namespace FLua.Common.Diagnostics;

/// <summary>
/// Represents a diagnostic message (error, warning, etc.)
/// </summary>
public class FLuaDiagnostic
{
    /// <summary>
    /// The diagnostic code (e.g., "FLU-0001")
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// The severity of the diagnostic
    /// </summary>
    public ErrorSeverity Severity { get; set; }
    
    /// <summary>
    /// The main diagnostic message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// The source location where the diagnostic occurred
    /// </summary>
    public SourceLocation? Location { get; set; }
    
    /// <summary>
    /// Optional help text suggesting how to fix the issue
    /// </summary>
    public string? Help { get; set; }
    
    /// <summary>
    /// The source code context around the error
    /// </summary>
    public string? SourceContext { get; set; }
    
    /// <summary>
    /// Additional notes or related information
    /// </summary>
    public List<string> Notes { get; set; } = new();
    
    public FLuaDiagnostic()
    {
    }
    
    public FLuaDiagnostic(string code, ErrorSeverity severity, string message, SourceLocation? location = null)
    {
        Code = code;
        Severity = severity;
        Message = message;
        Location = location;
    }
    
    /// <summary>
    /// Creates an error diagnostic
    /// </summary>
    public static FLuaDiagnostic Error(string code, string message, SourceLocation? location = null)
    {
        return new FLuaDiagnostic(code, ErrorSeverity.Error, message, location);
    }
    
    /// <summary>
    /// Creates a warning diagnostic
    /// </summary>
    public static FLuaDiagnostic Warning(string code, string message, SourceLocation? location = null)
    {
        return new FLuaDiagnostic(code, ErrorSeverity.Warning, message, location);
    }
    
    /// <summary>
    /// Creates an info diagnostic
    /// </summary>
    public static FLuaDiagnostic Info(string code, string message, SourceLocation? location = null)
    {
        return new FLuaDiagnostic(code, ErrorSeverity.Info, message, location);
    }
    
    /// <summary>
    /// Creates a hint diagnostic
    /// </summary>
    public static FLuaDiagnostic Hint(string code, string message, SourceLocation? location = null)
    {
        return new FLuaDiagnostic(code, ErrorSeverity.Hint, message, location);
    }
}