using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HospitadentApi.Entity
{
    public class IEntity
    {
        public int Id { get; set; }
        public DateTime saved_on { get; set; }
        public DateTime? updated_on { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
