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
            var coroutineTable = env.GetVariable("coroutine").AsTable!;
            var createFunc = (LuaFunction)coroutineTable.Get(new LuaString("create"));
            
            var testFunc = new LuaUserFunction(args =>
            {
                return new[] { new LuaString("Hello from coroutine!") };
            });
            
            var result = createFunc.Call(new[] { testFunc });
            
            Assert.AreEqual(1, result.Length);
            Assert.IsInstanceOfType(result[0], typeof(LuaCoroutine));
        }

        [TestMethod]
        public void TestCoroutineResume()
        {
            var env = LuaEnvironment.CreateStandardEnvironment();
            var coroutineTable = env.GetVariable("coroutine").AsTable!;
            var createFunc = (LuaFunction)coroutineTable.Get(new LuaString("create"));
            var resumeFunc = (LuaFunction)coroutineTable.Get(new LuaString("resume"));
            
            var testFunc = new LuaUserFunction(args =>
            {
                return new[] { new LuaString("Result: "), args.Length > 0 ? args[0] : new LuaString("no args") };
            });
            
            var createResult = createFunc.Call(new[] { testFunc });
            var coroutine = createResult[0];
            
            var resumeResult = resumeFunc.Call(new LuaValue[] { coroutine, new LuaString("test arg") });
            
            Assert.IsTrue(resumeResult.Length >= 3);
            Assert.IsTrue(((LuaBoolean)resumeResult[0]).Value); // Success
            Assert.AreEqual("Result: ", ((LuaString)resumeResult[1]).Value);
            Assert.AreEqual("test arg", ((LuaString)resumeResult[2]).Value);
        }

        [TestMethod]
        public void TestCoroutineStatus()
        {
            var env = LuaEnvironment.CreateStandardEnvironment();
            var coroutineTable = env.GetVariable("coroutine").AsTable!;
            var createFunc = (LuaFunction)coroutineTable.Get(new LuaString("create"));
            var statusFunc = (LuaFunction)coroutineTable.Get(new LuaString("status"));
            
            var testFunc = new LuaUserFunction(args => new[] { new LuaString("done") });
            
            var createResult = createFunc.Call(new[] { testFunc });
            var coroutine = createResult[0];
            
            var statusResult = statusFunc.Call(new LuaValue[] { coroutine });
            Assert.AreEqual("suspended", ((LuaString)statusResult[0]).Value);
        }

        [TestMethod]
        public void TestCoroutineWrap()
        {
            var env = LuaEnvironment.CreateStandardEnvironment();
            var coroutineTable = env.GetVariable("coroutine").AsTable!;
            var wrapFunc = (LuaFunction)coroutineTable.Get(new LuaString("wrap"));
            
            var testFunc = new LuaUserFunction(args =>
            {
                return new[] { new LuaString("wrapped result") };
            });
            
            var wrapResult = wrapFunc.Call(new[] { testFunc });
            Assert.AreEqual(1, wrapResult.Length);
            Assert.IsInstanceOfType(wrapResult[0], typeof(LuaFunction));
            
            var wrappedFunc = (LuaFunction)wrapResult[0];
            var callResult = wrappedFunc.Call(Array.Empty<LuaValue>());
            
            Assert.IsTrue(callResult.Length > 0);
            Assert.AreEqual("wrapped result", ((LuaString)callResult[0]).Value);
        }

        [TestMethod]
        public void TestCoroutineRunning()
        {
            var env = LuaEnvironment.CreateStandardEnvironment();
            var coroutineTable = env.GetVariable("coroutine").AsTable!;
            var runningFunc = (LuaFunction)coroutineTable.Get(new LuaString("running"));
            
            // Outside of coroutine context
            var runningResult = runningFunc.Call(Array.Empty<LuaValue>());
            Assert.AreEqual(2, runningResult.Length);
            Assert.IsInstanceOfType(runningResult[0], typeof(LuaNil));
            Assert.IsTrue(((LuaBoolean)runningResult[1]).Value); // Main thread
        }

        [TestMethod]
        public void TestCoroutineIsYieldable()
        {
            var env = LuaEnvironment.CreateStandardEnvironment();
            var coroutineTable = env.GetVariable("coroutine").AsTable!;
            var isYieldableFunc = (LuaFunction)coroutineTable.Get(new LuaString("isyieldable"));
            
            // Outside of coroutine context - should not be yieldable
            var result = isYieldableFunc.Call(Array.Empty<LuaValue>());
            Assert.AreEqual(1, result.Length);
            Assert.IsFalse(((LuaBoolean)result[0]).Value);
        }
    }
}
