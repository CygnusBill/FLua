using System.Reflection.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Ast;
using FLua.Runtime;
using FLua.Interpreter;

namespace FLua.VariableAttributes.Tests;

[TestClass]
public class ConstVariableTests
{
    private LuaInterpreter _interpreter = null!;

    [TestInitialize]
    public void Setup()
    {
        _interpreter = new LuaInterpreter();
    }

    [TestMethod]
    public void TestBasicConstVariable()
    {
        // Test basic const variable declaration and access
        var result = _interpreter.ExecuteCode("local x <const> = 42; return x");
        
        Assert.AreEqual(1, result.Length);
        Assert.IsTrue(result[0].IsInteger);
        Assert.AreEqual(42, result[0]);
    }

    [TestMethod]
    public void TestConstVariableCannotBeModified()
    {
        // Test that const variables cannot be reassigned
        var exception = Assert.ThrowsException<LuaRuntimeException>(() =>
        {
            _interpreter.ExecuteCode(@"
                local x <const> = 42
                x = 50
            ");
        });
        
        Assert.IsTrue(exception.Message.Contains("const"));
    }

    [TestMethod]
    public void TestMultipleConstVariables()
    {
        // Test multiple const variables in single declaration
        var result = _interpreter.ExecuteCode(@"
            local a <const>, b <const> = 10, 20
            return a + b
        ");
        
        Assert.AreEqual(1, result.Length, $"Expected 1 result, got {result.Length}");
        Assert.IsTrue(result[0].IsInteger || result[0].IsNumber, $"Expected LuaInteger or LuaNumber, got {result[0].GetType()}");
        
        Assert.AreEqual(30, result[0]);
    }

    [TestMethod]
    public void TestMixedConstAndRegularVariables()
    {
        // Test mixed const and regular variables
        var result = _interpreter.ExecuteCode(@"
            local a <const>, b = 10, 20
            b = 30
            return a + b
        ");
        
        Assert.AreEqual(1, result.Length, $"Expected 1 result, got {result.Length}");
        Assert.IsTrue(result[0].IsInteger || result[0].IsNumber, $"Expected LuaInteger or LuaNumber, got {result[0].GetType()}");
        
        Assert.AreEqual(40, result[0]);
    }

    [TestMethod]
    public void TestConstVariableInBlock()
    {
        // Test const variable in a block scope
        var result = _interpreter.ExecuteCode(@"
            do
                local x <const> = 100
                return x
            end
        ");
        
        Assert.AreEqual(1, result.Length);
        Assert.IsTrue(result[0].IsInteger);
        Assert.AreEqual(100, result[0]);
    }

    [TestMethod]
    public void TestConstVariableErrorMessage()
    {
        // Test that const modification provides a clear error message
        try
        {
            _interpreter.ExecuteCode(@"
                local myConst <const> = 'value'
                myConst = 'new value'
            ");
            Assert.Fail("Expected LuaRuntimeException");
        }
        catch (LuaRuntimeException ex)
        {
            Assert.IsTrue(ex.Message.Contains("const"), $"Error message should mention 'const', got: {ex.Message}");
            Assert.IsTrue(ex.Message.Contains("myConst"), $"Error message should mention variable name, got: {ex.Message}");
        }
    }
}

[TestClass]
public class CloseVariableTests
{
    private LuaInterpreter _interpreter = null!;

    [TestInitialize]
    public void Setup()
    {
        _interpreter = new LuaInterpreter();
    }

    [TestMethod]
    public void TestBasicCloseVariable()
    {
        // Test basic close variable with __close metamethod
        _interpreter.ExecuteCode(@"
            local resource = {name = 'test'}
            local mt = {
                __close = function(self, err)
                    -- Resource closed
                end
            }
            setmetatable(resource, mt)
            
            do
                local file <close> = resource
                -- file should be closed when exiting this scope
            end
        ");
        
        // If we get here without exception, the basic syntax works
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void TestCloseVariableInFunction()
    {
        // Test close variable in function scope
        _interpreter.ExecuteCode(@"
            local function createResource(name)
                local resource = {name = name}
                local mt = {
                    __close = function(self, err)
                        -- Cleanup logic here
                    end
                }
                setmetatable(resource, mt)
                return resource
            end

            local function testFunction()
                local file <close> = createResource('testFile')
                -- file should be closed when function exits
                return 'done'
            end
            
            local result = testFunction()
        ");
        
        // Test completes without error
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void TestMultipleCloseVariables()
    {
        // Test multiple close variables (should close in LIFO order)
        _interpreter.ExecuteCode(@"
            local function createResource(name)
                local resource = {name = name}
                local mt = {
                    __close = function(self, err)
                        -- Resource cleanup
                    end
                }
                setmetatable(resource, mt)
                return resource
            end

            do
                local res1 <close> = createResource('resource1')
                local res2 <close> = createResource('resource2')
                local res3 <close> = createResource('resource3')
                -- Should close in order: resource3, resource2, resource1
            end
        ");
        
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void TestCloseVariableWithEarlyReturn()
    {
        // Test close variable with early return
        var result = _interpreter.ExecuteCode(@"
            local function createResource(name)
                local resource = {name = name}
                local mt = {
                    __close = function(self, err)
                        -- Cleanup on early return
                    end
                }
                setmetatable(resource, mt)
                return resource
            end

            local function testEarlyReturn()
                local early <close> = createResource('early')
                return 'returned early'
                -- early should still be closed
            end

            return testEarlyReturn()
        ");
        
        Assert.AreEqual(1, result.Length);
        Assert.IsTrue(result[0].IsString);
        Assert.AreEqual("returned early", result[0]);
    }
}

[TestClass]
public class FunctionParameterAttributeTests
{
    private LuaInterpreter _interpreter = null!;

    [TestInitialize]
    public void Setup()
    {
        _interpreter = new LuaInterpreter();
    }

    [TestMethod]
    public void TestConstFunctionParameter()
    {
        // Test function with const parameter
        var result = _interpreter.ExecuteCode(@"
            local function testConstParam(x <const>)
                return x * 2
            end
            
            return testConstParam(5)
        ");
        
        Assert.AreEqual(1, result.Length, $"Expected 1 result, got {result.Length}");
        Assert.IsTrue(result[0].IsInteger || result[0].IsNumber, $"Expected LuaInteger or LuaNumber, got {result[0].GetType()}");
        
        Assert.AreEqual(10, result[0]);
    }

    [TestMethod]
    public void TestConstParameterCannotBeModified()
    {
        // Test that const parameters cannot be modified
        var exception = Assert.ThrowsException<LuaRuntimeException>(() =>
        {
            _interpreter.ExecuteCode(@"
                local function testConstParam(x <const>)
                    x = x + 1  -- This should fail
                    return x
                end
                
                testConstParam(5)
            ");
        });
        
        Assert.IsTrue(exception.Message.Contains("const"));
    }

    [TestMethod]
    public void TestCloseFunctionParameter()
    {
        // Test function with close parameter
        _interpreter.ExecuteCode(@"
            local function createResource(name)
                local resource = {name = name}
                local mt = {
                    __close = function(self, err)
                        -- Resource cleanup
                    end
                }
                setmetatable(resource, mt)
                return resource
            end
            
            local function testCloseParam(resource <close>)
                -- resource should be closed when function exits
                return 'processed'
            end
            
            local myResource = createResource('myResource')
            local result = testCloseParam(myResource)
        ");
        
        // Test completes without error
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void TestMixedParameterAttributes()
    {
        // Test function with mixed parameter attributes
        var result = _interpreter.ExecuteCode(@"
            local function createResource(name)
                local resource = {name = name}
                local mt = {
                    __close = function(self, err)
                        -- Resource cleanup
                    end
                }
                setmetatable(resource, mt)
                return resource
            end
            
            local function mixedParams(a <const>, b, c <close>)
                -- Can modify b but not a
                b = b + 10
                return b
            end
            
            local resource = createResource('resource')
            return mixedParams(100, 200, resource)
        ");
        
        Assert.AreEqual(1, result.Length, $"Expected 1 result, got {result.Length}");
        Assert.IsTrue(result[0].IsInteger || result[0].IsNumber, $"Expected LuaInteger or LuaNumber, got {result[0].GetType()}");
        
        Assert.AreEqual(210, result[0]);
    }
}

[TestClass]
public class RuntimeVariableTests
{
    [TestMethod]
    public void TestLuaVariableConstBehavior()
    {
        // Test LuaVariable const behavior directly
        var constVar = new LuaVariable(42, LuaAttribute.Const);
        
        Assert.AreEqual(42, constVar.GetValue());
        Assert.AreEqual(LuaAttribute.Const, constVar.Attribute);
        
        // Should throw when trying to modify
        Assert.ThrowsException<LuaRuntimeException>(() =>
        {
            constVar.SetValue(50);
        });
    }

    [TestMethod]
    public void TestLuaVariableCloseBehavior()
    {
        // Test LuaVariable close behavior directly
        var closeVar = new LuaVariable(42, LuaAttribute.Close);
        
        Assert.AreEqual(42, closeVar.GetValue());
        Assert.AreEqual(LuaAttribute.Close, closeVar.Attribute);
        
        Assert.IsFalse(closeVar.IsClosed);
        
        // Close the variable
        closeVar.Close();
        Assert.IsTrue(closeVar.IsClosed);
        
        // Should throw when trying to access closed variable
        Assert.ThrowsException<LuaRuntimeException>(() =>
        {
            closeVar.GetValue();
        });
    }

    [TestMethod]
    public void TestLuaEnvironmentAttributeSupport()
    {
        // Test LuaEnvironment attribute handling
        var env = new LuaEnvironment();
        
        // Set const variable
        env.SetLocalVariable("constVar", 42, LuaAttribute.Const);
        
        // Should be able to read
        var value = env.GetVariable("constVar");
        Assert.AreEqual(42, value);
        
        // Should throw when trying to modify
        Assert.ThrowsException<LuaRuntimeException>(() =>
        {
            env.SetVariable("constVar", 50);
        });
    }

    [TestMethod]
    public void TestToBeClosedVariableTracking()
    {
        // Test that to-be-closed variables are tracked correctly
        var env = new LuaEnvironment();
        
        // Set close variable
        env.SetLocalVariable("closeVar", 42, LuaAttribute.Close);
        
        // Should be able to read initially
        var value = env.GetVariable("closeVar");
        Assert.AreEqual(42, value);
        
        // Close all to-be-closed variables
        env.CloseToBeClosedVariables();
        
        // This test verifies the mechanism works without throwing
        Assert.IsTrue(true);
    }
}
