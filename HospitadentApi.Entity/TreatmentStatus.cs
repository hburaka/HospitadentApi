using System;

namespace HospitadentApi.Entity
{
    public class TreatmentStatus : EntityBase
    {
        public string? StatusName { get; set; }
        //public string? LabelCss { get; set; }
        //public string Description { get; set; } = string.Empty;
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