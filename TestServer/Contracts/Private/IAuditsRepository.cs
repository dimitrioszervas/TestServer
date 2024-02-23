using TestServer.Models.Private;
using TestServer.Contracts;

namespace TestServer.Contracts.Private
{
    public interface IAuditsRepository : IGenericRepository<Audit>
    {
        Task<List<Audit>> GetByNodeId(ulong nodeId);
        Task<List<Audit>> GetByNodeIdAndType(ulong nodeId, AuditType type);
        Task<Audit> GetLatestByUserIdAndType(ulong userId, AuditType type);
        Task<List<Audit>> GetUserActions(ulong userNodeId);
        Task<Audit> GetLastAccessByNodeId(ulong? nodeId);
        Task<Audit> GetMostRecentByNodeId(ulong? nodeId);
        Task<List<Audit>> GetReviewsAndApprovalsByNodeId(ulong nodeId);
    }
}
