using HospitadentApi.Entity;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HospitadentApi.Repository
{
    public class ClinicRepository : IRepository<Clinic>
    {
        private readonly string _connectionString;

        public ClinicRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string must be provided.", nameof(connectionString));
            _connectionString = connectionString;
        }

        public Clinic? Load(int Id)
        {
            DBHelper db = new DBHelper(_connectionString);
            db.ParametreEkle("@Id", Id);

            MySqlDataReader rd = db.ExecuteReaderSql("select * from clinics where id=@Id and isDeleted=0");

            Clinic _clinic = null;
            if (rd.Read())
            {
                _clinic = new Clinic();
                _clinic.Id = rd.GetInt32("id");
                _clinic.Name = rd.GetString("clinic_name");
                _clinic.Status = rd.GetBoolean("status");
                _clinic.IsDeleted = rd.GetBoolean("isDeleted");
            }
            rd.Close();
            return _clinic;
        }

        public IList<Clinic> LoadAll()
        {
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
            }
            catch (Exception ex)
            {
                throw new Exception("Error loading clinics", ex);
            }
            return clinics;
        }

        public IList<Clinic> GetByIds(IEnumerable<int>? ids)
        {
            var result = new List<Clinic>();
            if (ids == null)
                return result;

            // Normalize id list: remove duplicates and invalid ids
            var idList = ids.Where(i => i > 0).Distinct().ToList();
            if (!idList.Any())
                return result;

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

                // Select the columns you need. Adjust column names as per your schema.
                var sql = $@"
                    SELECT
                      c.id,
                      c.clinic_name
                    FROM clinics c
                    WHERE c.id IN ({inClause}) AND c.isDeleted = 0
                ";

                using var rd = db.ExecuteReaderSql(sql);
                if (rd == null)
                    return result;

                var ordId = rd.GetOrdinal("id");
                var ordName = rd.GetOrdinal("clinic_name");
                int ordIsActive = -1;
                // optional columns may not exist in all schemas, guard with try/catch for GetOrdinal
                try { ordIsActive = rd.GetOrdinal("is_active"); } catch { ordIsActive = -1; }

                // Read into dictionary keyed by id so we can preserve input order later
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

                // Return clinics in the same order as requested ids
                foreach (var id in idList)
                {
                    if (map.TryGetValue(id, out var clinic))
                        result.Add(clinic);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error loading clinics by ids", ex);
            }

            return result;
        }

        public int Delete(Clinic instance) => throw new NotImplementedException();
        public int Insert(Clinic instance) => throw new NotImplementedException();
        public int Update(Clinic instance) => throw new NotImplementedException();
    }
}
