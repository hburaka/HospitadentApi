using HospitadentApi.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;

namespace HospitadentApi.Repository
{
    public class DoctorBranchCodeRepository : IRepository<DoctorBranchCode>
    {
        private readonly string _connectionString;

        public DoctorBranchCodeRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string must be provided.", nameof(connectionString));
            _connectionString = connectionString;
        }

        private static DateTime? ReadNullableDate(MySqlDataReader rd, int ordinal)
        {
            var val = rd.GetValue(ordinal);
            if (val == null || val == DBNull.Value)
                return null;

            // MySqlDateTime protects against zero-date values
            if (val is MySql.Data.Types.MySqlDateTime mySqlDt)
                return mySqlDt.IsValidDateTime ? mySqlDt.GetDateTime() : (DateTime?)null;

            if (val is DateTime dt)
                return dt;

            if (DateTime.TryParse(val.ToString(), out var parsed))
                return parsed;

            return null;
        }

        public DoctorBranchCode? Load(int Id)
        {
            try
            {
                using var db = new DBHelper(_connectionString);
                db.ParametreEkle("@Id", Id);

                using var rd = db.ExecuteReaderSql("select * from doctor_branch_codes where id = @Id");
                if (!rd.Read())
                    return null;

                var ordId = rd.GetOrdinal("id");
                var ordName = rd.GetOrdinal("branch_name");
                var ordIsDeleted = rd.GetOrdinal("is_deleted");

                var item = new DoctorBranchCode();

                if (!rd.IsDBNull(ordId))
                    item.Id = rd.GetInt32(ordId);

                if (!rd.IsDBNull(ordName))
                    item.Name = rd.GetString(ordName);

                if (!rd.IsDBNull(ordIsDeleted))
                    item.IsDeleted = rd.GetBoolean(ordIsDeleted);

                return item;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading DoctorBranchCode with Id {Id}", ex);
            }
        }

        public IList<DoctorBranchCode> LoadAll()
        {
            var list = new List<DoctorBranchCode>();
            try
            {
                using var db = new DBHelper(_connectionString);
                using var rd = db.ExecuteReaderSql("select * from doctor_branch_codes where is_deleted=0 and is_dental = 1");

                // get ordinals once
                var ordId = rd.GetOrdinal("id");
                var ordName = rd.GetOrdinal("branch_name");
                var ordIsDeleted = rd.GetOrdinal("is_deleted");

                while (rd.Read())
                {
                    var item = new DoctorBranchCode();

                    if (!rd.IsDBNull(ordId))
                        item.Id = rd.GetInt32(ordId);

                    if (!rd.IsDBNull(ordName))
                        item.Name = rd.GetString(ordName);

                    if (!rd.IsDBNull(ordIsDeleted))
                        item.IsDeleted = rd.GetBoolean(ordIsDeleted);

                    list.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error loading DoctorBranchCode list", ex);
            }

            return list;
        }

        public int Update(DoctorBranchCode instance)
        {
            throw new NotImplementedException();
        }
        public int Delete(DoctorBranchCode instance)
        {
            throw new NotImplementedException();
        }

        public int Insert(DoctorBranchCode instance)
        {
            throw new NotImplementedException();
        }
    }
}
