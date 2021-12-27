using System.Threading.Tasks;

namespace Dpa.Repository.Implements
{
    internal interface IRepositoryQuery<T, ID>
    {
        QueryAndParameter<ID> Select { get; }
        QueryAndParameter<T> Insert { get; }
        QueryAndParameter<T> Update { get; }
        QueryAndParameter<ID> Delete { get; }
    }
}
