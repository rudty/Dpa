using System.Data.Common;
using System.Reflection;

namespace Dpa.Repository.Implements
{
    /// <summary>
    /// 기본 Repository 구현체
    /// </summary>
    public class BaseRepository
    {
        public static readonly FieldInfo ConnectionField = typeof(BaseRepository)
            .GetField(nameof(connection), BindingFlags.NonPublic | BindingFlags.Instance);

        protected readonly DbConnection connection;

        public BaseRepository(DbConnection connection)
        {
            this.connection = connection;
        }
    }

}
