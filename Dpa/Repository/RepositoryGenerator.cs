using Dpa.Repository.Implements;
using Dpa.Repository.Implements.Runtime;
using System;
using System.Data.Common;
using System.Threading.Tasks;

namespace Dpa.Repository
{
    public static class RepositoryGenerator
    {
        /// <summary>
        /// select, insert, update, delete 쿼리로 조회합니다 
        /// T에는 entity 타입을, id에는 T에서 고유한 자료형입니다
        /// </summary>
        /// <typeparam name="T">entity 타입</typeparam>
        /// <typeparam name="ID">id 타입</typeparam>
        /// <param name="dbConnection">연결</param>
        public static Task<ICrudRepository<T, ID>> Default<T, ID>(DbConnection dbConnection)
        {
            ICrudRepository<T, ID> instance = new DefaultCrudRepository<T, ID>(dbConnection, new TextRepositoryQuery<T, ID>());
            return Task.FromResult(instance);
        }

        /// <summary>
        /// mssql 에서 사용하는 저장 프로시저 사용
        /// </summary>
        /// <typeparam name="T">entity 타입</typeparam>
        /// <typeparam name="ID">id 타입</typeparam>
        /// <param name="dbConnection">연결</param>
        public static Task<IStoreProcedureCrudRepository<T, ID>> SqlServerStoreProcedure<T, ID>(DbConnection dbConnection)
        {
            IStoreProcedureCrudRepository<T, ID> instance = new DefaultCrudRepository<T, ID>(
                dbConnection, 
                new StoreProcedureRepositoryQuery<T, ID>());
            return Task.FromResult(instance);
        }

        /// <summary>
        /// 저장 프로시저 + 직접 구현한 인터페이스
        /// </summary>
        /// <typeparam name="Repo">구현체 인터페이스</typeparam>
        /// <typeparam name="T">entity 타입</typeparam>
        /// <typeparam name="ID">id 타입</typeparam>
        /// <param name="dbConnection">연결</param>
        public static Task<Repo> CustomAndSqlServerStoreProcedure<Repo, T, ID>(DbConnection dbConnection) 
            where Repo : IStoreProcedureCrudRepository<T, ID>
        {
            StoreProcedureRepositoryQuery<T, ID> repositoryQuery = new StoreProcedureRepositoryQuery<T, ID>();
            Repo repository = (Repo)CustomQueryRepo(
                dbConnection,
                typeof(Repo),
                typeof(DefaultCrudRepository<T, ID>),
                repositoryQuery);
            return Task.FromResult(repository);
        }

        /// <summary>
        /// 쿼리 + 직접 구현한 인터페이스
        /// </summary>
        /// <typeparam name="Repo">구현체 인터페이스</typeparam>
        /// <typeparam name="T">entity 타입</typeparam>
        /// <typeparam name="ID">id 타입</typeparam>
        /// <param name="dbConnection">연결</param>
        public static Task<Repo> CustomAndTextQuery<Repo, T, ID>(DbConnection dbConnection)
            where Repo : ICrudRepository<T, ID>
        {
            TextRepositoryQuery<T, ID> repositoryQuery = new TextRepositoryQuery<T, ID>();
            Repo repository = (Repo)CustomQueryRepo(
                dbConnection,
                typeof(Repo),
                typeof(DefaultCrudRepository<T, ID>),
                repositoryQuery);
            return Task.FromResult(repository);
        }

        /// <summary>
        /// 직접 구현한 interface로 사용 가능합니다 
        /// </summary>
        /// <typeparam name="Repo">Repository interface 구현</typeparam>
        /// <param name="dbConnection">연결</param>
        public static Task<Repo> Custom<Repo>(DbConnection dbConnection)
        {
            Type repoType = typeof(Repo);
            if (!repoType.IsInterface)
            {
                throw new ArgumentException("interface only");
            }

            Type baseType;
            object repositoryQuery;
            Type baseImplementRepositoryType;
            if ((baseImplementRepositoryType = repoType.GetInterface(typeof(IStoreProcedureCrudRepository<,>).Name)) != null) 
            {
                Type[] genericArgumentTypes = baseImplementRepositoryType.GetGenericArguments();

                baseType = typeof(DefaultCrudRepository<,>).MakeGenericType(genericArgumentTypes);
                Type repositoryQueryType = typeof(StoreProcedureRepositoryQuery<,>)
                    .MakeGenericType(genericArgumentTypes);
                repositoryQuery = Activator.CreateInstance(repositoryQueryType);
            }
            else if ((baseImplementRepositoryType = repoType.GetInterface(typeof(ICrudRepository<,>).Name)) != null)
            {
                Type[] genericArgumentTypes = baseImplementRepositoryType.GetGenericArguments();

                baseType = typeof(DefaultCrudRepository<,>)
                    .MakeGenericType(genericArgumentTypes);
                Type repositoryQueryType = typeof(TextRepositoryQuery<,>)
                    .MakeGenericType(genericArgumentTypes);
                repositoryQuery = Activator.CreateInstance(repositoryQueryType);
            }
            else
            {
                baseType = typeof(BaseRepository);
                repositoryQuery = null;
            }
            
            Repo repository = (Repo)CustomQueryRepo(dbConnection, repoType, baseType, repositoryQuery);
            return Task.FromResult(repository);
        }

        private static object CustomQueryRepo(DbConnection dbConnection, Type repoType, Type baseType, object repositoryQuery)
        {
            if (!repoType.IsInterface)
            {
                throw new ArgumentException("interface only");
            }

            Type customType = RuntimeTypeGenerator.Generate(baseType, repoType);
            object[] argument;
            if (repositoryQuery != null)
            {
                argument = new object[] { dbConnection, repositoryQuery };
            }
            else
            {
                argument = new object[] { dbConnection };
            }
            object instance = Activator.CreateInstance(customType, argument, null);
            return instance;
        }

    }
}

