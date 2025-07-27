using System.Text;

namespace FLua.Common.Diagnostics;

/// <summary>
/// Formats diagnostics for display
/// </summary>
public class DiagnosticFormatter
{
    private readonly bool _useColor;
    
    public DiagnosticFormatter(bool useColor = true)
    {
        _useColor = useColor;
    }
    
    /// <summary>
    /// Formats a diagnostic message in a Rust-like error format
    /// </summary>
    public string Format(FLuaDiagnostic diagnostic, string? sourceCode = null)
    {
        var sb = new StringBuilder();
        
        // Severity and code
        var severityStr = diagnostic.Severity switch
        {
            ErrorSeverity.Error => "error",
            ErrorSeverity.Warning => "warning",
            ErrorSeverity.Info => "info",
            ErrorSeverity.Hint => "hint",
            _ => "note"
        };
        
        if (_useColor)
        {
            var color = diagnostic.Severity switch
            {
                ErrorSeverity.Error => "\x1b[31m",    // Red
                ErrorSeverity.Warning => "\x1b[33m",  // Yellow
                ErrorSeverity.Info => "\x1b[36m",     // Cyan
                ErrorSeverity.Hint => "\x1b[32m",     // Green
                _ => "\x1b[0m"
            };
            sb.Append($"{color}{severityStr}[{diagnostic.Code}]\x1b[0m: ");
        }
        else
        {
            sb.Append($"{severityStr}[{diagnostic.Code}]: ");
        }
        
        sb.AppendLine(diagnostic.Message);
        
        // Location
        if (diagnostic.Location != null)
        {
            sb.AppendLine($"  --> {diagnostic.Location}");
            
            // Source context
            if (!string.IsNullOrEmpty(diagnostic.SourceContext) || !string.IsNullOrEmpty(sourceCode))
            {
                var context = diagnostic.SourceContext ?? ExtractSourceContext(sourceCode!, diagnostic.Location);
                if (!string.IsNullOrEmpty(context))
                {
                    sb.AppendLine("   |");
                    AppendSourceContext(sb, context, diagnostic.Location);
                    sb.AppendLine("   |");
                }
            }
        }
        
        // Help text
        if (!string.IsNullOrEmpty(diagnostic.Help))
        {
            sb.AppendLine($"   = help: {diagnostic.Help}");
        }
        
        // Additional notes
        foreach (var note in diagnostic.Notes)
        {
            sb.AppendLine($"   = note: {note}");
        }
        
        return sb.ToString();
    }
    
    private void AppendSourceContext(StringBuilder sb, string context, SourceLocation location)
    {
        var lines = context.Split('\n');
        var lineNumWidth = Math.Max(3, location.Line.ToString().Length + 1);
        
        for (int i = 0; i < lines.Length; i++)
        {
            var lineNum = location.Line - (lines.Length / 2) + i;
            if (lineNum == location.Line)
            {
                // Highlight the error line
                sb.Append($"{lineNum,{lineNumWidth}} | {lines[i]}\n");
                
                // Add caret pointing to error position
                sb.Append($"{"",{lineNumWidth}} | ");
                sb.Append(new string(' ', Math.Max(0, location.Column - 1)));
                sb.Append('^');
                
                if (location.Length > 1)
                {
                    sb.Append(new string('~', location.Length - 1));
                }
                
                // Add inline message if short enough
                if (location.Column + location.Length + 2 + context.Length < 80)
                {
                    sb.Append($" {context}");
                }
                sb.AppendLine();
            }
            else
            {
                sb.AppendLine($"{lineNum,{lineNumWidth}} | {lines[i]}");
            }
        }
    }
    
    private string ExtractSourceContext(string sourceCode, SourceLocation location)
    {
        var lines = sourceCode.Split('\n');
        if (location.Line <= 0 || location.Line > lines.Length)
            return string.Empty;
        
        // Get the line and surrounding context
        var startLine = Math.Max(1, location.Line - 1);
        var endLine = Math.Min(lines.Length, location.Line + 1);
        
        var contextLines = new List<string>();
        for (int i = startLine; i <= endLine; i++)
        {
            contextLines.Add(lines[i - 1]);
        }
        
        return string.Join("\n", contextLines);
    }
    
    /// <summary>
    /// Formats multiple diagnostics
    /// </summary>
    public string FormatAll(IEnumerable<FLuaDiagnostic> diagnostics, string? sourceCode = null)
    {
        var sb = new StringBuilder();
        foreach (var diagnostic in diagnostics)
        {
            sb.AppendLine(Format(diagnostic, sourceCode));
        }
        return sb.ToString();
    }
}