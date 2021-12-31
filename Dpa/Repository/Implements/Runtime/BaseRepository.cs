using System.Data.Common;

namespace Dpa.Repository.Implements.Runtime
{
    /// <summary>
    /// 단순히 DbConnection 을 가집니다
    /// 런타임에 DefaultCrudRepository 또는 이것을 상속받고 있으므로
    /// connection 변수 이름을 변경 시 주의가 필요합니다
    /// </summary>
    public class BaseRepository
    {
        protected readonly DbConnection connection;

        public BaseRepository(DbConnection connection)
        {
            this.connection = connection;
        }
    }

}
