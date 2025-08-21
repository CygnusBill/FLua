using FLua.Ast;
using FLua.Common.Diagnostics;
using FLua.Runtime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace FLua.Compiler;

/// <summary>
/// Lua compiler backend using Roslyn for C# code generation
/// </summary>
public class RoslynLuaCompiler : ILuaCompiler
{
    public string BackendName => "Roslyn";
    
    public IEnumerable<CompilationTarget> SupportedTargets =>
    [
        CompilationTarget.Library,
        CompilationTarget.ConsoleApp,
        CompilationTarget.NativeAot,
        CompilationTarget.Lambda,
        CompilationTarget.Expression
    ];

    public CompilationResult Compile(IList<Statement> ast, CompilerOptions options)
    {
        var diagnostics = new DiagnosticCollector();
        
        try
        {
            // For AOT, we need to generate console app code
            var effectiveOptions = options;
            if (options.Target == CompilationTarget.NativeAot)
            {
                effectiveOptions = options with { Target = CompilationTarget.ConsoleApp };
            }
            
            // Generate C# code from Lua AST
            var (csharpCode, codeGenDiagnostics) = GenerateCSharpCode(ast, effectiveOptions);
            
            // Collect any diagnostics from code generation
            foreach (var diag in codeGenDiagnostics.GetDiagnostics())
            {
                diagnostics.Report(diag);
            }
            
            // Check for compilation errors
            var errors = diagnostics.GetDiagnostics()
                .Where(d => d.Severity == ErrorSeverity.Error)
                .ToArray();
            
            if (errors.Length > 0)
            {
                return new CompilationResult(
                    Success: false,
                    Errors: errors.Select(e => e.Message),
                    Warnings: diagnostics.GetDiagnostics()
                        .Where(d => d.Severity == ErrorSeverity.Warning)
                        .Select(d => d.Message)
                );
            }
            
            // Debug: Write generated C# code to file for inspection
            if (options.IncludeDebugInfo)
            {
                var debugFile = Path.ChangeExtension(options.OutputPath, ".cs");
                File.WriteAllText(debugFile, csharpCode);
            }
            
            // Handle special compilation targets
            if (options.Target == CompilationTarget.NativeAot)
            {
                return CompileAot(csharpCode, options, diagnostics);
            }
            else if (options.Target == CompilationTarget.Lambda)
            {
                return CompileLambda(csharpCode, ast, options, diagnostics);
            }
            else if (options.Target == CompilationTarget.Expression)
            {
                return CompileExpression(ast, options, diagnostics);
            }
            
            // Compile using Roslyn for regular targets
            return CompileWithRoslyn(csharpCode, options, diagnostics);
        }
        catch (Exception ex)
        {
            return new CompilationResult(
                Success: false,
                Errors: [$"Compilation failed: {ex.Message}"],
                Warnings: diagnostics.GetDiagnostics()
                    .Where(d => d.Severity == ErrorSeverity.Warning)
                    .Select(d => d.Message)
            );
        }
    }

    private (string code, IDiagnosticCollector diagnostics) GenerateCSharpCode(IList<Statement> ast, CompilerOptions options)
    {
        // Create a diagnostic collector for code generation
        var codeGenDiagnostics = new DiagnosticCollector();
        
        // Use the new Roslyn-based code generator
        var generator = new RoslynCodeGenerator(codeGenDiagnostics);
        var syntaxTree = generator.Generate(ast, options);
        
        // Convert syntax tree to string
        return (syntaxTree.ToFullString(), codeGenDiagnostics);
    }

    private CompilationResult CompileWithRoslyn(string csharpCode, CompilerOptions options, IDiagnosticCollector diagnostics)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(csharpCode);
        
        var references = GetReferences(options);
        
        var compilation = CSharpCompilation.Create(
            assemblyName: options.AssemblyName ?? "LuaScript",
            syntaxTrees: [syntaxTree],
            references: references,
            options: new CSharpCompilationOptions(
                outputKind: options.Target == CompilationTarget.ConsoleApp 
                    ? OutputKind.ConsoleApplication 
                    : OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: options.Optimization == OptimizationLevel.Release 
                    ? Microsoft.CodeAnalysis.OptimizationLevel.Release 
                    : Microsoft.CodeAnalysis.OptimizationLevel.Debug
            )
        );

        using var ms = new MemoryStream();
        var result = compilation.Emit(ms);

        if (!result.Success)
        {
            var errors = result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => d.ToString());
                
            return new CompilationResult(
                Success: false,
                Errors: errors,
                Warnings: diagnostics.GetDiagnostics()
                    .Where(d => d.Severity == ErrorSeverity.Warning)
                    .Select(d => d.Message)
            );
        }

        var assembly = ms.ToArray();
        
        // Optionally write to file
        if (!string.IsNullOrEmpty(options.OutputPath))
        {
            File.WriteAllBytes(options.OutputPath, assembly);
            
            // For console apps, also generate runtime config file
            if (options.Target == CompilationTarget.ConsoleApp)
            {
                var runtimeConfigPath = Path.ChangeExtension(options.OutputPath, ".runtimeconfig.json");
                var runtimeConfig = $$"""
                {
                  "runtimeOptions": {
                    "tfm": "net10.0",
                    "framework": {
                      "name": "Microsoft.NETCore.App",
                      "version": "10.0.0-preview.4.25258.110"
                    }
                  }
                }
                """;
                File.WriteAllText(runtimeConfigPath, runtimeConfig);
            }
        }

        return new CompilationResult(
            Success: true,
            Assembly: assembly,
            AssemblyPath: options.OutputPath,
            Warnings: diagnostics.GetDiagnostics()
                .Where(d => d.Severity == ErrorSeverity.Warning)
                .Select(d => d.Message)
        );
    }

    private static IEnumerable<MetadataReference> GetReferences(CompilerOptions options)
    {
        var references = new List<MetadataReference>
        {
            // Core .NET references
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
            
            // Find System.Runtime assembly
            MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "System.Runtime.dll")),
            
            // FLua runtime reference
            MetadataReference.CreateFromFile(typeof(FLua.Runtime.LuaValue).Assembly.Location),
        };

        // Add custom references
        if (options.References != null)
        {
            foreach (var reference in options.References)
            {
                references.Add(MetadataReference.CreateFromFile(reference));
            }
        }

        return references;
    }
    
    private CompilationResult CompileAot(string csharpCode, CompilerOptions options, IDiagnosticCollector diagnostics)
    {
        // Create a temporary directory for the AOT project
        var tempDir = Path.Combine(Path.GetTempPath(), $"flua_aot_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            // Write the C# code to the temp directory
            var csFile = Path.Combine(tempDir, "Program.cs");
            File.WriteAllText(csFile, csharpCode);
            
            // Generate the project file
            var projectFile = Path.Combine(tempDir, "Program.csproj");
            // For AOT, we always want a console app (even for library scripts)
            var isConsoleApp = true;
            AotProjectGenerator.GenerateProjectFile(projectFile, 
                options.AssemblyName ?? "LuaScript", 
                isConsoleApp);
            
            // Generate runtime config template
            var runtimeConfigFile = Path.Combine(tempDir, "runtimeconfig.template.json");
            AotProjectGenerator.GenerateRuntimeConfigTemplate(runtimeConfigFile);
            
            // Copy FLua.Runtime.dll to the temp directory for reference
            var runtimePath = typeof(FLua.Runtime.LuaValue).Assembly.Location;
            var runtimeDestPath = Path.Combine(tempDir, "FLua.Runtime.dll");
            File.Copy(runtimePath, runtimeDestPath);
            
            // Run dotnet publish to create AOT executable
            // Ensure output directory exists
            var outputDir = Path.GetDirectoryName(options.OutputPath);
            if (string.IsNullOrEmpty(outputDir))
            {
                outputDir = Directory.GetCurrentDirectory();
            }
            else if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }
            
            var publishProcess = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"publish -c Release -o \"{outputDir}\"",
                    WorkingDirectory = tempDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            
            var output = new StringBuilder();
            var errors = new StringBuilder();
            
            publishProcess.OutputDataReceived += (_, e) => { if (e.Data != null) output.AppendLine(e.Data); };
            publishProcess.ErrorDataReceived += (_, e) => { if (e.Data != null) errors.AppendLine(e.Data); };
            
            publishProcess.Start();
            publishProcess.BeginOutputReadLine();
            publishProcess.BeginErrorReadLine();
            publishProcess.WaitForExit();
            
            if (publishProcess.ExitCode != 0)
            {
                return new CompilationResult(
                    Success: false,
                    Errors: [$"AOT compilation failed:\n{errors}\n{output}"],
                    Warnings: diagnostics.GetDiagnostics()
                        .Where(d => d.Severity == ErrorSeverity.Warning)
                        .Select(d => d.Message)
                );
            }
            
            // The executable should be in the output directory
            var exeName = Path.GetFileNameWithoutExtension(options.OutputPath);
            if (OperatingSystem.IsWindows())
                exeName += ".exe";
            
            var exePath = Path.Combine(outputDir, exeName);
            
            if (!File.Exists(exePath))
            {
                // Try with assembly name instead
                var assemblyName = options.AssemblyName ?? "LuaScript";
                var altExePath = Path.Combine(outputDir, assemblyName);
                if (OperatingSystem.IsWindows())
                    altExePath += ".exe";
                
                if (File.Exists(altExePath))
                {
                    exePath = altExePath;
                }
                else
                {
                    return new CompilationResult(
                        Success: false,
                        Errors: [$"AOT compilation succeeded but executable not found at: {exePath} or {altExePath}\nOutput:\n{output}"
                        ],
                        Warnings: diagnostics.GetDiagnostics()
                            .Where(d => d.Severity == ErrorSeverity.Warning)
                            .Select(d => d.Message)
                    );
                }
            }
            
            // Move the executable to the desired output path
            var finalPath = options.OutputPath;
            if (File.Exists(finalPath))
                File.Delete(finalPath);
            File.Move(exePath, finalPath);
            
            // Make it executable on Unix systems
            if (!OperatingSystem.IsWindows())
            {
                var chmodProcess = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "chmod",
                        Arguments = $"+x \"{finalPath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                chmodProcess.Start();
                chmodProcess.WaitForExit();
            }
            
            var warnings = new List<string>();
            if (output.Length > 0)
            {
                warnings.Add(output.ToString());
            }
            warnings.AddRange(diagnostics.GetDiagnostics()
                .Where(d => d.Severity == ErrorSeverity.Warning)
                .Select(d => d.Message));
            
            return new CompilationResult(
                Success: true,
                AssemblyPath: finalPath,
                Warnings: warnings.Count > 0 ? warnings : null
            );
        }
        finally
        {
            // Clean up temporary directory
            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }
    
    private CompilationResult CompileLambda(string csharpCode, IList<Statement> ast, CompilerOptions options, IDiagnosticCollector diagnostics)
    {
        try
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(csharpCode);
            var references = GetReferences(options);
            
            var compilation = CSharpCompilation.Create(
                assemblyName: options.AssemblyName ?? "LuaLambda",
                syntaxTrees: [syntaxTree],
                references: references,
                options: new CSharpCompilationOptions(
                    outputKind: OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: options.Optimization == OptimizationLevel.Release 
                        ? Microsoft.CodeAnalysis.OptimizationLevel.Release 
                        : Microsoft.CodeAnalysis.OptimizationLevel.Debug
                )
            );

            using var ms = new MemoryStream();
            var result = compilation.Emit(ms);

            if (!result.Success)
            {
                var errors = result.Diagnostics
                    .Where(d => d.Severity == DiagnosticSeverity.Error)
                    .Select(d => d.ToString());
                    
                return new CompilationResult(
                    Success: false,
                    Errors: errors,
                    Warnings: diagnostics.GetDiagnostics()
                        .Where(d => d.Severity == ErrorSeverity.Warning)
                        .Select(d => d.Message)
                );
            }

            // Load the assembly and create a delegate
            var assembly = Assembly.Load(ms.ToArray());
            var scriptType = assembly.GetType($"{options.AssemblyName ?? "CompiledLuaScript"}.LuaScript");
            
            if (scriptType == null)
            {
                return new CompilationResult(
                    Success: false,
                    Errors: ["Could not find LuaScript type in compiled assembly"]
                );
            }
            
            var executeMethod = scriptType.GetMethod("Execute", BindingFlags.Public | BindingFlags.Static);
            if (executeMethod == null)
            {
                return new CompilationResult(
                    Success: false,
                    Errors: ["Could not find Execute method in LuaScript type"]
                );
            }
            
            // Detect if the Execute method has varargs signature
            var parameters = executeMethod.GetParameters();
            Type delegateType;
            Delegate compiledDelegate;
            
            if (parameters.Length == 2 && 
                parameters[0].ParameterType == typeof(LuaEnvironment) &&
                parameters[1].ParameterType == typeof(LuaValue[]) &&
                parameters[1].GetCustomAttribute<ParamArrayAttribute>() != null)
            {
                // This is a varargs method: Execute(LuaEnvironment env, params LuaValue[] args)
                delegateType = typeof(Func<LuaEnvironment, LuaValue[], LuaValue[]>);
                compiledDelegate = Delegate.CreateDelegate(delegateType, executeMethod);
            }
            else
            {
                // This is a regular method: Execute(LuaEnvironment env)
                delegateType = typeof(Func<LuaEnvironment, LuaValue[]>);
                compiledDelegate = Delegate.CreateDelegate(delegateType, executeMethod);
            }

            return new CompilationResult(
                Success: true,
                Assembly: ms.ToArray(),
                CompiledDelegate: compiledDelegate,
                GeneratedType: scriptType,
                Warnings: diagnostics.GetDiagnostics()
                    .Where(d => d.Severity == ErrorSeverity.Warning)
                    .Select(d => d.Message)
            );
        }
        catch (Exception ex)
        {
            return new CompilationResult(
                Success: false,
                Errors: [$"Lambda compilation failed: {ex.Message}"],
                Warnings: diagnostics.GetDiagnostics()
                    .Where(d => d.Severity == ErrorSeverity.Warning)
                    .Select(d => d.Message)
            );
        }
    }
    
    private CompilationResult CompileExpression(IList<Statement> ast, CompilerOptions options, IDiagnosticCollector diagnostics)
    {
        try
        {
            var generator = new MinimalExpressionTreeGenerator(diagnostics);
            var expressionTree = generator.Generate(ast);
            
            return new CompilationResult(
                Success: true,
                ExpressionTree: expressionTree,
                Warnings: diagnostics.GetDiagnostics()
                    .Where(d => d.Severity == ErrorSeverity.Warning)
                    .Select(d => d.Message)
            );
        }
        catch (Exception ex)
        {
            return new CompilationResult(
                Success: false,
                Errors: [$"Expression tree generation failed: {ex.Message}"],
                Warnings: diagnostics.GetDiagnostics()
                    .Where(d => d.Severity == ErrorSeverity.Warning)
                    .Select(d => d.Message)
            );
        }
    }
}