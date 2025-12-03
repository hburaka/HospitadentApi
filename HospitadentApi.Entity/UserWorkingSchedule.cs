// HospitadentApi.Entity\UserWorkingSchedule.cs
using System;

namespace HospitadentApi.Entity
{
    public class UserWorkingSchedule : EntityBase
    {
        public long CompanyId { get; set; }
        public int ClinicId { get; set; }
        public int UserId { get; set; }
        public DateTime? CustomDate { get; set; }
        public string Day { get; set; } = string.Empty;
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int SavedBy { get; set; }
        public DateTime SavedOn { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public bool IsDeleted { get; set; }
        public int DeletedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
    }
}