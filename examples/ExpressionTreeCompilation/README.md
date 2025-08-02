# Expression Tree Compilation Example

This example demonstrates FLua's experimental support for compiling Lua code into LINQ expression trees.

## Overview

Expression trees are a powerful .NET feature that represents code as data. Unlike compiled delegates, expression trees can be:
- Analyzed and transformed at runtime
- Translated to other languages (SQL, JavaScript)
- Used with LINQ providers (Entity Framework, MongoDB)
- Optimized based on runtime conditions

## Understanding Expression Trees

### What Are Expression Trees?

Expression trees are object graphs that represent code structure:

```csharp
// This lambda:
x => x + 1

// Becomes this tree:
Lambda
  └── Add
      ├── Parameter(x)
      └── Constant(1)
```

### Why Use Expression Trees?

1. **Database Queries**: Translate to SQL
   ```csharp
   dbContext.Users.Where(u => u.Age > 18)  // Becomes SQL WHERE clause
   ```

2. **Dynamic Compilation**: Build code at runtime
   ```csharp
   var param = Expression.Parameter(typeof(int), "x");
   var body = Expression.Add(param, Expression.Constant(1));
   var lambda = Expression.Lambda<Func<int, int>>(body, param);
   ```

3. **Code Analysis**: Inspect and modify expressions
   ```csharp
   if (expr.Body is BinaryExpression binary && binary.NodeType == ExpressionType.Add)
   {
       // Found an addition operation
   }
   ```

## Code Walkthrough

### Step 1: Current Implementation Status

```csharp
var arithmeticScript = @"
    return 10 + 20 * 3
";

var expr = host.CompileToExpression<double>(arithmeticScript);
```

Currently supported:
- ✅ Literals (numbers, strings, booleans)
- ✅ Basic arithmetic (+, -, *, /)
- ✅ Simple comparisons (==, <, >)
- ✅ String concatenation
- ❌ Variables (limited support)
- ❌ Functions
- ❌ Tables
- ❌ Control flow

### Step 2: How It Works

The compilation process:

```
Lua AST → Expression Tree Generator → LINQ Expression Tree
```

Example transformation:

```lua
-- Lua code:
return 5 + 3

-- AST representation:
Return(Binary(Add, Literal(5), Literal(3)))

-- Expression tree:
Expression.Add(
    Expression.Constant(5.0),
    Expression.Constant(3.0)
)
```

### Step 3: The Generator Implementation

```csharp
public class MinimalExpressionTreeGenerator
{
    public Expression<Func<LuaEnvironment, LuaValue[]>> Generate(IList<Statement> statements)
    {
        // Find return statement
        foreach (var stmt in statements)
        {
            if (stmt.IsReturn)
            {
                var returnStmt = (Statement.Return)stmt;
                var expr = GenerateExpression(returnStmt.Item.Value[0]);
                var arrayInit = Expression.NewArrayInit(typeof(LuaValue), expr);
                return Expression.Lambda<Func<LuaEnvironment, LuaValue[]>>(arrayInit, _envParameter);
            }
        }
    }
    
    private Expression GenerateExpression(Expr expr)
    {
        switch (expr)
        {
            case Expr.Literal literal:
                return GenerateLiteral(literal.Item);
                
            case Expr.Binary binary:
                var left = GenerateExpression(binary.Item1);
                var right = GenerateExpression(binary.Item3);
                return GenerateBinaryOp(left, binary.Item2, right);
                
            default:
                return Expression.Field(null, typeof(LuaValue), "Nil");
        }
    }
}
```

### Step 4: Practical Use Cases

#### Use Case 1: Dynamic LINQ Queries

```csharp
// Future capability: Compile Lua to LINQ predicates
var luaFilter = "return function(user) return user.age > 18 and user.active end";
var predicate = host.CompileToExpression<Func<User, bool>>(luaFilter);

// Use with Entity Framework
var adults = dbContext.Users.Where(predicate);
```

#### Use Case 2: Formula Engines

```csharp
// Dynamic formula compilation
public class FormulaEngine
{
    public Expression<Func<Dictionary<string, double>, double>> CompileFormula(string lua)
    {
        // Lua: "return vars.price * vars.quantity * (1 - vars.discount)"
        return host.CompileToExpression<Func<Dictionary<string, double>, double>>(lua);
    }
}
```

#### Use Case 3: Rule Engines

```csharp
// Business rules as expression trees
public class RuleEngine
{
    private List<Expression<Func<Order, bool>>> rules = new();
    
    public void AddRule(string luaRule)
    {
        // Lua: "return function(order) return order.total > 1000 end"
        var rule = host.CompileToExpression<Func<Order, bool>>(luaRule);
        rules.Add(rule);
    }
    
    public bool Evaluate(Order order)
    {
        return rules.All(rule => rule.Compile()(order));
    }
}
```

### Step 5: Expression Tree Benefits

1. **Queryable Providers**
   ```csharp
   // Expression trees can be translated to SQL
   IQueryable<T> query = source.Where(expressionTree);
   ```

2. **Runtime Optimization**
   ```csharp
   // Modify expression tree based on conditions
   if (useCache)
   {
       expr = Expression.Call(cacheMethod, expr);
   }
   ```

3. **Cross-Platform Code**
   ```csharp
   // Same expression tree → SQL, JavaScript, etc.
   var sql = SqlTranslator.Translate(expr);
   var js = JavaScriptTranslator.Translate(expr);
   ```

### Step 6: Current Limitations

The current implementation is minimal because:

1. **Variable Scope**: Lua's dynamic scoping vs C#'s static typing
2. **Type System**: Lua's dynamic types vs expression trees' static types
3. **Functions**: First-class functions are complex to represent
4. **Tables**: Dynamic structure doesn't map well to static expressions

### Step 7: Future Roadmap

Full expression tree support would enable:

```csharp
// Complex Lua expressions as LINQ
var expr = host.CompileToExpression<Func<Product, bool>>(@"
    return function(p)
        return p.price < 100 and 
               p.category == 'electronics' and
               p.inStock > 0
    end
");

// Use with any LINQ provider
var cheapElectronics = products.Where(expr);
```

## Complete Example: Expression Visitor

```csharp
// Example of working with generated expression trees
public class LuaExpressionOptimizer : ExpressionVisitor
{
    protected override Expression VisitBinary(BinaryExpression node)
    {
        // Optimize constant folding
        if (node.Left is ConstantExpression left && 
            node.Right is ConstantExpression right)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Add:
                    var sum = (double)left.Value + (double)right.Value;
                    return Expression.Constant(sum);
                    
                case ExpressionType.Multiply:
                    var product = (double)left.Value * (double)right.Value;
                    return Expression.Constant(product);
            }
        }
        
        return base.VisitBinary(node);
    }
}

// Usage
var expr = host.CompileToExpression<double>("return 2 + 3 * 4");
var optimizer = new LuaExpressionOptimizer();
var optimized = optimizer.Visit(expr.Body);
```

## Integration Scenarios

### Scenario 1: Dynamic Filters
```csharp
public IQueryable<T> ApplyLuaFilter<T>(IQueryable<T> query, string luaFilter)
{
    var expr = host.CompileToExpression<Func<T, bool>>(luaFilter);
    return query.Where(expr);
}
```

### Scenario 2: Calculated Properties
```csharp
public Expression<Func<T, double>> CreateCalculation<T>(string luaCalc)
{
    return host.CompileToExpression<Func<T, double>>(luaCalc);
}
```

### Scenario 3: Validation Rules
```csharp
public class Validator<T>
{
    private Expression<Func<T, bool>> validationExpr;
    
    public void SetRule(string luaRule)
    {
        validationExpr = host.CompileToExpression<Func<T, bool>>(luaRule);
    }
}
```

## Technical Deep Dive

### Expression Types in .NET

```csharp
// Constants
Expression.Constant(42)

// Parameters
Expression.Parameter(typeof(int), "x")

// Binary operations
Expression.Add(left, right)
Expression.Multiply(left, right)

// Member access
Expression.Property(obj, "PropertyName")

// Method calls
Expression.Call(instance, methodInfo, args)

// Conditionals
Expression.Condition(test, ifTrue, ifFalse)

// Lambdas
Expression.Lambda<Func<int, int>>(body, parameters)
```

### Lua to Expression Mapping

| Lua | Expression Tree |
|-----|----------------|
| `42` | `Expression.Constant(42.0)` |
| `x + y` | `Expression.Add(x, y)` |
| `a.b` | `Expression.Property(a, "b")` |
| `f(x)` | `Expression.Call(f, x)` |
| `{1,2}` | Complex (no direct mapping) |

## Performance Considerations

- Expression tree compilation is slower than delegate compilation
- Once compiled, performance is identical
- Tree manipulation adds overhead
- Best for scenarios where tree analysis is needed

## Next Steps

- Understand current limitations
- Consider if expression trees fit your use case
- For pure performance, use [Lambda Compilation](../LambdaCompilation)
- For complex scenarios, contribute to improving support!

## Running the Example

```bash
dotnet run
```

The example will:
1. Show current capabilities
2. Demonstrate limitations
3. Explain potential use cases
4. Provide integration patterns