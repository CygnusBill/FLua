namespace FLua.Common.Diagnostics;

/// <summary>
/// Standard error codes for FLua diagnostics
/// 
/// Format: FLU-XYZZ where:
///   X = Severity (1=Error, 2=Warning, 3=Info, 4=Hint)
///   Y = Area (0=Parser, 1=Runtime, 2=Compiler, 3=Type, 4=Module, 5=Library, 9=Internal)
///   ZZ = Sequential number
/// </summary>
public static class ErrorCodes
{
    // Parser errors (1-0-xx)
    public const string UnexpectedToken = "FLU-1001";
    public const string MissingClosingDelimiter = "FLU-1002";
    public const string InvalidExpression = "FLU-1003";
    public const string ReservedWordMisuse = "FLU-1004";
    public const string InvalidNumberFormat = "FLU-1005";
    public const string InvalidStringEscape = "FLU-1006";
    public const string UnterminatedString = "FLU-1007";
    public const string UnterminatedComment = "FLU-1008";
    public const string InvalidTableConstructor = "FLU-1009";
    public const string InvalidFunctionDefinition = "FLU-1010";
    
    // Runtime errors (1-1-xx)
    public const string NilValueAccess = "FLU-1101";
    public const string TypeMismatch = "FLU-1102";
    public const string UnknownVariable = "FLU-1103";
    public const string StackOverflow = "FLU-1104";
    public const string InvalidOperation = "FLU-1105";
    public const string IndexOutOfBounds = "FLU-1106";
    public const string InvalidArgument = "FLU-1107";
    public const string DivisionByZero = "FLU-1108";
    public const string InvalidMetamethod = "FLU-1109";
    public const string ConstAssignment = "FLU-1110";
    
    // Compiler errors (1-2-xx)
    public const string UnsupportedFeature = "FLU-1201";
    public const string CodeGenerationFailure = "FLU-1202";
    public const string InvalidCompilationTarget = "FLU-1203";
    public const string MissingRuntimeDependency = "FLU-1204";
    public const string InvalidSyntaxForCompilation = "FLU-1205";
    
    // Type errors (1-3-xx)
    public const string InvalidTypeConversion = "FLU-1301";
    public const string IncompatibleTypes = "FLU-1302";
    public const string NilNotAllowed = "FLU-1303";
    
    // Module/require errors (1-4-xx)
    public const string ModuleNotFound = "FLU-1401";
    public const string CircularDependency = "FLU-1402";
    public const string InvalidModulePath = "FLU-1403";
    public const string ModuleLoadError = "FLU-1404";
    
    // Built-in library errors (1-5-xx)
    public const string InvalidPatternSyntax = "FLU-1501";
    public const string FileOperationFailed = "FLU-1502";
    public const string InvalidDateFormat = "FLU-1503";
    public const string CoroutineError = "FLU-1504";
    
    // Internal errors (1-9-xx)
    public const string InternalError = "FLU-1901";
    public const string NotImplemented = "FLU-1902";
    public const string AssertionFailed = "FLU-1903";
    
    // Parser warnings (2-0-xx)
    public const string UnusedLabel = "FLU-2001";
    public const string EmptyStatement = "FLU-2002";
    
    // Runtime warnings (2-1-xx)
    public const string DeprecatedFunction = "FLU-2101";
    public const string PossibleNilAccess = "FLU-2102";
    
    // Compiler warnings (2-2-xx)
    public const string DynamicFeatureUsed = "FLU-2201";
    public const string PerformanceWarning = "FLU-2202";
    public const string PotentialRuntimeIncompatibility = "FLU-2203";
    public const string UnusedVariable = "FLU-2204";
    public const string ShadowedVariable = "FLU-2205";
    
    // Type warnings (2-3-xx)
    public const string ImplicitTypeConversion = "FLU-2301";
    public const string PossibleTypeMismatch = "FLU-2302";
    
    // Parser info (3-0-xx)
    public const string ParseRecovered = "FLU-3001";
    
    // Runtime info (3-1-xx)
    public const string GarbageCollected = "FLU-3101";
    
    // Compiler info (3-2-xx)
    public const string OptimizationApplied = "FLU-3201";
    public const string CompiledSuccessfully = "FLU-3202";
    
    // Parser hints (4-0-xx)
    public const string UseParentheses = "FLU-4001";
    public const string SimplifyExpression = "FLU-4002";
    
    // Runtime hints (4-1-xx)
    public const string UseLocal = "FLU-4101";
    public const string CheckNil = "FLU-4102";
    
    // Compiler hints (4-2-xx)
    public const string ConsiderPrecompiling = "FLU-4201";
    public const string UseStaticPattern = "FLU-4202";
}