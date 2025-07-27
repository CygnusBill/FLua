namespace FLua.Common.Diagnostics;

/// <summary>
/// Represents a location in source code
/// </summary>
public class SourceLocation
{
    /// <summary>
    /// The file name or source identifier
    /// </summary>
    public string FileName { get; set; } = string.Empty;
    
    /// <summary>
    /// The line number (1-based)
    /// </summary>
    public int Line { get; set; }
    
    /// <summary>
    /// The column number (1-based)
    /// </summary>
    public int Column { get; set; }
    
    /// <summary>
    /// The length of the source span
    /// </summary>
    public int Length { get; set; }
    
    /// <summary>
    /// Gets the end column (Column + Length)
    /// </summary>
    public int EndColumn => Column + Length;
    
    public SourceLocation()
    {
    }
    
    public SourceLocation(string fileName, int line, int column, int length = 1)
    {
        FileName = fileName;
        Line = line;
        Column = column;
        Length = length;
    }
    
    public override string ToString()
    {
        return $"{FileName}:{Line}:{Column}";
    }
}