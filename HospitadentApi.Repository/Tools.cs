using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System;
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
        // IP adresi için yeni yardımcı metot
        public static string GetClientIpAddress(HttpContext httpContext)
        {
            string? ipAddress = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();

            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            }

            return ipAddress ?? "Unknown";
        }

        // User-Agent'dan cihaz tipini belirlemek için yardımcı metot
        public static string GetDeviceType(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
                return "Unknown";

            return userAgent.ToLower().Contains("mobile") ? "Mobile" : "Desktop";
        }

        // HTTP isteklerinden User-Agent almak için yardımcı metot
        public static string GetUserAgent(HttpContext httpContext)
        {
            return httpContext.Request.Headers["User-Agent"].ToString();
        }

        // Tüm istemci bilgilerini tek bir çağrıda almak için birleştirilmiş metot
        public static (string IpAddress, string UserAgent, string DeviceType) GetClientInfo(HttpContext httpContext)
        {
            string userAgent = GetUserAgent(httpContext);
            return (
                IpAddress: GetClientIpAddress(httpContext),
                UserAgent: userAgent,
                DeviceType: GetDeviceType(userAgent)
            );
        }

        public static string GetEnumDisplayName(this Enum value)
        {
            Type type = value.GetType();
            string name = Enum.GetName(type, value);
            if (name != null)
            {
                System.Reflection.FieldInfo field = type.GetField(name);
                if (field != null)
                {
                    string attr = field.GetCustomAttributesData()[0].NamedArguments[0].TypedValue.Value.ToString();
                    if (attr == null)
                    {
                        return name;
                    }
                    else
                    {
                        return attr;
                    }
                }
            }
            return null;
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
                    string attr = field.GetCustomAttributesData()[0].NamedArguments[1].TypedValue.Value.ToString();
                    if (attr == null)
                    {
                        return name;
                    }
                    else
                    {
                        return attr;
                    }
                }
            }
            return null;
        }

        public static Boolean GetBoolean(this SqlDataReader reader, string column)
        {
            if (reader[column] == DBNull.Value)
                return false;

            return (Boolean)reader[column];
        }
        public static byte? GetByte(this SqlDataReader reader, string column)
        {
            if (reader[column] == DBNull.Value)
                return null;

            return (Byte)reader[column];
        }

        public static int? GetInt16(this SqlDataReader reader, string column)
        {
            if (reader[column] == DBNull.Value)
                return null;

            return (Int16)reader[column];
        }

        public static int? GetInt32Nullable(this SqlDataReader reader, string column)
        {
            if (reader[column] == DBNull.Value)
                return null;
            else
                return (int)reader[column];
        }

        public static int GetInt32(this SqlDataReader reader, string column)
        {
            if (reader[column] == DBNull.Value)
                return 0;

            return (int)reader[column];
        }

        public static long? GetInt64Nullable(this SqlDataReader reader, string column)
        {
            if (reader[column] == DBNull.Value)
                return null;

            return (long)reader[column];
        }

        public static string GetString(this SqlDataReader reader, string column)
        {
            return reader[column].ToString();
        }

        public static DateTime? GetDateTime(this SqlDataReader reader, string column)
        {
            if (reader[column] != DBNull.Value)
                return Convert.ToDateTime(reader[column]);

            return null;
        }

        public static TimeSpan? GetTimeSpan(this SqlDataReader reader, string column)
        {
            if (reader[column] != DBNull.Value)
                return (TimeSpan)(reader[column]);

            return null;
        }

        public static decimal GetDecimal(this SqlDataReader reader, string column)
        {
            if (reader[column] == null || reader[column] == DBNull.Value)
            {
                return 0.00M;
            }
            else
            {
                return Convert.ToDecimal(reader[column]);
            }
        }

        public static double GetDouble(this SqlDataReader reader, string column)
        {
            if (reader[column] == null || reader[column] == DBNull.Value)
            {
                return 0.00D;
            }
            else
            {
                return Convert.ToDouble(reader[column]);
            }
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

            if (Rgx.IsMatch(param))
            {
                if (param.Length == 10)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }
        public static bool IsValidEmail(string mail)
        {
            //var email = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
            return new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(mail);// ? "Geçerli" : "Geçersiz";
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

        // Nullable versiyonu da eklenebilir
        public static DateTime? GetTurkiyeDateNullable()
        {
            try
            {
                return GetTurkiyeDate();
            }
            catch
            {
                return null;
            }
        }

        public static DateTime? ReadNullableDate(MySqlDataReader rd, int ordinal)
        {
            var val = rd.GetValue(ordinal);
            if (val == null || val == DBNull.Value)
                return null;

            if (val is MySqlDateTime mySqlDt)
                return mySqlDt.IsValidDateTime ? mySqlDt.GetDateTime() : (DateTime?)null;

            if (val is DateTime dt)
                return dt;

            if (DateTime.TryParse(val.ToString(), out var parsed))
                return parsed;

            return null;
        }


        /// <summary>
        /// Safely reads a date/datetime column from MySqlDataReader treating MySQL zero-dates as null.
        /// </summary>
        public static DateTime? SafeGetNullableDate(this MySqlDataReader rd, int ord)
        {
            try
            {
                if (rd.IsDBNull(ord))
                    return null;

                var val = rd.GetValue(ord);
                if (val == null || val == DBNull.Value)
                    return null;

                // Handle MySqlDateTime type (handles invalid dates like 0000-00-00)
                if (val is MySqlDateTime mySqlDt)
                    return mySqlDt.IsValidDateTime ? mySqlDt.GetDateTime() : (DateTime?)null;

                // Handle regular DateTime
                if (val is DateTime dt)
                    return dt;

                // Try parsing as string
                var s = val.ToString();
                if (string.IsNullOrEmpty(s) || s.StartsWith("0000-00-00"))
                    return null;

                if (DateTime.TryParse(s, out var parsed))
                    return parsed;

                return null;
            }
            catch (MySqlConversionException)
            {
                // MySQL invalid date (0000-00-00) conversion exception
                return null;
            }
            catch (Exception)
            {
                // Any other exception, return null
                return null;
            }
        }

        /// <summary>
        /// Reads a datetime column, returns defaultValue when value is null / zero-date / invalid.
        /// </summary>
        public static DateTime SafeGetDateTimeOrDefault(this MySqlDataReader rd, int ord, DateTime defaultValue)
        {
            var v = rd.SafeGetNullableDate(ord);
            return v ?? defaultValue;
        }
    }
}
