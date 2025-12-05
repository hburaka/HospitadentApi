using HospitadentApi.Entity;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace HospitadentApi.Repository
{
    public class DepartmentRepository : IRepository<Department>
    {
        private readonly string _connectionString;
        private readonly ILogger<DepartmentRepository> _logger;

        public DepartmentRepository(string connectionString, ILogger<DepartmentRepository> logger)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string must be provided.", nameof(connectionString));
            _connectionString = connectionString;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Department? Load(int Id)
        {
            _logger.LogDebug("Load called: Id={Id}", Id);
            try
            {
                using var db = new DBHelper(_connectionString);
                db.ParametreEkle("@Id", Id);

                using var rd = db.ExecuteReaderSql("select * from user_departments where id=@Id and isDeleted=0");
                if (!rd.Read())
                {
                    _logger.LogInformation("Department not found: Id={Id}", Id);
                    return null;
                }

                var _department = new Department();
                _department.Id = rd.GetInt32("id");
                _department.Name = rd.GetString("department_name");
                _department.IsDeleted = rd.GetBoolean("isDeleted");

                _logger.LogInformation("Loaded Department Id={Id} Name={Name}", _department.Id, _department.Name);
                return _department;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading Department Id={Id}", Id);
                throw new Exception($"Error loading Department with Id {Id}", ex);
            }
        }

        public IList<Department> LoadAll()
        {
            _logger.LogDebug("LoadAll called");
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

                _logger.LogInformation("LoadAll returned {Count} departments", department.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading departments");
                throw new Exception("Error loading department", ex);
            }
            return department;
        }

        public int Delete(Department instance) => throw new NotImplementedException();
        public int Insert(Department instance) => throw new NotImplementedException();
        public int Update(Department instance) => throw new NotImplementedException();
    }
}
