namespace FLua.Common.Diagnostics;

/// <summary>
/// Builder for creating user-friendly diagnostics
/// </summary>
public static class DiagnosticBuilder
{
    // Parser errors
    public static FLuaDiagnostic UnexpectedToken(string found, string expected, string context, SourceLocation? location = null)
    {
        var diagnostic = new FLuaDiagnostic(
            ErrorCodes.UnexpectedToken,
            ErrorSeverity.Error,
            DiagnosticMessages.UnexpectedToken(found, expected, context),
            location);
        
        // Add contextual help
        if (found == "(" && expected.Contains("statement"))
            diagnostic.Help = DiagnosticMessages.Help.CheckFunctionCall;
        else if (expected.Contains("'end'"))
            diagnostic.Help = DiagnosticMessages.Help.CheckEnd;
            
        return diagnostic;
    }
    
    public static FLuaDiagnostic MissingClosingDelimiter(string delimiter, string opened, SourceLocation? location = null)
    {
        var diagnostic = new FLuaDiagnostic(
            ErrorCodes.MissingClosingDelimiter,
            ErrorSeverity.Error,
            DiagnosticMessages.MissingClosingDelimiter(delimiter, opened),
            location);
            
        diagnostic.Help = delimiter switch
        {
            ")" => DiagnosticMessages.Help.CheckParentheses,
            "]" => DiagnosticMessages.Help.CheckBrackets,
            "}" => DiagnosticMessages.Help.CheckBraces,
            "end" => DiagnosticMessages.Help.CheckEnd,
            _ => null
        };
        
        return diagnostic;
    }
    
    // Runtime errors
    public static FLuaDiagnostic NilValueAccess(string operation, SourceLocation? location = null)
    {
        var diagnostic = new FLuaDiagnostic(
            ErrorCodes.NilValueAccess,
            ErrorSeverity.Error,
            DiagnosticMessages.NilValueAccess(operation),
            location);
            
        diagnostic.Help = DiagnosticMessages.Help.CheckSpelling;
        diagnostic.Notes.Add("Variables must be defined before use");
        
        return diagnostic;
    }
    
    public static FLuaDiagnostic UnknownVariable(string name, SourceLocation? location = null)
    {
        var diagnostic = new FLuaDiagnostic(
            ErrorCodes.UnknownVariable,
            ErrorSeverity.Error,
            DiagnosticMessages.UnknownVariable(name),
            location);
            
        diagnostic.Help = DiagnosticMessages.Help.UseLocal;
        
        // Check for common typos
        if (name.ToLower() == "print" || name.ToLower() == "require")
        {
            diagnostic.Notes.Add($"Did you mean '{name.ToLower()}'? Lua is case-sensitive.");
        }
        
        return diagnostic;
    }
    
    // Compiler errors
    public static FLuaDiagnostic DynamicLoadingNotSupported(string function, SourceLocation? location = null)
    {
        var diagnostic = new FLuaDiagnostic(
            ErrorCodes.UnsupportedFeature,
            ErrorSeverity.Error,
            DiagnosticMessages.DynamicLoadingNotSupported(),
            location);
            
        diagnostic.Help = DiagnosticMessages.Help.UseRequire;
        diagnostic.Notes.Add($"The '{function}' function requires runtime compilation which is not available in native executables.");
        
        return diagnostic;
    }
    
    // Compiler warnings
    public static FLuaDiagnostic DynamicFeatureWarning(string feature, SourceLocation? location = null)
    {
        var diagnostic = new FLuaDiagnostic(
            ErrorCodes.DynamicFeatureUsed,
            ErrorSeverity.Warning,
            DiagnosticMessages.DynamicFeatureUsed(feature),
            location);
            
        diagnostic.Help = "This code will fail at runtime in compiled executables.";
        diagnostic.Notes.Add("Consider using the interpreter if dynamic features are required.");
        
        return diagnostic;
    }
    
    public static FLuaDiagnostic UnusedVariable(string name, SourceLocation? location = null)
    {
        var diagnostic = new FLuaDiagnostic(
            ErrorCodes.UnusedVariable,
            ErrorSeverity.Warning,
            DiagnosticMessages.UnusedVariable(name),
            location);
            
        return diagnostic;
    }
    
    public static FLuaDiagnostic ShadowedVariable(string name, SourceLocation? previousLocation, SourceLocation? location = null)
    {
        var diagnostic = new FLuaDiagnostic(
            ErrorCodes.ShadowedVariable,
            ErrorSeverity.Warning,
            DiagnosticMessages.ShadowedVariable(name, previousLocation?.ToString() ?? "previous scope"),
            location);
            
        diagnostic.Notes.Add("Shadowing can make code harder to understand and may hide bugs.");
        
        return diagnostic;
    }
    
    // Type errors
    public static FLuaDiagnostic TypeMismatch(string operation, string expected, string actual, SourceLocation? location = null)
    {
        var diagnostic = new FLuaDiagnostic(
            ErrorCodes.TypeMismatch,
            ErrorSeverity.Error,
            DiagnosticMessages.TypeMismatch(operation, expected, actual),
            location);
            
        // Add conversion help
        if (expected == "number" && actual == "string")
            diagnostic.Help = DiagnosticMessages.Help.UseToNumber;
        else if (expected == "string")
            diagnostic.Help = DiagnosticMessages.Help.UseToString;
            
        return diagnostic;
    }
}