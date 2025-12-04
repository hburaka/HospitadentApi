using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HospitadentApi.Entity
{
    public class URole : EntityBase
    {
        public string? Name { get; set; }

        public Department? Department { get; set; }
    }

}
