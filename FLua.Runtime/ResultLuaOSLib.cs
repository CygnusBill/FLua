using System;
using System.Globalization;
using System.IO;
using FLua.Common;

namespace FLua.Runtime
{
    /// <summary>
    /// Result-based version of Lua OS Library implementation
    /// Converts all exception-based error handling to Result pattern
    /// </summary>
    public static class ResultLuaOSLib
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        #region Time Functions
        
        public static Result<LuaValue[]> ClockResult(LuaValue[] args)
        {
            // Return CPU time used by the program in seconds
            // Use Environment.TickCount64 to avoid wrap-around issues
            var ticks = Environment.TickCount64;
            var seconds = ticks / 1000.0;
            return Result<LuaValue[]>.Success([LuaValue.Number(seconds)]);
        }
        
        public static Result<LuaValue[]> TimeResult(LuaValue[] args)
        {
            DateTime dateTime;
            
            if (args.Length == 0)
            {
                // Return current time as seconds since Unix epoch
                dateTime = DateTime.UtcNow;
            }
            else if (args.Length == 1 && args[0].IsTable)
            {
                // Parse time table
                var table = args[0].AsTable<LuaTable>();
                try
                {
                    var yearVal = table.Get(LuaValue.String("year"));
                    var year = yearVal.IsInteger ? (int)yearVal.AsInteger() : DateTime.Now.Year;
                    
                    var monthVal = table.Get(LuaValue.String("month"));
                    var month = monthVal.IsInteger ? (int)monthVal.AsInteger() : 1;
                    
                    var dayVal = table.Get(LuaValue.String("day"));
                    var day = dayVal.IsInteger ? (int)dayVal.AsInteger() : 1;
                    
                    var hourVal = table.Get(LuaValue.String("hour"));
                    var hour = hourVal.IsInteger ? (int)hourVal.AsInteger() : 0;
                    
                    var minVal = table.Get(LuaValue.String("min"));
                    var min = minVal.IsInteger ? (int)minVal.AsInteger() : 0;
                    
                    var secVal = table.Get(LuaValue.String("sec"));
                    var sec = secVal.IsInteger ? (int)secVal.AsInteger() : 0;
                    
                    dateTime = new DateTime(year, month, day, hour, min, sec, DateTimeKind.Utc);
                }
                catch (ArgumentException)
                {
                    return Result<LuaValue[]>.Failure("time result cannot be represented in this installation");
                }
            }
            else
            {
                return Result<LuaValue[]>.Failure("bad argument to 'time' (table expected)");
            }
            
            var unixTime = (long)(dateTime - UnixEpoch).TotalSeconds;
            return Result<LuaValue[]>.Success([LuaValue.Integer(unixTime)]);
        }
        
        public static Result<LuaValue[]> DateResult(LuaValue[] args)
        {
            var format = args.Length > 0 ? args[0].AsString() : "%c";
            long? time = null;
            if (args.Length > 1 && args[1].IsInteger)
                time = args[1].AsInteger();
            
            DateTime dateTime;
            if (time.HasValue)
            {
                dateTime = UnixEpoch.AddSeconds(time.Value);
            }
            else
            {
                dateTime = DateTime.Now;
            }
            
            bool isUtc = format.StartsWith("!");
            if (isUtc)
            {
                format = format.Substring(1);
                dateTime = dateTime.ToUniversalTime();
            }
            
            if (format == "*t")
            {
                // Return a table with date/time components
                var result = new LuaTable();
                result.Set(LuaValue.String("year"), LuaValue.Integer(dateTime.Year));
                result.Set(LuaValue.String("month"), LuaValue.Integer(dateTime.Month));
                result.Set(LuaValue.String("day"), LuaValue.Integer(dateTime.Day));
                result.Set(LuaValue.String("hour"), LuaValue.Integer(dateTime.Hour));
                result.Set(LuaValue.String("min"), LuaValue.Integer(dateTime.Minute));
                result.Set(LuaValue.String("sec"), LuaValue.Integer(dateTime.Second));
                result.Set(LuaValue.String("wday"), LuaValue.Integer(((int)dateTime.DayOfWeek) + 1)); // Lua uses 1-7
                result.Set(LuaValue.String("yday"), LuaValue.Integer(dateTime.DayOfYear));
                result.Set(LuaValue.String("isdst"), LuaValue.Boolean(dateTime.IsDaylightSavingTime()));
                
                return Result<LuaValue[]>.Success([LuaValue.Table(result)]);
            }
            else
            {
                // Format the date string (simplified implementation)
                var formatted = FormatDate(format, dateTime);
                return Result<LuaValue[]>.Success([LuaValue.String(formatted)]);
            }
        }
        
        public static Result<LuaValue[]> DiffTimeResult(LuaValue[] args)
        {
            if (args.Length < 2)
                return Result<LuaValue[]>.Failure("bad argument #2 to 'difftime' (number expected)");
            
            var t2 = args[0];
            var t1 = args[1];
            
            if (!t2.IsNumber || !t1.IsNumber)
                return Result<LuaValue[]>.Failure("bad argument to 'difftime' (number expected)");
            
            var diff = t2.AsDouble() - t1.AsDouble();
            return Result<LuaValue[]>.Success([LuaValue.Number(diff)]);
        }
        
        #endregion
        
        #region Environment Functions
        
        public static Result<LuaValue[]> GetEnvResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'getenv' (string expected)");
            
            try
            {
                // Convert argument to string (Lua behavior)
                var varName = args[0].IsString ? args[0].AsString() : 
                             args[0].IsNumber ? (args[0].IsInteger ? args[0].AsInteger().ToString() : args[0].AsDouble().ToString()) : 
                             args[0].ToString();
                var value = Environment.GetEnvironmentVariable(varName);
                
                return Result<LuaValue[]>.Success(value != null ? [LuaValue.String(value)] : [LuaValue.Nil]);
            }
            catch (Exception ex)
            {
                return Result<LuaValue[]>.Failure($"error accessing environment variable: {ex.Message}");
            }
        }
        
        public static Result<LuaValue[]> SetLocaleResult(LuaValue[] args)
        {
            var locale = args.Length > 0 ? args[0].AsString() : null;
            var category = args.Length > 1 ? args[1].AsString() : "all";
            
            try
            {
                if (string.IsNullOrEmpty(locale))
                {
                    // Return current locale
                    var current = CultureInfo.CurrentCulture.Name;
                    return Result<LuaValue[]>.Success([LuaValue.String(current.Length == 0 ? "C" : current)]);
                }
                
                CultureInfo culture;
                if (locale == "C" || locale == "POSIX")
                {
                    culture = CultureInfo.InvariantCulture;
                }
                else
                {
                    // Validate that this is a real culture, not a synthetic one
                    culture = CultureInfo.GetCultureInfo(locale);
                    
                    // Check if this is a valid culture by seeing if it's in the available cultures
                    // or if it follows proper locale format
                    var availableCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
                    bool isValidCulture = false;
                    
                    foreach (var availableCulture in availableCultures)
                    {
                        if (string.Equals(availableCulture.Name, culture.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            isValidCulture = true;
                            break;
                        }
                    }
                    
                    if (!isValidCulture)
                    {
                        return Result<LuaValue[]>.Success([LuaValue.Nil]);
                    }
                }
                
                // Set the culture based on category
                switch (category.ToLowerInvariant())
                {
                    case "all":
                        CultureInfo.CurrentCulture = culture;
                        CultureInfo.CurrentUICulture = culture;
                        break;
                    case "collate":
                    case "ctype":
                    case "monetary":
                    case "numeric":
                    case "time":
                        // For simplicity, we set the overall culture
                        CultureInfo.CurrentCulture = culture;
                        break;
                    default:
                        return Result<LuaValue[]>.Failure($"bad argument #2 to 'setlocale' (invalid category '{category}')");
                }
                
                return Result<LuaValue[]>.Success([LuaValue.String(culture.Name.Length == 0 ? "C" : culture.Name)]);
            }
            catch (CultureNotFoundException)
            {
                return Result<LuaValue[]>.Success([LuaValue.Nil]);
            }
            catch (ArgumentException)
            {
                return Result<LuaValue[]>.Success([LuaValue.Nil]);
            }
            catch (Exception ex)
            {
                return Result<LuaValue[]>.Failure($"error setting locale: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Process Functions
        
        public static Result<LuaValue[]> ExitResult(LuaValue[] args)
        {
            var exitCode = 0;
            if (args.Length > 0)
            {
                if (args[0].IsBoolean)
                {
                    exitCode = args[0].AsBoolean() ? 0 : 1;
                }
                else if (args[0].IsInteger)
                {
                    exitCode = (int)args[0].AsInteger();
                }
            }
            
            try
            {
                Environment.Exit(exitCode);
                
                // This line will never be reached, but needed for compiler
                return Result<LuaValue[]>.Success([]);
            }
            catch (Exception ex)
            {
                return Result<LuaValue[]>.Failure($"error exiting application: {ex.Message}");
            }
        }
        
        #endregion
        
        #region File System Functions
        
        public static Result<LuaValue[]> RemoveResult(LuaValue[] args)
        {
            if (args.Length == 0)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'remove' (string expected)");
            
            if (!args[0].IsString)
                return Result<LuaValue[]>.Failure("bad argument #1 to 'remove' (string expected)");
            
            var filename = args[0].AsString();
            
            try
            {
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                    return Result<LuaValue[]>.Success([LuaValue.Boolean(true)]);
                }
                else if (Directory.Exists(filename))
                {
                    Directory.Delete(filename);
                    return Result<LuaValue[]>.Success([LuaValue.Boolean(true)]);
                }
                else
                {
                    return Result<LuaValue[]>.Success([LuaValue.Nil, LuaValue.String($"No such file or directory: {filename}")]);
                }
            }
            catch (Exception ex)
            {
                return Result<LuaValue[]>.Success([LuaValue.Nil, LuaValue.String(ex.Message)]);
            }
        }
        
        #endregion
        
        #region System Information
        
        public static Result<LuaValue[]> TmpNameResult(LuaValue[] args)
        {
            try
            {
                var tempFileName = System.IO.Path.GetTempFileName();
                return Result<LuaValue[]>.Success([LuaValue.String(tempFileName)]);
            }
            catch (Exception ex)
            {
                return Result<LuaValue[]>.Failure($"unable to generate a unique filename: {ex.Message}");
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        /// <summary>
        /// Formats a date string using Lua-style format specifiers
        /// This is a simplified implementation of strftime
        /// </summary>
        private static string FormatDate(string format, DateTime dateTime)
        {
            var result = format;
            
            // Basic format specifiers (simplified implementation)
            result = result.Replace("%Y", dateTime.Year.ToString("D4"));
            result = result.Replace("%y", (dateTime.Year % 100).ToString("D2"));
            result = result.Replace("%m", dateTime.Month.ToString("D2"));
            result = result.Replace("%d", dateTime.Day.ToString("D2"));
            result = result.Replace("%H", dateTime.Hour.ToString("D2"));
            result = result.Replace("%M", dateTime.Minute.ToString("D2"));
            result = result.Replace("%S", dateTime.Second.ToString("D2"));
            result = result.Replace("%w", ((int)dateTime.DayOfWeek).ToString());
            result = result.Replace("%j", dateTime.DayOfYear.ToString("D3"));
            
            // Month names
            result = result.Replace("%B", dateTime.ToString("MMMM"));
            result = result.Replace("%b", dateTime.ToString("MMM"));
            
            // Day names
            result = result.Replace("%A", dateTime.ToString("dddd"));
            result = result.Replace("%a", dateTime.ToString("ddd"));
            
            // Common formats
            result = result.Replace("%c", dateTime.ToString("F"));
            result = result.Replace("%x", dateTime.ToString("d"));
            result = result.Replace("%X", dateTime.ToString("T"));
            
            // Escape sequences
            result = result.Replace("%%", "%");
            
            return result;
        }
        
        #endregion
    }
}