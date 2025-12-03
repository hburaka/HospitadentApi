using HospitadentApi.Entity;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HospitadentApi.Repository
{
    public class URoleRepository : IRepository<URole>
    {
        private readonly string _connectionString;

        public URoleRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string must be provided.", nameof(connectionString));
            _connectionString = connectionString;
        }

        public URole? Load(int Id)
        {
            try
            {
                using var db = new DBHelper(_connectionString);
                db.ParametreEkle("@Id", Id);

                using var rd = db.ExecuteReaderSql("select * from user_roles where id = @Id and isDeleted=0");
                if (!rd.Read())
                    return null;

                var ordId = rd.GetOrdinal("id");
                var ordName = rd.GetOrdinal("roleName");
                var ordDepartmentId = rd.GetOrdinal("department_id");
                var ordIsDeleted = rd.GetOrdinal("isDeleted");

                var item = new URole();

                if (!rd.IsDBNull(ordId))
                    item.Id = rd.GetInt32(ordId);

                if (!rd.IsDBNull(ordName))
                    item.Name = rd.GetString(ordName);

                if (!rd.IsDBNull(ordDepartmentId))
                {
                    var deptId = rd.GetInt32(ordDepartmentId);
                    item.Department = new Department { Id = deptId };
                }
                return item;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading URole with Id {Id}", ex);
            }
        }

        public IList<URole> LoadAll()
        {
            var list = new List<URole>();
            try
            {
                using var db = new DBHelper(_connectionString);
                using var rd = db.ExecuteReaderSql("select * from user_roles where isDeleted=0");

                var ordId = rd.GetOrdinal("id");
                var ordName = rd.GetOrdinal("roleName");
                var ordDepartmentId = rd.GetOrdinal("department_id");
                var ordIsDeleted = rd.GetOrdinal("isDeleted");

                while (rd.Read())
                {
                    var item = new URole();

                    if (!rd.IsDBNull(ordId))
                        item.Id = rd.GetInt32(ordId);

                    if (!rd.IsDBNull(ordName))
                        item.Name = rd.GetString(ordName);

                    if (!rd.IsDBNull(ordDepartmentId))
                    {
                        var deptId = rd.GetInt32(ordDepartmentId);
                        item.Department = new Department { Id = deptId };
                    }

                    list.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error loading URole list", ex);
            }

            return list;
        }

        public int Delete(URole instance) => throw new NotImplementedException();
        public int Insert(URole instance) => throw new NotImplementedException();
        public int Update(URole instance) => throw new NotImplementedException();
    }
}