using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Hosting;
using FLua.Hosting.Security;
using FLua.Hosting.Environment;
using FLua.Runtime;
using System;
using System.Linq;

namespace FLua.Hosting.Tests;

/// <summary>
/// Tests for trust level security enforcement following Lee Copeland standards:
/// - Equivalence Partitioning: Different trust levels with different capabilities
/// - Boundary Value Analysis: Testing at trust level boundaries
/// - Decision Table Testing: Function/library availability per trust level
/// - Error Condition Testing: Security violations and access denials
/// </summary>
[TestClass]
public class TrustLevelSecurityTests
{
    private StandardSecurityPolicy _securityPolicy = null!;
    private FilteredEnvironmentProvider _environmentProvider = null!;

    [TestInitialize]
    public void Setup()
    {
        _securityPolicy = new StandardSecurityPolicy();
        _environmentProvider = new FilteredEnvironmentProvider(_securityPolicy);
    }

    #region Equivalence Partitioning - Trust Level Capabilities

    [TestMethod]
    public void CreateEnvironment_UntrustedLevel_MinimalFunctionsOnly()
    {
        // Testing Approach: Equivalence Partitioning - Untrusted level capabilities
        // Arrange
        var env = _environmentProvider.CreateEnvironment(TrustLevel.Untrusted);

        // Assert - Basic functions available
        Assert.IsNotNull(env.GetVariable("print"));
        Assert.IsNotNull(env.GetVariable("type"));
        Assert.IsNotNull(env.GetVariable("tostring"));
        Assert.IsNotNull(env.GetVariable("tonumber"));
        
        // Assert - Advanced functions not available
        Assert.IsNull(env.GetVariable("pcall"));
        Assert.IsNull(env.GetVariable("error"));
        Assert.IsNull(env.GetVariable("load"));
        Assert.IsNull(env.GetVariable("require"));
        
        // Assert - No libraries available
        Assert.IsNull(env.GetVariable("io"));
        Assert.IsNull(env.GetVariable("os"));
        Assert.IsNull(env.GetVariable("debug"));
    }

    [TestMethod]
    public void CreateEnvironment_SandboxLevel_SafeFunctionsAndLibraries()
    {
        // Testing Approach: Equivalence Partitioning - Sandbox level capabilities
        // Arrange
        var env = _environmentProvider.CreateEnvironment(TrustLevel.Sandbox);

        // Assert - Error handling functions available
        Assert.IsNotNull(env.GetVariable("pcall"));
        Assert.IsNotNull(env.GetVariable("xpcall"));
        Assert.IsNotNull(env.GetVariable("error"));
        
        // Assert - Table functions available
        Assert.IsNotNull(env.GetVariable("pairs"));
        Assert.IsNotNull(env.GetVariable("ipairs"));
        Assert.IsNotNull(env.GetVariable("next"));
        
        // Assert - Safe libraries available
        Assert.IsNotNull(env.GetVariable("math"));
        Assert.IsNotNull(env.GetVariable("string"));
        Assert.IsNotNull(env.GetVariable("table"));
        Assert.IsNotNull(env.GetVariable("coroutine"));
        
        // Assert - Dangerous functions/libraries not available
        Assert.IsNull(env.GetVariable("load"));
        Assert.IsNull(env.GetVariable("io"));
        Assert.IsNull(env.GetVariable("os"));
    }

    [TestMethod]
    public void CreateEnvironment_TrustedLevel_MostFunctionsAvailable()
    {
        // Testing Approach: Equivalence Partitioning - Trusted level capabilities
        // Arrange
        var env = _environmentProvider.CreateEnvironment(TrustLevel.Trusted);

        // Assert - Dynamic loading available
        Assert.IsNotNull(env.GetVariable("load"));
        Assert.IsNotNull(env.GetVariable("collectgarbage"));
        
        // Assert - I/O and OS libraries available
        Assert.IsNotNull(env.GetVariable("io"));
        Assert.IsNotNull(env.GetVariable("os"));
        
        // Assert - Debug library still not available
        Assert.IsNull(env.GetVariable("debug"));
    }

    [TestMethod]
    public void CreateEnvironment_FullTrustLevel_AllFunctionsAvailable()
    {
        // Testing Approach: Equivalence Partitioning - FullTrust level capabilities
        // Arrange
        var env = _environmentProvider.CreateEnvironment(TrustLevel.FullTrust);

        // Assert - All standard functions available
        Assert.IsNotNull(env.GetVariable("load"));
        Assert.IsNotNull(env.GetVariable("debug"));
        Assert.IsNotNull(env.GetVariable("io"));
        Assert.IsNotNull(env.GetVariable("os"));
    }

    #endregion

    #region Boundary Value Analysis - Trust Level Transitions

    [TestMethod]
    public void SecurityPolicy_UntrustedToSandbox_FunctionAvailabilityChanges()
    {
        // Testing Approach: Boundary Value Analysis - Trust level boundary
        // Arrange & Act
        var untrustedFunctions = _securityPolicy.GetBlockedFunctions(TrustLevel.Untrusted);
        var sandboxFunctions = _securityPolicy.GetBlockedFunctions(TrustLevel.Sandbox);

        // Assert - Sandbox has fewer blocked functions
        Assert.IsTrue(untrustedFunctions.Count() > sandboxFunctions.Count());
        Assert.IsTrue(untrustedFunctions.Contains("pcall"));
        Assert.IsFalse(sandboxFunctions.Contains("pcall"));
    }

    [TestMethod]
    public void SecurityPolicy_SandboxToRestricted_LibraryAvailabilityChanges()
    {
        // Testing Approach: Boundary Value Analysis - Library availability boundary
        // Arrange & Act
        var sandboxLibs = _securityPolicy.GetAllowedLibraries(TrustLevel.Sandbox);
        var restrictedLibs = _securityPolicy.GetAllowedLibraries(TrustLevel.Restricted);

        // Assert - Restricted has more libraries
        Assert.IsTrue(restrictedLibs.Count() > sandboxLibs.Count());
        Assert.IsFalse(sandboxLibs.Contains("os"));
        Assert.IsTrue(restrictedLibs.Contains("os"));
    }

    #endregion

    #region Decision Table Testing - Function/Library Availability Matrix

    [TestMethod]
    [DataRow(TrustLevel.Untrusted, "print", true)]
    [DataRow(TrustLevel.Untrusted, "pcall", false)]
    [DataRow(TrustLevel.Untrusted, "load", false)]
    [DataRow(TrustLevel.Sandbox, "print", true)]
    [DataRow(TrustLevel.Sandbox, "pcall", true)]
    [DataRow(TrustLevel.Sandbox, "load", false)]
    [DataRow(TrustLevel.Trusted, "print", true)]
    [DataRow(TrustLevel.Trusted, "pcall", true)]
    [DataRow(TrustLevel.Trusted, "load", true)]
    [DataRow(TrustLevel.FullTrust, "load", true)]
    public void IsAllowedFunction_VariousTrustLevelsAndFunctions_ReturnsExpectedResult(
        TrustLevel trustLevel, string functionName, bool expectedAllowed)
    {
        // Testing Approach: Decision Table Testing - Function availability matrix
        // Arrange & Act
        var isAllowed = _securityPolicy.IsAllowedFunction(functionName, trustLevel);

        // Assert
        Assert.AreEqual(expectedAllowed, isAllowed,
            $"Function '{functionName}' at trust level {trustLevel} should be {(expectedAllowed ? "allowed" : "blocked")}");
    }

    [TestMethod]
    [DataRow(TrustLevel.Untrusted, "math", true)]
    [DataRow(TrustLevel.Untrusted, "io", false)]
    [DataRow(TrustLevel.Sandbox, "coroutine", true)]
    [DataRow(TrustLevel.Sandbox, "io", false)]
    [DataRow(TrustLevel.Restricted, "os", true)]
    [DataRow(TrustLevel.Restricted, "debug", false)]
    [DataRow(TrustLevel.Trusted, "io", true)]
    [DataRow(TrustLevel.Trusted, "debug", false)]
    [DataRow(TrustLevel.FullTrust, "debug", true)]
    public void IsAllowedLibrary_VariousTrustLevelsAndLibraries_ReturnsExpectedResult(
        TrustLevel trustLevel, string libraryName, bool expectedAllowed)
    {
        // Testing Approach: Decision Table Testing - Library availability matrix
        // Arrange & Act
        var isAllowed = _securityPolicy.IsAllowedLibrary(libraryName, trustLevel);

        // Assert
        Assert.AreEqual(expectedAllowed, isAllowed,
            $"Library '{libraryName}' at trust level {trustLevel} should be {(expectedAllowed ? "allowed" : "blocked")}");
    }

    #endregion

    #region Error Condition Testing - Security Violations

    [TestMethod]
    public void CreateEnvironment_RestrictedLevel_CannotAccessDebugLibrary()
    {
        // Testing Approach: Error Condition Testing - Access violation
        // Arrange
        var env = _environmentProvider.CreateEnvironment(TrustLevel.Restricted);

        // Act & Assert
        var debugLib = env.GetVariable("debug");
        Assert.IsNull(debugLib, "Debug library should not be accessible at Restricted trust level");
    }

    [TestMethod]
    public void CreateEnvironment_UntrustedLevel_CannotUseDangerousFunctions()
    {
        // Testing Approach: Error Condition Testing - Function access denial
        // Arrange
        var env = _environmentProvider.CreateEnvironment(TrustLevel.Untrusted);
        
        // Act & Assert - Verify dangerous functions are blocked
        var dangerousFunctions = new[] { "load", "loadfile", "dofile", "require", "collectgarbage" };
        foreach (var func in dangerousFunctions)
        {
            Assert.IsNull(env.GetVariable(func), 
                $"Dangerous function '{func}' should not be available at Untrusted level");
        }
    }

    #endregion

    #region State Transition Testing - Environment Modification

    [TestMethod]
    public void EnvironmentProvider_AddHostFunctions_FunctionsAccessible()
    {
        // Testing Approach: State Transition Testing - Environment state changes
        // Arrange
        var env = _environmentProvider.CreateEnvironment(TrustLevel.Sandbox);
        var hostFunctions = new Dictionary<string, Func<LuaValue[], LuaValue>>
        {
            ["customFunc"] = args => "custom result"
        };

        // Act
        _environmentProvider.AddHostFunctions(env, hostFunctions);

        // Assert
        var customFunc = env.GetVariable("customFunc");
        Assert.IsNotNull(customFunc, "Host function should be added to environment");
        Assert.IsInstanceOfType(customFunc, typeof(BuiltinFunction));
    }

    [TestMethod]
    public void EnvironmentProvider_InjectHostContext_VariablesAccessible()
    {
        // Testing Approach: State Transition Testing - Context injection
        // Arrange
        var env = _environmentProvider.CreateEnvironment(TrustLevel.Sandbox);
        var hostContext = new Dictionary<string, object>
        {
            ["appName"] = "TestApp",
            ["version"] = 1.0,
            ["isDebug"] = true
        };

        // Act
        _environmentProvider.InjectHostContext(env, hostContext);

        // Assert
        Assert.AreEqual("TestApp", env.GetVariable("appName").AsString());
        Assert.AreEqual(1.0, env.GetVariable("version").AsDouble());
        Assert.AreEqual(true, env.GetVariable("isDebug").AsBoolean());
    }

    #endregion
}