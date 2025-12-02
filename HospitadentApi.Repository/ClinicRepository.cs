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

            MySqlDataReader rd = db.ExecuteReaderSql("select * from clinics where id=@Id");

            Clinic _clinic = null;
            if (rd.Read())
            {
                _clinic = new Clinic();
                _clinic.Id = rd.GetInt32("id");
                _clinic.ClinicName = rd.GetString("clinic_name");
                _clinic.Status = rd.GetBoolean("status");
                _clinic.IsDeleted = rd.GetBoolean("IsDeleted");
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
                using var rd = db.ExecuteReaderSql("select * from clinics where IsDeleted = 0 and status = 1");
                int ordId = rd.GetOrdinal("id");
                int ordName = rd.GetOrdinal("clinic_name");
                int ordStatus = rd.GetOrdinal("status");
                int ordIsDeleted = rd.GetOrdinal("IsDeleted");

                while (rd.Read())
                {
                    var c = new Clinic
                    {
                        Id = rd.IsDBNull(ordId) ? 0 : rd.GetInt32(ordId),
                        ClinicName = rd.IsDBNull(ordName) ? string.Empty : rd.GetString(ordName),
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

        public int Delete(Clinic instance) => throw new NotImplementedException();
        public int Insert(Clinic instance) => throw new NotImplementedException();
        public int Update(Clinic instance) => throw new NotImplementedException();
    }
}
