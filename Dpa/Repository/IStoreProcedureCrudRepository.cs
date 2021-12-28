using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Dpa.Repository
{
    public interface IStoreProcedureCrudRepository<T, ID> : ICrudRepository<T, ID>
    {
        Task EnsureStoreProcedure();
    }
}
