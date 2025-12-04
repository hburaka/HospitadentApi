using System;

namespace HospitadentApi.Entity
{
    public class AppointmentStatus : EntityBase
    {
        public string? StatusName { get; set; }
        //public string? TitleColor { get; set; }
        //public string? ContainerColor { get; set; }
        //public string? BorderColor { get; set; }
        //public string? TextColor { get; set; }
        //public string? ClassName { get; set; }
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