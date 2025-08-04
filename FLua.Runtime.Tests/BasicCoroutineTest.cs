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
                if (!createResult[0].IsUserData)
                    throw new Exception("Failed to create coroutine");
                var coroutine = createResult[0].AsUserData<LuaCoroutine>();
                
                Assert.IsNotNull(coroutine, "Coroutine should be created");
                
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
        public void TestCoroutineProducerPattern()
        {
                var env = LuaEnvironment.CreateStandardEnvironment();
                var coroutineTableValue = env.GetVariable("coroutine");
                var coroutineTable = coroutineTableValue.AsTable<LuaTable>();
                var createFunc = coroutineTable.Get(LuaValue.String("create")).AsFunction<LuaFunction>();
                var resumeFunc = coroutineTable.Get(LuaValue.String("resume")).AsFunction<LuaFunction>();
                var statusFunc = coroutineTable.Get(LuaValue.String("status")).AsFunction<LuaFunction>();
                var yieldFuncValue = coroutineTable.Get(LuaValue.String("yield"));
                var yieldFunc = yieldFuncValue.AsFunction<LuaFunction>();
                
                var producer = new BuiltinFunction(args =>
                {
                    yieldFunc.Call(new[] { LuaValue.String("first") });
                    yieldFunc.Call(new[] { LuaValue.String("second") });
                    return [LuaValue.String("final")];
                });
                
                var producerCoroutineResult = createFunc.Call(new[] { LuaValue.Function(producer) });
                var producerCoroutine = producerCoroutineResult[0];
                
                // Resume first time - should get "first"
                var result1 = resumeFunc.Call(new[] { producerCoroutine });
                Assert.IsTrue(result1[0].AsBoolean());
                Assert.AreEqual("first", result1[1].AsString());
                
                // Resume second time - should get "second"
                var result2 = resumeFunc.Call(new[] { producerCoroutine });
                Assert.IsTrue(result2[0].AsBoolean());
                Assert.AreEqual("second", result2[1].AsString());
                
                // Resume third time - should get "final"
                var result3 = resumeFunc.Call(new[] { producerCoroutine });
                Assert.IsTrue(result3[0].AsBoolean());
                Assert.AreEqual("final", result3[1].AsString());
                
                // Resume fourth time - coroutine should be dead
                var result4 = resumeFunc.Call(new[] { producerCoroutine });
                Assert.IsFalse(result4[0].AsBoolean());
                
                var finalStatus = statusFunc.Call(new[] { producerCoroutine })[0];
                Assert.AreEqual("dead", finalStatus.AsString());
        }
    }
}
