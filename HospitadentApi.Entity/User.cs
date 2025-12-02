using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace HospitadentApi.Entity
{
    public class User : EntityBase
    {
        public string Name { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int UserType { get; set; }
        public Clinic? Clinic { get; set; }

        // raw CSV from DB (kept for compatibility)
        public string AllowedClinic { get; set; } = string.Empty;

        // parsed list of Clinic objects (IDs set; populate full Clinic if needed)
        public List<Clinic> AllowedClinics { get; set; } = new();

        public Department? Department { get; set; }
        public URole? URole { get; set; }
        public int ShowInCalender { get; set; }
    }
}
