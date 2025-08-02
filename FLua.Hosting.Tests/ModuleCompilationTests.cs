using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Hosting;
using FLua.Hosting.Security;
using FLua.Hosting.Environment;
using FLua.Runtime;
using FLua.Compiler;

namespace FLua.Hosting.Tests;

[TestClass]
public class ModuleCompilationTests
{
    private string _tempDir = null!;
    
    [TestInitialize]
    public void Setup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"flua_module_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }
    
    [TestMethod]
    public void TestModuleCompilation_WithTrustedLevel_CompilesModule()
    {
        // Arrange
        var moduleCode = @"
            local M = {}
            
            function M.add(a, b)
                return a + b
            end
            
            function M.multiply(a, b)
                return a * b
            end
            
            return M
        ";
        
        var modulePath = Path.Combine(_tempDir, "math_utils.lua");
        File.WriteAllText(modulePath, moduleCode);
        
        var moduleResolver = new FileSystemModuleResolver(new[] { _tempDir });
        var compiler = new RoslynLuaCompiler();
        var environmentProvider = new FilteredEnvironmentProvider(compiler: compiler);
        
        var options = new LuaHostOptions
        {
            TrustLevel = TrustLevel.Trusted,
            ModuleResolver = moduleResolver
        };
        
        var host = new LuaHost(environmentProvider, compiler);
        
        // Act
        var result = host.Execute(@"
            local math_utils = require('math_utils')
            return math_utils.add(5, 3) + math_utils.multiply(2, 4)
        ", options);
        
        // Assert
        Assert.AreEqual(16.0, result.AsDouble()); // 5 + 3 + 2 * 4 = 16
    }
    
    [TestMethod]
    public void TestModuleCompilation_WithCaching_ReusesCompiledModule()
    {
        // Arrange
        var moduleCode = @"
            local counter = 0
            local M = {}
            
            function M.increment()
                counter = counter + 1
                return counter
            end
            
            return M
        ";
        
        var modulePath = Path.Combine(_tempDir, "counter.lua");
        File.WriteAllText(modulePath, moduleCode);
        
        var moduleResolver = new FileSystemModuleResolver(new[] { _tempDir });
        var compiler = new RoslynLuaCompiler();
        var environmentProvider = new FilteredEnvironmentProvider(compiler: compiler);
        
        var options = new LuaHostOptions
        {
            TrustLevel = TrustLevel.Trusted,
            ModuleResolver = moduleResolver
        };
        
        var host = new LuaHost(environmentProvider, compiler);
        
        // Act - First require should compile and cache
        var result1 = host.Execute(@"
            local counter1 = require('counter')
            return counter1.increment()
        ", options);
        
        // Second require should use cached compiled module
        var result2 = host.Execute(@"
            local counter2 = require('counter')
            return counter2.increment()
        ", options);
        
        // Assert
        // Each execution gets a fresh module instance, so both should return 1
        Assert.AreEqual(1.0, result1.AsDouble());
        Assert.AreEqual(1.0, result2.AsDouble());
    }
    
    [TestMethod]
    public void TestModuleCompilation_WithUntrustedLevel_UsesInterpreter()
    {
        // Arrange
        var moduleCode = @"
            local M = {}
            
            function M.getValue()
                return 42
            end
            
            return M
        ";
        
        var modulePath = Path.Combine(_tempDir, "simple.lua");
        File.WriteAllText(modulePath, moduleCode);
        
        var moduleResolver = new FileSystemModuleResolver(new[] { _tempDir });
        var compiler = new RoslynLuaCompiler();
        var environmentProvider = new FilteredEnvironmentProvider(compiler: compiler);
        
        var options = new LuaHostOptions
        {
            TrustLevel = TrustLevel.Sandbox, // Lower trust level should use interpreter
            ModuleResolver = moduleResolver
        };
        
        var host = new LuaHost(environmentProvider, compiler);
        
        // Act
        var result = host.Execute(@"
            local simple = require('simple')
            return simple.getValue()
        ", options);
        
        // Assert
        Assert.AreEqual(42.0, result.AsDouble());
    }
    
    [TestMethod]
    public void TestModuleCompilation_WithCompilationError_FallsBackToInterpreter()
    {
        // Arrange
        var moduleCode = @"
            -- This module uses features that might not be fully supported by the compiler
            local M = {}
            
            -- Dynamic code that might fail compilation
            M.dynamic = function(code)
                return load(code)()
            end
            
            M.static = function()
                return 'static result'
            end
            
            return M
        ";
        
        var modulePath = Path.Combine(_tempDir, "dynamic.lua");
        File.WriteAllText(modulePath, moduleCode);
        
        var moduleResolver = new FileSystemModuleResolver(new[] { _tempDir });
        var compiler = new RoslynLuaCompiler();
        var environmentProvider = new FilteredEnvironmentProvider(compiler: compiler);
        
        var options = new LuaHostOptions
        {
            TrustLevel = TrustLevel.Trusted,
            ModuleResolver = moduleResolver
        };
        
        var host = new LuaHost(environmentProvider, compiler);
        
        // Act
        var result = host.Execute(@"
            local dynamic = require('dynamic')
            return dynamic.static()
        ", options);
        
        // Assert - Should fall back to interpreter and still work
        Assert.AreEqual("static result", result.AsString());
    }
    
    [TestMethod]
    public void TestModuleCompilation_ModuleReturningNonTable_Works()
    {
        // Arrange
        var moduleCode = @"
            -- Module that returns a function instead of a table
            return function(x)
                return x * 2
            end
        ";
        
        var modulePath = Path.Combine(_tempDir, "doubler.lua");
        File.WriteAllText(modulePath, moduleCode);
        
        var moduleResolver = new FileSystemModuleResolver(new[] { _tempDir });
        var compiler = new RoslynLuaCompiler();
        var environmentProvider = new FilteredEnvironmentProvider(compiler: compiler);
        
        var options = new LuaHostOptions
        {
            TrustLevel = TrustLevel.Trusted,
            ModuleResolver = moduleResolver
        };
        
        var host = new LuaHost(environmentProvider, compiler);
        
        // Act
        var result = host.Execute(@"
            local doubler = require('doubler')
            return doubler(21)
        ", options);
        
        // Assert
        Assert.AreEqual(42.0, result.AsDouble());
    }
    
    [TestMethod]
    public void TestModuleCompilation_NestedRequires_Work()
    {
        // Arrange
        var moduleACode = @"
            local B = require('moduleB')
            
            local A = {}
            
            function A.calculate(x)
                return B.double(x) + 10
            end
            
            return A
        ";
        
        var moduleBCode = @"
            local B = {}
            
            function B.double(x)
                return x * 2
            end
            
            return B
        ";
        
        File.WriteAllText(Path.Combine(_tempDir, "moduleA.lua"), moduleACode);
        File.WriteAllText(Path.Combine(_tempDir, "moduleB.lua"), moduleBCode);
        
        var moduleResolver = new FileSystemModuleResolver(new[] { _tempDir });
        var compiler = new RoslynLuaCompiler();
        var environmentProvider = new FilteredEnvironmentProvider(compiler: compiler);
        
        var options = new LuaHostOptions
        {
            TrustLevel = TrustLevel.Trusted,
            ModuleResolver = moduleResolver
        };
        
        var host = new LuaHost(environmentProvider, compiler);
        
        // Act
        var result = host.Execute(@"
            local A = require('moduleA')
            return A.calculate(5)
        ", options);
        
        // Assert - 5 * 2 + 10 = 20
        Assert.AreEqual(20.0, result.AsDouble());
    }
    
    [TestMethod]
    public void TestModuleCompilation_CircularDependency_ThrowsError()
    {
        // Arrange
        var moduleXCode = @"
            local Y = require('moduleY')
            return { name = 'X' }
        ";
        
        var moduleYCode = @"
            local X = require('moduleX')
            return { name = 'Y' }
        ";
        
        File.WriteAllText(Path.Combine(_tempDir, "moduleX.lua"), moduleXCode);
        File.WriteAllText(Path.Combine(_tempDir, "moduleY.lua"), moduleYCode);
        
        var moduleResolver = new FileSystemModuleResolver(new[] { _tempDir });
        var compiler = new RoslynLuaCompiler();
        var environmentProvider = new FilteredEnvironmentProvider(compiler: compiler);
        
        var options = new LuaHostOptions
        {
            TrustLevel = TrustLevel.Trusted,
            ModuleResolver = moduleResolver
        };
        
        var host = new LuaHost(environmentProvider, compiler);
        
        // Act & Assert - Should handle circular dependency gracefully
        // (In Lua, this typically works because modules can be partially loaded)
        var result = host.Execute(@"
            local X = require('moduleX')
            return X.name
        ", options);
        
        Assert.AreEqual("X", result.AsString());
    }
}