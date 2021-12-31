﻿using Dpa.Repository.Implements;
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
        /// 직접 구현한 interface로 사용 가능합니다 
        /// </summary>
        /// <typeparam name="Repo">Repository interface 구현</typeparam>
        /// <param name="dbConnection">연결</param>
        public static Task<Repo> Custom<Repo>(DbConnection dbConnection)
        {
            Type customType = RuntimeRepositoryGenerator.Generate(typeof(BaseRepository), typeof(Repo));
            Repo instance = (Repo)Activator.CreateInstance(customType, dbConnection);
            return Task.FromResult(instance);
        }

        /// <summary>
        /// 직접 구현한 interface + crud repository로 사용 가능합니다 
        /// </summary>
        /// <typeparam name="Repo">Repository interface 구현</typeparam>
        /// <param name="dbConnection">연결</param>
        public static Task<Repo> CustomCrud<Repo, T, ID>(DbConnection dbConnection)
        {
            Type customType = RuntimeRepositoryGenerator.Generate(typeof(DefaultCrudRepository<T, ID>), typeof(Repo));
            Repo instance = (Repo)Activator.CreateInstance(customType, dbConnection, new TextRepositoryQuery<T, ID>());
            return Task.FromResult(instance);
        }
    }
}

