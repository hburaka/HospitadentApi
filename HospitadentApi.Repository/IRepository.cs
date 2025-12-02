using HospitadentApi.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HospitadentApi.Repository
{
    public interface IRepository<T> where T : EntityBase
    {
        int Insert(T instance);

        int Delete(T instance);

        int Update(T instance);

        T? Load(int Id);

        IList<T> LoadAll();
    }
}
