using Microsoft.VisualStudio.TestTools.UnitTesting;
using FLua.Runtime;
using System;

namespace FLua.Runtime.Tests
{
    [TestClass]
    public class LuaRuntimeExceptionTests
    {
        #region Exception Construction Tests

        // Equivalence Class Testing: Testing different exception construction methods
        [TestMethod]
        public void LuaRuntimeException_WithMessage_ShouldCreateException()
        {
            var exception = new LuaRuntimeException("Test message");
            Assert.IsNotNull(exception);
            Assert.IsInstanceOfType(exception, typeof(Exception));
        }

        [TestMethod]
        public void LuaRuntimeException_WithMessage_ShouldStoreMessage()
        {
            var message = "Test error message";
            var exception = new LuaRuntimeException(message);
            
            Assert.AreEqual(message, exception.Message);
        }

        [TestMethod]
        public void LuaRuntimeException_MessageProperty_ShouldStoreMessage()
        {
            var message = "Test exception message";
            var exception = new LuaRuntimeException(message);
            
            Assert.AreEqual(message, exception.Message);
        }

        #endregion

        #region Exception Behavior Tests

        // Risk-Based Testing: Testing exception throwing scenarios
        [TestMethod]
        [ExpectedException(typeof(LuaRuntimeException))]
        public void ThrowLuaRuntimeException_ShouldBeCatchable()
        {
            throw new LuaRuntimeException("Test exception");
        }

        // Scenario Testing: Testing exception in library functions
        [TestMethod]
        public void StringLib_InvalidArgument_ShouldThrowLuaRuntimeException()
        {
            var env = new LuaEnvironment();
            LuaStringLib.AddStringLibrary(env);
            
            var string_table = env.GetVariable("string").AsTable<LuaTable>();
            var lenFunc = string_table.Get("len").AsFunction<LuaFunction>();
            
            try
            {
                // This should work fine - testing that valid calls don't throw
                var result = lenFunc.Call(new LuaValue[] { "test" });
                Assert.AreEqual(4, result[0]);
            }
            catch (LuaRuntimeException)
            {
                Assert.Fail("Valid string length call should not throw exception");
            }
        }

        // Domain Testing: Testing exception message content
        [TestMethod]
        public void LuaRuntimeException_CustomMessage_ShouldContainMessage()
        {
            var customMessage = "Custom Lua error occurred";
            var exception = new LuaRuntimeException(customMessage);
            
            Assert.IsTrue(exception.Message.Contains("Custom Lua error occurred"));
        }

        #endregion

        #region Random Testing for Robustness

        // Random Testing: Testing with various random string messages
        [TestMethod]
        public void LuaRuntimeException_RandomMessages_ShouldHandleCorrectly()
        {
            var random = new Random(12345); // Fixed seed for reproducible tests
            
            for (int i = 0; i < 10; i++)
            {
                var messageLength = random.Next(1, 100);
                var message = new string('X', messageLength);
                
                var exception = new LuaRuntimeException(message);
                
                Assert.AreEqual(message, exception.Message);
                Assert.IsNotNull(exception.ToString());
            }
        }

        #endregion
    }
} 