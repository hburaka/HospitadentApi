using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HospitadentApi.Entity
{
    public class Appointment : EntityBase
    {
        public Clinic? Clinic { get; set; }
    }
}
