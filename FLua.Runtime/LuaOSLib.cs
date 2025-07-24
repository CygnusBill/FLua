using System;
using System.Globalization;

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
            osTable.Set(new LuaString("clock"), new BuiltinFunction(Clock));
            osTable.Set(new LuaString("time"), new BuiltinFunction(Time));
            osTable.Set(new LuaString("date"), new BuiltinFunction(Date));
            osTable.Set(new LuaString("difftime"), new BuiltinFunction(DiffTime));
            
            // Environment functions
            osTable.Set(new LuaString("getenv"), new BuiltinFunction(GetEnv));
            osTable.Set(new LuaString("setlocale"), new BuiltinFunction(SetLocale));
            
            // Process functions
            osTable.Set(new LuaString("exit"), new BuiltinFunction(Exit));
            
            // System information
            osTable.Set(new LuaString("tmpname"), new BuiltinFunction(TmpName));
            
            env.SetVariable("os", osTable);
        }
        
        #region Time Functions
        
        private static LuaValue[] Clock(LuaValue[] args)
        {
            // Return CPU time used by the program in seconds
            var ticks = Environment.TickCount;
            var seconds = ticks / 1000.0;
            return new[] { new LuaNumber(seconds) };
        }
        
        private static LuaValue[] Time(LuaValue[] args)
        {
            DateTime dateTime;
            
            if (args.Length == 0)
            {
                // Return current time as seconds since Unix epoch
                dateTime = DateTime.UtcNow;
            }
            else if (args.Length == 1 && args[0] is LuaTable table)
            {
                // Parse time table
                try
                {
                    var year = (int)(table.Get(new LuaString("year")).AsInteger ?? DateTime.Now.Year);
                    var month = (int)(table.Get(new LuaString("month")).AsInteger ?? 1);
                    var day = (int)(table.Get(new LuaString("day")).AsInteger ?? 1);
                    var hour = (int)(table.Get(new LuaString("hour")).AsInteger ?? 0);
                    var min = (int)(table.Get(new LuaString("min")).AsInteger ?? 0);
                    var sec = (int)(table.Get(new LuaString("sec")).AsInteger ?? 0);
                    
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
            return new[] { new LuaInteger(unixTime) };
        }
        
        private static LuaValue[] Date(LuaValue[] args)
        {
            var format = args.Length > 0 ? args[0].AsString : "%c";
            var time = args.Length > 1 ? args[1].AsInteger : null;
            
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
                result.Set(new LuaString("year"), new LuaInteger(dateTime.Year));
                result.Set(new LuaString("month"), new LuaInteger(dateTime.Month));
                result.Set(new LuaString("day"), new LuaInteger(dateTime.Day));
                result.Set(new LuaString("hour"), new LuaInteger(dateTime.Hour));
                result.Set(new LuaString("min"), new LuaInteger(dateTime.Minute));
                result.Set(new LuaString("sec"), new LuaInteger(dateTime.Second));
                result.Set(new LuaString("wday"), new LuaInteger(((int)dateTime.DayOfWeek) + 1)); // Lua uses 1-7
                result.Set(new LuaString("yday"), new LuaInteger(dateTime.DayOfYear));
                result.Set(new LuaString("isdst"), new LuaBoolean(dateTime.IsDaylightSavingTime()));
                
                return new[] { result };
            }
            else
            {
                // Format the date string (simplified implementation)
                var formatted = FormatDate(format, dateTime);
                return new[] { new LuaString(formatted) };
            }
        }
        
        private static LuaValue[] DiffTime(LuaValue[] args)
        {
            if (args.Length < 2)
                throw new LuaRuntimeException("bad argument #2 to 'difftime' (number expected)");
            
            var t2 = args[0];
            var t1 = args[1];
            
            if (!t2.AsNumber.HasValue || !t1.AsNumber.HasValue)
                throw new LuaRuntimeException("bad argument to 'difftime' (number expected)");
            
            var diff = t2.AsNumber.Value - t1.AsNumber.Value;
            return new[] { new LuaNumber(diff) };
        }
        
        #endregion
        
        #region Environment Functions
        
        private static LuaValue[] GetEnv(LuaValue[] args)
        {
            if (args.Length == 0)
                throw new LuaRuntimeException("bad argument #1 to 'getenv' (string expected)");
            
            var varName = args[0].AsString;
            var value = Environment.GetEnvironmentVariable(varName);
            
            return value != null ? new[] { new LuaString(value) } : new[] { LuaNil.Instance };
        }
        
        private static LuaValue[] SetLocale(LuaValue[] args)
        {
            var locale = args.Length > 0 ? args[0].AsString : null;
            var category = args.Length > 1 ? args[1].AsString : "all";
            
            try
            {
                if (string.IsNullOrEmpty(locale))
                {
                    // Return current locale
                    var current = CultureInfo.CurrentCulture.Name;
                    return new[] { new LuaString(current.Length == 0 ? "C" : current) };
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
                
                return new[] { new LuaString(culture.Name.Length == 0 ? "C" : culture.Name) };
            }
            catch (CultureNotFoundException)
            {
                return new[] { LuaNil.Instance };
            }
            catch (ArgumentException)
            {
                return new[] { LuaNil.Instance };
            }
        }
        
        #endregion
        
        #region Process Functions
        
        private static LuaValue[] Exit(LuaValue[] args)
        {
            var exitCode = 0;
            if (args.Length > 0)
            {
                if (args[0] is LuaBoolean boolVal)
                {
                    exitCode = boolVal.Value ? 0 : 1;
                }
                else if (args[0].AsInteger.HasValue)
                {
                    exitCode = (int)args[0].AsInteger!.Value;
                }
            }
            
            Environment.Exit(exitCode);
            
            // This line will never be reached, but needed for compiler
            return Array.Empty<LuaValue>();
        }
        
        #endregion
        
        #region System Information
        
        private static LuaValue[] TmpName(LuaValue[] args)
        {
            try
            {
                var tempFileName = System.IO.Path.GetTempFileName();
                return new[] { new LuaString(tempFileName) };
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