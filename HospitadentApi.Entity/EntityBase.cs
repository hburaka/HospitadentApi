using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace HospitadentApi.Entity
{
    [Serializable]
    public abstract class EntityBase : IEntity
    {
        #region IEntity Members

        public int Id { get; set; }

        //[DisplayFormat(ConvertEmptyStringToNull = true, NullDisplayText = "00.00.0000 00:00", DataFormatString = "{0:dd.MM.yyyy HH:mm}")  ]
        public DateTime CreatedDate { get; set; }

        //[DisplayFormat(ConvertEmptyStringToNull = true, NullDisplayText = "00.00.0000 00:00", DataFormatString = "{0:dd.MM.yyyy HH:mm}")]
        public DateTime? ModifiedDate { get; set; }

        [Display(Name = "Aktif mi ?")]
        public bool? IsActive { get; set; }

        #endregion

        public override int GetHashCode()
        {
            return Id.GetHashCode() * 57;
        }

        public override bool Equals(object obj)
        {
            EntityBase that = obj as EntityBase;
            if (that != null && that.Id == this.Id)
                return true;
            return false;
        }
    }
}
