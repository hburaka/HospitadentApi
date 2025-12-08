using HospitadentApi.Entity;
using System.Text;
using Microsoft.Extensions.Logging;

namespace HospitadentApi.Repository
{
    public class UserWorkingScheduleRepository : IRepository<UserWorkingSchedule>
    {
        private readonly string _connectionString;
        private readonly ILogger<UserWorkingScheduleRepository> _logger;

        public UserWorkingScheduleRepository(string connectionString, ILogger<UserWorkingScheduleRepository> logger)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string must be provided.", nameof(connectionString));
            _connectionString = connectionString;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public UserWorkingSchedule? Load(int Id)
        {
            _logger.LogDebug("Load called: Id={Id}", Id);
            try
            {
                using var db = new DBHelper(_connectionString);
                db.ParametreEkle("@Id", Id);

                using var rd = db.ExecuteReaderSql("SELECT id, clinic_id, user_id, custom_date, day, " +
                    "start_time, end_time " +
                    "FROM user_working_schedule " +
                    "WHERE id = @Id AND is_deleted = 0");
                if (!rd.Read())
                    return null;

                int ordId = rd.GetOrdinal("id");
                int ordClinic = rd.GetOrdinal("clinic_id");
                int ordUser = rd.GetOrdinal("user_id");
                int ordCustomDate = rd.GetOrdinal("custom_date");
                int ordDay = rd.GetOrdinal("day");
                int ordStart = rd.GetOrdinal("start_time");
                int ordEnd = rd.GetOrdinal("end_time");

                var ent = new UserWorkingSchedule();

                if (!rd.IsDBNull(ordId)) ent.Id = rd.GetInt32(ordId);
                if (!rd.IsDBNull(ordClinic)) ent.ClinicId = rd.GetInt32(ordClinic);
                if (!rd.IsDBNull(ordUser)) ent.UserId = rd.GetInt32(ordUser);

                // safe reads for date columns
                ent.CustomDate = rd.SafeGetNullableDate(ordCustomDate);          // DateTime?
                if (!rd.IsDBNull(ordDay)) ent.Day = rd.GetString(ordDay);
                if (!rd.IsDBNull(ordStart)) ent.StartTime = rd.GetTimeSpan(ordStart);
                if (!rd.IsDBNull(ordEnd)) ent.EndTime = rd.GetTimeSpan(ordEnd);

                return ent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading UserWorkingSchedule with Id {Id}", Id);
                throw new Exception($"Error loading UserWorkingSchedule with Id {Id}", ex);
            }
        }

        public IList<UserWorkingSchedule> LoadAll()
        {
            _logger.LogDebug("LoadAll called");
            var list = new List<UserWorkingSchedule>();
            try
            {
                using var db = new DBHelper(_connectionString);
                using var rd = db.ExecuteReaderSql("SELECT id, clinic_id, user_id, custom_date, day, " +
                    "start_time, end_time " +
                    "FROM user_working_schedule " +
                    "WHERE is_deleted = 0 ORDER BY id DESC");

                int ordId = rd.GetOrdinal("id");
                int ordClinic = rd.GetOrdinal("clinic_id");
                int ordUser = rd.GetOrdinal("user_id");
                int ordCustomDate = rd.GetOrdinal("custom_date");
                int ordDay = rd.GetOrdinal("day");
                int ordStart = rd.GetOrdinal("start_time");
                int ordEnd = rd.GetOrdinal("end_time");

                while (rd.Read())
                {
                    var ent = new UserWorkingSchedule();

                    if (!rd.IsDBNull(ordId)) ent.Id = rd.GetInt32(ordId);
                    if (!rd.IsDBNull(ordClinic)) ent.ClinicId = rd.GetInt32(ordClinic);
                    if (!rd.IsDBNull(ordUser)) ent.UserId = rd.GetInt32(ordUser);

                    ent.CustomDate = rd.SafeGetNullableDate(ordCustomDate);
                    if (!rd.IsDBNull(ordDay)) ent.Day = rd.GetString(ordDay);
                    if (!rd.IsDBNull(ordStart)) ent.StartTime = rd.GetTimeSpan(ordStart);
                    if (!rd.IsDBNull(ordEnd)) ent.EndTime = rd.GetTimeSpan(ordEnd);

                    list.Add(ent);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading UserWorkingSchedule list");
                throw new Exception("Error loading UserWorkingSchedule list", ex);
            }
            return list;
        }

        public IList<UserWorkingSchedule> GetByUser(int userId)
        {
            _logger.LogDebug("GetByUser called: UserId={UserId}", userId);
            var result = new List<UserWorkingSchedule>();
            using var db = new DBHelper(_connectionString);
            db.ParametreEkle("@UserId", userId);

            using var rd = db.ExecuteReaderSql("SELECT id, clinic_id, user_id, custom_date, day," +
                "start_time, end_time " +
                "FROM user_working_schedule " +
                "WHERE user_id = @UserId AND is_deleted = 0 ORDER BY custom_date DESC, start_time");
            if (rd == null) return result;

            int ordId = rd.GetOrdinal("id");
            int ordClinic = rd.GetOrdinal("clinic_id");
            int ordUser = rd.GetOrdinal("user_id");
            int ordCustomDate = rd.GetOrdinal("custom_date");
            int ordDay = rd.GetOrdinal("day");
            int ordStart = rd.GetOrdinal("start_time");
            int ordEnd = rd.GetOrdinal("end_time");

            while (rd.Read())
            {
                var ent = new UserWorkingSchedule();

                if (!rd.IsDBNull(ordId)) ent.Id = rd.GetInt32(ordId);
                if (!rd.IsDBNull(ordClinic)) ent.ClinicId = rd.GetInt32(ordClinic);
                if (!rd.IsDBNull(ordUser)) ent.UserId = rd.GetInt32(ordUser);

                ent.CustomDate = rd.SafeGetNullableDate(ordCustomDate);
                if (!rd.IsDBNull(ordDay)) ent.Day = rd.GetString(ordDay);
                if (!rd.IsDBNull(ordStart)) ent.StartTime = rd.GetTimeSpan(ordStart);
                if (!rd.IsDBNull(ordEnd)) ent.EndTime = rd.GetTimeSpan(ordEnd);

                result.Add(ent);
            }

            return result;
        }

        public IList<UserWorkingSchedule> GetByCriteria(int userId, DateTime? from = null, DateTime? to = null, int? clinicId = null)
        {
            _logger.LogDebug("GetByCriteria called: userId={UserId}, from={From}, to={To}, clinicId={ClinicId}", userId, from, to, clinicId);
            if (userId <= 0) throw new ArgumentException("userId must be provided and greater than zero.", nameof(userId));

            var result = new List<UserWorkingSchedule>();
            var start = from?.Date ?? Tools.GetTurkiyeDate().Date;
            var end = to?.Date ?? start.AddDays(30); // default 30 days ahead if not specified
            if (end < start) end = start;

            try
            {
                using var db = new DBHelper(_connectionString);
                db.ParametreEkle("@UserId", userId);
                db.ParametreEkle("@FromDate", start);
                db.ParametreEkle("@ToDate", end);

                var dateRange = Enumerable.Range(0, (end - start).Days + 1)
                                          .Select(d => start.AddDays(d))
                                          .ToList();

                // gather day names present in the date range (e.g. "Monday", "Tuesday", ...)
                var days = dateRange.Select(d => d.DayOfWeek.ToString()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

                // build day IN clause parameters
                var dayParamNames = new List<string>();
                for (int i = 0; i < days.Count; i++)
                {
                    var pname = $"@d{i}";
                    dayParamNames.Add(pname);
                    db.ParametreEkle(pname, days[i]);
                }

                var sb = new StringBuilder();
                sb.Append(@"
                    SELECT id, clinic_id, user_id, custom_date, day, start_time, end_time
                    FROM user_working_schedule
                    WHERE user_id = @UserId AND is_deleted = 0
                ");

                // fetch explicit custom_date entries in range
                sb.Append(" AND ( (custom_date IS NOT NULL AND custom_date <> '0000-00-00' AND custom_date BETWEEN @FromDate AND @ToDate) ");

                // include recurring day-based entries (custom_date null/zero) if any day params exist
                if (dayParamNames.Count > 0)
                {
                    var inClause = string.Join(", ", dayParamNames);
                    sb.Append($" OR ( (custom_date IS NULL OR custom_date = '0000-00-00') AND day IN ({inClause}) ) ");
                }

                sb.Append(" ) ");

                if (clinicId.HasValue)
                {
                    db.ParametreEkle("@ClinicId", clinicId.Value);
                    sb.Append(" AND clinic_id = @ClinicId ");
                }

                sb.Append(" ORDER BY custom_date, start_time ");

                using var rd = db.ExecuteReaderSql(sb.ToString());
                if (rd == null) return result;

                int ordId = rd.GetOrdinal("id");
                int ordClinic = rd.GetOrdinal("clinic_id");
                int ordUser = rd.GetOrdinal("user_id");
                int ordCustomDate = rd.GetOrdinal("custom_date");
                int ordDay = rd.GetOrdinal("day");
                int ordStart = rd.GetOrdinal("start_time");
                int ordEnd = rd.GetOrdinal("end_time");

                // collect rows first, expand day-based rows into concrete dates
                var dayRows = new List<(int Id, int ClinicId, int UserId, string? Day, TimeSpan? Start, TimeSpan? End, DateTime? CustomDate)>();

                while (rd.Read())
                {
                    DateTime? customDate = null;
                    object val = rd.GetValue(ordCustomDate);
                    if (val != null && val != DBNull.Value)
                    {
                        // handle MySQL '0000-00-00' as null
                        var s = val.ToString();
                        if (!string.IsNullOrEmpty(s) && s != "0000-00-00")
                        {
                            if (DateTime.TryParse(s, out var parsed))
                                customDate = parsed.Date;
                        }
                    }

                    var day = rd.IsDBNull(ordDay) ? null : rd.GetString(ordDay);
                    TimeSpan? startTime = rd.IsDBNull(ordStart) ? null : rd.GetTimeSpan(ordStart);
                    TimeSpan? endTime = rd.IsDBNull(ordEnd) ? null : rd.GetTimeSpan(ordEnd);

                    var row = (
                        Id: rd.IsDBNull(ordId) ? 0 : rd.GetInt32(ordId),
                        ClinicId: rd.IsDBNull(ordClinic) ? 0 : rd.GetInt32(ordClinic),
                        UserId: rd.IsDBNull(ordUser) ? 0 : rd.GetInt32(ordUser),
                        Day: day,
                        Start: startTime,
                        End: endTime,
                        CustomDate: customDate
                    );

                    dayRows.Add(row);
                }

                // expand rows: if CustomDate present -> add single entry; else expand to each matching date in range
                foreach (var r in dayRows)
                {
                    if (r.CustomDate.HasValue)
                    {
                        var ent = new UserWorkingSchedule
                        {
                            Id = r.Id,
                            ClinicId = r.ClinicId,
                            UserId = r.UserId,
                            CustomDate = r.CustomDate,
                            Day = r.Day ?? string.Empty,
                            StartTime = r.Start ?? TimeSpan.Zero,
                            EndTime = r.End ?? TimeSpan.Zero
                        };
                        result.Add(ent);
                    }
                    else if (!string.IsNullOrEmpty(r.Day))
                    {
                        // for each date in range that matches day name, create an entry with CustomDate set
                        foreach (var d in dateRange)
                        {
                            if (string.Equals(d.DayOfWeek.ToString(), r.Day, StringComparison.OrdinalIgnoreCase))
                            {
                                var ent = new UserWorkingSchedule
                                {
                                    Id = r.Id,
                                    ClinicId = r.ClinicId,
                                    UserId = r.UserId,
                                    CustomDate = d,
                                    Day = r.Day,
                                    StartTime = r.Start ?? TimeSpan.Zero,
                                    EndTime = r.End ?? TimeSpan.Zero
                                };
                                result.Add(ent);
                            }
                        }
                    }
                }

                // order results by date then start time
                return result.OrderBy(x => x.CustomDate ?? DateTime.MinValue)
                             .ThenBy(x => x.StartTime)
                             .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error querying user working schedule for userId {UserId}", userId);
                var errorMessage = $"Error querying user working schedule for userId {userId}";
                if (ex.InnerException != null)
                    errorMessage += $". Inner exception: {ex.InnerException.Message}";
                else
                    errorMessage += $". Exception: {ex.Message}";
                throw new Exception(errorMessage, ex);
            }
        }

        // Not implemented write operations - implement as needed
        public int Insert(UserWorkingSchedule instance) => throw new NotImplementedException();
        public int Update(UserWorkingSchedule instance) => throw new NotImplementedException();
        public int Delete(UserWorkingSchedule instance) => throw new NotImplementedException();
    }
}

