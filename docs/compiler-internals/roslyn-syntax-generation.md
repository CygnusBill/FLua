# Roslyn Syntax Generation Guide

## Overview
This document provides a comprehensive reference for using Roslyn's syntax generation APIs, particularly the SyntaxFactory and SyntaxGenerator classes used in the FLua compiler.

## Key Classes

### 1. SyntaxFactory (C# Specific)
Low-level API for creating C# syntax nodes directly.

```csharp
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
```

### 2. SyntaxGenerator (Language Agnostic)
Higher-level API that works across C# and VB.NET.

```csharp
using Microsoft.CodeAnalysis.Editing;
```

## Creating Basic Syntax Nodes

### Identifiers and Names
```csharp
// Simple identifier
var name = IdentifierName("variable");

// Generic name
var genericName = GenericName("List")
    .AddTypeArgumentListArguments(IdentifierName("string"));

// Qualified name (namespace.class)
var qualifiedName = QualifiedName(
    IdentifierName("System"),
    IdentifierName("String"));

// Member access (object.Member)
var memberAccess = MemberAccessExpression(
    SyntaxKind.SimpleMemberAccessExpression,
    IdentifierName("obj"),
    IdentifierName("Method"));
```

### Literals
```csharp
// Numeric literals
var intLiteral = LiteralExpression(
    SyntaxKind.NumericLiteralExpression, 
    Literal(42));

var doubleLiteral = LiteralExpression(
    SyntaxKind.NumericLiteralExpression, 
    Literal(3.14));

// String literal
var stringLiteral = LiteralExpression(
    SyntaxKind.StringLiteralExpression, 
    Literal("Hello"));

// Boolean literals
var trueLiteral = LiteralExpression(SyntaxKind.TrueLiteralExpression);
var falseLiteral = LiteralExpression(SyntaxKind.FalseLiteralExpression);

// Null literal
var nullLiteral = LiteralExpression(SyntaxKind.NullLiteralExpression);
```

## Creating Expressions

### Binary Expressions
```csharp
// Arithmetic
var add = BinaryExpression(
    SyntaxKind.AddExpression,
    left,
    right);

var multiply = BinaryExpression(
    SyntaxKind.MultiplyExpression,
    left,
    right);

// Comparison
var lessThan = BinaryExpression(
    SyntaxKind.LessThanExpression,
    left,
    right);

var equals = BinaryExpression(
    SyntaxKind.EqualsExpression,
    left,
    right);

// Logical
var and = BinaryExpression(
    SyntaxKind.LogicalAndExpression,
    left,
    right);

var or = BinaryExpression(
    SyntaxKind.LogicalOrExpression,
    left,
    right);

// Assignment
var assignment = AssignmentExpression(
    SyntaxKind.SimpleAssignmentExpression,
    left,
    right);
```

### Unary Expressions
```csharp
// Negation
var negate = PrefixUnaryExpression(
    SyntaxKind.UnaryMinusExpression,
    expression);

// Logical not
var not = PrefixUnaryExpression(
    SyntaxKind.LogicalNotExpression,
    expression);

// Post-increment
var postIncrement = PostfixUnaryExpression(
    SyntaxKind.PostIncrementExpression,
    expression);
```

### Method Invocations
```csharp
// Simple method call
var methodCall = InvocationExpression(
    IdentifierName("Method"))
    .AddArgumentListArguments(
        Argument(arg1),
        Argument(arg2));

// Method call on object
var objectMethodCall = InvocationExpression(
    MemberAccessExpression(
        SyntaxKind.SimpleMemberAccessExpression,
        IdentifierName("obj"),
        IdentifierName("Method")))
    .AddArgumentListArguments(
        Argument(expression));

// Generic method call
var genericCall = InvocationExpression(
    GenericName("Method")
        .AddTypeArgumentListArguments(
            IdentifierName("string")))
    .AddArgumentListArguments(
        Argument(arg));
```

### Object Creation
```csharp
// Simple object creation
var newObj = ObjectCreationExpression(
    IdentifierName("MyClass"))
    .AddArgumentListArguments(
        Argument(arg1),
        Argument(arg2));

// Array creation
var newArray = ArrayCreationExpression(
    ArrayType(IdentifierName("int"))
        .WithRankSpecifiers(
            SingletonList(ArrayRankSpecifier())))
    .WithInitializer(
        InitializerExpression(
            SyntaxKind.ArrayInitializerExpression,
            SeparatedList(new[] { expr1, expr2 })));
```

### Casting and Type Checking
```csharp
// Cast expression
var cast = CastExpression(
    IdentifierName("TargetType"),
    expression);

// As expression
var asExpr = BinaryExpression(
    SyntaxKind.AsExpression,
    expression,
    IdentifierName("TargetType"));

// Is expression
var isExpr = BinaryExpression(
    SyntaxKind.IsExpression,
    expression,
    IdentifierName("TargetType"));
```

## Creating Statements

### Variable Declarations
```csharp
// Local variable with initializer
var localVar = LocalDeclarationStatement(
    VariableDeclaration(IdentifierName("var"))
        .AddVariables(
            VariableDeclarator("myVar")
                .WithInitializer(
                    EqualsValueClause(expression))));

// Explicit type
var typedVar = LocalDeclarationStatement(
    VariableDeclaration(IdentifierName("string"))
        .AddVariables(
            VariableDeclarator("name")
                .WithInitializer(
                    EqualsValueClause(
                        LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            Literal("value"))))));
```

### Control Flow

#### If Statements
```csharp
var ifStatement = IfStatement(
    condition,
    Block(thenStatements));

// With else
var ifElse = IfStatement(
    condition,
    Block(thenStatements),
    ElseClause(Block(elseStatements)));

// With else if
var ifElseIf = IfStatement(
    condition1,
    Block(statements1),
    ElseClause(
        IfStatement(
            condition2,
            Block(statements2))));
```

#### Loops
```csharp
// While loop
var whileLoop = WhileStatement(
    condition,
    Block(statements));

// Do-while loop
var doWhile = DoStatement(
    Block(statements),
    condition);

// For loop
var forLoop = ForStatement(
    declaration,    // Variable declaration
    condition,      // Condition
    incrementors,   // Increment expressions
    Block(statements));

// Foreach loop
var forEach = ForEachStatement(
    IdentifierName("var"),
    "item",
    collection,
    Block(statements));
```

#### Switch Statements
```csharp
var switchStmt = SwitchStatement(expression)
    .AddSections(
        SwitchSection()
            .AddLabels(CaseSwitchLabel(literal1))
            .AddStatements(statements1)
            .AddStatements(BreakStatement()),
        SwitchSection()
            .AddLabels(DefaultSwitchLabel())
            .AddStatements(defaultStatements)
            .AddStatements(BreakStatement()));
```

### Try-Catch-Finally
```csharp
var tryCatch = TryStatement()
    .WithBlock(Block(tryStatements))
    .AddCatches(
        CatchClause()
            .WithDeclaration(
                CatchDeclaration(IdentifierName("Exception"))
                    .WithIdentifier(Identifier("ex")))
            .WithBlock(Block(catchStatements)))
    .WithFinally(
        FinallyClause(Block(finallyStatements)));
```

## Creating Declarations

### Method Declarations
```csharp
var method = MethodDeclaration(
    returnType: IdentifierName("int"),
    identifier: Identifier("Calculate"))
    .AddModifiers(Token(SyntaxKind.PublicKeyword))
    .AddParameterListParameters(
        Parameter(Identifier("x"))
            .WithType(IdentifierName("int")),
        Parameter(Identifier("y"))
            .WithType(IdentifierName("int")))
    .WithBody(Block(statements));
```

### Class Declarations
```csharp
var classDecl = ClassDeclaration("MyClass")
    .AddModifiers(
        Token(SyntaxKind.PublicKeyword),
        Token(SyntaxKind.SealedKeyword))
    .AddBaseListTypes(
        SimpleBaseType(IdentifierName("BaseClass")))
    .AddMembers(
        field,
        property,
        constructor,
        method);
```

### Property Declarations
```csharp
var property = PropertyDeclaration(
    IdentifierName("string"),
    Identifier("Name"))
    .AddModifiers(Token(SyntaxKind.PublicKeyword))
    .AddAccessorListAccessors(
        AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)),
        AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
```

## Common Patterns in FLua Compiler

### Creating LuaValue Operations
```csharp
// LuaValue.Number(42)
var luaNumber = InvocationExpression(
    MemberAccessExpression(
        SyntaxKind.SimpleMemberAccessExpression,
        IdentifierName("LuaValue"),
        IdentifierName("Number")))
    .AddArgumentListArguments(
        Argument(LiteralExpression(
            SyntaxKind.NumericLiteralExpression,
            Literal(42))));

// value.AsTable<LuaTable>()
var asTable = InvocationExpression(
    MemberAccessExpression(
        SyntaxKind.SimpleMemberAccessExpression,
        expression,
        GenericName("AsTable")
            .AddTypeArgumentListArguments(
                IdentifierName("LuaTable"))));

// value.IsTruthy()
var isTruthy = InvocationExpression(
    MemberAccessExpression(
        SyntaxKind.SimpleMemberAccessExpression,
        expression,
        IdentifierName("IsTruthy")));
```

### Environment Operations
```csharp
// env.GetVariable("name")
var getVar = InvocationExpression(
    MemberAccessExpression(
        SyntaxKind.SimpleMemberAccessExpression,
        IdentifierName("env"),
        IdentifierName("GetVariable")))
    .AddArgumentListArguments(
        Argument(LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            Literal("varName"))));

// env.SetVariable("name", value)
var setVar = InvocationExpression(
    MemberAccessExpression(
        SyntaxKind.SimpleMemberAccessExpression,
        IdentifierName("env"),
        IdentifierName("SetVariable")))
    .AddArgumentListArguments(
        Argument(LiteralExpression(
            SyntaxKind.StringLiteralExpression,
            Literal("varName"))),
        Argument(valueExpression));
```

## Best Practices

1. **Use Static Imports**: Import `SyntaxFactory` statically to reduce verbosity
2. **Builder Pattern**: Chain method calls for cleaner code
3. **Reuse Common Patterns**: Create helper methods for frequently used patterns
4. **Preserve Formatting**: Use `SyntaxTrivia` for proper spacing and indentation
5. **Validate Syntax**: Use `Compilation.GetDiagnostics()` to check generated code

## Debugging Tips

1. **Visualize Syntax Trees**: Use Roslyn's Syntax Visualizer in Visual Studio
2. **ToString()**: Call `ToString()` on nodes to see generated code
3. **Format Code**: Use `Formatter.Format()` to prettify output
4. **Check Diagnostics**: Always check compilation diagnostics for errors

## References

- [Roslyn GitHub Repository](https://github.com/dotnet/roslyn)
- [SyntaxFactory API Documentation](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.csharp.syntaxfactory)
- [SyntaxGenerator API Documentation](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.editing.syntaxgenerator)