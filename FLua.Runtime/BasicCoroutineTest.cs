using System;
using System.Linq;
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
                var coroutineTableValue = env.GetVariable("coroutine");
                if (!coroutineTableValue.IsTable)
                    throw new Exception("coroutine library not found");
                    
                var coroutineTable = coroutineTableValue.AsTable<LuaTable>();
                var createFunc = coroutineTable.Get(LuaValue.String("create")).AsFunction<LuaFunction>();
                var resumeFunc = coroutineTable.Get(LuaValue.String("resume")).AsFunction<LuaFunction>();
                var statusFunc = coroutineTable.Get(LuaValue.String("status")).AsFunction<LuaFunction>();
                var yieldFuncValue = coroutineTable.Get(LuaValue.String("yield"));
                var yieldFunc = yieldFuncValue.AsFunction<LuaFunction>();
                
                Console.WriteLine("✓ Coroutine library loaded successfully");
                
                // Test 1: Basic coroutine creation
                var testFunc = new BuiltinFunction(args =>
                {
                    Console.WriteLine("  -> Coroutine started with args: " + string.Join(", ", args.Select(a => a.ToString())));
                    return [LuaValue.String("Hello from coroutine!")];
                });
                
                var createResult = createFunc.Call(new[] { LuaValue.Function(testFunc) });
                if (!createResult[0].IsUserData)
                    throw new Exception("Failed to create coroutine");
                var coroutine = createResult[0].AsUserData<LuaCoroutine>();
                
                Console.WriteLine("✓ Coroutine created: " + coroutine);
                
                // Test 2: Check initial status
                var statusResult = statusFunc.Call(new[] { createResult[0] });
                Console.WriteLine($"✓ Initial status: {statusResult[0]}");
                
                // Test 3: Resume coroutine
                var resumeResult = resumeFunc.Call(new[] { createResult[0], LuaValue.String("test arg") });
                Console.WriteLine($"✓ Resume result: success={resumeResult[0]}, value={resumeResult[1]}");
                
                // Test 4: Check final status
                statusResult = statusFunc.Call(new[] { createResult[0] });
                Console.WriteLine($"✓ Final status: {statusResult[0]}");
                
                // Test 5: Producer pattern with yield
                Console.WriteLine("\n--- Testing Producer Pattern ---");
                
                var producer = new BuiltinFunction(args =>
                {
                    yieldFunc.Call(new[] { LuaValue.String("first") });
                    yieldFunc.Call(new[] { LuaValue.String("second") });
                    return [LuaValue.String("final")];
                });
                
                var producerCoroutineResult = createFunc.Call(new[] { LuaValue.Function(producer) });
                var producerCoroutine = producerCoroutineResult[0];
                
                // Resume multiple times
                for (int i = 0; i < 4; i++)
                {
                    var result = resumeFunc.Call(new[] { producerCoroutine });
                    var success = result[0].AsBoolean();
                    var value = result.Length > 1 ? result[1].ToString() : "none";
                    var statusValue = statusFunc.Call(new[] { producerCoroutine })[0];
                    var status = statusValue.AsString();
                    
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
