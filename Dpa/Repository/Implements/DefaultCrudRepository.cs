using Dpa.Repository.Implements.Types;
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
            TypeMapper.SetType(typeof(T));
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

        Task<int> ICrudRepository<T, ID>.InsertRow(T value, DbTransaction transaction)
        {
            return ExecuteInternal(repositoryQuery.Insert, value, transaction);
        }

        Task<int> ICrudRepository<T, ID>.Insert(IEnumerable<T> values, DbTransaction transaction)
        {
            return ExecuteInternal(repositoryQuery.Insert, values, transaction);
        }

        Task<int> ICrudRepository<T, ID>.UpdateRow(T value, DbTransaction transaction)
        {
            return ExecuteInternal(repositoryQuery.Update, value, transaction);
        }

        Task<int> ICrudRepository<T, ID>.Update(IEnumerable<T> values, DbTransaction transaction)
        {
            return ExecuteInternal(repositoryQuery.Update, values, transaction);
        }

        Task<int> ICrudRepository<T, ID>.DeleteRow(ID id, DbTransaction transaction)
        {
            return ExecuteInternal(repositoryQuery.Delete, id, transaction);
        }

        Task<int> ICrudRepository<T, ID>.Delete(IEnumerable<ID> values, DbTransaction transaction)
        {
            return ExecuteInternal(repositoryQuery.Delete, values, transaction);
        }

        Task<T> ICrudRepository<T, ID>.SelectRow(ID id, DbTransaction transaction)
        {
            return Dapper.SqlMapper.QueryFirstOrDefaultAsync<T>(
                connection,
                repositoryQuery.Select.query,
                repositoryQuery.Select.parameterBinder(id),
                transaction,
                commandType: repositoryQuery.CommandType);
        }

        async Task<IReadOnlyCollection<T>> ICrudRepository<T, ID>.Select(ID id, DbTransaction transaction)
        {
            IEnumerable<T> r = await Dapper.SqlMapper.QueryAsync<T>(
                connection,
                repositoryQuery.Select.query,
                repositoryQuery.Select.parameterBinder(id),
                transaction,
                commandType: repositoryQuery.CommandType).ConfigureAwait(false);

            if (r is IReadOnlyCollection<T> c)
            {
                return c;
            }

            return r.ToList();
        }

        private Task<int> ExecuteInternal<E>(QueryAndParameter<E> queryAndParameter, E value, DbTransaction transaction)
        {
            return Dapper.SqlMapper.ExecuteAsync(
                        connection,
                        sql: queryAndParameter.query,
                        param: queryAndParameter.parameterBinder(value),
                        transaction,
                        commandType: repositoryQuery.CommandType);
        }


        private Task<int> ExecuteInternal<E>(QueryAndParameter<E> queryAndParameter, IEnumerable<E> values, DbTransaction transaction)
        {
            return Dapper.SqlMapper.ExecuteAsync(
                  connection,
                  sql: queryAndParameter.query,
                  param: values.Select(queryAndParameter.parameterBinder),
                  transaction,
                  commandType: repositoryQuery.CommandType);
         }
    }
}
