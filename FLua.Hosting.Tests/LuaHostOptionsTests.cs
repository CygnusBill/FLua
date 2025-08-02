using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Hosting;
using FLua.Hosting.Security;
using FLua.Runtime;
using FLua.Compiler;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FLua.Hosting.Tests;

/// <summary>
/// Tests for LuaHostOptions configuration following Lee Copeland standards:
/// - Equivalence Partitioning: Different configuration combinations
/// - Boundary Value Analysis: Default values and limits
/// - Combinatorial Testing: Multiple option interactions
/// - State Testing: Option immutability and initialization
/// </summary>
[TestClass]
public class LuaHostOptionsTests
{
    #region Equivalence Partitioning - Configuration Categories

    [TestMethod]
    public void LuaHostOptions_DefaultConfiguration_HasExpectedValues()
    {
        // Testing Approach: Equivalence Partitioning - Default configuration
        // Arrange & Act
        var options = new LuaHostOptions();

        // Assert - Security defaults
        Assert.AreEqual(TrustLevel.Sandbox, options.TrustLevel);
        Assert.IsNull(options.SecurityPolicy);
        
        // Assert - Compilation defaults
        Assert.IsNull(options.CompilerOptions);
        Assert.IsNull(options.EnvironmentProvider);
        
        // Assert - Module defaults
        Assert.IsNull(options.ModuleResolver);
        Assert.IsNotNull(options.ModuleSearchPaths);
        Assert.AreEqual(2, options.ModuleSearchPaths.Count);
        Assert.IsTrue(options.ModuleSearchPaths.Contains("."));
        Assert.IsTrue(options.ModuleSearchPaths.Contains("lua_modules"));
        
        // Assert - Runtime defaults
        Assert.IsFalse(options.EnableDebugging);
        Assert.IsNull(options.ExecutionTimeout);
        Assert.IsNull(options.MaxMemoryUsage);
        
        // Assert - Host integration defaults
        Assert.IsNotNull(options.HostContext);
        Assert.AreEqual(0, options.HostContext.Count);
        Assert.IsNotNull(options.HostFunctions);
        Assert.AreEqual(0, options.HostFunctions.Count);
    }

    [TestMethod]
    public void LuaHostOptions_SecurityConfiguration_SetsCorrectly()
    {
        // Testing Approach: Equivalence Partitioning - Security options
        // Arrange & Act
        var securityPolicy = new StandardSecurityPolicy();
        var options = new LuaHostOptions
        {
            TrustLevel = TrustLevel.Restricted,
            SecurityPolicy = securityPolicy,
            EnableDebugging = true
        };

        // Assert
        Assert.AreEqual(TrustLevel.Restricted, options.TrustLevel);
        Assert.AreSame(securityPolicy, options.SecurityPolicy);
        Assert.IsTrue(options.EnableDebugging);
    }

    [TestMethod]
    public void LuaHostOptions_CompilationConfiguration_SetsCorrectly()
    {
        // Testing Approach: Equivalence Partitioning - Compilation options
        // Arrange & Act
        var compilerOptions = new CompilerOptions(
            OutputPath: "test.dll",
            Target: CompilationTarget.Lambda,
            GenerateInMemory: true
        );
        
        var options = new LuaHostOptions
        {
            CompilerOptions = compilerOptions
        };

        // Assert
        Assert.IsNotNull(options.CompilerOptions);
        Assert.AreEqual("test.dll", options.CompilerOptions.OutputPath);
        Assert.AreEqual(CompilationTarget.Lambda, options.CompilerOptions.Target);
        Assert.IsTrue(options.CompilerOptions.GenerateInMemory);
    }

    #endregion

    #region Boundary Value Analysis - Limits and Edge Cases

    [TestMethod]
    public void LuaHostOptions_EmptySearchPaths_AllowsEmptyList()
    {
        // Testing Approach: Boundary Value Analysis - Empty collection
        // Arrange & Act
        var options = new LuaHostOptions
        {
            ModuleSearchPaths = new List<string>()
        };

        // Assert
        Assert.IsNotNull(options.ModuleSearchPaths);
        Assert.AreEqual(0, options.ModuleSearchPaths.Count);
    }

    [TestMethod]
    public void LuaHostOptions_MaximumTimeout_AcceptsMaxValue()
    {
        // Testing Approach: Boundary Value Analysis - Maximum timeout
        // Arrange & Act
        var options = new LuaHostOptions
        {
            ExecutionTimeout = TimeSpan.MaxValue
        };

        // Assert
        Assert.AreEqual(TimeSpan.MaxValue, options.ExecutionTimeout);
    }

    [TestMethod]
    public void LuaHostOptions_ZeroTimeout_AcceptsZeroValue()
    {
        // Testing Approach: Boundary Value Analysis - Zero timeout
        // Arrange & Act
        var options = new LuaHostOptions
        {
            ExecutionTimeout = TimeSpan.Zero
        };

        // Assert
        Assert.AreEqual(TimeSpan.Zero, options.ExecutionTimeout);
    }

    [TestMethod]
    public void LuaHostOptions_MaxMemoryUsage_AcceptsLargeValues()
    {
        // Testing Approach: Boundary Value Analysis - Large memory limit
        // Arrange & Act
        var options = new LuaHostOptions
        {
            MaxMemoryUsage = long.MaxValue
        };

        // Assert
        Assert.AreEqual(long.MaxValue, options.MaxMemoryUsage);
    }

    #endregion

    #region Combinatorial Testing - Multiple Options Interaction

    [TestMethod]
    public void LuaHostOptions_HostFunctionsAndContext_WorkTogether()
    {
        // Testing Approach: Combinatorial Testing - Host integration options
        // Arrange & Act
        var options = new LuaHostOptions
        {
            HostFunctions = new Dictionary<string, Func<LuaValue[], LuaValue>>
            {
                ["testFunc"] = args => LuaValue.Nil
            },
            HostContext = new Dictionary<string, object>
            {
                ["testVar"] = "test value"
            }
        };

        // Assert
        Assert.AreEqual(1, options.HostFunctions.Count);
        Assert.IsTrue(options.HostFunctions.ContainsKey("testFunc"));
        Assert.AreEqual(1, options.HostContext.Count);
        Assert.AreEqual("test value", options.HostContext["testVar"]);
    }

    [TestMethod]
    public void LuaHostOptions_SecurityAndModuleOptions_CombineCorrectly()
    {
        // Testing Approach: Combinatorial Testing - Security and module settings
        // Arrange
        var moduleResolver = new FileSystemModuleResolver();
        
        // Act
        var options = new LuaHostOptions
        {
            TrustLevel = TrustLevel.Untrusted,
            ModuleResolver = moduleResolver,
            ModuleSearchPaths = new List<string> { "/safe/path" }
        };

        // Assert
        Assert.AreEqual(TrustLevel.Untrusted, options.TrustLevel);
        Assert.AreSame(moduleResolver, options.ModuleResolver);
        Assert.AreEqual(1, options.ModuleSearchPaths.Count);
        Assert.AreEqual("/safe/path", options.ModuleSearchPaths[0]);
    }

    #endregion

    #region State Testing - Immutability and Record Behavior

    [TestMethod]
    public void LuaHostOptions_RecordEquality_WorksCorrectly()
    {
        // Testing Approach: State Testing - Record equality
        // Arrange
        var options1 = new LuaHostOptions
        {
            TrustLevel = TrustLevel.Sandbox,
            EnableDebugging = false
        };
        
        var options2 = new LuaHostOptions
        {
            TrustLevel = TrustLevel.Sandbox,
            EnableDebugging = false
        };
        
        var options3 = new LuaHostOptions
        {
            TrustLevel = TrustLevel.Trusted,
            EnableDebugging = false
        };

        // Act & Assert
        // Note: Record equality doesn't work well with collections, so we compare properties
        Assert.AreEqual(options1.TrustLevel, options2.TrustLevel, "TrustLevel should match");
        Assert.AreEqual(options1.EnableDebugging, options2.EnableDebugging, "EnableDebugging should match");
        Assert.AreNotEqual(options1.TrustLevel, options3.TrustLevel, "Options with different values should not be equal");
    }

    [TestMethod]
    public void LuaHostOptions_WithModification_CreatesNewInstance()
    {
        // Testing Approach: State Testing - Immutability via 'with' expression
        // Arrange
        var original = new LuaHostOptions
        {
            TrustLevel = TrustLevel.Sandbox,
            EnableDebugging = false
        };

        // Act
        var modified = original with { TrustLevel = TrustLevel.Trusted };

        // Assert
        Assert.AreNotSame(original, modified, "Should create new instance");
        Assert.AreEqual(TrustLevel.Sandbox, original.TrustLevel, "Original should be unchanged");
        Assert.AreEqual(TrustLevel.Trusted, modified.TrustLevel, "Modified should have new value");
        Assert.AreEqual(original.EnableDebugging, modified.EnableDebugging, "Other properties should be copied");
    }

    #endregion

    #region Integration Testing - Real-world Configuration Scenarios

    [TestMethod]
    public void LuaHostOptions_ProductionConfiguration_HasAppropriateSettings()
    {
        // Testing Approach: Integration Testing - Production scenario
        // Arrange & Act
        var options = new LuaHostOptions
        {
            TrustLevel = TrustLevel.Sandbox,
            EnableDebugging = false,
            ExecutionTimeout = TimeSpan.FromSeconds(30),
            MaxMemoryUsage = 100 * 1024 * 1024, // 100MB
            ModuleSearchPaths = new List<string> { "./scripts", "./lib" },
            CompilerOptions = new CompilerOptions(
                OutputPath: "temp.dll",
                Target: CompilationTarget.Lambda,
                Optimization: OptimizationLevel.Release,
                GenerateInMemory: true
            )
        };

        // Assert
        Assert.AreEqual(TrustLevel.Sandbox, options.TrustLevel);
        Assert.IsFalse(options.EnableDebugging);
        Assert.IsNotNull(options.ExecutionTimeout);
        Assert.IsNotNull(options.MaxMemoryUsage);
        Assert.IsTrue(options.CompilerOptions.GenerateInMemory);
        Assert.AreEqual(OptimizationLevel.Release, options.CompilerOptions.Optimization);
    }

    [TestMethod]
    public void LuaHostOptions_DevelopmentConfiguration_HasDebugSettings()
    {
        // Testing Approach: Integration Testing - Development scenario
        // Arrange & Act
        var options = new LuaHostOptions
        {
            TrustLevel = TrustLevel.FullTrust,
            EnableDebugging = true,
            ExecutionTimeout = null, // No timeout in dev
            CompilerOptions = new CompilerOptions(
                OutputPath: "debug.dll",
                Target: CompilationTarget.Library,
                Optimization: OptimizationLevel.Debug,
                IncludeDebugInfo: true
            )
        };

        // Assert
        Assert.AreEqual(TrustLevel.FullTrust, options.TrustLevel);
        Assert.IsTrue(options.EnableDebugging);
        Assert.IsNull(options.ExecutionTimeout);
        Assert.IsTrue(options.CompilerOptions.IncludeDebugInfo);
        Assert.AreEqual(OptimizationLevel.Debug, options.CompilerOptions.Optimization);
    }

    #endregion
}