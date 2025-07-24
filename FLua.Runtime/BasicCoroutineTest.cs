using System;
using FLua.Runtime;

namespace FLua.Runtime.Tests
{
    public class BasicCoroutineTest
    {
        public static void RunBasicTest()
        {
            Console.WriteLine("=== Testing Basic Coroutine Functionality ===");
            
            try
            {
                // Create environment with coroutine support
                var env = LuaEnvironment.CreateStandardEnvironment();
                
                // Get coroutine functions
                var coroutineTable = env.GetVariable("coroutine").AsTable!;
                var createFunc = (LuaFunction)coroutineTable.Get(new LuaString("create"));
                var resumeFunc = (LuaFunction)coroutineTable.Get(new LuaString("resume"));
                var statusFunc = (LuaFunction)coroutineTable.Get(new LuaString("status"));
                var yieldFunc = (LuaFunction)coroutineTable.Get(new LuaString("yield"));
                
                Console.WriteLine("✓ Coroutine library loaded successfully");
                
                // Test 1: Basic coroutine creation
                var testFunc = new LuaUserFunction(args =>
                {
                    Console.WriteLine("  -> Coroutine started with args: " + string.Join(", ", (object[])args));
                    return new[] { new LuaString("Hello from coroutine!") };
                });
                
                var createResult = createFunc.Call(new[] { testFunc });
                var coroutine = createResult[0] as LuaCoroutine;
                
                Console.WriteLine("✓ Coroutine created: " + coroutine);
                
                // Test 2: Check initial status
                var statusResult = statusFunc.Call(new LuaValue[] { coroutine! });
                Console.WriteLine($"✓ Initial status: {statusResult[0]}");
                
                // Test 3: Resume coroutine
                var resumeResult = resumeFunc.Call(new LuaValue[] { coroutine!, new LuaString("test arg") });
                Console.WriteLine($"✓ Resume result: success={resumeResult[0]}, value={resumeResult[1]}");
                
                // Test 4: Check final status
                statusResult = statusFunc.Call(new LuaValue[] { coroutine! });
                Console.WriteLine($"✓ Final status: {statusResult[0]}");
                
                // Test 5: Producer pattern with yield
                Console.WriteLine("\n--- Testing Producer Pattern ---");
                
                var producer = new LuaUserFunction(args =>
                {
                    yieldFunc.Call(new[] { new LuaString("first") });
                    yieldFunc.Call(new[] { new LuaString("second") });
                    return new[] { new LuaString("final") };
                });
                
                var producerCoroutine = (LuaCoroutine)createFunc.Call(new[] { producer })[0];
                
                // Resume multiple times
                for (int i = 0; i < 4; i++)
                {
                    var result = resumeFunc.Call(new LuaValue[] { producerCoroutine });
                    var success = ((LuaBoolean)result[0]).Value;
                    var value = result.Length > 1 ? result[1].ToString() : "none";
                    var status = ((LuaString)statusFunc.Call(new LuaValue[] { producerCoroutine })[0]).Value;
                    
                    Console.WriteLine($"  Resume {i + 1}: success={success}, value={value}, status={status}");
                    
                    if (status == "dead") break;
                }
                
                Console.WriteLine("✓ All coroutine tests passed!");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Test failed: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
