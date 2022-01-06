using Dpa.Repository.Implements.Runtime;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Dpa.Repository.Implements
{
    public class DefaultCrudRepository<T, ID> : BaseRepository, IStoreProcedureCrudRepository<T, ID>
    {
        protected readonly IRepositoryQuery<T, ID> repositoryQuery;

        public DefaultCrudRepository(DbConnection connection, IRepositoryQuery<T, ID> repositoryQuery)
            : base(connection)
        {
            this.repositoryQuery = repositoryQuery;
        }

        Task IStoreProcedureCrudRepository<T, ID>.EnsureStoreProcedure()
        {
            StoreProcedureRepositoryQuery<T, ID> q = repositoryQuery as StoreProcedureRepositoryQuery<T, ID>;
            if (repositoryQuery is null)
            {
                return Task.CompletedTask;
            }
            return q.EnsureStoreProcedure(connection);
        }

        Task<T> ICrudRepository<T, ID>.SelectRow(ID id)
        {
            return Dapper.SqlMapper.QueryFirstOrDefaultAsync<T>(
                connection, 
                repositoryQuery.Select.query, 
                repositoryQuery.Select.parameterBinder(id), 
                commandType: repositoryQuery.CommandType);
        }

        async Task<IReadOnlyCollection<T>> ICrudRepository<T, ID>.Select(ID id)
        {
            IEnumerable<T> r = await Dapper.SqlMapper.QueryAsync<T>(
                connection, 
                repositoryQuery.Select.query, 
                repositoryQuery.Select.parameterBinder(id), 
                commandType: repositoryQuery.CommandType).ConfigureAwait(false);

            if (r is IReadOnlyCollection<T> c)
            {
                return c;
            }

            return r.ToList();
        }

        Task<int> ICrudRepository<T, ID>.InsertRow(T value)
        {
            return ExecuteInternal(repositoryQuery.Insert, value);
        }

        Task<int> ICrudRepository<T, ID>.Insert(IEnumerable<T> values)
        {
            return ExecuteInternal(repositoryQuery.Insert, values);
        }

        Task<int> ICrudRepository<T, ID>.UpdateRow(T value)
        {
            return ExecuteInternal(repositoryQuery.Update, value);
        }

        Task<int> ICrudRepository<T, ID>.Update(IEnumerable<T> values)
        {
            return ExecuteInternal(repositoryQuery.Update, values);
        }

        Task<int> ICrudRepository<T, ID>.DeleteRow(ID id)
        {
            return ExecuteInternal(repositoryQuery.Delete, id);
        }

        Task<int> ICrudRepository<T, ID>.Delete(IEnumerable<ID> values)
        {
            return ExecuteInternal(repositoryQuery.Delete, values);
        }

        private async Task<int> ExecuteInternal<E>(QueryAndParameter<E> queryAndParameter, E value)
        {
            return await Dapper.SqlMapper.ExecuteAsync(
                        connection,
                        sql: queryAndParameter.query,
                        param: queryAndParameter.parameterBinder(value), 
                        commandType: repositoryQuery.CommandType).ConfigureAwait(false);
        }


        private async Task<int> ExecuteInternal<E>(QueryAndParameter<E> queryAndParameter, IEnumerable<E> values)
        {
            return await Dapper.SqlMapper.ExecuteAsync(
                  connection,
                  sql: queryAndParameter.query,
                  param: values.Select(queryAndParameter.parameterBinder),
                  commandType: repositoryQuery.CommandType).ConfigureAwait(false);
         }
    }
}
