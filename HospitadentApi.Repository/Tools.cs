using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using System;
using System.Data.Common;

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
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            Configuration = builder.Build();
        }

        public static string GetDeviceType(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent)) return "Unknown";
            return userAgent.ToLower().Contains("mobile") ? "Mobile" : "Desktop";
        }

        public static string GetEnumDisplayName(this Enum value)
        {
            var type = value.GetType();
            var name = Enum.GetName(type, value) ?? string.Empty;
            var field = type.GetField(name);
            if (field == null) return name;
            var attrs = field.GetCustomAttributesData();
            if (attrs.Count > 0 && attrs[0].NamedArguments.Count > 0)
                return attrs[0].NamedArguments[0].TypedValue.Value?.ToString() ?? name;
            return name;
        }

        public static string GetEnumDescription(this Enum value)
        {
            var type = value.GetType();
            var name = Enum.GetName(type, value) ?? string.Empty;
            var field = type.GetField(name);
            if (field == null) return name;
            var attrs = field.GetCustomAttributesData();
            if (attrs.Count > 0 && attrs[0].NamedArguments.Count > 1)
                return attrs[0].NamedArguments[1].TypedValue.Value?.ToString() ?? name;
            return name;
        }

        // Provider-agnostic reader helpers - use DbDataReader so both MySql.Data and MySqlConnector work
        public static bool GetBoolean(this DbDataReader reader, string column)
        {
            var v = reader[column];
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        public static int GetInt32(this DbDataReader reader, string column)
        {
            var v = reader[column];
            return (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v);
        }

        public static int? GetInt32Nullable(this DbDataReader reader, string column)
        {
            var v = reader[column];
            return (v == null || v == DBNull.Value) ? null : Convert.ToInt32(v);
        }

        public static long? GetInt64Nullable(this DbDataReader reader, string column)
        {
            var v = reader[column];
            return (v == null || v == DBNull.Value) ? null : Convert.ToInt64(v);
        }

        public static string GetString(this DbDataReader reader, string column)
        {
            var v = reader[column];
            return v?.ToString() ?? string.Empty;
        }

        public static decimal GetDecimal(this DbDataReader reader, string column)
        {
            var v = reader[column];
            return (v == null || v == DBNull.Value) ? 0m : Convert.ToDecimal(v);
        }

        public static double GetDouble(this DbDataReader reader, string column)
        {
            var v = reader[column];
            return (v == null || v == DBNull.Value) ? 0d : Convert.ToDouble(v);
        }

        public static TimeSpan? GetTimeSpan(this DbDataReader reader, string column)
        {
            var v = reader[column];
            if (v == null || v == DBNull.Value) return null;
            if (v is TimeSpan ts) return ts;
            if (TimeSpan.TryParse(v.ToString(), out var parsed)) return parsed;
            if (double.TryParse(v.ToString(), out var seconds)) return TimeSpan.FromSeconds(seconds);
            return null;
        }

        // Handles provider-specific MySqlDateTime via reflection, and zero-dates like "0000-00-00"
        public static DateTime? GetDateTime(this DbDataReader reader, string column)
        {
            var v = reader[column];
            if (v == null || v == DBNull.Value) return null;

            var type = v.GetType();
            var isValidProp = type.GetProperty("IsValidDateTime");
            if (isValidProp != null)
            {
                var isValid = (bool?)isValidProp.GetValue(v) ?? false;
                if (!isValid) return null;
                var getMethod = type.GetMethod("GetDateTime");
                if (getMethod != null)
                {
                    var dtValue = getMethod.Invoke(v, null);
                    if (dtValue is DateTime dtVal) return dtVal;
                }
            }

            if (v is DateTime dt2) return dt2;
            var s = v.ToString();
            if (string.IsNullOrEmpty(s) || s.StartsWith("0000-00-00")) return null;
            return DateTime.TryParse(s, out var parsed) ? parsed : null;
        }

        public static DateTime? SafeGetNullableDate(this DbDataReader reader, int ord)
        {
            try
            {
                if (reader.IsDBNull(ord)) return null;
                var v = reader.GetValue(ord);
                if (v == null || v == DBNull.Value) return null;

                var type = v.GetType();
                var isValidProp = type.GetProperty("IsValidDateTime");
                if (isValidProp != null)
                {
                    var isValid = (bool?)isValidProp.GetValue(v) ?? false;
                    if (!isValid) return null;
                    var getMethod = type.GetMethod("GetDateTime");
                    if (getMethod != null)
                    {
                        var dtValue = getMethod.Invoke(v, null);
                        if (dtValue is DateTime dtVal) return dtVal;
                    }
                    return null;
                }

                if (v is DateTime dt) return dt;
                var s = v.ToString();
                if (string.IsNullOrEmpty(s) || s.StartsWith("0000-00-00")) return null;
                return DateTime.TryParse(s, out var parsed) ? parsed : null;
            }
            catch
            {
                return null;
            }
        }

        public static DateTime SafeGetDateTimeOrDefault(this DbDataReader reader, int ord, DateTime defaultValue)
        {
            var v = reader.SafeGetNullableDate(ord);
            return v ?? defaultValue;
        }

        // Compatibility overloads expected by other code (e.g. AppointmentRepository)
        public static DateTime? ReadNullableDate(this DbDataReader reader, string column)
        {
            return reader.GetDateTime(column);
        }

        public static DateTime? ReadNullableDate(this DbDataReader reader, int ord)
        {
            return reader.SafeGetNullableDate(ord);
        }

        // Validators (unchanged semantics)
        public static bool IsTextNumber(string param) => !string.IsNullOrEmpty(param) && Regex.IsMatch(param, @"\d");
        public static bool IsValidYear(string param) => !string.IsNullOrEmpty(param) && Regex.IsMatch(param, @"^(19|20)[0-9][0-9]");
        public static bool IsValidPhone(string param) => !string.IsNullOrEmpty(param) && Regex.IsMatch(param, @"\d{10}");
        public static bool IsMsisdn(string param) => !string.IsNullOrEmpty(param) && Regex.IsMatch(param, @"^5\d{9}$");
        public static bool IsValidEmail(string mail) => new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(mail);
        public static bool IsValidCurrency(string str) => float.TryParse(str, System.Globalization.NumberStyles.Currency, System.Globalization.CultureInfo.GetCultureInfo("tr-TR"), out _);
        public static bool IsValidDate(string str)
        {
            string pattern = @"^(?:(?:31(\/|-|\.)(?:0?[13578]|1[02]))\1|(?:(?:29|30)(\/|-|\.)(?:0?[1,3-9]|1[0-2])\2))(?:(?:1[6-9]|[2-9]\d)?\d{2})$|^(?:29(\/|-|\.)0?2\3(?:(?:(?:1[6-9]|[2-9]\d)?(?:0[48]|[2468][048]|[13579][26])|(?:(?:16|[2468][048]|[3579][26])00))))$|^(?:0?[1-9]|1\d|2[0-8])(\/|-|\.)(?:(?:0?[1-9])|(?:1[0-2]))\4(?:(?:1[6-9]|[2-9]\d)?\d{2})$";
            return !string.IsNullOrEmpty(str) && Regex.IsMatch(str, pattern);
        }

        public static DateTime GetTurkiyeDate()
        {
            var turkeyZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyZone);
        }

        public static DateTime? GetTurkiyeDateNullable()
        {
            try { return GetTurkiyeDate(); } catch { return null; }
        }
    }
}
