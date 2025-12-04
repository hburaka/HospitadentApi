using System;

namespace HospitadentApi.Entity
{
    public class Appointment : EntityBase
    {
        // Kimlik ve temel ilişkiler
        public long CompanyId { get; set; }
        public int ClinicId { get; set; }
        public int DoctorId { get; set; }
        public int? PatientId { get; set; }

        // Zaman bilgileri (randevunun gerçek başlangıç/bitişi)
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Durum / tip bilgileri (isteğe göre genişletilebilir)
        public byte AppointmentStatus { get; set; }          // örn: 0=Boş, 1=Onaylı, 2=İptal vb.
        public byte AppointmentType { get; set; }            // muayene, kontrol vb.
        public byte TreatmentType { get; set; }              // tedavi türü (opsiyonel)

        // Açıklama / not
        public string? Description { get; set; }
    }
}
