using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Runtime;
using System;

namespace FLua.Runtime.Tests
{
    [TestClass]
    public class WorkingCoroutineTests
    {
        [TestMethod]
        public void TestBasicCoroutineCreation()
        {
            var env = LuaEnvironment.CreateStandardEnvironment();
            var coroutineTable = env.GetVariable("coroutine").AsTable<LuaTable>();
            var createFunc = coroutineTable.Get("create").AsFunction<LuaFunction>();
            
            var testFunc = new BuiltinFunction(args =>
            {
                return new[] { LuaValue.String("Hello from coroutine!") };
            });
            
            var result = createFunc.Call(testFunc);
            
            Assert.AreEqual(1, result.Length);
            Assert.IsTrue(result[0].IsThread);
        }

        [TestMethod]
        public void TestCoroutineResume()
        {
            var env = LuaEnvironment.CreateStandardEnvironment();
            var coroutineTable = env.GetVariable("coroutine").AsTable<LuaTable>();
            var createFunc = coroutineTable.Get("create").AsFunction<LuaFunction>();
            var resumeFunc = coroutineTable.Get("resume").AsFunction<LuaFunction>();
            
            var testFunc = new BuiltinFunction(args =>
            {
                return new[] { LuaValue.String("Result: "), args.Length > 0 ? args[0] : LuaValue.String("no args") };
            });
            
            var createResult = createFunc.Call(testFunc);
            var coroutine = createResult[0];
            
            var resumeResult = resumeFunc.Call(coroutine, "test arg");
            
            Assert.IsTrue(resumeResult.Length >= 3);
            Assert.IsTrue(resumeResult[0].AsBoolean()); // Success
            Assert.AreEqual("Result: ", resumeResult[1].AsString());
            Assert.AreEqual("test arg", resumeResult[2].AsString());
        }

        [TestMethod]
        public void TestCoroutineStatus()
        {
            var env = LuaEnvironment.CreateStandardEnvironment();
            var coroutineTable = env.GetVariable("coroutine").AsTable<LuaTable>();
            var createFunc = coroutineTable.Get("create").AsFunction<LuaFunction>();
            var statusFunc = coroutineTable.Get("status").AsFunction<LuaFunction>();
            
            var testFunc = new BuiltinFunction(args => new[] { LuaValue.String("done") });
            
            var createResult = createFunc.Call(testFunc);
            var coroutine = createResult[0];
            
            var statusResult = statusFunc.Call(coroutine);
            Assert.AreEqual("suspended", statusResult[0].AsString());
        }

        [TestMethod]
        public void TestCoroutineWrap()
        {
            var env = LuaEnvironment.CreateStandardEnvironment();
            var coroutineTable = env.GetVariable("coroutine").AsTable<LuaTable>();
            var wrapFunc = coroutineTable.Get("wrap").AsFunction<LuaFunction>();
            
            var testFunc = new BuiltinFunction(args =>
            {
                return new[] { LuaValue.String("wrapped result") };
            });
            
            var wrapResult = wrapFunc.Call(testFunc);
            Assert.AreEqual(1, wrapResult.Length);
            Assert.IsTrue(wrapResult[0].IsFunction);
            
            var wrappedFunc = wrapResult[0].AsFunction<LuaFunction>();
            var callResult = wrappedFunc.Call(Array.Empty<LuaValue>());
            
            Assert.IsTrue(callResult.Length > 0);
            Assert.AreEqual("wrapped result", callResult[0].AsString());
        }

        [TestMethod]
        public void TestCoroutineRunning()
        {
            var env = LuaEnvironment.CreateStandardEnvironment();
            var coroutineTable = env.GetVariable("coroutine").AsTable<LuaTable>();
            var runningFunc = coroutineTable.Get("running").AsFunction<LuaFunction>();
            
            // Outside of coroutine context
            var runningResult = runningFunc.Call(Array.Empty<LuaValue>());
            Assert.AreEqual(2, runningResult.Length);
            Assert.IsTrue(runningResult[0].IsNil);
            Assert.IsTrue(runningResult[1].AsBoolean()); // Main thread
        }

        [TestMethod]
        public void TestCoroutineIsYieldable()
        {
            var env = LuaEnvironment.CreateStandardEnvironment();
            var coroutineTable = env.GetVariable("coroutine").AsTable<LuaTable>();
            var isYieldableFunc = coroutineTable.Get("isyieldable").AsFunction<LuaFunction>();
            
            // Outside of coroutine context - should not be yieldable
            var result = isYieldableFunc.Call(Array.Empty<LuaValue>());
            Assert.AreEqual(1, result.Length);
            Assert.IsFalse(result[0].AsBoolean());
        }
    }
}
