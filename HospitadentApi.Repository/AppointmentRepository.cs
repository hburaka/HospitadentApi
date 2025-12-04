using HospitadentApi.Entity;
using System.Text;

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

        /// <summary>
        /// Query appointments with optional filters. Matches the Appointment entity shape.
        /// clinicId uses equality (a.clinic_id = @clinic_id) as requested.
        /// </summary>
        public IList<Appointment> GetByCriteria(
            int? doctorId = null,
            DateTime? from = null,
            DateTime? to = null,
            int? clinicId = null,
            int? appointmentStatus = null,
            int? appointmentType = null,
            int? treatmentType = null,
            bool? isConfirmed = null)
        {
            var result = new List<Appointment>();

            var start = from?.Date ?? Tools.GetTurkiyeDate().Date;
            var end = to?.Date ?? start;
            if (end < start) end = start;

            try
            {
                using var db = new DBHelper(_connectionString);
                db.ParametreEkle("@FromDate", start);
                db.ParametreEkle("@ToDate", end);

                var sb = new StringBuilder();
                sb.AppendLine(@"
                    SELECT
                        a.id,
                        c.id AS clinic_id,
                        c.clinic_name,
                        a.doctor_id,
                        usrdoctor.first_name as doctorFirstName,
                        usrdoctor.last_name as doctorLastName,
                        a.patient_id,
                        p.first_name as patientName,
                        p.last_name as patientLastName,
                        a.appointment_type,
                        att.type_name AS appointment_type_name,
                        a.appointment_status,
                        ass.status_name AS appointment_status_name,
                        a.treatment_type,
                        tt.treatment_type_name AS treatment_type_name,
                        a.is_allday,
                        a.start_date,
                        a.end_date,
                        a.description,
                        a.is_emergency,
                        a.is_urgent,
                        a.is_confirmed,
                        a.confirmed_by,
                        a.confirmed_on,
                        a.postponed_saved_by,
                        a.postponed_saved_on,
                        a.cancelled_saved_by,
                        a.cancelled_saved_on,
                        a.protocol_no,
                        a.status,
                        a.saved_by,
                        a.saved_on,
                        a.updated_by,
                        a.updated_on,
                        a.deleted_by,
                        a.deleted_on
                    FROM appointments a
                    LEFT JOIN clinics c ON a.clinic_id = c.id
                    LEFT JOIN appointment_types att ON a.appointment_type = att.id
                    LEFT JOIN appointment_status ass ON a.appointment_status = ass.id
                    LEFT JOIN treatment_types tt ON a.treatment_type = tt.id
                    left join users usrdoctor on a.doctor_id = usrdoctor.id
                    LEFT JOIN patients p ON a.patient_id = p.id
                    WHERE a.deleted_by = 0
                ");

                if (doctorId.HasValue)
                {
                    db.ParametreEkle("@DoctorId", doctorId.Value);
                    sb.AppendLine(" AND a.doctor_id = @DoctorId");
                }

                sb.AppendLine(" AND DATE(a.start_date) BETWEEN @FromDate AND @ToDate");

                // clinic equality filter (not IN)
                if (clinicId.HasValue)
                {
                    db.ParametreEkle("@clinic_id", clinicId.Value);
                    sb.AppendLine(" AND a.clinic_id = @clinic_id");
                }

                if (appointmentStatus.HasValue)
                {
                    db.ParametreEkle("@appointment_status", appointmentStatus.Value);
                    sb.AppendLine(" AND a.appointment_status = @appointment_status");
                }

                if (appointmentType.HasValue)
                {
                    db.ParametreEkle("@appointment_type", appointmentType.Value);
                    sb.AppendLine(" AND a.appointment_type = @appointment_type");
                }

                if (treatmentType.HasValue)
                {
                    db.ParametreEkle("@treatment_type", treatmentType.Value);
                    sb.AppendLine(" AND a.treatment_type = @treatment_type");
                }

                if (isConfirmed.HasValue)
                {
                    db.ParametreEkle("@is_confirmed", isConfirmed.Value ? 1 : 0);
                    sb.AppendLine(" AND a.is_confirmed = @is_confirmed");
                }

                sb.AppendLine(" ORDER BY a.start_date");

                using var rd = db.ExecuteReaderSql(sb.ToString());
                if (rd == null) return result;

                int ordId = rd.GetOrdinal("id");
                int ordClinicId = rd.GetOrdinal("clinic_id");
                int ordClinicName = rd.GetOrdinal("clinic_name");
                int ordDoctorId = rd.GetOrdinal("doctor_id");
                int ordDoctorFirstName = rd.GetOrdinal("doctorFirstName");
                int ordDoctorLastName = rd.GetOrdinal("doctorLastName");
                int ordPatientId = rd.GetOrdinal("patient_id");
                int ordpatientName = rd.GetOrdinal("patientName");
                int ordpatientLastName = rd.GetOrdinal("patientLastName");
                int ordAppointmentType = rd.GetOrdinal("appointment_type");
                int ordAppointmentTypeName = rd.GetOrdinal("appointment_type_name");
                int ordAppointmentStatus = rd.GetOrdinal("appointment_status");
                int ordAppointmentStatusName = rd.GetOrdinal("appointment_status_name");
                int ordTreatmentType = rd.GetOrdinal("treatment_type");
                int ordTreatmentTypeName = rd.GetOrdinal("treatment_type_name");
                int ordIsAllDay = rd.GetOrdinal("is_allday");
                int ordStartDate = rd.GetOrdinal("start_date");
                int ordEndDate = rd.GetOrdinal("end_date");
                int ordDescription = rd.GetOrdinal("description");
                int ordIsEmergency = rd.GetOrdinal("is_emergency");
                int ordIsUrgent = rd.GetOrdinal("is_urgent");
                int ordIsConfirmed = rd.GetOrdinal("is_confirmed");
                int ordConfirmedBy = rd.GetOrdinal("confirmed_by");
                int ordConfirmedOn = rd.GetOrdinal("confirmed_on");
                int ordPostponedSavedBy = rd.GetOrdinal("postponed_saved_by");
                int ordPostponedSavedOn = rd.GetOrdinal("postponed_saved_on");
                int ordCancelledSavedBy = rd.GetOrdinal("cancelled_saved_by");
                int ordCancelledSavedOn = rd.GetOrdinal("cancelled_saved_on");
                int ordProtocolNo = rd.GetOrdinal("protocol_no");
                int ordStatus = rd.GetOrdinal("status");
                int ordSavedBy = rd.GetOrdinal("saved_by");
                int ordSavedOn = rd.GetOrdinal("saved_on");
                int ordUpdatedBy = rd.GetOrdinal("updated_by");
                int ordUpdatedOn = rd.GetOrdinal("updated_on");
                int ordDeletedBy = rd.GetOrdinal("deleted_by");
                int ordDeletedOn = rd.GetOrdinal("deleted_on");

                while (rd.Read())
                {
                    var appointment = new Appointment
                    {
                        Id = rd.IsDBNull(ordId) ? 0 : rd.GetInt32(ordId),

                        // relations
                        Clinic = rd.IsDBNull(ordClinicId) ? null : new Clinic { Id = rd.GetInt32(ordClinicId), Name = rd.GetString(ordClinicName) },
                        User = rd.IsDBNull(ordDoctorId) ? null : new User { Id = rd.GetInt32(ordDoctorId), Name = rd.GetString(ordDoctorFirstName), LastName = rd.GetString(ordDoctorLastName) },
                        Patient = rd.IsDBNull(ordPatientId) ? null : new Patient
                        {
                            Id = rd.GetInt32(ordPatientId),
                            Name = rd.IsDBNull(ordpatientName) ? null : rd.GetString(ordpatientName),
                            LastName = rd.IsDBNull(ordpatientLastName) ? null : rd.GetString(ordpatientLastName)
                        },
                        // nested type objects (only small projection)
                        AppointmentType = rd.IsDBNull(ordAppointmentType) ? null : new AppointmentType
                        {
                            Id = rd.GetInt32(ordAppointmentType),
                            TypeName = rd.IsDBNull(ordAppointmentTypeName) ? null : rd.GetString(ordAppointmentTypeName)
                        },

                        AppointmentStatus = new AppointmentStatus
                        {
                            Id = rd.IsDBNull(ordAppointmentStatus) ? 0 : rd.GetInt32(ordAppointmentStatus),
                            StatusName = rd.IsDBNull(ordAppointmentStatusName) ? null : rd.GetString(ordAppointmentStatusName)
                        },

                        TreatmentType = new TreatmentType
                        {
                            Id = rd.IsDBNull(ordTreatmentType) ? 0 : rd.GetInt32(ordTreatmentType),
                            TreatmentTypeName = rd.IsDBNull(ordTreatmentTypeName) ? null : rd.GetString(ordTreatmentTypeName)
                        },

                        IsAllDay = rd.IsDBNull(ordIsAllDay) ? false : rd.GetBoolean(ordIsAllDay),
                        Description = rd.IsDBNull(ordDescription) ? null : rd.GetString(ordDescription),
                        IsEmergency = rd.IsDBNull(ordIsEmergency) ? false : rd.GetBoolean(ordIsEmergency),
                        IsUrgent = rd.IsDBNull(ordIsUrgent) ? false : rd.GetBoolean(ordIsUrgent),
                        IsConfirmed = rd.IsDBNull(ordIsConfirmed) ? false : rd.GetBoolean(ordIsConfirmed),
                        ConfirmedBy = rd.IsDBNull(ordConfirmedBy) ? 0 : rd.GetInt32(ordConfirmedBy),

                        ProtocolNo = rd.IsDBNull(ordProtocolNo) ? null : rd.GetString(ordProtocolNo),
                        Status = rd.IsDBNull(ordStatus) ? 0 : rd.GetInt32(ordStatus),
                        SavedBy = rd.IsDBNull(ordSavedBy) ? 0 : rd.GetInt32(ordSavedBy),
                        UpdatedBy = rd.IsDBNull(ordUpdatedBy) ? 0 : rd.GetInt32(ordUpdatedBy),
                        DeletedBy = rd.IsDBNull(ordDeletedBy) ? 0 : rd.GetInt32(ordDeletedBy),

                        ConfirmedOn = Tools.ReadNullableDate(rd, ordConfirmedOn),
                        PostponedSavedOn = Tools.ReadNullableDate(rd, ordPostponedSavedOn),
                        CancelledSavedOn = Tools.ReadNullableDate(rd, ordCancelledSavedOn),
                        SavedOn = Tools.ReadNullableDate(rd, ordSavedOn) ?? DateTime.MinValue,
                        UpdatedOn = Tools.ReadNullableDate(rd, ordUpdatedOn),
                        DeletedOn = Tools.ReadNullableDate(rd, ordDeletedOn)
                    };

                    // core date fields
                    appointment.StartDate = Tools.ReadNullableDate(rd, ordStartDate) ?? DateTime.MinValue;
                    appointment.EndDate = Tools.ReadNullableDate(rd, ordEndDate) ?? appointment.StartDate;

                    result.Add(appointment);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error querying appointments by criteria", ex);
            }

            return result;
        }

        // IRepository required members left as NotImplemented
        public int Insert(Appointment instance) => throw new NotImplementedException();
        public int Delete(Appointment instance) => throw new NotImplementedException();
        public int Update(Appointment instance) => throw new NotImplementedException();
        public Appointment? Load(int Id) => throw new NotImplementedException();
        public IList<Appointment> LoadAll() => throw new NotImplementedException();
    }
}