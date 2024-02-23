using TestServer.Models.Public;
using TestServer.Contracts;

namespace TestServer.Contracts.Public
{
    public interface IBlockchainsRepository : IGenericRepository<Blockchain>
    {
        Task<Blockchain> GetByOrgNodeIdAsync(ulong? orgNodeId);
    }
}
