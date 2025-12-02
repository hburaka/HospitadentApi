using HospitadentApi.Entity;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HospitadentApi.Repository
{
    public class UserRepository : IRepository<User>
    {
        private readonly string _connectionString;

        public UserRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string must be provided.", nameof(connectionString));
            _connectionString = connectionString;
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
                      u.allowed_clinics,
                      u.department,
                      u.role,
                      u.show_in_calendar
                    FROM users u
                    WHERE u.id = @Id AND u.isDeleted = 0
                    ");
                if (!rd.Read())
                    return null;

                var ordName = rd.GetOrdinal("Name");
                var ordLastName = rd.GetOrdinal("LastName");
                var ordUserType = rd.GetOrdinal("user_type");
                var ordClinicId = rd.GetOrdinal("clinic_id");
                var ordAllowedClinics = rd.GetOrdinal("allowed_clinics");
                var ordDepartment = rd.GetOrdinal("department");
                var ordRole = rd.GetOrdinal("role");
                var ordShowInCalendar = rd.GetOrdinal("show_in_calendar");

                var item = new User();

                if (!rd.IsDBNull(ordName))
                    item.Name = rd.GetString(ordName);

                if (!rd.IsDBNull(ordLastName))
                    item.LastName = rd.GetString(ordLastName);

                if (!rd.IsDBNull(ordUserType))
                    item.UserType = rd.GetInt32(ordUserType);

                if (!rd.IsDBNull(ordClinicId))
                {
                    var clinicId = rd.GetInt32(ordClinicId);
                    item.Clinic = new Clinic { Id = clinicId };
                }

                if (!rd.IsDBNull(ordAllowedClinics))
                {
                    var csv = rd.GetString(ordAllowedClinics);
                    item.AllowedClinic = csv;
                    var ids = ParseIdList(csv);
                    item.AllowedClinics = ids.Select(i => new Clinic { Id = i }).ToList();

                    // OPTIONAL: if you need full Clinic objects (name, address, etc.)
                    // you can query the clinics table here using the ids and replace AllowedClinics with full objects.
                }

                if (!rd.IsDBNull(ordDepartment))
                {
                    var deptId = rd.GetInt32(ordDepartment);
                    item.Department = new Department { Id = deptId };
                }

                if (!rd.IsDBNull(ordRole))
                {
                    var roleId = rd.GetInt32(ordRole);
                    item.URole = new URole { Id = roleId };
                }

                if (!rd.IsDBNull(ordShowInCalendar))
                    item.ShowInCalender = rd.GetInt32(ordShowInCalendar);

                return item;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading User with Id {Id}", ex);
            }
        }

        public IList<User> LoadAll()
        {
            var list = new List<User>();
            try
            {
                using var db = new DBHelper(_connectionString);
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
  u.allowed_clinics,
  u.department,
  u.role,
  u.show_in_calendar
FROM users u
WHERE u.isDeleted = 0
");

                var ordName = rd.GetOrdinal("Name");
                var ordLastName = rd.GetOrdinal("LastName");
                var ordUserType = rd.GetOrdinal("user_type");
                var ordClinicId = rd.GetOrdinal("clinic_id");
                var ordAllowedClinics = rd.GetOrdinal("allowed_clinics");
                var ordDepartment = rd.GetOrdinal("department");
                var ordRole = rd.GetOrdinal("role");
                var ordShowInCalendar = rd.GetOrdinal("show_in_calendar");

                while (rd.Read())
                {
                    var item = new User();

                    if (!rd.IsDBNull(ordName))
                        item.Name = rd.GetString(ordName);

                    if (!rd.IsDBNull(ordLastName))
                        item.LastName = rd.GetString(ordLastName);

                    if (!rd.IsDBNull(ordUserType))
                        item.UserType = rd.GetInt32(ordUserType);

                    if (!rd.IsDBNull(ordClinicId))
                    {
                        var clinicId = rd.GetInt32(ordClinicId);
                        item.Clinic = new Clinic { Id = clinicId };
                    }

                    if (!rd.IsDBNull(ordAllowedClinics))
                    {
                        var csv = rd.GetString(ordAllowedClinics);
                        item.AllowedClinic = csv;
                        var ids = ParseIdList(csv);
                        item.AllowedClinics = ids.Select(i => new Clinic { Id = i }).ToList();
                    }

                    if (!rd.IsDBNull(ordDepartment))
                    {
                        var deptId = rd.GetInt32(ordDepartment);
                        item.Department = new Department { Id = deptId };
                    }

                    if (!rd.IsDBNull(ordRole))
                    {
                        var roleId = rd.GetInt32(ordRole);
                        item.URole = new URole { Id = roleId };
                    }

                    if (!rd.IsDBNull(ordShowInCalendar))
                        item.ShowInCalender = rd.GetInt32(ordShowInCalendar);

                    list.Add(item);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error loading User list", ex);
            }

            return list;
        }

        public int Update(User instance)
        {
            throw new NotImplementedException();
        }

        public int Delete(User instance)
        {
            throw new NotImplementedException();
        }

        public int Insert(User instance)
        {
            throw new NotImplementedException();
        }
    }
}