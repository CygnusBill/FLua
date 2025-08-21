using FLua.Runtime;
using FLua.Common;

namespace FLua.Runtime.LibraryTests;

/// <summary>
/// Comprehensive unit tests for LuaEnvironment using Lee Copeland's testing methodology.
/// 
/// Testing Approach Overview:
/// 1. Boundary Value Analysis - Test edge cases for variable names, scoping, and values
/// 2. Equivalence Class Partitioning - Group similar behaviors and test representatives
/// 3. Decision Table Testing - Test different combinations of environment states
/// 4. Error Condition Testing - Verify proper error handling and exceptions
/// 5. Control Flow Testing - Ensure all code paths are exercised
/// 
/// Coverage Areas:
/// - Variable management (get, set, local, global)
/// - Environment hierarchy (parent-child relationships)
/// - Built-in function registration and access
/// - Standard environment creation
/// - Environment disposal and cleanup
/// - Error conditions and edge cases
/// </summary>
[TestClass]
public class LuaEnvironmentTests
{
    private LuaEnvironment _env = null!;
    
    [TestInitialize]
    public void Setup()
    {
        _env = new LuaEnvironment();
    }
    
    [TestCleanup]
    public void Cleanup()
    {
        _env.Dispose();
    }

    #region Constructor Tests
    
    [TestMethod]
    public void Constructor_CreatesEmptyEnvironment()
    {
        // Testing Approach: Basic functionality verification
        var env = new LuaEnvironment();
        
        Assert.IsNotNull(env);
        Assert.IsNotNull(env.Globals);
        
        env.Dispose();
    }
    
    #endregion
    
    #region Variable Management Tests - Core Functionality
    
    [TestMethod]
    public void GetVariable_NonexistentVariable_ReturnsNil()
    {
        // Testing Approach: Equivalence Class - undefined variables
        var result = _env.GetVariable("nonexistent");
        
        Assert.AreEqual(LuaType.Nil, result.Type);
    }
    
    [TestMethod]
    public void SetVariable_GlobalScope_CanRetrieve()
    {
        // Testing Approach: Basic set/get functionality
        _env.SetVariable("test", LuaValue.String("value"));
        var result = _env.GetVariable("test");
        
        Assert.AreEqual("value", result.AsString());
    }
    
    [TestMethod]
    public void SetVariable_OverwriteExisting_UpdatesValue()
    {
        // Testing Approach: State transition testing
        _env.SetVariable("test", LuaValue.Integer(42));
        _env.SetVariable("test", LuaValue.String("updated"));
        
        var result = _env.GetVariable("test");
        Assert.AreEqual("updated", result.AsString());
    }
    
    [TestMethod]
    public void SetVariable_DifferentTypes_AllSupported()
    {
        // Testing Approach: Equivalence Class Partitioning - different value types
        _env.SetVariable("int", LuaValue.Integer(42));
        _env.SetVariable("double", LuaValue.Float(3.14f));
        _env.SetVariable("string", LuaValue.String("test"));
        _env.SetVariable("bool", LuaValue.Boolean(true));
        _env.SetVariable("nil", LuaValue.Nil);
        
        Assert.AreEqual(42L, _env.GetVariable("int").AsInteger());
        Assert.AreEqual(3.14f, _env.GetVariable("double").AsFloat(), 1e-6f);
        Assert.AreEqual("test", _env.GetVariable("string").AsString());
        Assert.AreEqual(true, _env.GetVariable("bool").AsBoolean());
        Assert.AreEqual(LuaType.Nil, _env.GetVariable("nil").Type);
    }
    
    #endregion
    
    #region Local Variable Tests
    
    [TestMethod]
    public void SetLocalVariable_CreatesInCurrentScope()
    {
        // Testing Approach: Scope isolation testing
        _env.SetLocalVariable("local_var", LuaValue.Integer(100));
        
        // Note: HasLocalVariable is private, so we test via GetVariable instead
        Assert.AreEqual(100L, _env.GetVariable("local_var").AsInteger());
        Assert.AreEqual(100L, _env.GetVariable("local_var").AsInteger());
    }
    
    [TestMethod]
    public void SetLocalVariable_WithAttributes_HandlesCorrectly()
    {
        // Testing Approach: Feature completeness - const attributes
        _env.SetLocalVariable("const_var", LuaValue.String("constant"), LuaAttribute.Const);
        
        Assert.AreEqual("constant", _env.GetVariable("const_var").AsString());
        Assert.AreEqual("constant", _env.GetVariable("const_var").AsString());
    }
    
    [TestMethod]
    public void LocalVariable_ExistingLocal_CanRetrieve()
    {
        // Testing Approach: Local variable verification
        _env.SetLocalVariable("exists", LuaValue.Integer(1));
        
        Assert.AreEqual(1L, _env.GetVariable("exists").AsInteger());
    }
    
    [TestMethod]
    public void LocalVariable_NonexistentLocal_ReturnsNil()
    {
        // Testing Approach: Local variable verification - negative case
        Assert.AreEqual(LuaType.Nil, _env.GetVariable("nonexistent").Type);
    }
    
    [TestMethod]
    public void HasVariable_ChecksLocalThenGlobal()
    {
        // Testing Approach: Decision table - local vs global precedence
        _env.SetLocalVariable("var", LuaValue.String("local"));
        _env.Globals.Set("var", LuaValue.String("global"));
        
        Assert.IsTrue(_env.HasVariable("var"));
        // Local should take precedence
        Assert.AreEqual("local", _env.GetVariable("var").AsString());
    }
    
    #endregion
    
    #region Environment Hierarchy Tests
    
    [TestMethod]
    public void CreateChild_CreatesChildEnvironment()
    {
        // Testing Approach: Hierarchical structure verification
        var child = _env.CreateChild();
        
        Assert.IsNotNull(child);
        Assert.AreNotSame(_env, child);
        
        child.Dispose();
    }
    
    [TestMethod]
    public void ChildEnvironment_InheritsParentVariables()
    {
        // Testing Approach: Inheritance behavior verification
        _env.SetVariable("parent_var", LuaValue.Integer(42));
        var child = _env.CreateChild();
        
        // Child should see parent's variable
        Assert.AreEqual(42L, child.GetVariable("parent_var").AsInteger());
        
        child.Dispose();
    }
    
    [TestMethod]
    public void ChildEnvironment_LocalVariablesDontAffectParent()
    {
        // Testing Approach: Scope isolation verification
        var child = _env.CreateChild();
        child.SetLocalVariable("child_var", LuaValue.String("child"));
        
        // Parent should not see child's local variable
        // Note: HasLocalVariable is private, testing via GetVariable
        Assert.AreEqual(LuaType.Nil, _env.GetVariable("child_var").Type);
        
        child.Dispose();
    }
    
    [TestMethod]
    public void ChildEnvironment_CanOverrideParentVariables()
    {
        // Testing Approach: Variable shadowing behavior
        _env.SetLocalVariable("shared", LuaValue.String("parent"));
        var child = _env.CreateChild();
        child.SetLocalVariable("shared", LuaValue.String("child"));
        
        // Each should see its own version
        Assert.AreEqual("parent", _env.GetVariable("shared").AsString());
        Assert.AreEqual("child", child.GetVariable("shared").AsString());
        
        child.Dispose();
    }
    
    #endregion
    
    #region Standard Environment Tests
    
    [TestMethod]
    public void CreateStandardEnvironment_HasBasicFunctions()
    {
        // Testing Approach: Standard library verification
        var stdEnv = LuaEnvironment.CreateStandardEnvironment();
        
        // Test essential built-in functions
        Assert.AreEqual(LuaType.Function, stdEnv.GetVariable("print").Type);
        Assert.AreEqual(LuaType.Function, stdEnv.GetVariable("type").Type);
        Assert.AreEqual(LuaType.Function, stdEnv.GetVariable("tostring").Type);
        Assert.AreEqual(LuaType.Function, stdEnv.GetVariable("tonumber").Type);
        Assert.AreEqual(LuaType.Function, stdEnv.GetVariable("assert").Type);
        
        stdEnv.Dispose();
    }
    
    [TestMethod]
    public void CreateStandardEnvironment_HasTableFunctions()
    {
        // Testing Approach: Standard library completeness
        var stdEnv = LuaEnvironment.CreateStandardEnvironment();
        
        Assert.AreEqual(LuaType.Function, stdEnv.GetVariable("pairs").Type);
        Assert.AreEqual(LuaType.Function, stdEnv.GetVariable("ipairs").Type);
        Assert.AreEqual(LuaType.Function, stdEnv.GetVariable("next").Type);
        
        stdEnv.Dispose();
    }
    
    [TestMethod]
    public void CreateStandardEnvironment_HasRawOperations()
    {
        // Testing Approach: Standard library completeness
        var stdEnv = LuaEnvironment.CreateStandardEnvironment();
        
        Assert.AreEqual(LuaType.Function, stdEnv.GetVariable("rawget").Type);
        Assert.AreEqual(LuaType.Function, stdEnv.GetVariable("rawset").Type);
        Assert.AreEqual(LuaType.Function, stdEnv.GetVariable("rawequal").Type);
        Assert.AreEqual(LuaType.Function, stdEnv.GetVariable("rawlen").Type);
        
        stdEnv.Dispose();
    }
    
    [TestMethod]
    public void CreateStandardEnvironment_HasMetatableFunctions()
    {
        // Testing Approach: Standard library completeness
        var stdEnv = LuaEnvironment.CreateStandardEnvironment();
        
        Assert.AreEqual(LuaType.Function, stdEnv.GetVariable("setmetatable").Type);
        Assert.AreEqual(LuaType.Function, stdEnv.GetVariable("getmetatable").Type);
        
        stdEnv.Dispose();
    }
    
    [TestMethod]
    public void CreateStandardEnvironment_HasStandardLibraries()
    {
        // Testing Approach: Standard library presence verification
        var stdEnv = LuaEnvironment.CreateStandardEnvironment();
        
        // Check for standard library tables
        Assert.AreEqual(LuaType.Table, stdEnv.GetVariable("math").Type);
        Assert.AreEqual(LuaType.Table, stdEnv.GetVariable("string").Type);
        Assert.AreEqual(LuaType.Table, stdEnv.GetVariable("table").Type);
        Assert.AreEqual(LuaType.Table, stdEnv.GetVariable("io").Type);
        Assert.AreEqual(LuaType.Table, stdEnv.GetVariable("os").Type);
        Assert.AreEqual(LuaType.Table, stdEnv.GetVariable("utf8").Type);
        Assert.AreEqual(LuaType.Table, stdEnv.GetVariable("coroutine").Type);
        
        stdEnv.Dispose();
    }
    
    #endregion
    
    #region Built-in Function Behavior Tests
    
    [TestMethod]
    public void Print_Function_IsCallable()
    {
        // Testing Approach: Built-in function verification
        var stdEnv = LuaEnvironment.CreateStandardEnvironment();
        var printFunc = stdEnv.GetVariable("print").AsFunction();
        
        Assert.IsNotNull(printFunc);
        
        // Should not throw when called with various arguments
        var result = printFunc.Call([]);
        Assert.AreEqual(0, result.Length);
        
        result = printFunc.Call([LuaValue.String("test")]);
        Assert.AreEqual(0, result.Length);
        
        result = printFunc.Call([LuaValue.Integer(42), LuaValue.String("test")]);
        Assert.AreEqual(0, result.Length);
        
        stdEnv.Dispose();
    }
    
    [TestMethod]
    public void Type_Function_ReturnsCorrectTypes()
    {
        // Testing Approach: Built-in function correctness
        var stdEnv = LuaEnvironment.CreateStandardEnvironment();
        var typeFunc = stdEnv.GetVariable("type").AsFunction();
        
        var result = typeFunc.Call([LuaValue.Integer(42)]);
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual("number", result[0].AsString());
        
        result = typeFunc.Call([LuaValue.String("test")]);
        Assert.AreEqual("string", result[0].AsString());
        
        result = typeFunc.Call([LuaValue.Boolean(true)]);
        Assert.AreEqual("boolean", result[0].AsString());
        
        result = typeFunc.Call([LuaValue.Nil]);
        Assert.AreEqual("nil", result[0].AsString());
        
        stdEnv.Dispose();
    }
    
    [TestMethod]
    public void Assert_Function_WorksCorrectly()
    {
        // Testing Approach: Built-in function behavior verification
        var stdEnv = LuaEnvironment.CreateStandardEnvironment();
        var assertFunc = stdEnv.GetVariable("assert").AsFunction();
        
        // Assert with true condition should return the value
        var result = assertFunc.Call([LuaValue.Boolean(true), LuaValue.String("message")]);
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual(true, result[0].AsBoolean());
        Assert.AreEqual("message", result[1].AsString());
        
        // Assert with truthy value should return the value
        result = assertFunc.Call([LuaValue.Integer(42)]);
        Assert.AreEqual(1, result.Length);
        Assert.AreEqual(42L, result[0].AsInteger());
        
        stdEnv.Dispose();
    }
    
    [TestMethod]
    public void Assert_Function_ThrowsOnFalse()
    {
        // Testing Approach: Error condition testing
        var stdEnv = LuaEnvironment.CreateStandardEnvironment();
        var assertFunc = stdEnv.GetVariable("assert").AsFunction();
        
        // Assert with false should throw
        Assert.ThrowsException<LuaRuntimeException>(() => 
            assertFunc.Call([LuaValue.Boolean(false)]));
            
        // Assert with nil should throw
        Assert.ThrowsException<LuaRuntimeException>(() => 
            assertFunc.Call([LuaValue.Nil]));
        
        stdEnv.Dispose();
    }
    
    #endregion
    
    #region Error Handling Tests
    
    [TestMethod] 
    public void ProtectedCall_Function_CatchesErrors()
    {
        // Testing Approach: Error handling verification
        var stdEnv = LuaEnvironment.CreateStandardEnvironment();
        var pcallFunc = stdEnv.GetVariable("pcall").AsFunction();
        var errorFunc = stdEnv.GetVariable("error").AsFunction();
        
        // pcall should catch errors and return false + error message
        var result = pcallFunc.Call([errorFunc, LuaValue.String("test error")]);
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual(false, result[0].AsBoolean());
        Assert.IsTrue(result[1].AsString().Contains("test error"));
        
        stdEnv.Dispose();
    }
    
    [TestMethod]
    public void ProtectedCall_Function_ReturnsSuccessResults()
    {
        // Testing Approach: Success path verification
        var stdEnv = LuaEnvironment.CreateStandardEnvironment();
        var pcallFunc = stdEnv.GetVariable("pcall").AsFunction();
        var typeFunc = stdEnv.GetVariable("type").AsFunction();
        
        // pcall should return true + function results on success
        var result = pcallFunc.Call([typeFunc, LuaValue.Integer(42)]);
        Assert.AreEqual(2, result.Length);
        Assert.AreEqual(true, result[0].AsBoolean());
        Assert.AreEqual("number", result[1].AsString());
        
        stdEnv.Dispose();
    }
    
    #endregion
    
    #region Boundary Value Analysis Tests
    
    [TestMethod]
    public void VariableName_EmptyString_HandledCorrectly()
    {
        // Testing Approach: Boundary Value Analysis - edge case variable names
        _env.SetVariable("", LuaValue.String("empty"));
        var result = _env.GetVariable("");
        
        Assert.AreEqual("empty", result.AsString());
    }
    
    [TestMethod]
    public void VariableName_VeryLongString_HandledCorrectly()
    {
        // Testing Approach: Boundary Value Analysis - long variable names
        var longName = new string('a', 1000);
        _env.SetVariable(longName, LuaValue.Integer(999));
        var result = _env.GetVariable(longName);
        
        Assert.AreEqual(999L, result.AsInteger());
    }
    
    [TestMethod]
    public void VariableName_SpecialCharacters_HandledCorrectly()
    {
        // Testing Approach: Equivalence Class - special character names
        var specialNames = new[] { "_test", "__test", "test123", "test_123", "тест", "🎯" };
        
        for (int i = 0; i < specialNames.Length; i++)
        {
            _env.SetVariable(specialNames[i], LuaValue.Integer(i));
            var result = _env.GetVariable(specialNames[i]);
            Assert.AreEqual((long)i, result.AsInteger(), $"Failed for variable name: {specialNames[i]}");
        }
    }
    
    #endregion
    
    #region Environment Lifecycle Tests
    
    [TestMethod]
    public void Dispose_ReleasesResources()
    {
        // Testing Approach: Resource management verification
        var env = new LuaEnvironment();
        env.SetVariable("test", LuaValue.String("value"));
        
        // Should not throw
        env.Dispose();
        
        // Multiple dispose calls should be safe
        env.Dispose();
    }
    
    [TestMethod]
    public void Dispose_WithChildEnvironments_HandledCorrectly()
    {
        // Testing Approach: Cascading disposal behavior
        var parent = new LuaEnvironment();
        var child = parent.CreateChild();
        
        // Disposing parent should not affect child access to its own variables
        child.SetLocalVariable("child_var", LuaValue.String("test"));
        parent.Dispose();
        
        // Child should still be functional for its own variables
        Assert.AreEqual("test", child.GetVariable("child_var").AsString());
        
        child.Dispose();
    }
    
    #endregion
    
    #region Performance and Stress Tests
    
    [TestMethod]
    public void ManyVariables_Performance_Acceptable()
    {
        // Testing Approach: Performance verification
        const int numVariables = 1000;
        
        // Set many variables
        for (int i = 0; i < numVariables; i++)
        {
            _env.SetVariable($"var_{i}", LuaValue.Integer(i));
        }
        
        // Retrieve them all
        for (int i = 0; i < numVariables; i++)
        {
            var result = _env.GetVariable($"var_{i}");
            Assert.AreEqual((long)i, result.AsInteger());
        }
    }
    
    [TestMethod]
    public void DeepEnvironmentHierarchy_HandledCorrectly()
    {
        // Testing Approach: Deep nesting behavior
        const int depth = 50;
        var environments = new LuaEnvironment[depth];
        
        environments[0] = new LuaEnvironment();
        environments[0].SetVariable("root", LuaValue.Integer(0));
        
        // Create deep hierarchy
        for (int i = 1; i < depth; i++)
        {
            environments[i] = environments[i - 1].CreateChild();
            environments[i].SetLocalVariable($"level_{i}", LuaValue.Integer(i));
        }
        
        // Deepest environment should see root variable
        var deepest = environments[depth - 1];
        Assert.AreEqual(0L, deepest.GetVariable("root").AsInteger());
        
        // And its own local variables
        Assert.AreEqual((long)(depth - 1), deepest.GetVariable($"level_{depth - 1}").AsInteger());
        
        // Cleanup
        for (int i = depth - 1; i >= 0; i--)
        {
            environments[i].Dispose();
        }
    }
    
    #endregion
}