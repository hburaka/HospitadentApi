using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HospitadentApi.Entity;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Logging;

namespace HospitadentApi.Repository
{
    public class PatientRepository : IRepository<Patient>
    {
        private readonly string _connectionString;
        private readonly ILogger<PatientRepository> _logger;

        public PatientRepository(string connectionString, ILogger<PatientRepository> logger)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string must be provided.", nameof(connectionString));
            _connectionString = connectionString;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Patient? Load(int Id)
        {
            _logger.LogDebug("Load called for Patient Id={Id}", Id);
            try
            {
                using var db = new DBHelper(_connectionString);
                db.ParametreEkle("@Id", Id);

                var sql = @"
                    SELECT
                        p.id,
                        p.first_name,
                        p.last_name,
                        p.tc_no,
                        p.passport_no,
                        p.mobile_cc,
                        p.mobile,
                        p.clinic_id,
                        c.clinic_name
                    FROM patients p
                    LEFT JOIN clinics c ON p.clinic_id = c.id
                    WHERE p.id = @Id AND p.isDeleted = 0
                    LIMIT 1
                ";

                using var rd = db.ExecuteReaderSql(sql);
                if (rd == null || !rd.Read()) return null;

                int ordId = rd.GetOrdinal("id");
                int ordFirst = rd.GetOrdinal("first_name");
                int ordLast = rd.GetOrdinal("last_name");
                int ordTc = rd.GetOrdinal("tc_no");
                int ordPass = rd.GetOrdinal("passport_no");
                int ordMobileCc = rd.GetOrdinal("mobile_cc");
                int ordMobile = rd.GetOrdinal("mobile");
                int ordClinicId = rd.GetOrdinal("clinic_id");
                int ordClinicName = rd.GetOrdinal("clinic_name");

                var patient = new Patient
                {
                    Id = rd.IsDBNull(ordId) ? 0 : rd.GetInt32(ordId),
                    FirstName = rd.IsDBNull(ordFirst) ? null : rd.GetString(ordFirst),
                    LastName = rd.IsDBNull(ordLast) ? null : rd.GetString(ordLast),
                    TcNo = rd.IsDBNull(ordTc) ? null : rd.GetString(ordTc),
                    PassportNo = rd.IsDBNull(ordPass) ? null : rd.GetString(ordPass)
                };

                // build MobilePhone from mobile_cc + mobile
                patient.MobileCc = rd.IsDBNull(ordMobileCc) ? null : rd.GetString(ordMobileCc);
                patient.Mobile = rd.IsDBNull(ordMobile) ? null : rd.GetString(ordMobile);

                if (!rd.IsDBNull(ordClinicId))
                    patient.Clinic = new Clinic { Id = rd.GetInt32(ordClinicId), Name = rd.IsDBNull(ordClinicName) ? null : rd.GetString(ordClinicName) };

                return patient;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Patient with Id {Id}", Id);
                throw new Exception($"Error loading Patient with Id {Id}", ex);
            }
        }

        // Search with explicit fields:
        // - id (exact)
        // - fullName (single string containing name and/or surname)
        // - mobile (raw like "5321112233" or with formatting)
        // - tcNo (identity number)
        public IList<Patient> GetByCriteria(
            int? id = null,
            string? fullName = null,
            string? mobile = null,
            string? tcNo = null,
            int? clinicId = null,
            int limit = 25)
        {
            _logger.LogDebug("Search called with id={Id} fullName={FullName} mobile={Mobile} tcNo={TcNo} clinicId={ClinicId} limit={Limit}",
                id, fullName, mobile, tcNo, clinicId, limit);

            var result = new List<Patient>();

            // require at least one search criterion
            if (id == null && string.IsNullOrWhiteSpace(fullName) && string.IsNullOrWhiteSpace(mobile) && string.IsNullOrWhiteSpace(tcNo) && clinicId == null)
                return result;

            try
            {
                using var db = new DBHelper(_connectionString);

                var sb = new StringBuilder();
                sb.Append(@"
                    SELECT
                        p.id,
                        p.first_name,
                        p.last_name,
                        p.tc_no,
                        p.passport_no,
                        p.mobile_cc,
                        p.mobile,
                        p.mobile_cleaned,
                        p.clinic_id,
                        c.clinic_name
                    FROM patients p
                    LEFT JOIN clinics c ON p.clinic_id = c.id
                    WHERE p.isDeleted = 0
                ");

                var where = new List<string>();

                if (id.HasValue && id.Value > 0)
                {
                    where.Add("p.id = @id");
                    db.ParametreEkle("@id", id.Value);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(fullName))
                    {
                        var cleaned = Regex.Replace(fullName.Trim(), @"\s+", " ");
                        var parts = cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length == 1)
                        {
                            var p = "%" + parts[0] + "%";
                            where.Add("(p.first_name LIKE @name OR p.last_name LIKE @name)");
                            db.ParametreEkle("@name", p);
                        }
                        else
                        {
                            var first = "%" + parts[0] + "%";
                            var last = "%" + parts[^1] + "%";
                            var combined = "%" + cleaned + "%";
                            // try exact first+last, fallback to contains either
                            where.Add("((p.first_name LIKE @first AND p.last_name LIKE @last) OR p.first_name LIKE @combined OR p.last_name LIKE @combined)");
                            db.ParametreEkle("@first", first);
                            db.ParametreEkle("@last", last);
                            db.ParametreEkle("@combined", combined);
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(tcNo))
                    {
                        where.Add("p.tc_no LIKE @tcNo");
                        db.ParametreEkle("@tcNo", $"%{tcNo.Trim()}%");
                    }

                    if (!string.IsNullOrWhiteSpace(mobile))
                    {
                        // normalize digits for comparison against mobile_cleaned
                        var digits = Regex.Replace(mobile, @"\D+", "");
                        if (!string.IsNullOrEmpty(digits))
                        {
                            where.Add("(p.mobile_cleaned LIKE @mobileDigits OR p.mobile LIKE @mobileRaw)");
                            db.ParametreEkle("@mobileDigits", $"%{digits}%");
                            db.ParametreEkle("@mobileRaw", $"%{mobile.Trim()}%");
                        }
                    }
                }

                // clinicId filter (works with id or other filters)
                if (clinicId.HasValue && clinicId.Value > 0)
                {
                    where.Add("p.clinic_id = @clinicId");
                    db.ParametreEkle("@clinicId", clinicId.Value);
                }

                if (where.Count == 0) return result; // avoid full table scan

                sb.Append(" AND " + string.Join(" AND ", where));
                sb.Append(" ORDER BY p.first_name, p.last_name LIMIT " + Math.Max(1, limit));

                using var rd = db.ExecuteReaderSql(sb.ToString());
                if (rd == null) return result;

                int ordId = rd.GetOrdinal("id");
                int ordFirst = rd.GetOrdinal("first_name");
                int ordLast = rd.GetOrdinal("last_name");
                int ordTc = rd.GetOrdinal("tc_no");
                int ordPass = rd.GetOrdinal("passport_no");
                int ordMobileCc = rd.GetOrdinal("mobile_cc");
                int ordMobile = rd.GetOrdinal("mobile");
                int ordClinicId = rd.GetOrdinal("clinic_id");
                int ordClinicName = rd.GetOrdinal("clinic_name");

                while (rd.Read())
                {
                    var p = new Patient
                    {
                        Id = rd.IsDBNull(ordId) ? 0 : rd.GetInt32(ordId),
                        FirstName = rd.IsDBNull(ordFirst) ? null : rd.GetString(ordFirst),
                        LastName = rd.IsDBNull(ordLast) ? null : rd.GetString(ordLast),
                        TcNo = rd.IsDBNull(ordTc) ? null : rd.GetString(ordTc),
                        PassportNo = rd.IsDBNull(ordPass) ? null : rd.GetString(ordPass)
                    };

                    p.MobileCc = rd.IsDBNull(ordMobileCc) ? null : rd.GetString(ordMobileCc);
                    p.Mobile = rd.IsDBNull(ordMobile) ? null : rd.GetString(ordMobile);

                    if (!rd.IsDBNull(ordClinicId))
                        p.Clinic = new Clinic { Id = rd.GetInt32(ordClinicId), Name = rd.IsDBNull(ordClinicName) ? null : rd.GetString(ordClinicName) };

                    result.Add(p);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching patients with criteria id={Id} fullName={FullName} clinicId={ClinicId}", id, fullName, clinicId);
                throw new Exception("Error searching patients", ex);
            }

            return result;
        }

        public int Insert(Patient instance) => throw new NotImplementedException();
        public int Update(Patient instance) => throw new NotImplementedException();
        public int Delete(Patient instance) => throw new NotImplementedException();
        public IList<Patient> LoadAll() => throw new NotImplementedException();
    }
}