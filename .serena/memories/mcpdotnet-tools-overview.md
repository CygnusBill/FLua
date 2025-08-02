# McpDotnet Tools Overview

The McpDotnet MCP server provides powerful semantic analysis tools for C# codebases.

## Core Discovery Tools

### Class and Type Analysis
- `dotnet-find-class` - Find classes with pattern matching (`User*`, `*Service`)
- `dotnet-find-derived-types` - Find classes inheriting from base class
- `dotnet-find-implementations` - Find interface implementations

### Method Analysis  
- `dotnet-find-method` - Find methods with wildcards (`Get*`, `Parse*`)
- `dotnet-find-method-calls` - Find all calls to specific methods
- `dotnet-find-method-callers` - Find what calls a specific method
- `dotnet-find-overrides` - Find overrides of virtual/abstract methods

### Property and Symbol Analysis
- `dotnet-find-property` - Find properties with pattern matching
- `dotnet-find-references` - Find all references to a symbol
- `dotnet-rename-symbol` - Safe renaming across entire workspace

## Advanced Search with RoslynPath

### Statement Analysis
- `dotnet-find-statements` - Uses RoslynPath XPath-like syntax
  - Text patterns: `pattern="Console.WriteLine"`
  - Regex patterns: `patternType="regex"`
  - RoslynPath queries: `patternType="roslynpath"`

Examples:
```
// Find all async methods without await
pattern="//method[@async and not(.//expression[@contains='await'])]"
patternType="roslynpath"

// Find methods with too many statements  
pattern="//method[count(.//statement) > 20]"
patternType="roslynpath"
```

## Code Modification Tools

### Structural Changes
- `dotnet-edit-code` - Add/modify/remove code in classes
- `dotnet-insert-statement` - Insert statements before/after existing ones
- `dotnet-replace-statement` - Replace specific statements
- `dotnet-remove-statement` - Remove specific statements

### Pattern-Based Changes
- `dotnet-fix-pattern` - Find and replace with regex patterns
- Supports preview mode for safety

## Analysis and Inspection

### Syntax Analysis
- `dotnet-analyze-syntax` - Analyze syntax tree structure
- `dotnet-get-symbols` - Get symbol information from code

### Statement Tracking
- `dotnet-mark-statement` - Mark statements with ephemeral markers
- `dotnet-find-marked-statements` - Find marked statements
- `dotnet-clear-markers` - Clear all markers

## Best Practices for FLua Analysis

1. **Start with broad discovery**: Use `dotnet-find-class` and `dotnet-find-method` to map the codebase
2. **Use RoslynPath for complex queries**: Leverage semantic understanding over text search
3. **Combine tools**: Use find tools to discover, then analyze with `dotnet-get-symbols`
4. **Preview changes**: Always use preview mode for modifications
5. **Track progress**: Use markers to track analysis progress across sessions