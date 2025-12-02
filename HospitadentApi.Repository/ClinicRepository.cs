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

        public int Delete(Clinic instance) => throw new NotImplementedException();
        public int Insert(Clinic instance) => throw new NotImplementedException();

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

        public IList<Clinic> LoadAll() => throw new NotImplementedException();
        public int Update(Clinic instance) => throw new NotImplementedException();
    }
}
