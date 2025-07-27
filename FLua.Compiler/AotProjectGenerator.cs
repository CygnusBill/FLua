using System;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace FLua.Compiler;

/// <summary>
/// Generates project files for AOT compilation
/// </summary>
public static class AotProjectGenerator
{
    public static void GenerateProjectFile(string projectPath, string assemblyName, bool isConsoleApp)
    {
        var projectFile = new XElement("Project",
            new XAttribute("Sdk", "Microsoft.NET.Sdk"),
            new XElement("PropertyGroup",
                new XElement("OutputType", isConsoleApp ? "Exe" : "Library"),
                new XElement("TargetFramework", "net10.0"),
                new XElement("ImplicitUsings", "enable"),
                new XElement("Nullable", "enable"),
                new XElement("AssemblyName", assemblyName),
                new XElement("PublishAot", "true"),
                new XElement("InvariantGlobalization", "true"),
                new XElement("StripSymbols", "true"),
                new XElement("PublishTrimmed", "true"),
                new XElement("PublishSingleFile", "true"),
                new XElement("SelfContained", "true"),
                new XElement("RuntimeIdentifier", GetRuntimeIdentifier())
            ),
            new XElement("ItemGroup",
                new XElement("Reference",
                    new XAttribute("Include", "FLua.Runtime"),
                    new XElement("HintPath", "FLua.Runtime.dll")
                )
            )
        );
        
        File.WriteAllText(projectPath, projectFile.ToString());
    }
    
    public static void GenerateRuntimeConfigTemplate(string outputPath)
    {
        // Use manual JSON construction to avoid reflection-based serialization
        var json = @"{
  ""configProperties"": {
    ""System.Globalization.Invariant"": true,
    ""System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization"": false
  }
}";
        
        File.WriteAllText(outputPath, json);
    }
    
    private static string GetRuntimeIdentifier()
    {
        if (OperatingSystem.IsWindows())
            return Environment.Is64BitOperatingSystem ? "win-x64" : "win-x86";
        else if (OperatingSystem.IsMacOS())
            return System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64 
                ? "osx-arm64" 
                : "osx-x64";
        else if (OperatingSystem.IsLinux())
            return Environment.Is64BitOperatingSystem ? "linux-x64" : "linux-x86";
        else
            throw new PlatformNotSupportedException("Unsupported platform for AOT compilation");
    }
}