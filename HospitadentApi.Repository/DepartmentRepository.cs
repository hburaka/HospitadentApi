using HospitadentApi.Entity;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HospitadentApi.Repository
{
    public class DepartmentRepository : IRepository<Department>
    {
        private readonly string _connectionString;

        public DepartmentRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string must be provided.", nameof(connectionString));
            _connectionString = connectionString;
        }

        public Department? Load(int Id)
        {
            DBHelper db = new DBHelper(_connectionString);
            db.ParametreEkle("@Id", Id);

            MySqlDataReader rd = db.ExecuteReaderSql("select * from user_departments where id=@Id and isDeleted=0");

            Department _department = null;
            if (rd.Read())
            {
                _department = new Department();
                _department.Id = rd.GetInt32("id");
                _department.Name = rd.GetString("department_name");
                _department.IsDeleted = rd.GetBoolean("isDeleted");
            }
            rd.Close();
            return _department;
        }

        public IList<Department> LoadAll()
        {
            var department = new List<Department>();
            using var db = new DBHelper(_connectionString);
            try
            {
                using var rd = db.ExecuteReaderSql("select * from user_departments where isDeleted=0");
                int ordId = rd.GetOrdinal("id");
                int ordName = rd.GetOrdinal("department_name");
                int ordIsDeleted = rd.GetOrdinal("isDeleted");

                while (rd.Read())
                {
                    var d = new Department
                    {
                        Id = rd.IsDBNull(ordId) ? 0 : rd.GetInt32(ordId),
                        Name = rd.IsDBNull(ordName) ? string.Empty : rd.GetString(ordName),
                        IsDeleted = rd.IsDBNull(ordIsDeleted) ? false : rd.GetBoolean(ordIsDeleted)
                    };
                    department.Add(d);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error loading department", ex);
            }
            return department;
        }

        public int Insert(Department instance)
        {
            throw new NotImplementedException();
        }

        public int Delete(Department instance)
        {
            throw new NotImplementedException();
        }

        public int Update(Department instance)
        {
            throw new NotImplementedException();
        }
    }
}
