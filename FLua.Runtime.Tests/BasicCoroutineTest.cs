using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Runtime;

namespace FLua.Runtime.Tests
{
    [TestClass]
    public class BasicCoroutineTest
    {
        [TestMethod]
        public void TestCoroutineYieldPattern()
        {
            // Test coroutine yield pattern
            
                // Create environment with coroutine support
                var env = LuaEnvironment.CreateStandardEnvironment();
                
                // Get coroutine functions
                var coroutineTableValue = env.GetVariable("coroutine");
                if (!coroutineTableValue.IsTable)
                    throw new Exception("coroutine library not found");
                    
                var coroutineTable = coroutineTableValue.AsTable<LuaTable>();
                var createFunc = coroutineTable.Get(LuaValue.String("create")).AsFunction<LuaFunction>();
                var resumeFunc = coroutineTable.Get(LuaValue.String("resume")).AsFunction<LuaFunction>();
                var statusFunc = coroutineTable.Get(LuaValue.String("status")).AsFunction<LuaFunction>();
                var yieldFuncValue = coroutineTable.Get(LuaValue.String("yield"));
                var yieldFunc = yieldFuncValue.AsFunction<LuaFunction>();
                
                Assert.IsNotNull(createFunc, "Create function should exist");
                Assert.IsNotNull(resumeFunc, "Resume function should exist");
                Assert.IsNotNull(statusFunc, "Status function should exist");
                Assert.IsNotNull(yieldFunc, "Yield function should exist");
                
                // Test 1: Basic coroutine creation
                var testFunc = new BuiltinFunction(args =>
                {
                    return new[] { LuaValue.String("Hello from coroutine!") };
                });
                
                var createResult = createFunc.Call(new[] { LuaValue.Function(testFunc) });
                Assert.AreEqual(1, createResult.Length);
                Assert.IsTrue(createResult[0].IsThread, "Result should be a thread");
                var coroutine = createResult[0];
                
                Assert.IsTrue(coroutine.IsThread, "Coroutine should be a thread");
                
                // Test 2: Check initial status
                var statusResult = statusFunc.Call(new[] { createResult[0] });
                Assert.AreEqual("suspended", statusResult[0].AsString(), "Initial status should be suspended");
                
                // Test 3: Resume coroutine
                var resumeResult = resumeFunc.Call(new[] { createResult[0], LuaValue.String("test arg") });
                Assert.IsTrue(resumeResult[0].AsBoolean(), "Resume should succeed");
                Assert.AreEqual("Hello from coroutine!", resumeResult[1].AsString());
                
                // Test 4: Check final status
                statusResult = statusFunc.Call(new[] { createResult[0] });
                Assert.AreEqual("dead", statusResult[0].AsString(), "Final status should be dead");
                
        }

        [TestMethod]
        public void TestCoroutineStatusTransitions()
        {
                var env = LuaEnvironment.CreateStandardEnvironment();
                var coroutineTableValue = env.GetVariable("coroutine");
                var coroutineTable = coroutineTableValue.AsTable<LuaTable>();
                var createFunc = coroutineTable.Get(LuaValue.String("create")).AsFunction<LuaFunction>();
                var resumeFunc = coroutineTable.Get(LuaValue.String("resume")).AsFunction<LuaFunction>();
                var statusFunc = coroutineTable.Get(LuaValue.String("status")).AsFunction<LuaFunction>();
                
                // Simple function that returns immediately
                var simpleFunc = new BuiltinFunction(args =>
                {
                    return new[] { LuaValue.String("completed") };
                });
                
                var createResult = createFunc.Call(new[] { LuaValue.Function(simpleFunc) });
                var coroutine = createResult[0];
                
                // Initial status should be suspended
                var status1 = statusFunc.Call(new[] { coroutine })[0];
                Assert.AreEqual("suspended", status1.AsString());
                
                // Resume - should complete immediately
                var resumeResult = resumeFunc.Call(new[] { coroutine });
                Assert.IsTrue(resumeResult[0].AsBoolean());
                Assert.AreEqual("completed", resumeResult[1].AsString());
                
                // Final status should be dead
                var status2 = statusFunc.Call(new[] { coroutine })[0];
                Assert.AreEqual("dead", status2.AsString());
                
                // Resuming dead coroutine should fail
                var resumeResult2 = resumeFunc.Call(new[] { coroutine });
                Assert.IsFalse(resumeResult2[0].AsBoolean());
        }
    }
}
