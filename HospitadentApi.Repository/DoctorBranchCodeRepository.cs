using HospitadentApi.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Logging;

namespace HospitadentApi.Repository
{
    public class DoctorBranchCodeRepository : IRepository<DoctorBranchCode>
    {
        private readonly string _connectionString;
        private readonly ILogger<DoctorBranchCodeRepository> _logger;

        public DoctorBranchCodeRepository(string connectionString, ILogger<DoctorBranchCodeRepository> logger)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string must be provided.", nameof(connectionString));
            _connectionString = connectionString;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public DoctorBranchCode? Load(int Id)
        {
            _logger.LogDebug("Load called: Id={Id}", Id);
            try
            {
                using var db = new DBHelper(_connectionString);
                db.ParametreEkle("@Id", Id);

                using var rd = db.ExecuteReaderSql("select * from doctor_branch_codes where id=@Id and is_deleted=0 and is_dental=1");
                if (!rd.Read())
                {
                    _logger.LogInformation("DoctorBranchCode not found: Id={Id}", Id);
                    return null;
                }

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

                _logger.LogInformation("Loaded DoctorBranchCode Id={Id} Name={Name}", item.Id, item.Name);
                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading DoctorBranchCode Id={Id}", Id);
                throw new Exception($"Error loading DoctorBranchCode with Id {Id}", ex);
            }
        }

        public IList<DoctorBranchCode> LoadAll()
        {
            _logger.LogDebug("LoadAll called");
            var list = new List<DoctorBranchCode>();
            try
            {
                using var db = new DBHelper(_connectionString);
                using var rd = db.ExecuteReaderSql("select * from doctor_branch_codes where is_deleted=0 and is_dental=1");

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

                _logger.LogInformation("LoadAll returned {Count} doctor branch codes", list.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading DoctorBranchCode list");
                throw new Exception("Error loading DoctorBranchCode list", ex);
            }

            return list;
        }

        public int Update(DoctorBranchCode instance)
        {
            _logger.LogDebug("Update called: Id={Id}", instance?.Id);
            throw new NotImplementedException();
        }
        public int Delete(DoctorBranchCode instance)
        {
            _logger.LogDebug("Delete called: Id={Id}", instance?.Id);
            throw new NotImplementedException();
        }
        public int Insert(DoctorBranchCode instance)
        {
            _logger.LogDebug("Insert called");
            throw new NotImplementedException();
        }
    }
}
