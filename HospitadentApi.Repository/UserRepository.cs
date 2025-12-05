                    using HospitadentApi.Entity;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;

namespace HospitadentApi.Repository
{
    public class UserRepository : IRepository<User>
    {
        private readonly ClinicRepository _clinicRepo;
        private readonly string _connectionString;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(string connectionString, ClinicRepository clinicRepo, ILogger<UserRepository> logger)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string must be provided.", nameof(connectionString));
            _connectionString = connectionString;
            _clinicRepo = clinicRepo ?? throw new ArgumentNullException(nameof(clinicRepo));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        private static List<int> ParseIdList(string? csv)
        {
            var result = new List<int>();
            if (string.IsNullOrWhiteSpace(csv))
                return result;

            foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (int.TryParse(part, out var id))
                    result.Add(id);
            }
            return result;
        }

        public User? Load(int Id)
        {
            _logger.LogDebug("Load called: Id={Id}", Id);
            try
            {
                using var db = new DBHelper(_connectionString);
                db.ParametreEkle("@Id", Id);

                using var rd = db.ExecuteReaderSql(@"
                    SELECT
                      CASE
                        WHEN u.middle_name IS NULL OR u.middle_name = '' OR u.middle_name = '0' THEN u.first_name
                        WHEN CONCAT(' ', u.first_name, ' ') LIKE CONCAT('% ', u.middle_name, ' %') THEN u.first_name
                        ELSE CONCAT(u.first_name, ' ', u.middle_name)
                      END AS `Name`,
                      u.last_name AS `LastName`,
                      u.user_type,
                      u.clinic_id,
                      c.clinic_name AS clinic_name,
                      c.status AS clinic_status,
                      c.isDeleted AS clinic_isDeleted,
                      u.allowed_clinics,
                      u.department,
                      d.department_name AS department_name,
                      d.isDeleted AS department_isDeleted,
                      u.role,
                      r.roleName AS role_name,
                      r.department_id AS role_department_id,
                      u.show_in_calendar
                    FROM users u
                    LEFT JOIN clinics c ON u.clinic_id = c.id AND c.isDeleted = 0
                    LEFT JOIN user_departments d ON u.department = d.id AND d.isDeleted = 0
                    LEFT JOIN user_roles r ON u.role = r.id AND r.isDeleted = 0
                    WHERE u.id = @Id AND u.isDeleted = 0
                    ");

                if (!rd.Read())
                {
                    _logger.LogInformation("User not found: Id={Id}", Id);
                    return null;
                }

                var ordName = rd.GetOrdinal("Name");
                var ordLastName = rd.GetOrdinal("LastName");
                var ordUserType = rd.GetOrdinal("user_type");
                var ordClinicId = rd.GetOrdinal("clinic_id");
                var ordClinicName = rd.GetOrdinal("clinic_name");
                var ordClinicStatus = rd.GetOrdinal("clinic_status");
                var ordClinicIsDeleted = rd.GetOrdinal("clinic_isDeleted");
                var ordAllowedClinics = rd.GetOrdinal("allowed_clinics");
                var ordDepartment = rd.GetOrdinal("department");
                var ordDepartmentName = rd.GetOrdinal("department_name");
                var ordDepartmentIsDeleted = rd.GetOrdinal("department_isDeleted");
                var ordRole = rd.GetOrdinal("role");
                var ordRoleName = rd.GetOrdinal("role_name");
                var ordRoleDeptId = rd.GetOrdinal("role_department_id");
                var ordShowInCalendar = rd.GetOrdinal("show_in_calendar");

                var item = new User();

                if (!rd.IsDBNull(ordName))
                    item.Name = rd.GetString(ordName);

                if (!rd.IsDBNull(ordLastName))
                    item.LastName = rd.GetString(ordLastName);

                if (!rd.IsDBNull(ordUserType))
                    item.UserType = rd.GetInt32(ordUserType);

                // Clinic (full object from joined table if available)
                if (!rd.IsDBNull(ordClinicId))
                {
                    var clinicId = rd.GetInt32(ordClinicId);
                    var clinic = new Clinic { Id = clinicId };

                    if (!rd.IsDBNull(ordClinicName))
                        clinic.Name = rd.GetString(ordClinicName);

                    if (!rd.IsDBNull(ordClinicStatus))
                        clinic.Status = rd.GetBoolean(ordClinicStatus);

                    if (!rd.IsDBNull(ordClinicIsDeleted))
                        clinic.IsDeleted = rd.GetBoolean(ordClinicIsDeleted);

                    item.Clinic = clinic;
                }

                if (!rd.IsDBNull(ordAllowedClinics))
                {
                    var csv = rd.GetString(ordAllowedClinics);
                    item.AllowedClinic = csv;
                    var ids = ParseIdList(csv);
                    item.AllowedClinics = _clinicRepo.GetByIds(ids).ToList();
                }

                // Department (full object from joined table if available)
                if (!rd.IsDBNull(ordDepartment))
                {
                    var deptId = rd.GetInt32(ordDepartment);
                    var dept = new Department { Id = deptId };

                    if (!rd.IsDBNull(ordDepartmentName))
                        dept.Name = rd.GetString(ordDepartmentName);

                    if (!rd.IsDBNull(ordDepartmentIsDeleted))
                        dept.IsDeleted = rd.GetBoolean(ordDepartmentIsDeleted);

                    item.Department = dept;
                }

                // Role (full object from joined table if available)
                if (!rd.IsDBNull(ordRole))
                {
                    var roleId = rd.GetInt32(ordRole);
                    var role = new URole { Id = roleId };

                    if (!rd.IsDBNull(ordRoleName))
                        role.Name = rd.GetString(ordRoleName);

                    if (!rd.IsDBNull(ordRoleDeptId))
                    {
                        var roleDeptId = rd.GetInt32(ordRoleDeptId);
                        role.Department = new Department { Id = roleDeptId };
                    }

                    item.URole = role;
                }

                if (!rd.IsDBNull(ordShowInCalendar))
                    item.ShowInCalender = rd.GetInt32(ordShowInCalendar);

                _logger.LogInformation("Loaded user Id={Id} Name={Name}", item.Id, item.Name);
                return item;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading User Id={Id}", Id);
                throw new Exception($"Error loading User with Id {Id}", ex);
            }
        }

        public IList<User> LoadAll()
        {
            _logger.LogDebug("LoadAll called");
            var list = new List<User>();
            try
            {
                using var db = new DBHelper(_connectionString);
                using var rd = db.ExecuteReaderSql(@"
                    SELECT
                      u.id,
                      CASE
                        WHEN u.middle_name IS NULL OR u.middle_name = '' OR u.middle_name = '0' THEN u.first_name
                        WHEN CONCAT(' ', u.first_name, ' ') LIKE CONCAT('% ', u.middle_name, ' %') THEN u.first_name
                        ELSE CONCAT(u.first_name, ' ', u.middle_name)
                      END AS `Name`,
                      u.last_name AS `LastName`,
                      u.user_type,
                      u.clinic_id,
                      c.clinic_name AS clinic_name,
                      c.status AS clinic_status,
                      c.isDeleted AS clinic_isDeleted,
                      u.allowed_clinics,
                      u.department,
                      d.department_name AS department_name,
                      d.isDeleted AS department_isDeleted,
                      u.role,
                      r.roleName AS role_name,
                      r.department_id AS role_department_id,
                      u.show_in_calendar,
                      u.is_active
                    FROM users u
                    LEFT JOIN clinics c ON u.clinic_id = c.id AND c.isDeleted = 0
                    LEFT JOIN user_departments d ON u.department = d.id AND d.isDeleted = 0
                    LEFT JOIN user_roles r ON u.role = r.id AND r.isDeleted = 0
                    WHERE u.isDeleted = 0
                    ");

                var ordId = rd.GetOrdinal("id");
                var ordName = rd.GetOrdinal("Name");
                var ordLastName = rd.GetOrdinal("LastName");
                var ordUserType = rd.GetOrdinal("user_type");
                var ordClinicId = rd.GetOrdinal("clinic_id");
                var ordClinicName = rd.GetOrdinal("clinic_name");
                var ordClinicStatus = rd.GetOrdinal("clinic_status");
                var ordClinicIsDeleted = rd.GetOrdinal("clinic_isDeleted");
                var ordAllowedClinics = rd.GetOrdinal("allowed_clinics");
                var ordDepartment = rd.GetOrdinal("department");
                var ordDepartmentName = rd.GetOrdinal("department_name");
                var ordDepartmentIsDeleted = rd.GetOrdinal("department_isDeleted");
                var ordRole = rd.GetOrdinal("role");
                var ordRoleName = rd.GetOrdinal("role_name");
                var ordRoleDeptId = rd.GetOrdinal("role_department_id");
                var ordShowInCalendar = rd.GetOrdinal("show_in_calendar");
                var ordIsActive = rd.GetOrdinal("is_active");

                while (rd.Read())
                {
                    var item = new User();

                    if (!rd.IsDBNull(ordId))
                        item.Id = rd.GetInt32(ordId);

                    if (!rd.IsDBNull(ordName))
                        item.Name = rd.GetString(ordName);

                    if (!rd.IsDBNull(ordLastName))
                        item.LastName = rd.GetString(ordLastName);

                    if (!rd.IsDBNull(ordUserType))
                        item.UserType = rd.GetInt32(ordUserType);

                    // Clinic (full object from joined table if available)
                    if (!rd.IsDBNull(ordClinicId))
                    {
                        var clinicId = rd.GetInt32(ordClinicId);
                        var clinic = new Clinic { Id = clinicId };

                        if (!rd.IsDBNull(ordClinicName))
                            clinic.Name = rd.GetString(ordClinicName);

                        if (!rd.IsDBNull(ordClinicStatus))
                            clinic.Status = rd.GetBoolean(ordClinicStatus);

                        if (!rd.IsDBNull(ordClinicIsDeleted))
                            clinic.IsDeleted = rd.GetBoolean(ordClinicIsDeleted);

                        item.Clinic = clinic;
                    }

                    if (!rd.IsDBNull(ordAllowedClinics))
                    {
                        var csv = rd.GetString(ordAllowedClinics);
                        item.AllowedClinic = csv;
                        var ids = ParseIdList(csv);
                        item.AllowedClinics = _clinicRepo.GetByIds(ids).ToList();
                    }

                    // Department (full object from joined table if available)
                    if (!rd.IsDBNull(ordDepartment))
                    {
                        var deptId = rd.GetInt32(ordDepartment);
                        var dept = new Department { Id = deptId };

                        if (!rd.IsDBNull(ordDepartmentName))
                            dept.Name = rd.GetString(ordDepartmentName);

                        if (!rd.IsDBNull(ordDepartmentIsDeleted))
                            dept.IsDeleted = rd.GetBoolean(ordDepartmentIsDeleted);

                        item.Department = dept;
                    }

                    // Role (full object from joined table if available)
                    if (!rd.IsDBNull(ordRole))
                    {
                        var roleId = rd.GetInt32(ordRole);
                        var role = new URole { Id = roleId };

                        if (!rd.IsDBNull(ordRoleName))
                            role.Name = rd.GetString(ordRoleName);

                        if (!rd.IsDBNull(ordRoleDeptId))
                            role.Department = new Department { Id = rd.GetInt32(ordRoleDeptId) };

                        item.URole = role;
                    }

                    if (!rd.IsDBNull(ordShowInCalendar))
                        item.ShowInCalender = rd.GetInt32(ordShowInCalendar);

                    if (!rd.IsDBNull(ordIsActive))
                        item.IsActive = rd.GetBoolean(ordIsActive);

                    list.Add(item);
                }

                _logger.LogInformation("LoadAll returned {Count} users", list.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading User list");
                throw new Exception("Error loading User list", ex);
            }

            return list;
        }

        public IList<User> GetByCriteria(int Id, string? Name, bool? IsActive, bool? IsDeleted)
        {
            _logger.LogDebug("GetByCriteria called: Id={Id} Name={Name} IsActive={IsActive} IsDeleted={IsDeleted}", Id, Name, IsActive, IsDeleted);
            var result = new List<User>();
            using var db = new DBHelper(_connectionString);

            var sb = new StringBuilder();
            sb.Append(@"
                SELECT
                  u.id,
                  CASE
                    WHEN u.middle_name IS NULL OR u.middle_name = '' OR u.middle_name = '0' THEN u.first_name
                    WHEN CONCAT(' ', u.first_name, ' ') LIKE CONCAT('% ', u.middle_name, ' %') THEN u.first_name
                    ELSE CONCAT(u.first_name, ' ', u.middle_name)
                  END AS `Name`,
                  u.last_name AS `LastName`,
                  u.user_type,
                  u.clinic_id,
                  c.clinic_name AS clinic_name,
                  c.status AS clinic_status,
                  c.isDeleted AS clinic_isDeleted,
                  u.allowed_clinics,
                  u.department,
                  d.department_name AS department_name,
                  d.isDeleted AS department_isDeleted,
                  u.role,
                  r.roleName AS role_name,
                  r.department_id AS role_department_id,
                  u.show_in_calendar,
                  u.is_active
                FROM users u
                LEFT JOIN clinics c ON u.clinic_id = c.id AND c.isDeleted = 0
                LEFT JOIN user_departments d ON u.department = d.id AND d.isDeleted = 0
                LEFT JOIN user_roles r ON u.role = r.id AND r.isDeleted = 0
                WHERE u.isDeleted = 0
                ");

            if (Id != 0)
            {
                db.ParametreEkle("@Id", Id);
                sb.Append(" AND u.id = @Id");
            }

            if (!string.IsNullOrEmpty(Name))
            {
                db.ParametreEkle("@Name", Name);
                sb.Append(" AND (u.first_name LIKE CONCAT('%', @Name, '%') OR u.last_name LIKE CONCAT('%', @Name, '%') OR CONCAT(u.first_name, ' ', u.middle_name) LIKE CONCAT('%', @Name, '%'))");
            }

            if (IsActive.HasValue)
            {
                db.ParametreEkle("@IsActive", IsActive.Value ? 1 : 0);
                sb.Append(" AND u.is_active = @IsActive");
            }

            if (IsDeleted.HasValue)
            {
                db.ParametreEkle("@IsDeleted", IsDeleted.Value ? 1 : 0);
                sb.Append(" AND u.isDeleted = @IsDeleted");
            }

            sb.Append(" ORDER BY `Name`");

            using var rd = db.ExecuteReaderSql(sb.ToString());
            if (rd == null)
            {
                _logger.LogInformation("GetByCriteria returned no rows");
                return result;
            }

            int ordId = rd.GetOrdinal("id");
            int ordName = rd.GetOrdinal("Name");
            int ordLastName = rd.GetOrdinal("LastName");
            int ordUserType = rd.GetOrdinal("user_type");
            int ordClinicId = rd.GetOrdinal("clinic_id");
            int ordClinicName = rd.GetOrdinal("clinic_name");
            int ordClinicStatus = rd.GetOrdinal("clinic_status");
            int ordClinicIsDeleted = rd.GetOrdinal("clinic_isDeleted");
            int ordAllowedClinics = rd.GetOrdinal("allowed_clinics");
            int ordDepartment = rd.GetOrdinal("department");
            int ordDepartmentName = rd.GetOrdinal("department_name");
            int ordDepartmentIsDeleted = rd.GetOrdinal("department_isDeleted");
            int ordRole = rd.GetOrdinal("role");
            int ordRoleName = rd.GetOrdinal("role_name");
            int ordRoleDeptId = rd.GetOrdinal("role_department_id");
            int ordShowInCalendar = rd.GetOrdinal("show_in_calendar");
            int ordIsActive = rd.GetOrdinal("is_active");

            while (rd.Read())
            {
                var u = new User();

                if (!rd.IsDBNull(ordId))
                    u.Id = rd.GetInt32(ordId);

                if (!rd.IsDBNull(ordName))
                    u.Name = rd.GetString(ordName);

                if (!rd.IsDBNull(ordLastName))
                    u.LastName = rd.GetString(ordLastName);

                if (!rd.IsDBNull(ordUserType))
                    u.UserType = rd.GetInt32(ordUserType);

                // Clinic (full object)
                if (!rd.IsDBNull(ordClinicId))
                {
                    var clinicId = rd.GetInt32(ordClinicId);
                    var clinic = new Clinic { Id = clinicId };

                    if (!rd.IsDBNull(ordClinicName))
                        clinic.Name = rd.GetString(ordClinicName);

                    if (!rd.IsDBNull(ordClinicStatus))
                        clinic.Status = rd.GetBoolean(ordClinicStatus);

                    if (!rd.IsDBNull(ordClinicIsDeleted))
                        clinic.IsDeleted = rd.GetBoolean(ordClinicIsDeleted);

                    u.Clinic = clinic;
                }

                if (!rd.IsDBNull(ordAllowedClinics))
                {
                    var csv = rd.GetString(ordAllowedClinics);
                    u.AllowedClinic = csv;
                    var ids = ParseIdList(csv);
                    u.AllowedClinics = _clinicRepo.GetByIds(ids).ToList();
                }

                // Department (full object)
                if (!rd.IsDBNull(ordDepartment))
                {
                    var deptId = rd.GetInt32(ordDepartment);
                    var dept = new Department { Id = deptId };

                    if (!rd.IsDBNull(ordDepartmentName))
                        dept.Name = rd.GetString(ordDepartmentName);

                    if (!rd.IsDBNull(ordDepartmentIsDeleted))
                        dept.IsDeleted = rd.GetBoolean(ordDepartmentIsDeleted);

                    u.Department = dept;
                }

                // Role (full object)
                if (!rd.IsDBNull(ordRole))
                {
                    var roleId = rd.GetInt32(ordRole);
                    var role = new URole { Id = roleId };

                    if (!rd.IsDBNull(ordRoleName))
                        role.Name = rd.GetString(ordRoleName);

                    if (!rd.IsDBNull(ordRoleDeptId))
                        role.Department = new Department { Id = rd.GetInt32(ordRoleDeptId) };

                    u.URole = role;
                }

                if (!rd.IsDBNull(ordShowInCalendar))
                    u.ShowInCalender = rd.GetInt32(ordShowInCalendar);

                if (!rd.IsDBNull(ordIsActive))
                    u.IsActive = rd.GetBoolean(ordIsActive);

                result.Add(u);
            }

            _logger.LogInformation("GetByCriteria returning {Count} users", result.Count);
            return result;
        }

        public int Delete(User instance) => throw new NotImplementedException();
        public int Insert(User instance) => throw new NotImplementedException();
        public int Update(User instance) => throw new NotImplementedException();
    }
}