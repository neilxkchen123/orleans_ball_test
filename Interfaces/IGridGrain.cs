using System.Threading.Tasks;
using Orleans;
using Orleans.Concurrency;

namespace Interfaces
{
    public interface IGridGrain : IGrainWithIntegerKey
    {
        [Transaction(TransactionOption.Create)]
        Task Init();
        
        [Transaction(TransactionOption.Create)]
		[AlwaysInterleave]
        Task Move();

        [Transaction(TransactionOption.CreateOrJoin)]
		[AlwaysInterleave]
        Task AddBall(long ball_id);
    }
}
