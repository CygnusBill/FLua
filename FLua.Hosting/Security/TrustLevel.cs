namespace FLua.Hosting.Security;

/// <summary>
/// Defines the trust level for hosted Lua code execution.
/// Higher trust levels provide more functionality but less security.
/// </summary>
public enum TrustLevel
{
    /// <summary>
    /// Most restrictive - no IO, OS, or potentially dangerous functions.
    /// Only basic computation and safe string/math operations allowed.
    /// </summary>
    Untrusted = 0,
    
    /// <summary>
    /// Limited functionality - safe computation only.
    /// Includes math, string, table operations but no external access.
    /// </summary>
    Sandbox = 1,
    
    /// <summary>
    /// Some IO allowed - file operations in designated areas only.
    /// Includes coroutines and package system with restrictions.
    /// </summary>
    Restricted = 2,
    
    /// <summary>
    /// Full standard library access except debug functions.
    /// Suitable for trusted scripts with full Lua functionality.
    /// </summary>
    Trusted = 3,
    
    /// <summary>
    /// Complete access including debug functions.
    /// No restrictions - equivalent to standard Lua environment.
    /// </summary>
    FullTrust = 4
}