using System;

namespace HospitadentApi.Entity
{
    public class TreatmentType : EntityBase
    {
        //public long CompanyId { get; set; }
        //public int ClinicId { get; set; }
        public string? TreatmentTypeName { get; set; }
        //public string? TreatmentColor { get; set; }

        // The enum column `treatment_type` (Consultation/Surgery) stored as string in DB
        //public string? TreatmentKind { get; set; }

        //public int SortBy { get; set; }
        //public int SavedBy { get; set; }
        //public DateTime SavedOn { get; set; }
        //public int UpdatedBy { get; set; }
        //public DateTime? UpdatedOn { get; set; }
        public bool IsDeleted { get; set; }
        //public int DeletedBy { get; set; }
        //public DateTime? DeletedOn { get; set; }
    }
}