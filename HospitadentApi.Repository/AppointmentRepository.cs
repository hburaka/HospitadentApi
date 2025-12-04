using System;
using System.Collections.Generic;
using HospitadentApi.Entity;
using MySql.Data.MySqlClient;

namespace HospitadentApi.Repository
{
    public class AppointmentRepository : IRepository<Appointment>
    {
        private readonly string _connectionString;

        public AppointmentRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string must be provided.", nameof(connectionString));
            _connectionString = connectionString;
        }
        public IList<Appointment> GetByDoctorAndDateRange(int doctorId, DateTime from, DateTime to)
        {
            if (doctorId <= 0)
                throw new ArgumentException("doctorId must be greater than zero.", nameof(doctorId));

            if (to < from)
                to = from;

            var list = new List<Appointment>();

            try
            {
                using var db = new DBHelper(_connectionString);
                db.ParametreEkle("@DoctorId", doctorId);
                db.ParametreEkle("@FromDate", from);
                db.ParametreEkle("@ToDate", to);

                // Temel alanları seçiyoruz; ihtiyaç oldukça genişletebiliriz.
                var sql = @"
                    SELECT
                        id,
                        company_id,
                        clinic_id,
                        doctor_id,
                        patient_id,
                        start_date,
                        end_date,
                        appointment_status,
                        appointment_type,
                        treatment_type,
                        description
                    FROM appointments
                    WHERE doctor_id = @DoctorId
                      AND start_date >= @FromDate
                      AND start_date <= @ToDate
                    ORDER BY start_date
                ";

                using var rd = db.ExecuteReaderSql(sql);
                if (rd == null)
                    return list;

                int ordId = rd.GetOrdinal("id");
                int ordCompanyId = rd.GetOrdinal("company_id");
                int ordClinicId = rd.GetOrdinal("clinic_id");
                int ordDoctorId = rd.GetOrdinal("doctor_id");
                int ordPatientId = rd.GetOrdinal("patient_id");
                int ordStartDate = rd.GetOrdinal("start_date");
                int ordEndDate = rd.GetOrdinal("end_date");
                int ordStatus = rd.GetOrdinal("appointment_status");
                int ordType = rd.GetOrdinal("appointment_type");
                int ordTreatmentType = rd.GetOrdinal("treatment_type");
                int ordDescription = rd.GetOrdinal("description");

                while (rd.Read())
                {
                    var ent = new Appointment
                    {
                        Id = rd.IsDBNull(ordId) ? 0 : rd.GetInt32(ordId),
                        CompanyId = rd.IsDBNull(ordCompanyId) ? 0L : rd.GetInt64(ordCompanyId),
                        ClinicId = rd.IsDBNull(ordClinicId) ? 0 : rd.GetInt32(ordClinicId),
                        DoctorId = rd.IsDBNull(ordDoctorId) ? 0 : rd.GetInt32(ordDoctorId),
                        PatientId = rd.IsDBNull(ordPatientId) ? null : rd.GetInt32(ordPatientId),
                        AppointmentStatus = rd.IsDBNull(ordStatus) ? (byte)0 : rd.GetByte(ordStatus),
                        AppointmentType = rd.IsDBNull(ordType) ? (byte)0 : rd.GetByte(ordType),
                        TreatmentType = rd.IsDBNull(ordTreatmentType) ? (byte)0 : rd.GetByte(ordTreatmentType),
                        Description = rd.IsDBNull(ordDescription) ? null : rd.GetString(ordDescription)
                    };

                    // tarih alanları için helper kullan
                    ent.StartDate = Tools.SafeGetNullableDate(rd, ordStartDate) ?? DateTime.MinValue;
                    ent.EndDate = Tools.SafeGetNullableDate(rd, ordEndDate) ?? ent.StartDate;

                    list.Add(ent);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error loading appointments for doctor {doctorId} between {from} and {to}", ex);
            }

            return list;
        }

        // IRepository zorunlu üyeleri – şimdilik ihtiyaç yoksa NotImplemented bırakıyoruz.
        public int Insert(Appointment instance) => throw new NotImplementedException();
        public int Delete(Appointment instance) => throw new NotImplementedException();
        public int Update(Appointment instance) => throw new NotImplementedException();
        public Appointment? Load(int Id) => throw new NotImplementedException();
        public IList<Appointment> LoadAll() => throw new NotImplementedException();
    }
}


