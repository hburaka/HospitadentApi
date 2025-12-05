using HospitadentApi.Entity;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace HospitadentApi.Repository
{
    public class ClinicRepository : IRepository<Clinic>
    {
        private readonly string _connectionString;
        private readonly ILogger<ClinicRepository> _logger;

        public ClinicRepository(string connectionString, ILogger<ClinicRepository> logger)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string must be provided.", nameof(connectionString));
            _connectionString = connectionString;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Clinic? Load(int Id)
        {
            _logger.LogDebug("Load called: Id={Id}", Id);
            try
            {
                using var db = new DBHelper(_connectionString);
                db.ParametreEkle("@Id", Id);

                using var rd = db.ExecuteReaderSql("select * from clinics where id=@Id and isDeleted=0");
                Clinic? _clinic = null;
                if (rd.Read())
                {
                    _clinic = new Clinic();
                    _clinic.Id = rd.GetInt32("id");
                    _clinic.Name = rd.GetString("clinic_name");
                    _clinic.Status = rd.GetBoolean("status");
                    _clinic.IsDeleted = rd.GetBoolean("isDeleted");
                    _logger.LogInformation("Loaded clinic Id={Id} Name={Name}", _clinic.Id, _clinic.Name);
                }
                else
                {
                    _logger.LogInformation("Clinic not found: Id={Id}", Id);
                }
                return _clinic;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading clinic Id={Id}", Id);
                throw new Exception($"Error loading clinic Id {Id}", ex);
            }
        }

        public IList<Clinic> LoadAll()
        {
            _logger.LogDebug("LoadAll called");
            var clinics = new List<Clinic>();
            using var db = new DBHelper(_connectionString);
            try
            {
                using var rd = db.ExecuteReaderSql("select * from clinics where isDeleted = 0 and status = 1");
                int ordId = rd.GetOrdinal("id");
                int ordName = rd.GetOrdinal("clinic_name");
                int ordStatus = rd.GetOrdinal("status");
                int ordIsDeleted = rd.GetOrdinal("isDeleted");

                while (rd.Read())
                {
                    var c = new Clinic
                    {
                        Id = rd.IsDBNull(ordId) ? 0 : rd.GetInt32(ordId),
                        Name = rd.IsDBNull(ordName) ? string.Empty : rd.GetString(ordName),
                        Status = rd.IsDBNull(ordStatus) ? false : rd.GetBoolean(ordStatus),
                        IsDeleted = rd.IsDBNull(ordIsDeleted) ? false : rd.GetBoolean(ordIsDeleted)
                    };
                    clinics.Add(c);
                }

                _logger.LogInformation("LoadAll returned {Count} clinics", clinics.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading clinics");
                throw new Exception("Error loading clinics", ex);
            }
            return clinics;
        }

        public IList<Clinic> GetByIds(IEnumerable<int>? ids)
        {
            _logger.LogDebug("GetByIds called: ids={Ids}", ids == null ? "null" : string.Join(",", ids));
            var result = new List<Clinic>();
            if (ids == null)
            {
                _logger.LogWarning("GetByIds called with null ids");
                return result;
            }

            // Normalize id list: remove duplicates and invalid ids
            var idList = ids.Where(i => i > 0).Distinct().ToList();
            if (!idList.Any())
            {
                _logger.LogWarning("GetByIds has no valid ids after normalization");
                return result;
            }

            try
            {
                using var db = new DBHelper(_connectionString);

                // Build parameterized IN clause: @id0, @id1, ...
                var paramNames = new List<string>(idList.Count);
                for (int i = 0; i < idList.Count; i++)
                {
                    var pname = $"@id{i}";
                    paramNames.Add(pname);
                    db.ParametreEkle(pname, idList[i]);
                }

                var inClause = string.Join(", ", paramNames);

                var sql = $@"
                    SELECT
                      c.id,
                      c.clinic_name
                    FROM clinics c
                    WHERE c.id IN ({inClause}) AND c.isDeleted = 0
                ";

                using var rd = db.ExecuteReaderSql(sql);
                if (rd == null)
                {
                    _logger.LogWarning("GetByIds reader returned null");
                    return result;
                }

                var ordId = rd.GetOrdinal("id");
                var ordName = rd.GetOrdinal("clinic_name");
                int ordIsActive = -1;
                try { ordIsActive = rd.GetOrdinal("is_active"); } catch { ordIsActive = -1; }

                var map = new Dictionary<int, Clinic>(idList.Count);
                while (rd.Read())
                {
                    if (rd.IsDBNull(ordId))
                        continue;

                    var id = rd.GetInt32(ordId);
                    var clinic = new Clinic { Id = id };

                    if (!rd.IsDBNull(ordName))
                        clinic.Name = rd.GetString(ordName);

                    if (ordIsActive >= 0 && !rd.IsDBNull(ordIsActive))
                        clinic.IsActive = rd.GetBoolean(ordIsActive);

                    map[id] = clinic;
                }

                foreach (var id in idList)
                {
                    if (map.TryGetValue(id, out var clinic))
                        result.Add(clinic);
                }

                _logger.LogInformation("GetByIds returning {Count} clinics for requested {RequestedCount} ids", result.Count, idList.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading clinics by ids");
                throw new Exception("Error loading clinics by ids", ex);
            }

            return result;
        }

        public int Delete(Clinic instance) => throw new NotImplementedException();
        public int Insert(Clinic instance) => throw new NotImplementedException();
        public int Update(Clinic instance) => throw new NotImplementedException();
    }
}
