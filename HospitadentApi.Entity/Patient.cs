using System;

namespace HospitadentApi.Entity
{
    public class Patient : EntityBase
    {
        // direct DB mappings
        public string? FirstName { get; set; }     // maps to patients.first_name
        public string? LastName { get; set; }      // maps to patients.last_name
        public Clinic? Clinic { get; set; }        // maps via patients.clinic_id
        public string? TcNo { get; set; }          // maps to patients.tc_no
        public string? PassportNo { get; set; }    // maps to patients.passport_no
        public string? MobileCc { get; set; }      // maps to patients.mobile_cc
        public string? Mobile { get; set; }        // maps to patients.mobile

        // convenience computed value (keeps calling code simple)
        public string? MobilePhone
        {
            get
            {
                var cc = string.IsNullOrWhiteSpace(MobileCc) ? string.Empty : MobileCc.Trim();
                var m = string.IsNullOrWhiteSpace(Mobile) ? string.Empty : Mobile.Trim();
                var combined = (cc + " " + m).Trim();
                return string.IsNullOrEmpty(combined) ? null : combined;
            }
        }
    }
}
