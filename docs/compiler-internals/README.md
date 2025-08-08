# FLua Compiler Internals Documentation

This directory contains detailed technical documentation about the FLua compiler implementation.

## Documents

### [lua-5.4-language-fundamentals.md](lua-5.4-language-fundamentals.md)
Comprehensive overview of Lua 5.4 language features that are essential for compiler implementation:
- Dynamic typing system
- Values and types
- Metatables and metamethods
- Lexical scoping and environments
- Functions and closures
- Control structures
- Multiple returns and assignment
- Tables and their implementation

### [roslyn-syntax-generation.md](roslyn-syntax-generation.md)
Complete guide to using Roslyn's syntax generation APIs:
- SyntaxFactory vs SyntaxGenerator
- Creating basic syntax nodes (identifiers, literals)
- Building expressions (binary, unary, method calls)
- Generating statements (declarations, control flow)
- Creating declarations (methods, classes, properties)
- Common patterns used in FLua compiler
- Best practices and debugging tips

### [flua-compiler-architecture.md](flua-compiler-architecture.md)
Detailed description of the FLua compiler architecture:
- AST type definitions (F#)
- Code generation pipeline
- Compilation patterns for expressions and statements
- Scope management and variable resolution
- Error handling strategies
- Runtime integration
- Testing approach
- Known limitations

### [closure-compilation-limitations.md](closure-compilation-limitations.md)
In-depth analysis of closure and variable capture limitations in compiled Lua:
- Why closures are difficult to compile to static code
- Variable capture and upvalue challenges
- Current compilation behavior (what works/doesn't work)
- Module compilation issues with private state
- Practical workarounds and refactoring patterns
- Best practices for compilable code
- Future possibilities for closure support

## Purpose

These documents serve as a reference for:
1. Understanding Lua language semantics for correct implementation
2. Learning Roslyn APIs for C# code generation
3. Maintaining consistency in compiler implementation
4. Onboarding new contributors
5. Debugging compiler issues

## Related Files

- `/FLua.Ast/AstTypes.fs` - AST type definitions
- `/FLua.Compiler/RoslynCodeGenerator.cs` - Main code generator
- `/FLua.Runtime/` - Runtime library used by generated code
- `/CLAUDE.md` - Project-wide documentation and guidelines