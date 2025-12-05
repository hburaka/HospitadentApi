using HospitadentApi.Entity;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace HospitadentApi.Repository
{
    public class URoleRepository : IRepository<URole>
    {
        private readonly string _connectionString;
        private readonly ILogger<URoleRepository> _logger;

        public URoleRepository(string connectionString, ILogger<URoleRepository> logger)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string must be provided.", nameof(connectionString));
            _connectionString = connectionString;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public URole? Load(int Id)
        {
            _logger.LogDebug("Load called: Id={Id}", Id);
            try
            {
                using var db = new DBHelper(_connectionString);
                db.ParametreEkle("@Id", Id);

                using var rd = db.ExecuteReaderSql("select * from user_roles where id = @Id and isDeleted=0");
                if (!rd.Read())
                {
                    _logger.LogInformation("URole not found: Id={Id}", Id);
                    return null;
                }

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

                _logger.LogInformation("Loaded URole Id={Id} Name={Name}", item.Id, item.Name);
                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading URole Id={Id}", Id);
                throw new Exception($"Error loading URole with Id {Id}", ex);
            }
        }

        public IList<URole> LoadAll()
        {
            _logger.LogDebug("LoadAll called");
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

                _logger.LogInformation("LoadAll returned {Count} roles", list.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading URole list");
                throw new Exception("Error loading URole list", ex);
            }

            return list;
        }

        public int Delete(URole instance)
        {
            _logger.LogDebug("Delete called: Id={Id}", instance?.Id);
            throw new NotImplementedException();
        }
        public int Insert(URole instance)
        {
            _logger.LogDebug("Insert called");
            throw new NotImplementedException();
        }
        public int Update(URole instance)
        {
            _logger.LogDebug("Update called: Id={Id}", instance?.Id);
            throw new NotImplementedException();
        }
    }
}