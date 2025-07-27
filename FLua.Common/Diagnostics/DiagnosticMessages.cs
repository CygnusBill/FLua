namespace FLua.Common.Diagnostics;

/// <summary>
/// Provides user-friendly diagnostic messages
/// </summary>
public static class DiagnosticMessages
{
    // Parser error messages
    public static string UnexpectedToken(string found, string expected, string context)
        => $"Encountered '{found}' while parsing {context}. Expected {expected}.";
    
    public static string MissingClosingDelimiter(string delimiter, string opened)
        => $"Missing closing '{delimiter}'. The '{opened}' opened here needs to be closed.";
    
    public static string InvalidExpression(string found, string context)
        => $"'{found}' is not a valid expression in {context}.";
    
    public static string ReservedWordMisuse(string word, string context)
        => $"Cannot use '{word}' here. '{word}' is a reserved word that can only be used in {context}.";
    
    public static string InvalidNumberFormat(string number)
        => $"'{number}' is not a valid number. Numbers should be like 123, 3.14, or 0xFF.";
    
    public static string InvalidStringEscape(string escape)
        => $"'\\{escape}' is not a valid escape sequence. Use \\n for newline, \\t for tab, etc.";
    
    public static string UnterminatedString(string quote)
        => $"This string is missing its closing {quote}. Strings must have matching quotes.";
    
    public static string UnterminatedComment()
        => "This comment block is missing its closing '--]]'. Long comments must be closed.";
    
    public static string InvalidTableConstructor(string issue)
        => $"This table has {issue}. Tables should look like {{1, 2, 3}} or {{x = 1, y = 2}}.";
    
    public static string InvalidFunctionDefinition(string issue)
        => $"This function definition {issue}. Functions should look like 'function name(params) ... end'.";
    
    // Runtime error messages
    public static string NilValueAccess(string operation, string hint = "")
        => $"Attempted to {operation} a nil value.{(string.IsNullOrEmpty(hint) ? "" : $" {hint}")}";
    
    public static string TypeMismatch(string operation, string expected, string actual)
        => $"Cannot {operation}: expected {expected} but got {actual}.";
    
    public static string UnknownVariable(string name)
        => $"'{name}' is not defined. Did you forget to declare it with 'local {name}'?";
    
    public static string InvalidOperation(string operation, string type)
        => $"Cannot {operation} on {type}. This operation is not supported for this type.";
    
    public static string IndexOutOfBounds(int index, int size)
        => $"Tried to access index {index}, but the table only has {size} elements.";
    
    public static string DivisionByZero()
        => "Cannot divide by zero. Check that the divisor is not zero before dividing.";
    
    public static string ConstAssignment(string variable)
        => $"Cannot change '{variable}' because it was declared as <const>. Remove <const> or use a different variable.";
    
    // Compiler error messages  
    public static string UnsupportedFeature(string feature, string context)
        => $"{feature} is not supported in compiled code. {context}";
    
    public static string DynamicLoadingNotSupported()
        => "Dynamic code loading (load/loadfile/dofile) is not supported in compiled executables. " +
           "All code must be available at compile time. Consider using require() with pre-compiled modules instead.";
    
    public static string CodeGenerationFailure(string construct, string reason)
        => $"Failed to compile {construct}: {reason}";
    
    // Compiler error messages  
    public static string DynamicFeatureUsed(string feature)
        => $"Function '{feature}' cannot be used in compiled code. Dynamic code loading is not supported in compiled executables.";
    
    public static string UnusedVariable(string name)
        => $"Variable '{name}' is defined but never used. Consider removing it or prefix with '_' to indicate it's intentionally unused.";
    
    public static string ShadowedVariable(string name, string previousLocation)
        => $"Variable '{name}' shadows a previous declaration at {previousLocation}. Consider using a different name to avoid confusion.";
    
    // Module error messages
    public static string ModuleNotFound(string module, string[] searchPaths)
        => $"Cannot find module '{module}'. Searched in:\n  " + string.Join("\n  ", searchPaths);
    
    public static string CircularDependency(string module, string chain)
        => $"Module '{module}' creates a circular dependency: {chain}. Restructure your modules to avoid circular requires.";
    
    // Type error messages
    public static string InvalidTypeConversion(string from, string to, string operation)
        => $"Cannot convert {from} to {to} for {operation}. Use to{to}() to convert explicitly.";
    
    public static string NilNotAllowed(string context)
        => $"nil is not allowed in {context}. Provide a valid value instead.";
    
    // Built-in library error messages
    public static string InvalidPatternSyntax(string pattern, string issue)
        => $"Pattern '{pattern}' is invalid: {issue}. See Lua pattern documentation for valid syntax.";
    
    public static string FileOperationFailed(string operation, string file, string reason)
        => $"Cannot {operation} file '{file}': {reason}";
    
    // Help suggestions
    public static class Help
    {
        public const string CheckSpelling = "Check for typos in variable or function names.";
        public const string UseLocal = "Did you mean to use 'local' to declare a variable?";
        public const string CheckParentheses = "Make sure all opening parentheses '(' have matching closing ')'.";
        public const string CheckBrackets = "Make sure all opening brackets '[' have matching closing ']'.";
        public const string CheckBraces = "Make sure all opening braces '{' have matching closing '}'.";
        public const string CheckEnd = "Make sure all blocks (if/for/while/function) have matching 'end'.";
        public const string UseRequire = "Use require() to load modules that are compiled ahead of time.";
        public const string CheckTableSyntax = "Table elements should be separated by commas or semicolons.";
        public const string CheckFunctionCall = "Function calls need parentheses: func() not func";
        public const string UseToString = "Use tostring() to convert values to strings.";
        public const string UseToNumber = "Use tonumber() to convert strings to numbers.";
    }
}