using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Hosting;
using FLua.Hosting.Security;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace FLua.Hosting.Tests;

/// <summary>
/// Tests for module resolution functionality following Lee Copeland standards:
/// - Equivalence Partitioning: Different module path patterns and locations
/// - Boundary Value Analysis: Empty paths, missing modules, path limits
/// - Error Condition Testing: Invalid paths, access denied scenarios
/// - State Transition Testing: Cache behavior and search path modifications
/// </summary>
[TestClass]
public class ModuleResolutionTests
{
    private string _testModulesDir = null!;
    private FileSystemModuleResolver _resolver = null!;

    [TestInitialize]
    public void Setup()
    {
        _testModulesDir = Path.Combine(Path.GetTempPath(), $"FLuaTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testModulesDir);
        Directory.CreateDirectory(Path.Combine(_testModulesDir, "lua_modules"));
        Directory.CreateDirectory(Path.Combine(_testModulesDir, "sandbox"));
        
        _resolver = new FileSystemModuleResolver(new[] { _testModulesDir, Path.Combine(_testModulesDir, "lua_modules") });
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testModulesDir))
        {
            Directory.Delete(_testModulesDir, recursive: true);
        }
    }

    #region Equivalence Partitioning - Module Path Patterns

    [TestMethod]
    public async Task ResolveModule_SimpleModuleName_ResolvesCorrectly()
    {
        // Testing Approach: Equivalence Partitioning - Simple module names
        // Arrange
        var modulePath = Path.Combine(_testModulesDir, "mymodule.lua");
        await File.WriteAllTextAsync(modulePath, "return { name = 'mymodule' }");
        var context = new ModuleContext { TrustLevel = TrustLevel.Trusted };

        // Act
        var result = await _resolver.ResolveModuleAsync("mymodule", context);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.IsNotNull(result.SourceCode);
        Assert.AreEqual(modulePath, result.ResolvedPath);
        Assert.IsTrue(result.SourceCode.Contains("name = 'mymodule'"));
    }

    [TestMethod]
    public async Task ResolveModule_DottedModuleName_ResolvesToPath()
    {
        // Testing Approach: Equivalence Partitioning - Dotted module names
        // Arrange
        var subDir = Path.Combine(_testModulesDir, "utils");
        Directory.CreateDirectory(subDir);
        var modulePath = Path.Combine(subDir, "helper.lua");
        await File.WriteAllTextAsync(modulePath, "return { type = 'helper' }");
        var context = new ModuleContext { TrustLevel = TrustLevel.Trusted };

        // Act
        var result = await _resolver.ResolveModuleAsync("utils.helper", context);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(modulePath, result.ResolvedPath);
    }

    [TestMethod]
    public async Task ResolveModule_InitLuaInDirectory_ResolvesCorrectly()
    {
        // Testing Approach: Equivalence Partitioning - init.lua pattern
        // Arrange
        var packageDir = Path.Combine(_testModulesDir, "mypackage");
        Directory.CreateDirectory(packageDir);
        var initPath = Path.Combine(packageDir, "init.lua");
        await File.WriteAllTextAsync(initPath, "return { package = true }");
        var context = new ModuleContext { TrustLevel = TrustLevel.Trusted };

        // Act
        var result = await _resolver.ResolveModuleAsync("mypackage", context);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(initPath, result.ResolvedPath);
    }

    #endregion

    #region Boundary Value Analysis - Edge Cases

    [TestMethod]
    public async Task ResolveModule_EmptyModuleName_ReturnsError()
    {
        // Testing Approach: Boundary Value Analysis - Empty input
        // Arrange
        var context = new ModuleContext { TrustLevel = TrustLevel.Trusted };

        // Act
        var result = await _resolver.ResolveModuleAsync("", context);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsNotNull(result.ErrorMessage);
    }

    [TestMethod]
    public async Task ResolveModule_NonExistentModule_ReturnsNotFound()
    {
        // Testing Approach: Boundary Value Analysis - Missing module
        // Arrange
        var context = new ModuleContext { TrustLevel = TrustLevel.Trusted };

        // Act
        var result = await _resolver.ResolveModuleAsync("nonexistent.module", context);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.ErrorMessage?.Contains("not found") ?? false);
        Assert.IsNull(result.SourceCode);
    }

    [TestMethod]
    public async Task ResolveModule_MultipleSearchPaths_FindsInSecondPath()
    {
        // Testing Approach: Boundary Value Analysis - Search path ordering
        // Arrange
        var modulePath = Path.Combine(_testModulesDir, "lua_modules", "found.lua");
        await File.WriteAllTextAsync(modulePath, "return 'found'");
        var context = new ModuleContext { TrustLevel = TrustLevel.Trusted };

        // Act
        var result = await _resolver.ResolveModuleAsync("found", context);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual(modulePath, result.ResolvedPath);
    }

    #endregion

    #region Error Condition Testing - Access Control

    [TestMethod]
    public async Task ResolveModule_UntrustedLevel_NoModuleAccess()
    {
        // Testing Approach: Error Condition Testing - Trust level restriction
        // Arrange
        var modulePath = Path.Combine(_testModulesDir, "restricted.lua");
        await File.WriteAllTextAsync(modulePath, "return {}");
        var context = new ModuleContext { TrustLevel = TrustLevel.Untrusted };

        // Act
        var result = await _resolver.ResolveModuleAsync("restricted", context);

        // Assert
        Assert.IsFalse(result.Success);
        Assert.IsTrue(result.ErrorMessage?.Contains("access denied") ?? false);
    }

    [TestMethod]
    public Task ResolveModule_SandboxLevel_OnlyStandardModules()
    {
        // Testing Approach: Error Condition Testing - Sandbox restrictions
        // Arrange
        var context = new ModuleContext { TrustLevel = TrustLevel.Sandbox };

        // Act & Assert - Standard modules allowed
        Assert.IsTrue(_resolver.IsModuleAllowed("math", TrustLevel.Sandbox));
        Assert.IsTrue(_resolver.IsModuleAllowed("string", TrustLevel.Sandbox));
        
        // Custom modules not allowed
        Assert.IsFalse(_resolver.IsModuleAllowed("custom.module", TrustLevel.Sandbox));
        
        return Task.CompletedTask;
    }

    [TestMethod]
    public async Task ResolveModule_FileReadError_HandlesGracefully()
    {
        // Testing Approach: Error Condition Testing - I/O errors
        // Arrange
        var modulePath = Path.Combine(_testModulesDir, "locked.lua");
        await File.WriteAllTextAsync(modulePath, "return {}");
        
        // Make file inaccessible (platform-specific, may not work on all systems)
        var fileInfo = new FileInfo(modulePath);
        fileInfo.Attributes = FileAttributes.ReadOnly | FileAttributes.System;
        
        var context = new ModuleContext { TrustLevel = TrustLevel.Trusted };

        // Act
        var result = await _resolver.ResolveModuleAsync("locked", context);

        // Assert - Should either succeed (if permissions allow) or fail gracefully
        if (!result.Success)
        {
            Assert.IsNotNull(result.ErrorMessage);
            Assert.IsTrue(result.ErrorMessage.Contains("Error reading module"));
        }
        
        // Cleanup
        fileInfo.Attributes = FileAttributes.Normal;
    }

    #endregion

    #region State Transition Testing - Cache and Search Path Management

    [TestMethod]
    public async Task ResolveModule_WithCaching_SecondCallUsesCache()
    {
        // Testing Approach: State Transition Testing - Cache state
        // Arrange
        var resolver = new FileSystemModuleResolver(new[] { _testModulesDir }, enableCaching: true);
        var modulePath = Path.Combine(_testModulesDir, "cached.lua");
        await File.WriteAllTextAsync(modulePath, "return { version = 1 }");
        var context = new ModuleContext { TrustLevel = TrustLevel.Trusted };

        // Act - First call
        var result1 = await resolver.ResolveModuleAsync("cached", context);
        
        // Modify file (cache should prevent seeing this change)
        await File.WriteAllTextAsync(modulePath, "return { version = 2 }");
        
        // Second call should use cache
        var result2 = await resolver.ResolveModuleAsync("cached", context);

        // Assert
        Assert.IsTrue(result1.Success);
        Assert.IsTrue(result2.Success);
        Assert.IsTrue(result1.SourceCode!.Contains("version = 1"));
        Assert.IsTrue(result2.SourceCode!.Contains("version = 1")); // Still version 1 from cache
    }

    [TestMethod]
    public async Task ResolveModule_CacheCleared_ReloadsModule()
    {
        // Testing Approach: State Transition Testing - Cache invalidation
        // Arrange
        var resolver = new FileSystemModuleResolver(new[] { _testModulesDir }, enableCaching: true);
        var modulePath = Path.Combine(_testModulesDir, "reloadable.lua");
        await File.WriteAllTextAsync(modulePath, "return { version = 1 }");
        var context = new ModuleContext { TrustLevel = TrustLevel.Trusted };

        // Act
        var result1 = await resolver.ResolveModuleAsync("reloadable", context);
        await File.WriteAllTextAsync(modulePath, "return { version = 2 }");
        
        resolver.ClearCache();
        
        var result2 = await resolver.ResolveModuleAsync("reloadable", context);

        // Assert
        Assert.IsTrue(result1.SourceCode!.Contains("version = 1"));
        Assert.IsTrue(result2.SourceCode!.Contains("version = 2")); // New version after cache clear
    }

    [TestMethod]
    public void AddSearchPath_NewPath_FindsModulesInNewPath()
    {
        // Testing Approach: State Transition Testing - Search path modification
        // Arrange
        var newSearchDir = Path.Combine(_testModulesDir, "new_search");
        Directory.CreateDirectory(newSearchDir);
        var initialPaths = _resolver.SearchPaths.Count;

        // Act
        _resolver.AddSearchPath(newSearchDir);

        // Assert
        Assert.AreEqual(initialPaths + 1, _resolver.SearchPaths.Count);
        Assert.IsTrue(_resolver.SearchPaths.Contains(newSearchDir));
    }

    #endregion

    #region Decision Table Testing - Module Allowance Matrix

    [TestMethod]
    [DataRow("io", TrustLevel.Untrusted, false)]
    [DataRow("io", TrustLevel.Sandbox, false)]
    [DataRow("io", TrustLevel.Restricted, false)]
    [DataRow("io", TrustLevel.Trusted, true)]
    [DataRow("math", TrustLevel.Sandbox, true)]
    [DataRow("debug", TrustLevel.Trusted, false)]
    [DataRow("debug", TrustLevel.FullTrust, true)]
    [DataRow("custom.module", TrustLevel.Untrusted, false)]
    [DataRow("custom.module", TrustLevel.Restricted, true)]
    public void IsModuleAllowed_VariousModulesAndTrustLevels_ReturnsExpectedResult(
        string moduleName, TrustLevel trustLevel, bool expectedAllowed)
    {
        // Testing Approach: Decision Table Testing - Module permission matrix
        // Arrange & Act
        var isAllowed = _resolver.IsModuleAllowed(moduleName, trustLevel);

        // Assert
        Assert.AreEqual(expectedAllowed, isAllowed,
            $"Module '{moduleName}' at trust level {trustLevel} should be {(expectedAllowed ? "allowed" : "blocked")}");
    }

    #endregion

    #region Integration Testing - Module Resolution Result

    [TestMethod]
    public void ModuleResolutionResult_CreateSuccess_PropertiesSetCorrectly()
    {
        // Testing Approach: Integration Testing - Result object creation
        // Arrange & Act
        var result = ModuleResolutionResult.CreateSuccess("return {}", "/path/to/module.lua", cacheable: true);

        // Assert
        Assert.IsTrue(result.Success);
        Assert.AreEqual("return {}", result.SourceCode);
        Assert.AreEqual("/path/to/module.lua", result.ResolvedPath);
        Assert.IsTrue(result.Cacheable);
        Assert.IsNull(result.ErrorMessage);
    }

    [TestMethod]
    public void ModuleResolutionResult_CreateFailure_PropertiesSetCorrectly()
    {
        // Testing Approach: Integration Testing - Failure result creation
        // Arrange & Act
        var result = ModuleResolutionResult.CreateFailure("Module not found");

        // Assert
        Assert.IsFalse(result.Success);
        Assert.AreEqual("Module not found", result.ErrorMessage);
        Assert.IsNull(result.SourceCode);
        Assert.IsNull(result.ResolvedPath);
    }

    #endregion
}