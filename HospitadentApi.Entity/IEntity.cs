using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HospitadentApi.Entity
{
    public class IEntity
    {
        int Id { get; set; }
        DateTime saved_on { get; set; }
        DateTime? updated_on { get; set; }
        bool? IsActive { get; set; }
        bool? IsDeleted { get; set; }
    }
}
