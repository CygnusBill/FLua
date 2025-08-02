# RoslynPath Syntax Guide

RoslynPath provides XPath-like syntax for navigating C# syntax trees in the McpDotnet MCP server.

## Core Syntax

### Basic Navigation
- `/` - Child navigation
- `//` - Find anywhere (descendant axis)
- `[]` - Node filter/selector
- Numbers - Position (1-based indexing)

### Node Types
- `compilation` - Root compilation unit
- `namespace`, `class`, `interface`, `struct`, `enum` - Type declarations
- `method`, `property`, `field`, `constructor` - Members
- `statement` - Any statement
- `expression` - Any expression
- `block` - Code blocks { }

### Selectors

#### Name Matching
- `[UserService]` - Exact name match
- `[Get*]` - Wildcard patterns
- `[*User*]` - Contains text

#### Type Filtering
- `[@type=IfStatement]` - Specific statement types
- `[@type=*Assignment*]` - Pattern matching on types

#### Content Search
- `[@contains='Console.WriteLine']` - Text content search (normalized)
- `[@matches='await.*Async']` - Regex matching
- `[@async]`, `[@public]` - Attribute/modifier checks

### Advanced Features

#### Complex Predicates
- `[@async and @public]` - Boolean combinations
- `[count(statement) > 10]` - Counting predicates
- `[.//statement[@type=ThrowStatement]]` - Nested path conditions

#### Navigation Axes
- `..` - Parent axis
- `/following-sibling::statement[1]` - Next sibling
- `/ancestor::method[1]` - Containing method

## Practical Examples

### Code Quality Checks
```
//method[@async and not(.//expression[@contains='await'])]  # Async without await
//statement[@type=IfStatement and @contains='== null']      # Null checks
//method[count(.//statement) > 20]                          # Long methods
//comment[@contains='TODO']                                 # TODO comments
```

### Architecture Analysis
```
//class[*Lua*]                           # All Lua-related classes
//method[Parse*]                         # All parser methods  
//class[@implements='IDisposable']       # Classes implementing IDisposable
//method[.//statement[@type=ThrowStatement]]  # Methods that throw
```

### Testing and Coverage
```
//method[*Test*]                         # Test methods
//method[*Test* and .//comment[@contains='TODO']]  # Incomplete tests
//class[count(method[@public]) = 0]      # Classes with no public methods
```

## Benefits for FLua Analysis
- **Semantic Understanding**: Goes beyond text search to understand code structure
- **Stability**: Paths survive code changes better than line/column positions  
- **Power**: Complex queries in concise syntax
- **Integration**: Works with McpDotnet editing and analysis tools