namespace FLua.Common.Diagnostics;

/// <summary>
/// Standard error codes for FLua diagnostics
/// </summary>
public static class ErrorCodes
{
    // Parser errors (FLU-0xxx)
    public const string UnexpectedToken = "FLU-0001";
    public const string MissingClosingDelimiter = "FLU-0002";
    public const string InvalidExpression = "FLU-0003";
    public const string ReservedWordMisuse = "FLU-0004";
    public const string InvalidNumberFormat = "FLU-0005";
    public const string InvalidStringEscape = "FLU-0006";
    public const string UnterminatedString = "FLU-0007";
    public const string UnterminatedComment = "FLU-0008";
    public const string InvalidTableConstructor = "FLU-0009";
    public const string InvalidFunctionDefinition = "FLU-0010";
    
    // Runtime errors (FLU-1xxx)
    public const string NilValueAccess = "FLU-1001";
    public const string TypeMismatch = "FLU-1002";
    public const string UnknownVariable = "FLU-1003";
    public const string StackOverflow = "FLU-1004";
    public const string InvalidOperation = "FLU-1005";
    public const string IndexOutOfBounds = "FLU-1006";
    public const string InvalidArgument = "FLU-1007";
    public const string DivisionByZero = "FLU-1008";
    public const string InvalidMetamethod = "FLU-1009";
    public const string ConstAssignment = "FLU-1010";
    
    // Compiler errors (FLU-2xxx)
    public const string UnsupportedFeature = "FLU-2001";
    public const string CodeGenerationFailure = "FLU-2002";
    public const string InvalidCompilationTarget = "FLU-2003";
    public const string MissingRuntimeDependency = "FLU-2004";
    public const string InvalidSyntaxForCompilation = "FLU-2005";
    
    // Compiler warnings (FLU-25xx)
    public const string DynamicFeatureUsed = "FLU-2501";
    public const string PerformanceWarning = "FLU-2502";
    public const string PotentialRuntimeIncompatibility = "FLU-2503";
    public const string UnusedVariable = "FLU-2504";
    public const string ShadowedVariable = "FLU-2505";
    
    // Type-related errors (FLU-3xxx)
    public const string InvalidTypeConversion = "FLU-3001";
    public const string IncompatibleTypes = "FLU-3002";
    public const string NilNotAllowed = "FLU-3003";
    
    // Module/require errors (FLU-4xxx)
    public const string ModuleNotFound = "FLU-4001";
    public const string CircularDependency = "FLU-4002";
    public const string InvalidModulePath = "FLU-4003";
    public const string ModuleLoadError = "FLU-4004";
    
    // Built-in library errors (FLU-5xxx)
    public const string InvalidPatternSyntax = "FLU-5001";
    public const string FileOperationFailed = "FLU-5002";
    public const string InvalidDateFormat = "FLU-5003";
    public const string CoroutineError = "FLU-5004";
    
    // Internal errors (FLU-9xxx)
    public const string InternalError = "FLU-9001";
    public const string NotImplemented = "FLU-9002";
    public const string AssertionFailed = "FLU-9003";
}