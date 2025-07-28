using System;
using System.Globalization;
using System.IO;

namespace FLua.Runtime
{
    /// <summary>
    /// Lua OS Library implementation (core operating system functions)
    /// </summary>
    public static class LuaOSLib
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        
        /// <summary>
        /// Adds the os library to the Lua environment
        /// </summary>
        public static void AddOSLibrary(LuaEnvironment env)
        {
            var osTable = new LuaTable();
            
            // Time functions
            osTable.Set(LuaValue.String("clock"), new BuiltinFunction(Clock));
            osTable.Set(LuaValue.String("time"), new BuiltinFunction(Time));
            osTable.Set(LuaValue.String("date"), new BuiltinFunction(Date));
            osTable.Set(LuaValue.String("difftime"), new BuiltinFunction(DiffTime));
            
            // Environment functions
            osTable.Set(LuaValue.String("getenv"), new BuiltinFunction(GetEnv));
            osTable.Set(LuaValue.String("setlocale"), new BuiltinFunction(SetLocale));
            
            // Process functions
            osTable.Set(LuaValue.String("exit"), new BuiltinFunction(Exit));
            
            // File system functions
            osTable.Set(LuaValue.String("remove"), new BuiltinFunction(Remove));
            
            // System information
            osTable.Set(LuaValue.String("tmpname"), new BuiltinFunction(TmpName));
            
            env.SetVariable("os", osTable);
        }
        
        #region Time Functions
        
        private static LuaValue[] Clock(LuaValue[] args)
        {
            // Return CPU time used by the program in seconds
            var ticks = Environment.TickCount;
            var seconds = ticks / 1000.0;
            return [LuaValue.Number(seconds)];
        }
        
        private static LuaValue[] Time(LuaValue[] args)
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
                    throw new LuaRuntimeException("time result cannot be represented in this installation");
                }
            }
            else
            {
                throw new LuaRuntimeException("bad argument to 'time' (table expected)");
            }
            
            var unixTime = (long)(dateTime - UnixEpoch).TotalSeconds;
            return [LuaValue.Integer(unixTime)];
        }
        
        private static LuaValue[] Date(LuaValue[] args)
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
                
                return [LuaValue.Table(result)];
            }
            else
            {
                // Format the date string (simplified implementation)
                var formatted = FormatDate(format, dateTime);
                return [LuaValue.String(formatted)];
            }
        }
        
        private static LuaValue[] DiffTime(LuaValue[] args)
        {
            if (args.Length < 2)
                throw new LuaRuntimeException("bad argument #2 to 'difftime' (number expected)");
            
            var t2 = args[0];
            var t1 = args[1];
            
            if (!t2.IsNumber || !t1.IsNumber)
                throw new LuaRuntimeException("bad argument to 'difftime' (number expected)");
            
            var diff = t2.AsDouble() - t1.AsDouble();
            return [LuaValue.Number(diff)];
        }
        
        #endregion
        
        #region Environment Functions
        
        private static LuaValue[] GetEnv(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'getenv' (string expected)");
            
            var varName = args[0].AsString();
            var value = Environment.GetEnvironmentVariable(varName);
            
            return value != null ? [LuaValue.String(value)] : [LuaValue.Nil];
        }
        
        private static LuaValue[] SetLocale(LuaValue[] args)
        {
            var locale = args.Length > 0 ? args[0].AsString() : null;
            var category = args.Length > 1 ? args[1].AsString() : "all";
            
            try
            {
                if (string.IsNullOrEmpty(locale))
                {
                    // Return current locale
                    var current = CultureInfo.CurrentCulture.Name;
                    return [LuaValue.String(current.Length == 0 ? "C" : current)];
                }
                
                CultureInfo culture;
                if (locale == "C" || locale == "POSIX")
                {
                    culture = CultureInfo.InvariantCulture;
                }
                else
                {
                    culture = CultureInfo.GetCultureInfo(locale);
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
                        throw new LuaRuntimeException($"bad argument #2 to 'setlocale' (invalid category '{category}')");
                }
                
                return [LuaValue.String(culture.Name.Length == 0 ? "C" : culture.Name)];
            }
            catch (CultureNotFoundException)
            {
                return [LuaValue.Nil];
            }
            catch (ArgumentException)
            {
                return [LuaValue.Nil];
            }
        }
        
        #endregion
        
        #region Process Functions
        
        private static LuaValue[] Exit(LuaValue[] args)
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
            
            Environment.Exit(exitCode);
            
            // This line will never be reached, but needed for compiler
            return [];
        }
        
        #endregion
        
        #region File System Functions
        
        private static LuaValue[] Remove(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'remove' (string expected)");
            
            var filename = args[0].AsString();
            
            try
            {
                if (File.Exists(filename))
                {
                    File.Delete(filename);
                    return [LuaValue.Boolean(true)];
                }
                else if (Directory.Exists(filename))
                {
                    Directory.Delete(filename);
                    return [LuaValue.Boolean(true)];
                }
                else
                {
                    return [LuaValue.Nil, LuaValue.String($"No such file or directory: {filename}")];
                }
            }
            catch (Exception ex)
            {
                return [LuaValue.Nil, LuaValue.String(ex.Message)];
            }
        }
        
        #endregion
        
        #region System Information
        
        private static LuaValue[] TmpName(LuaValue[] args)
        {
            try
            {
                var tempFileName = System.IO.Path.GetTempFileName();
                return [LuaValue.String(tempFileName)];
            }
            catch (Exception ex)
            {
                throw new LuaRuntimeException($"unable to generate a unique filename: {ex.Message}");
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