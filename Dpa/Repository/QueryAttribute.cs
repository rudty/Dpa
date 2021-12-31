using System;
using System.Data;

namespace Dpa.Repository
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class QueryAttribute : Attribute
    {
        public string Query { get; }

        public CommandType CommandType { get; }

        public QueryAttribute(string query, CommandType commandType = CommandType.Text)
        {
            Query = query;
            CommandType = commandType;
        }
    }
}
