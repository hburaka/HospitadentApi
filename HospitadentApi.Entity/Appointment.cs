using System;

namespace HospitadentApi.Entity
{
    public class Appointment : EntityBase
    {
        // Relations / friendly objects
        public Clinic? Clinic { get; set; }
        public User? User { get; set; }
        public Patient? Patient { get; set; }

        // Identifiers
        //public User? Assistant { get; set; }
        //public User? Hygienist { get; set; }
        //public string? ProtocolId { get; set; }
        //public int PcId { get; set; }

        // Time info
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        //public DateTime? ArrivalTime { get; set; }
        //public DateTime? TreatmentStartTime { get; set; }
        //public DateTime? LeftTheRoomTime { get; set; }
        //public DateTime? PatientOutTime { get; set; }

        // Types / names
        public AppointmentType? AppointmentType { get; set; }
        //public string? AppointmentTypeName { get; set; }
        public AppointmentStatus AppointmentStatus { get; set; }
        //public string? AppointmentStatusName { get; set; }
        public TreatmentType TreatmentType { get; set; }
        //public string? TreatmentTypeName { get; set; }

        // Flags
        public bool IsAllDay { get; set; }
        public bool IsEmergency { get; set; }
        public bool IsUrgent { get; set; }
        public bool IsConfirmed { get; set; }
        //public bool NotificationSms { get; set; }
        public int Status { get; set; } // stored as tinyint(1) but used as status

        // Confirmation / workflow
        public int ConfirmedBy { get; set; }
        public DateTime? ConfirmedOn { get; set; }

        public int PostponedSavedBy { get; set; }
        public DateTime? PostponedSavedOn { get; set; }

        public int CancelledSavedBy { get; set; }
        public DateTime? CancelledSavedOn { get; set; }

        public bool IsSeizure { get; set; }

        // Display / note
        public string? Description { get; set; }
        public string? ProtocolNo { get; set; }

        // Audit
        public int SavedBy { get; set; }
        public DateTime SavedOn { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int DeletedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
    }
}
