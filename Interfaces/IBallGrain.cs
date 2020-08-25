using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;

namespace Interfaces
{
    public interface IBallGrain : IGrainWithIntegerKey
    {
        [Transaction(TransactionOption.CreateOrJoin)]
        Task Init(double px, double py);

        [Transaction(TransactionOption.CreateOrJoin)]
        [AlwaysInterleave]
        Task Move();

        [Transaction(TransactionOption.CreateOrJoin)]
        [AlwaysInterleave]
        Task<long> GetInGridId();
    }
}
