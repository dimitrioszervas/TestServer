using AutoMapper;
using TestServer.Contracts.Private;
using TestServer.Models;
using TestServer.Models.Private;
using TestServer.Repositories;

namespace TestServer.Repositories.Private
{
    public class AuditsRepository : GenericRepository<Audit>, IAuditsRepository
    {
        private readonly EndocloudDbContext _context;
        public AuditsRepository(EndocloudDbContext context, IMapper mapper) : base(context, mapper)
        {
            _context = context;
        }

        public async Task<List<Audit>> GetByNodeId(ulong nodeId)
        {
            var audits = from a in _context.Audits
                         where a.NodeId == nodeId
                         orderby a.Timestamp descending
                         select a;

            return audits.ToList();
        }

        public async Task<List<Audit>> GetByNodeIdAndType(ulong nodeId, AuditType type)
        {
            var audits = from a in _context.Audits
                         where a.NodeId == nodeId && a.Type == (byte)type
                         orderby a.Timestamp descending
                         select a;

            return audits.Distinct().ToList();
        }

        public async Task<List<Audit>> GetUserActions(ulong userNodeId)
        {
            var audits = from a in _context.Audits
                         where a.UserNodeId == userNodeId
                         orderby a.Timestamp descending
                         select a;

            return audits.ToList();
        }

        public async Task<Audit> GetLatestByUserIdAndType(ulong userId, AuditType type)
        {
            var audit = (from a in _context.Audits
                         where a.UserNodeId == userId && a.Type == (byte)type
                         orderby a.Timestamp descending
                         select a).FirstOrDefault();

            return audit;
        }

        public async Task<Audit> GetLastAccessByNodeId(ulong? nodeId)
        {
            var audit = (from a in _context.Audits
                         where a.NodeId == nodeId && a.Type == (byte)AuditType.Read
                         orderby a.Timestamp descending
                         select a).FirstOrDefault();

            return audit;
        }

        public async Task<Audit> GetMostRecentByNodeId(ulong? nodeId)
        {
            var audit = (from a in _context.Audits
                         where a.NodeId == nodeId
                         orderby a.Timestamp descending
                         select a).FirstOrDefault();

            return audit;
        }

        public async Task<List<Audit>> GetReviewsAndApprovalsByNodeId(ulong nodeId)
        {
            var audits = from a in _context.Audits
                         where a.NodeId == nodeId && (a.Type == (byte)AuditType.AddReview || a.Type == (byte)AuditType.Approval)
                         orderby a.Timestamp descending
                         select a;

            return audits.Distinct().ToList();
        }
    }
}
