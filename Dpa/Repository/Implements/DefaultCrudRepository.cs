using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Dpa.Repository.Implements
{
    class DefaultCrudRepository<T, ID> : ICrudRepository<T, ID>
    {
        private readonly DbConnection connection;
        private readonly IRepositoryQuery<T, ID> repositoryQuery;

        internal DefaultCrudRepository(DbConnection connection, IRepositoryQuery<T, ID> repositoryQuery)
        {
            this.connection = connection;
            this.repositoryQuery = repositoryQuery;
        }

        Task<T> ICrudRepository<T, ID>.SelectFirst(ID id)
        {
            return Dapper.SqlMapper.QueryFirstAsync<T>(connection, repositoryQuery.Select.query, repositoryQuery.Select.parameterBinder(id));
        }

        async Task<IReadOnlyCollection<T>> ICrudRepository<T, ID>.Select(ID id)
        {
            IEnumerable<T> r = await Dapper.SqlMapper.QueryAsync<T>(connection, repositoryQuery.Select.query, repositoryQuery.Select.parameterBinder(id)).ConfigureAwait(false);

            if (r is IReadOnlyCollection<T> c)
            {
                return c;
            }

            return r.ToList();
        }

        Task<int> ICrudRepository<T, ID>.Insert(T value)
        {
            return ExecuteInternal(repositoryQuery.Insert, value, null);
        }

        Task<int> ICrudRepository<T, ID>.Insert(IEnumerable<T> values)
        {
            return ExecuteTransaction(repositoryQuery.Insert, values);
        }

        Task<int> ICrudRepository<T, ID>.Update(T value)
        {
            return ExecuteInternal(repositoryQuery.Update, value, null);
        }

        Task<int> ICrudRepository<T, ID>.Update(IEnumerable<T> values)
        {
            return ExecuteTransaction(repositoryQuery.Update, values);
        }

        Task<int> ICrudRepository<T, ID>.Delete(ID id)
        {
            return ExecuteInternal(repositoryQuery.Delete, id, null);
        }

        Task<int> ICrudRepository<T, ID>.Delete(IEnumerable<ID> values)
        {
            return ExecuteTransaction(repositoryQuery.Delete, values);
        }

        private async Task<int> ExecuteInternal<E>(QueryAndParameter<E> queryAndParameter, E value, DbTransaction transaction)
        {
            return await Dapper.SqlMapper.ExecuteAsync(
                        connection,
                        sql: queryAndParameter.query,
                        param: queryAndParameter.parameterBinder(value),
                        transaction: transaction).ConfigureAwait(false);
        }


        private async Task<int> ExecuteTransaction<E>(QueryAndParameter<E> queryAndParameter, IEnumerable<E> values)
        {
            int totalRowsAffected = 0;
            DbTransaction transaction = connection.BeginTransaction();
            try
            {
                foreach (E value in values)
                {
                    
                    int rowsAffected = await ExecuteInternal(queryAndParameter, value, transaction);
                    totalRowsAffected += rowsAffected;
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
            finally
            {
                try
                {
                    await transaction.DisposeAsync();
                }
                catch
                {
                }
            }

            return totalRowsAffected;
        }
    }
}
