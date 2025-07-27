using System.Collections.Generic;
using Microsoft.FSharp.Collections;

namespace FLua.Compiler;

/// <summary>
/// Helper methods for working with F# lists in C#
/// </summary>
internal static class FSharpListHelpers
{
    /// <summary>
    /// Convert an F# list to a C# list
    /// </summary>
    public static List<T> ToList<T>(FSharpList<T> fsharpList)
    {
        var result = new List<T>();
        var current = fsharpList;
        
        while (!current.IsEmpty)
        {
            result.Add(current.Head);
            current = current.Tail;
        }
        
        return result;
    }
}

/// <summary>
/// Extension methods for F# option types
/// </summary>
internal static class FSharpOptionExtensions
{
    /// <summary>
    /// Check if an F# option has a value
    /// </summary>
    public static bool HasValue<T>(this Microsoft.FSharp.Core.FSharpOption<T> option)
    {
        return Microsoft.FSharp.Core.FSharpOption<T>.get_IsSome(option);
    }
    
    /// <summary>
    /// Get the count of items in an F# list option
    /// </summary>
    public static int Count<T>(this FSharpList<T> list)
    {
        int count = 0;
        var current = list;
        
        while (!current.IsEmpty)
        {
            count++;
            current = current.Tail;
        }
        
        return count;
    }
}