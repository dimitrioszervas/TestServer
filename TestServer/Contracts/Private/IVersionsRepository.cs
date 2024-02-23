using TestServer.Contracts;

namespace TestServer.Contracts.Private
{
    public interface IVersionsRepository : IGenericRepository<Models.Private.Version>
    {
        Task<List<Models.Private.Version>> GetByNodeId(ulong nodeId);
    }
}
