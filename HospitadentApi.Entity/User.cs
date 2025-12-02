using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace HospitadentApi.Entity
{
    public class User : IEntity
    {
        public string Name { get; set; }

        public string LastName { get; set; }

        public int UserType { get; set; }

        public Clinic Clinic { get; set; }

        public string AllowedClinic { get; set; }

        public Department Department { get; set; }

        public URole URole { get; set; }

        public int ShowInCalender { get; set; }

    }
}
