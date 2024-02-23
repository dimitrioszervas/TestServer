using TestServer.Models.Private;
using TestServer.Contracts;

namespace TestServer.Contracts.Private
{
    public interface IPermissionsRepository : IGenericRepository<Permission>
    {
        Task<Permission> GetByNodeIdAndGranteeId(ulong? nodeId, ulong granteeId);
        Task<List<Permission>> GetPermissionsByNodeId(ulong? nodeId);
        Task<List<Permission>> GetSharedFilesByUserId(ulong? nodeId);
        Task<List<Permission>> GetGroupsByGranteeId(ulong? granteeId);
        Task<Permission> GetSharedByNodeIdAndGranteeId(ulong? nodeId, ulong granteeId);
        Task<Permission> GetByNodeIdAndUserNodeIdAndGranteedNodeId(ulong? nodeId, ulong userNodeId, ulong granteeNodeId);
    }
}
