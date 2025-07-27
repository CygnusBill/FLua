using System;

namespace FLua.Runtime
{
    /// <summary>
    /// Generic runner class for compiled Lua console applications
    /// </summary>
    public static class LuaConsoleRunner
    {
        /// <summary>
        /// Delegate for the Lua script execution function
        /// </summary>
        public delegate LuaValue[] LuaScriptDelegate(LuaEnvironment env);
        
        /// <summary>
        /// Runs a compiled Lua script as a console application
        /// </summary>
        /// <param name="scriptDelegate">The compiled Lua script delegate</param>
        /// <param name="args">Command line arguments (optional)</param>
        /// <returns>Exit code (0 for success, non-zero for errors)</returns>
        public static int Run(LuaScriptDelegate scriptDelegate, string[]? args = null)
        {
            try
            {
                // Create standard Lua environment
                var env = LuaEnvironment.CreateStandardEnvironment();
                
                // Set up arg table if command line arguments provided
                if (args != null && args.Length > 0)
                {
                    var argTable = new LuaTable();
                    for (int i = 0; i < args.Length; i++)
                    {
                        argTable.Set(i, args[i]);
                    }
                    env.SetVariable("arg", argTable);
                }
                
                // Execute the script
                var results = scriptDelegate(env);
                
                // If the script returns a number as the first value, use it as exit code
                if (results != null && results.Length > 0 && results[0].IsInteger)
                {
                    return (int)results[0].AsInteger();
                }
                
                return 0; // Success
            }
            catch (LuaRuntimeException ex)
            {
                Console.Error.WriteLine($"Lua runtime error: {ex.Message}");
                if (!string.IsNullOrEmpty(ex.StackTrace))
                {
                    Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
                }
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.Error.WriteLine($"Inner error: {ex.InnerException.Message}");
                }
                return 2;
            }
        }
    }
}