using TestServer.Models.Public;
using TestServer.Contracts;

namespace TestServer.Contracts.Public
{
    public interface ISealTransactionsRepository : IGenericRepository<SealTransaction>
    {
    }
}
