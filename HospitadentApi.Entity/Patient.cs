using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HospitadentApi.Entity
{
    public class Patient : EntityBase
    {
        public string? Name { get; set; }
        public string? LastName { get; set; }
    }
}
