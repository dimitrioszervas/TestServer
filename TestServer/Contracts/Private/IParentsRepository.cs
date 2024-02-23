using TestServer.Models.Private;
using TestServer.Contracts;

namespace TestServer.Contracts.Private
{
    public interface IParentsRepository : IGenericRepository<Parent>
    {
        Task<List<Parent>> GetChildrenByParentNodeId(ulong? parentNodeId);
        Task<Parent> GetCurrentParentByNodeId(ulong? nodeId);
    }
}
