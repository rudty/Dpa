using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dpa.Repository
{
    /// <summary>
    /// 쿼리를 날릴 수 있는 형식입니다
    /// 데이터 구조와 구조의 고유 값인 id를 통해서 조회할 수 있습니다
    /// </summary>
    /// <typeparam name="T">Entity</typeparam>
    /// <typeparam name="ID">조회할 ID 구조</typeparam>
    public interface ICrudRepository<T, ID> : IRepository
    {
        /// <summary>
        /// id를 찾고 1개를 가져옵니다
        /// </summary>
        /// <param name="id">아이디</param>
        /// <returns>row를 매핑</returns>
        Task<T> SelectRow(ID id);

        /// <summary>
        /// id를 찾고 모두 가져옵니다
        /// </summary>
        /// <param name="id">아이디</param>
        /// <returns>row를 매핑</returns>
        Task<IReadOnlyCollection<T>> Select(ID id);

        /// <summary>
        /// entity 1개를 넣습니다
        /// </summary>
        /// <param name="value">넣을 값</param>
        /// <returns>rows affected</returns>
        Task<int> InsertRow(T value);

        /// <summary>
        /// entity 여러개를 넣습니다
        /// </summary>
        /// <param name="value">넣을 값</param>
        /// <returns>rows affected</returns>
        Task<int> Insert(IEnumerable<T> values);

        /// <summary>
        /// entity를 수정합니다
        /// </summary>
        /// <param name="value">수정하는 값</param>
        /// <returns>rows affected</returns>
        Task<int> UpdateRow(T value);

        /// <summary>
        /// entity를 수정합니다
        /// </summary>
        /// <param name="values">수정하는 값</param>
        /// <returns>rows affected</returns>
        Task<int> Update(IEnumerable<T> values);

        /// <summary>
        /// entity를 삭제합니다
        /// </summary>
        /// <param name="value">삭제하는 값</param>
        /// <returns>rows affected</returns>
        Task<int> DeleteRow(ID value);

        /// <summary>
        /// entity를 삭제합니다
        /// </summary>
        /// <param name="values">삭제하는 값</param>
        /// <returns>rows affected</returns>
        Task<int> Delete(IEnumerable<ID> values);
    }
}
