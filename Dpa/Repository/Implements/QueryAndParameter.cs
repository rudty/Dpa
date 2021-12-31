using System;
using System.Collections.Generic;
using System.Text;

namespace Dpa.Repository.Implements
{
    public readonly struct QueryAndParameter<E>
    {
        /// <summary>
        /// sql query
        /// </summary>
        public readonly string query;

        /// <summary>
        /// entity 가 들어왔을때 sql param 으로 바꿀 함수
        /// </summary>
        public readonly Func<E, object> parameterBinder;

        public QueryAndParameter(string query, Func<E, object> parameter)
        {
            this.query = query;
            this.parameterBinder = parameter;
        }
    }
}
