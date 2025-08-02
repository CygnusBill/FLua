using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Hosting;
using FLua.Hosting.Security;
using FLua.Runtime;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Linq.Expressions;

namespace FLua.Hosting.Tests;

/// <summary>
/// Integration tests for complete hosting scenarios following Lee Copeland standards:
/// - End-to-End Testing: Complete hosting workflows
/// - Scenario Testing: Real-world usage patterns
/// - Performance Testing: Timeout and resource limit enforcement
/// - Security Testing: Sandboxing and access control verification
/// </summary>
[TestClass]
[Ignore("Tests require LuaHost implementation to be completed")]
public class HostingIntegrationTests
{
    private ILuaHost _host = null!;
    private string _testScriptsDir = null!;

    [TestInitialize]
    public void Setup()
    {
        // TODO: Uncomment when LuaHost is implemented
        // _host = new LuaHost();
        
        _testScriptsDir = Path.Combine(Path.GetTempPath(), $"FLuaHostingTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testScriptsDir);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_testScriptsDir))
        {
            Directory.Delete(_testScriptsDir, recursive: true);
        }
    }

    #region End-to-End Testing - Complete Workflows

    [TestMethod]
    public void Host_ExecuteSimpleScript_ReturnsCorrectResult()
    {
        // Testing Approach: End-to-End Testing - Basic execution
        // Arrange
        string luaCode = "return 2 + 2";

        // Act
        var result = _host.Execute(luaCode);

        // Assert
        Assert.AreEqual(4.0, result.AsDouble());
    }

    [TestMethod]
    public void Host_CompileToFunction_CreatesCallableDelegate()
    {
        // Testing Approach: End-to-End Testing - Function compilation
        // Arrange
        string luaCode = "return math.sqrt(16)";

        // Act
        var func = _host.CompileToFunction<double>(luaCode);
        var result = func();

        // Assert
        Assert.AreEqual(4.0, result);
    }

    [TestMethod]
    public async Task Host_ExecuteAsync_SupportsCancellation()
    {
        // Testing Approach: End-to-End Testing - Async execution with cancellation
        // Arrange
        string luaCode = @"
            local i = 0
            while true do
                i = i + 1
            end
        ";
        
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(100));

        // Act & Assert
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(async () =>
        {
            await _host.ExecuteAsync(luaCode, cancellationToken: cts.Token);
        });
    }

    #endregion

    #region Scenario Testing - Real-world Usage

    [TestMethod]
    public void Host_GameScriptingScenario_WorksCorrectly()
    {
        // Testing Approach: Scenario Testing - Game scripting use case
        // Arrange
        var options = new LuaHostOptions
        {
            TrustLevel = TrustLevel.Sandbox,
            HostContext = new()
            {
                ["player"] = new { Name = "Hero", Health = 100, Level = 5 }
            },
            HostFunctions = new()
            {
                ["getDamage"] = args => 
                {
                    var level = (int)args[0].AsDouble();
                    return level * 10;
                }
            }
        };

        string luaCode = @"
            local damage = getDamage(player.Level)
            return string.format('%s deals %d damage!', player.Name, damage)
        ";

        // Act
        var result = _host.Execute(luaCode, options);

        // Assert
        Assert.AreEqual("Hero deals 50 damage!", result.AsString());
    }

    [TestMethod]
    public void Host_ConfigurationScriptScenario_LoadsSettings()
    {
        // Testing Approach: Scenario Testing - Configuration loading
        // Arrange
        var configScript = @"
            return {
                server = {
                    host = 'localhost',
                    port = 8080
                },
                features = {
                    'logging',
                    'metrics',
                    'api'
                },
                debug = false
            }
        ";

        // Act
        var result = _host.Execute(configScript);
        var config = result.AsTable<LuaTable>();

        // Assert
        Assert.IsNotNull(config);
        var server = config.Get("server").AsTable<LuaTable>();
        Assert.AreEqual("localhost", server?.Get("host").AsString());
        Assert.AreEqual(8080.0, server?.Get("port").AsDouble());
    }

    [TestMethod]
    public void Host_DataTransformationScenario_ProcessesData()
    {
        // Testing Approach: Scenario Testing - ETL/data processing
        // Arrange
        var options = new LuaHostOptions
        {
            HostFunctions = new()
            {
                ["processRecord"] = args =>
                {
                    var record = args[0].AsTable<LuaTable>();
                    // Simulate data processing
                    return record;
                }
            }
        };

        string transformScript = @"
            local data = {
                {id = 1, value = 'A'},
                {id = 2, value = 'B'},
                {id = 3, value = 'C'}
            }
            
            local results = {}
            for i, record in ipairs(data) do
                results[i] = processRecord(record)
            end
            
            return results
        ";

        // Act
        var result = _host.Execute(transformScript, options);

        // Assert
        Assert.IsInstanceOfType(result, typeof(LuaTable));
        var table = result.AsTable<LuaTable>();
        Assert.AreEqual(3.0, table.Get(LuaValue.Integer(0)).AsDouble()); // Length
    }

    #endregion

    #region Security Testing - Sandboxing Verification

    [TestMethod]
    public void Host_UntrustedScript_CannotAccessFileSystem()
    {
        // Testing Approach: Security Testing - File system isolation
        // Arrange
        var options = new LuaHostOptions { TrustLevel = TrustLevel.Untrusted };
        string maliciousScript = "return io.open('/etc/passwd', 'r')";

        // Act & Assert
        Assert.ThrowsException<LuaRuntimeException>(() =>
        {
            _host.Execute(maliciousScript, options);
        });
    }

    [TestMethod]
    public void Host_SandboxedScript_CannotLoadDynamicCode()
    {
        // Testing Approach: Security Testing - Dynamic code loading prevention
        // Arrange
        var options = new LuaHostOptions { TrustLevel = TrustLevel.Sandbox };
        string dynamicScript = "return load('return os.execute(\"ls\")')()";

        // Act
        var result = _host.Execute(dynamicScript, options);

        // Assert - load function should not be available
        Assert.AreEqual(LuaValue.Nil, result);
    }

    [TestMethod]
    public void Host_RestrictedScript_HasLimitedOSAccess()
    {
        // Testing Approach: Security Testing - Restricted OS access
        // Arrange
        var options = new LuaHostOptions { TrustLevel = TrustLevel.Restricted };
        string osScript = @"
            local canGetTime = os.time ~= nil
            local canExecute = os.execute ~= nil
            return canGetTime and not canExecute
        ";

        // Act
        var result = _host.Execute(osScript, options);

        // Assert
        Assert.IsTrue(result.AsBoolean());
    }

    #endregion

    #region Performance Testing - Resource Limits

    [TestMethod]
    public void Host_ExecutionTimeout_EnforcedCorrectly()
    {
        // Testing Approach: Performance Testing - Timeout enforcement
        // Arrange
        var options = new LuaHostOptions
        {
            ExecutionTimeout = TimeSpan.FromMilliseconds(100)
        };
        
        string infiniteLoop = @"
            while true do
                -- Infinite loop
            end
        ";

        // Act & Assert
        Assert.ThrowsException<TimeoutException>(() =>
        {
            _host.Execute(infiniteLoop, options);
        });
    }

    [TestMethod]
    [Ignore("Memory limit enforcement not yet implemented")]
    public void Host_MemoryLimit_EnforcedCorrectly()
    {
        // Testing Approach: Performance Testing - Memory limit enforcement
        // Arrange
        var options = new LuaHostOptions
        {
            MaxMemoryUsage = 1024 * 1024 // 1MB limit
        };
        
        string memoryHog = @"
            local t = {}
            for i = 1, 1000000 do
                t[i] = string.rep('x', 1000)
            end
            return #t
        ";

        // Act & Assert
        Assert.ThrowsException<OutOfMemoryException>(() =>
        {
            _host.Execute(memoryHog, options);
        });
    }

    #endregion

    #region Module System Testing

    [TestMethod]
    public async Task Host_ModuleResolution_LoadsCustomModules()
    {
        // Testing Approach: Integration Testing - Module loading
        // Arrange
        var modulePath = Path.Combine(_testScriptsDir, "mylib.lua");
        await File.WriteAllTextAsync(modulePath, @"
            local M = {}
            function M.greet(name)
                return 'Hello, ' .. name .. '!'
            end
            return M
        ");

        var resolver = new FileSystemModuleResolver(new[] { _testScriptsDir });
        var options = new LuaHostOptions
        {
            ModuleResolver = resolver,
            TrustLevel = TrustLevel.Trusted
        };

        string mainScript = @"
            local mylib = require('mylib')
            return mylib.greet('World')
        ";

        // Act
        var result = _host.Execute(mainScript, options);

        // Assert
        Assert.AreEqual("Hello, World!", result.AsString());
    }

    #endregion

    #region Compilation Target Testing

    [TestMethod]
    public void Host_CompileToExpression_GeneratesExpressionTree()
    {
        // Testing Approach: Integration Testing - Expression tree generation
        // Arrange
        string luaCode = "return 10 + 20";

        // Act
        Expression<Func<double>> expr = _host.CompileToExpression<double>(luaCode);

        // Assert
        Assert.IsNotNull(expr);
        Assert.IsInstanceOfType(expr.Body, typeof(BinaryExpression));
        
        // Compile and execute expression
        var func = expr.Compile();
        Assert.AreEqual(30.0, func());
    }

    [TestMethod]
    public void Host_CompileToAssembly_CreatesLoadableAssembly()
    {
        // Testing Approach: Integration Testing - Assembly generation
        // Arrange
        string luaCode = @"
            function calculate(x, y)
                return x * y + 10
            end
            return calculate(5, 3)
        ";
        
        var options = new LuaHostOptions
        {
            CompilerOptions = new FLua.Compiler.CompilerOptions(
                OutputPath: Path.Combine(_testScriptsDir, "test.dll"),
                Target: FLua.Compiler.CompilationTarget.Library
            )
        };

        // Act
        var assembly = _host.CompileToAssembly(luaCode, options);

        // Assert
        Assert.IsNotNull(assembly);
        Assert.IsTrue(File.Exists(options.CompilerOptions.OutputPath));
    }

    #endregion
}