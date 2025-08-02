# Expression Tree Compilation Example

This example demonstrates FLua's experimental support for compiling Lua code into LINQ expression trees.

## What are Expression Trees?

Expression trees represent code as data structures that can be:
- Analyzed and transformed at runtime
- Compiled to efficient delegates
- Translated to other languages (like SQL)
- Used with LINQ providers

## Current Status

The expression tree compilation is currently in early stages with support for:
- Basic arithmetic operations (+, -, *, /)
- Literal values (numbers, strings, booleans)
- Simple comparisons (==, <, >)
- String concatenation

## Future Potential

Full expression tree support would enable:

### 1. Dynamic LINQ Queries
```csharp
// Lua: "return function(x) return x.Age > 18 and x.Name:match('John') end"
var filter = host.CompileToExpression<Func<Person, bool>>(luaFilter);
var adults = dbContext.People.Where(filter);
```

### 2. Runtime Code Generation
```csharp
// Generate optimized code from Lua expressions
var formula = host.CompileToExpression<Func<double, double, double>>(
    "return function(x, y) return math.sqrt(x*x + y*y) end"
);
```

### 3. Integration with ORMs
```csharp
// Use Lua to define database queries
var query = host.CompileToExpression<Func<IQueryable<Product>, IQueryable<Product>>>(
    "return function(q) return q:where(function(p) return p.Price < 100 end) end"
);
```

## Use Cases

- **Dynamic Filtering**: Let users define custom filters in Lua
- **Formula Engines**: Compile mathematical expressions for repeated evaluation
- **Query Builders**: Generate database queries from user input
- **Rule Engines**: Express business rules that compile to efficient code

## Running the Example

```bash
dotnet run
```

## Limitations

Current implementation is minimal and primarily demonstrates the concept. Full support would require:
- Variable and parameter handling
- Function definitions
- Table access patterns
- Method calls
- Control flow structures