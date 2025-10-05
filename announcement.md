# 🎉 FLua - Complete Lua 5.4 Implementation for .NET is Now Open Source!

**TL;DR**: I've built a complete Lua 5.4 implementation for .NET with interpreter, compiler, and native AOT backends. It's production-ready and available on NuGet now!

## 🚀 **What is FLua?**

FLua is a comprehensive Lua 5.4 implementation for .NET that provides:

- **Complete Lua 5.4 syntax support** - Full parser with FParsec
- **Multiple execution backends**:
  - Tree-walking interpreter (fast startup)
  - Roslyn-based compiler (optimized execution)
  - Expression tree compiler (dynamic)
  - Native AOT compilation (self-contained binaries)
- **Production-ready features**:
  - All Lua 5.4 language features
  - Complete standard library (`math`, `string`, `table`, `os`, `io`, etc.)
  - Coroutines, metatables, modules
  - Security sandboxing and hosting APIs
  - Interactive REPL with syntax highlighting

## 💡 **Why FLua?**

**For .NET Developers:**
- Embed Lua scripting in your C# applications
- Create domain-specific languages
- Add configuration/scripting capabilities
- Build game modding systems

**For Lua Developers:**
- Run Lua code on .NET platforms
- Access .NET libraries from Lua
- Cross-platform deployment with AOT compilation

**For Language Enthusiasts:**
- Study a complete language implementation
- See F# and C# interoperability in action
- Learn about different compilation strategies

## 🛠️ **Getting Started**

```bash
# Install CLI tool
dotnet tool install --global flua --version 1.0.0-alpha.1

# Try the REPL
flua repl

# Run a script
flua your_script.lua
```

```csharp
// Use in your .NET app
using FLua.Hosting;

// Create a Lua host
var host = new LuaHost();
var result = await host.ExecuteAsync("return 1 + 2 * 3");
// result.Value == 7
```

## 📊 **Quality & Testing**

- **1,222+ unit tests** covering all functionality
- **Lua 5.4 specification compliant**
- **Comprehensive documentation** and examples
- **Cross-platform** (Windows, macOS, Linux)

## 📦 **Available on NuGet**

```
FLua.Ast          # AST definitions
FLua.Parser       # F# Lua parser
FLua.Runtime      # Runtime & stdlib
FLua.Interpreter  # Tree-walking interpreter
FLua.Compiler     # Compilation backends
FLua.Hosting      # Hosting API
flua              # CLI tool
```

## 🔗 **Links**

- **GitHub**: https://github.com/CygnusBill/FLua
- **NuGet**: Search for "FLua" on nuget.org
- **Documentation**: Comprehensive docs in repo
- **Releases**: https://github.com/CygnusBill/FLua/releases

## 🎯 **Roadmap**

- [x] Complete Lua 5.4 implementation
- [x] Multiple compilation backends
- [x] Production-ready hosting APIs
- [x] Open source release
- [ ] Lua 5.5 features (future)
- [ ] Performance optimizations
- [ ] JIT compilation backend

---

**FLua brings the power and elegance of Lua to the .NET ecosystem!** 🦎✨

Built with ❤️ using F#, C#, and lots of compiler theory.

#lua #dotnet #csharp #fsharp #programming #opensource
