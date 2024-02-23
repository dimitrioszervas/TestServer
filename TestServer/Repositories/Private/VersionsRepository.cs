using AutoMapper;
using TestServer.Contracts.Private;
using TestServer.Models;
using TestServer.Models.Private;
using TestServer.Repositories;

namespace TestServer.Repositories.Private
{
    public class VersionsRepository : GenericRepository<Models.Private.Version>, IVersionsRepository
    {
        private readonly EndocloudDbContext _context;

        public VersionsRepository(EndocloudDbContext context, IMapper mapper) : base(context, mapper)
        {
            _context = context;
        }

        public async Task<List<Models.Private.Version>> GetByNodeId(ulong nodeId)
        {
            var versions = from v in _context.Versions
                           where v.NodeId == nodeId
                           orderby v.Timestamp descending
                           select v;

            return versions.ToList();
        }
    }
}
