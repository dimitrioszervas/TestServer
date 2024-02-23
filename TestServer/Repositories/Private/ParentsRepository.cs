using AutoMapper;
using TestServer.Contracts.Private;
using TestServer.Models;
using TestServer.Models.Private;
using TestServer.Repositories;

namespace TestServer.Repositories.Private
{
    public class ParentsRepository : GenericRepository<Parent>, IParentsRepository
    {
        private readonly EndocloudDbContext _context;

        public ParentsRepository(EndocloudDbContext context, IMapper mapper) : base(context, mapper)
        {
            _context = context;
        }

        public async Task<List<Parent>> GetChildrenByParentNodeId(ulong? parentNodeId)
        {
            List<Parent> parents = (from p in _context.Parents
                                    where p.ParentNodeId == parentNodeId && p.CurrentParent == true
                                    select p).ToList();
            return parents;
        }

        public async Task<Parent> GetCurrentParentByNodeId(ulong? nodeId)
        {
            var parents = (from p in _context.Parents
                           where p.NodeId == nodeId
                           orderby p.Timestamp descending
                           select p).Take(1);

            return parents.FirstOrDefault();
        }
    }
}
