using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Dpa.Repository.Implements
{
    internal class StoreProcedureCrudRepository<T, ID> : DefaultCrudRepository<T, ID>, IStoreProcedureCrudRepository<T, ID>
    {
        internal StoreProcedureCrudRepository(DbConnection connection, StoreProcedureRepositoryQuery<T, ID> repositoryQuery) 
            : base(connection, repositoryQuery, CommandType.StoredProcedure)
        {
        }

        Task IStoreProcedureCrudRepository<T, ID>.EnsureStoreProcedure()
        {
            StoreProcedureRepositoryQuery<T, ID> q = repositoryQuery as StoreProcedureRepositoryQuery<T, ID>;
            return q.EnsureStoreProcedure(connection);
        }
    }
}
