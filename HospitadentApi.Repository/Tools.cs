using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;
using MySql.Data.Types;

namespace HospitadentApi.Repository
{
    public static class Tools
    {
        public static int PageSize = 10;
        public static int MessagePageSize = 24;
        public static int MemberPageSize = 25;

        private static IConfiguration Configuration { get; set; }

        static Tools()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory) // This line will work now
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            Configuration = builder.Build();
        }

        //public static string GetClientIpAddress(HttpContext httpContext)
        //{
        //    string? ipAddress = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

        //    if (string.IsNullOrEmpty(ipAddress))
        //    {
        //        ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        //    }

        //    return ipAddress ?? "Unknown";
        //}

        public static string GetDeviceType(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "Unknown";

            return userAgent.ToLower().Contains("mobile") ? "Mobile" : "Desktop";
        }

        //public static string GetUserAgent(HttpContext httpContext)
        //{
        //    return httpContext.Request.Headers["User-Agent"].ToString();
        //}

        //public static (string IpAddress, string UserAgent, string DeviceType) GetClientInfo(HttpContext httpContext)
        //{
        //    string userAgent = GetUserAgent(httpContext);
        //    return (
        //        IpAddress: GetClientIpAddress(httpContext),
        //        UserAgent: userAgent,
        //        DeviceType: GetDeviceType(userAgent)
        //    );
        //}

        public static string GetEnumDisplayName(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                System.Reflection.FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    var attrs = field.GetCustomAttributesData();
                    if (attrs.Count > 0 && attrs[0].NamedArguments.Count > 0)
                    {
                        var arg = attrs[0].NamedArguments[0].TypedValue.Value;
                        return arg?.ToString() ?? name;
                    }
                    return name;
                }
            }
            return string.Empty;
        }

        public static string GetEnumDescription(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                System.Reflection.FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    var attrs = field.GetCustomAttributesData();
                    if (attrs.Count > 0 && attrs[0].NamedArguments.Count > 1)
                    {
                        var arg = attrs[0].NamedArguments[1].TypedValue.Value;
                        return arg?.ToString() ?? name;
                    }
                    return name;
                }
            }
            return string.Empty;
        }

        // Provider-specific helpers now use MySqlDataReader (project targets MariaDB)
        public static bool GetBoolean(this MySqlDataReader reader, string column)
        {
            var v = reader[column];
            if (v == DBNull.Value || v == null) return false;
            return Convert.ToBoolean(v);
        }

        public static byte? GetByte(this MySqlDataReader reader, string column)
        {
            var v = reader[column];
            if (v == DBNull.Value || v == null) return null;
            return Convert.ToByte(v);
        }

        public static short? GetInt16(this MySqlDataReader reader, string column)
        {
            var v = reader[column];
            if (v == DBNull.Value || v == null) return null;
            return Convert.ToInt16(v);
        }

        public static int? GetInt32Nullable(this MySqlDataReader reader, string column)
        {
            var v = reader[column];
            if (v == DBNull.Value || v == null) return null;
            return Convert.ToInt32(v);
        }

        public static int GetInt32(this MySqlDataReader reader, string column)
        {
            var v = reader[column];
            if (v == DBNull.Value || v == null) return 0;
            return Convert.ToInt32(v);
        }

        public static long? GetInt64Nullable(this MySqlDataReader reader, string column)
        {
            var v = reader[column];
            if (v == DBNull.Value || v == null) return null;
            return Convert.ToInt64(v);
        }

        public static string GetString(this MySqlDataReader reader, string column)
        {
            var v = reader[column];
            return v?.ToString() ?? string.Empty;
        }

        public static DateTime? GetDateTime(this MySqlDataReader reader, string column)
        {
            var v = reader[column];
            if (v == DBNull.Value || v == null) return null;
            if (v is MySqlDateTime mdt) return mdt.IsValidDateTime ? mdt.GetDateTime() : (DateTime?)null;
            if (v is DateTime dt) return dt;
            if (DateTime.TryParse(v.ToString(), out var parsed)) return parsed;
            return null;
        }

        public static TimeSpan? GetTimeSpan(this MySqlDataReader reader, string column)
        {
            try
            {
                var val = reader[column];
                if (val == null || val == DBNull.Value)
                    return null;

                if (val is TimeSpan ts)
                    return ts;

                if (TimeSpan.TryParse(val.ToString(), out var parsed))
                    return parsed;

                return null;
            }
            catch
            {
                return null;
            }
        }

        public static TimeSpan? GetTimeSpan(this MySqlDataReader reader, int ordinal)
        {
            try
            {
                if (reader.IsDBNull(ordinal))
                    return null;

                var val = reader.GetValue(ordinal);
                if (val == null || val == DBNull.Value)
                    return null;

                if (val is TimeSpan ts)
                    return ts;

                if (TimeSpan.TryParse(val.ToString(), out var parsed))
                    return parsed;

                return null;
            }
            catch
            {
                return null;
            }
        }

        public static decimal GetDecimal(this MySqlDataReader reader, string column)
        {
            var v = reader[column];
            if (v == null || v == DBNull.Value) return 0.00M;
            return Convert.ToDecimal(v);
        }

        public static double GetDouble(this MySqlDataReader reader, string column)
        {
            var v = reader[column];
            if (v == null || v == DBNull.Value) return 0.00D;
            return Convert.ToDouble(v);
        }

        public static bool IsTextNumber(string param)
        {
            if (string.IsNullOrEmpty(param)) return false;
            return Regex.IsMatch(param, @"\d");
        }

        public static bool IsValidYear(string param)
        {
            if (param == null) return false;
            Regex _regex = new Regex(@"^(19|20)[0-9][0-9]");
            return _regex.IsMatch(param);
        }

        public static bool IsValidPhone(string param)
        {
            if (param == null) return false;
            Regex _regex = new Regex(@"\d{10}");
            return _regex.IsMatch(param);
        }

        public static bool IsMsisdn(string param)
        {
            if (param == null) return false;
            Regex Rgx = new Regex(@"5\d{9}");
            if (Rgx.IsMatch(param)) return param.Length == 10;
            return false;
        }

        public static bool IsValidEmail(string mail)
        {
            return new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(mail);
        }

        public static bool IsValidCurrency(string str)
        {
            float num;
            bool isValid = float.TryParse(str, System.Globalization.NumberStyles.Currency, System.Globalization.CultureInfo.GetCultureInfo("tr-TR"), out num);
            return isValid;
        }

        public static bool IsValidDate(string str)
        {
            string pattern = @"^(?:(?:31(\/|-|\.)(?:0?[13578]|1[02]))\1|(?:(?:29|30)(\/|-|\.)(?:0?[1,3-9]|1[0-2])\2))(?:(?:1[6-9]|[2-9]\d)?\d{2})$|^(?:29(\/|-|\.)0?2\3(?:(?:(?:1[6-9]|[2-9]\d)?(?:0[48]|[2468][048]|[13579][26])|(?:(?:16|[2468][048]|[3579][26])00))))$|^(?:0?[1-9]|1\d|2[0-8])(\/|-|\.)(?:(?:0?[1-9])|(?:1[0-2]))\4(?:(?:1[6-9]|[2-9]\d)?\d{2})$";
            Regex _regex = new Regex(pattern);
            return _regex.IsMatch(str);
        }

        public static DateTime GetTurkiyeDate()
        {
            TimeZoneInfo turkeyZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyZone);
        }

        public static DateTime? GetTurkiyeDateNullable()
        {
            try { return GetTurkiyeDate(); } catch { return null; }
        }

        public static DateTime? ReadNullableDate(MySqlDataReader rd, int ordinal)
        {
            var val = rd.GetValue(ordinal);
            if (val == null || val == DBNull.Value) return null;

            if (val is MySqlDateTime mySqlDt)
                return mySqlDt.IsValidDateTime ? mySqlDt.GetDateTime() : (DateTime?)null;

            if (val is DateTime dt) return dt;

            if (DateTime.TryParse(val.ToString(), out var parsed)) return parsed;

            return null;
        }

        public static DateTime? SafeGetNullableDate(this MySqlDataReader rd, int ord)
        {
            try
            {
                if (rd.IsDBNull(ord)) return null;

                var val = rd.GetValue(ord);
                if (val == null || val == DBNull.Value) return null;

                if (val is MySqlDateTime mySqlDt)
                    return mySqlDt.IsValidDateTime ? mySqlDt.GetDateTime() : (DateTime?)null;

                if (val is DateTime dt)
                    return dt;

                var s = val.ToString();
                if (string.IsNullOrEmpty(s) || s.StartsWith("0000-00-00")) return null;

                if (DateTime.TryParse(s, out var parsed)) return parsed;

                return null;
            }
            catch (MySqlConversionException)
            {
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static DateTime SafeGetDateTimeOrDefault(this MySqlDataReader rd, int ord, DateTime defaultValue)
        {
            var v = rd.SafeGetNullableDate(ord);
            return v ?? defaultValue;
        }
    }
}
